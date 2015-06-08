using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Text;
using XdsObjects;
using XdsObjects.Enums;
using XdsObjects.Dsub;
using XdsObjects.EventArguments;
using XdsObjects.Internal;
using System.IO;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace XdsRegistry
{
    partial class Registry
    {
        public delegate void LogMessageHandler(String msg);
        public event LogMessageHandler LogMessageEvent;

        public bool readProperties()
        {
            try
            {
                authDomain = Properties.Settings.Default.AuthDomain;
                registryURI = Properties.Settings.Default.RegistryURI;
                repositoryId = Properties.Settings.Default.RepositoryId;
                repositoryPath = Properties.Settings.Default.RepositoryPath;
                registryLog = Properties.Settings.Default.RegistryLog;
                atnaHost = Properties.Settings.Default.ATNAHost;
                atnaPort = Properties.Settings.Default.ATNAPort;
                brokerURI = Properties.Settings.Default.BrokerURI;
                notificationRecipient = Properties.Settings.Default.NotificationRecipient;
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        #region "Varibles and Constants"
        // the Server object which handles the incoming Register and StoredQuery requests
        XdsSoapServer server;
        internal string dataSource;
        internal string authDomain;
        internal string registryURI;
        string repositoryPath;
        internal string registryLog;
        internal string atnaHost;
        internal int atnaPort;
        string repositoryId;
        internal string brokerURI;
        internal string notificationRecipient;

        // Home Community ID
        const string HomeCommunityID = "1.2.3.4.5.6.7";
        XdsDomain atnaTest = new XdsDomain();
        AuditEndpoint myAudit = new AuditEndpoint();


        #endregion

        internal void StartListen()
        {
            //XdsGlobal.LogToFile(registryLog, XdsObjects.Enums.LogLevel.All, 60);

            XdsGlobal.LogToFile(registryLog, XdsObjects.Enums.LogLevel.All, 3600);
            
            server = new XdsSoapServer();
            server.RegisterReceived += server_RegisterReceived;
            server.RegistryStoredQueryReceived += server_RegistryStoredQueryReceived;
            server.SubscriptionRequestReceived += server_SubscriptionRequestReceived;
            server.UnsubscriptionRequestReceived += server_UnsubscriptionRequestReceived;

            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            server.Listen(registryURI);
            server.Listen(brokerURI);
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": " + registryURI + " listening...");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": " + brokerURI + " listening...");
            server.StructureIncomingSubmissionSet = false;

            //set up ATNA
            myAudit.Host = atnaHost;
            myAudit.Port = atnaPort;
            AuditProtocol atnaProtocol = AuditProtocol.Tcp;
            myAudit.Protocol = atnaProtocol;
            atnaTest.RegistryEndpoint = new WebServiceEndpoint(registryURI);
            atnaTest.AuditRepositories.Add(myAudit);
            XdsAudit.ActorStart(atnaTest);
        }

        static string DummyContext = "";
        
        static int MaxLeafClassResults = 25;
        static int MaxObjectRefResults = 200;

        internal void StopListen()
        {
            if (server != null)
            {
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": " + registryURI + " not listening...");
                server.UnListenAll();
            }
        }

        #region "Server Events"

        XdsSubscriptionResponse server_SubscriptionRequestReceived(XdsSubscriptionRequest SubscriptionRequest, XdsRequestInfo RequestInfo)
        {
            XdsAudit.UserAuthentication(atnaTest, true);
            string patId = SubscriptionRequest.Query.PatientId;
            LogMessageEvent("--- --- ---");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Subscription request received...");
            try
            {
                //build the subscription response
                XdsSubscriptionResponse resp = new XdsSubscriptionResponse();
                Guid guid = Guid.NewGuid();
                resp.SubscriptionId = guid.ToString();
                resp.Address = brokerURI + "/" + resp.SubscriptionId;
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": " + guid.ToString());

                DateTime iniTermTime = SubscriptionRequest.InitialTerminationTime;
                string myString = iniTermTime.ToString();
                //resp.TerminationTime = iniTermTime;
                //resp.TerminationTime = SubscriptionRequest.InitialTerminationTime;
                DateTime DefaultTerminationTime;
                if (SubscriptionRequest.InitialTerminationTime == null)
                {
                    DefaultTerminationTime = DateTime.Now.AddDays(1);
                    resp.TerminationTime = DefaultTerminationTime;
                }
                else
                {
                    resp.TerminationTime = SubscriptionRequest.InitialTerminationTime;
                }

                using (XdsDataBase db = new XdsDataBase())
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Checking subscriptions...");
                    MySubscriptions mySub = new MySubscriptions();
                    //check that subscription for patient does not already exist
                    foreach(var sub in db.MySubscriptions)
                    {
                        string patientId = sub.patientId;
                        if(patientId == patId)
                        {
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Subscription already exists for patient " + patId);
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryError, "Subscription already exists for patient" + patId, "");
                            //return new XdsSubscriptionResponse(new Exception(), XdsObjects.Enums.XdsErrorCode.XDSUnknownPatientId);
                            return resp;
                        }
                    }

                    //build database update
                    mySub.Id = guid;
                    if (SubscriptionRequest.InitialTerminationTime == null)
                    {
                        DefaultTerminationTime = DateTime.Now.AddDays(1);
                        mySub.TerminationTime = DefaultTerminationTime;

                        
                    }
                    else
                    {
                        DateTime termTime = SubscriptionRequest.InitialTerminationTime;
                        mySub.TerminationTime = termTime;
                    }
                    mySub.Query = SubscriptionRequest;
                    mySub.TopicDialect = SubscriptionRequest.Query.QueryType.ToString();
                    mySub.ConsumerReferenceAddress = SubscriptionRequest.ConsumerReferenceAddress;
                    mySub.CancerType = SubscriptionRequest.Query.DocumentEntryHealthcareFacilityTypeCodes[0].CodeValue;
                    mySub.patientId = patId;
                    db.MySubscriptions.Add(mySub);
                    db.SaveChanges();

                    XdsAudit.UserAuthentication(atnaTest, false);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Subscription processed for patient " + patId);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                XdsAudit.UserAuthentication(atnaTest, false);
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": throwing general exception...");
                return new XdsSubscriptionResponse(ex, XdsObjects.Enums.XdsErrorCode.GeneralException);
            }
            
        }

        XdsUnsubscriptionResponse server_UnsubscriptionRequestReceived(XdsUnsubscriptionRequest UnsubscriptionRequest, XdsRequestInfo RequestInfo)
        {
            XdsAudit.UserAuthentication(atnaTest, true);
            LogMessageEvent("--- --- ---");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Unsubscription request received...");
            try
            {
                XdsUnsubscriptionResponse resp = new XdsUnsubscriptionResponse();
                using(XdsDataBase db = new XdsDataBase())
                {
                    foreach (var sub in db.MySubscriptions)
                    {
                        db.Entry(sub).Reference(p => p.Query).Load();
                    }

                    string guid = UnsubscriptionRequest.SubscriptionId.Segments[2];
                    var match = db.MySubscriptions.Find(Guid.Parse(guid));

                    db.MySubscriptions.Remove(match);
                    db.SaveChanges();
                }
                resp.Status = XdsObjects.Enums.RegistryResponseStatus.Success;
                XdsAudit.UserAuthentication(atnaTest, false);
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": subscription Id - " + UnsubscriptionRequest.SubscriptionId);
                return resp;
            }
            catch(Exception ex)
            {
                XdsAudit.UserAuthentication(atnaTest, false);
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": throwing general exception...");
                return new XdsUnsubscriptionResponse(ex, XdsObjects.Enums.XdsErrorCode.GeneralException);
            } 
        }

        //This has changed!!!!!
        XdsQueryResponse server_RegistryStoredQueryReceived(XdsQueryRequest Request, XdsRequestInfo test)
        {
            LogMessageEvent("--- --- ---");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Registry stored query received");

            try
            {
                switch (Request.QueryType)
                {
                    case QueryType.FindDocuments:
                        return HandleFindDocumentQuery(Request);

                    case QueryType.FindSubmissionSets:
                        return HandleFindSubmissionSetQuery(Request);

                    case QueryType.GetAll:
                        return HandleGetAll(Request);

                    case QueryType.GetAssociations:
                        return HandleGetAssociations(Request);

                    case QueryType.GetSubmissionSets:
                        return HandleGetSubmissionSets(Request);

                    case QueryType.GetDocuments:
                        return HandleGetDocuments(Request);

                    case QueryType.GetRelatedDocuments:
                        return HandleGetRelatedDocuments(Request);

                    case QueryType.FindFolders:
                        return HandleFindFolders(Request);

                    case QueryType.GetFolders:
                        return HandleGetFolders(Request);
                        
                    case QueryType.GetFoldersForDocument:
                        return HandleGetFoldersForDocument(Request);

                    case QueryType.GetFolderAndContents:
                        return HandleGetFolderAndContents(Request);

                    case QueryType.GetSubmissionSetAndContents:
                        return HandleGetSubmissionSetAndContents(Request);

                    case QueryType.GetDocumentsAndAssociations:
                        return HandleGetDocumentsAndAssociations(Request);

                    default:
                        return new XdsQueryResponse(XdsObjects.Enums.XdsErrorCode.XDSUnknownStoredQuery, DummyContext);
                }
            }
            catch (Exception e)
            {
                return new XdsQueryResponse(e, XdsErrorCode.GeneralException);
            }
        }

        private XdsQueryResponse HandleGetDocumentsAndAssociations(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Documents and Associations");

            IQueryable<XdsDocument>    document_matches;
            IQueryable<XdsAssociation> association_matches;

            XdsDataBase db = new XdsDataBase();
            
            if (String.IsNullOrEmpty(Request.DocumentEntryEntryUUID.ToString()) && String.IsNullOrEmpty(Request.DocumentEntryUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.DocumentEntryEntryUUID.Count() && (0 != Request.DocumentEntryUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");


            if (0 != Request.DocumentEntryEntryUUID.Count())
            {
                //using UUID
                List<String> uuids = new List<String>();
                foreach (var o in Request.DocumentEntryEntryUUID)
                    uuids.Add(o.UUID.ToString());

                document_matches = from document in db.Documents
                            from uid in uuids
                            where uid == document.UUID
                            select document;
            }
            else
            {
                //using UniqueID
                List<String> uids = new List<String>();
                foreach (var o in Request.DocumentEntryUniqueId)
                    uids.Add(o.UUID.ToString());

                document_matches = from document in db.Documents
                            from uid in uids
                            where uid == document.UniqueID
                            select document;
            }

            association_matches = from assoc in db.Associations
                                    from doc in document_matches
                                    where assoc.SourceUUID == doc.UUID || assoc.TargetUUID == doc.UUID
                                    select assoc;

            LogMessageEvent(string.Format("Found {0} documents", document_matches.Count()));
            LogMessageEvent(string.Format("Found {0} associations", association_matches.Count()));

            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass && 1 < document_matches.Count())
            {
                var query = document_matches.GroupBy(s => s.PatientID);
                if (1 != query.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }

            return ReturnResults(Request, db, document_matches, null, null, association_matches);
        }

        private XdsQueryResponse HandleGetSubmissionSetAndContents(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Submission sets and Contents");
            WebServiceEndpoint hssRegistry = new WebServiceEndpoint(registryURI);
            XdsAudit.RegistryStoredQuery(atnaTest, Request.QueryType.ToString(), hssRegistry, new XdsPatient(), Request, new XdsQueryResponse());
            IQueryable<XdsSubmissionSet> submissionset_matches;
            IQueryable<XdsAssociation>   association_matches;
            IQueryable<XdsDocument>      document_matches;
            IQueryable<XdsFolder>        folder_matches;

            XdsDataBase db = new XdsDataBase();

            if (Request.SubmissionSetEntryUUID == null && Request.SubmissionSetUniqueId == null)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (Request.SubmissionSetEntryUUID != null && Request.SubmissionSetUniqueId != null)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");

            int type_has_member = (int)AssociationType.HasMember;

            


            if (Request.SubmissionSetEntryUUID != null)
            {
                string submissionset_uuid = Request.SubmissionSetEntryUUID.UUID;

                //using UUID
                submissionset_matches = from   submissionset in db.SubmissionSets
                                        where  submissionset.UUID == submissionset_uuid
                                        select submissionset;

                

                association_matches   = from   assoc in db.Associations
                                        where  assoc.TypeAsInt == type_has_member
                                        &&     assoc.SourceUUID == submissionset_uuid
                                        select assoc;

                document_matches      = from   doc   in db.Documents
                                        from   assoc in association_matches
                                        where  doc.UUID == assoc.TargetUUID
                                        select doc;

                folder_matches        = from   folder in db.Folders
                                        from   assoc  in association_matches
                                        where  folder.UUID == assoc.TargetUUID
                                        select folder;
            }
            else
            {
                string uid = Request.SubmissionSetUniqueId.UUID;

                //using UniqueId
                submissionset_matches = from submissionset in db.SubmissionSets
                                        where submissionset.UniqueID == uid
                                        select submissionset;
                    
                //TODO but we need the UUID for any searching of the db.Assoications table is this correct
                string uuid = submissionset_matches.Single().UUID.ToString();

                association_matches   = from assoc in db.Associations
                                        where assoc.TypeAsInt == type_has_member
                                        && assoc.SourceUUID == uuid //TODO does the assoc table ever hold uniqueids
                                        select assoc;

                document_matches      = from doc in db.Documents
                                        from assoc in association_matches
                                        where doc.UUID == assoc.TargetUUID //TODO does the assoc table ever hold uniqueids
                                        select doc;


                folder_matches        = from folder in db.Folders
                                        from assoc in association_matches
                                        where folder.UUID == assoc.TargetUUID //TODO does the assoc table ever hold uniqueids
                                        select folder;
            }

            

            //Filter docs for FormatCodes And ConfidentialityCodes
            document_matches = FilterForCodes(Request, document_matches);

            LogMessageEvent(string.Format("Found {0} documents", document_matches.Count()));
            LogMessageEvent(string.Format("Found {0} associations", association_matches.Count()));
            LogMessageEvent(string.Format("Found {0} submission sets", submissionset_matches.Count()));
            LogMessageEvent(string.Format("Found {0} folders", folder_matches.Count()));

            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass)
            {
                //join the results on PatientID,  if result > 1 then records are not from single patient
                var res = submissionset_matches.Join(document_matches.Join(folder_matches, f => f.PatientID, d => d.PatientID, (f, d) => new { patientId = f.PatientID }), f => f.PatientID, d => d.patientId, (f, d) => new { patientId = f.PatientID });
                var patientIds = res.GroupBy(s => s.patientId);
                if (1 < patientIds.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }

            return ReturnResults(Request, db, document_matches, submissionset_matches, folder_matches, association_matches); ;
        }

        private XdsQueryResponse HandleGetFolderAndContents(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Folder and Contents");

            IQueryable<XdsFolder>      folder_matches;
            IQueryable<XdsAssociation> association_matches;
            IQueryable<XdsDocument>    document_matches;

            XdsDataBase db = new XdsDataBase();
            
            if (String.IsNullOrEmpty(Request.FolderEntryUUID.ToString()) && String.IsNullOrEmpty(Request.FolderUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.FolderEntryUUID.Count() && (0 != Request.FolderUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");

            List<string> uuids = new List<string>();
            int type_has_member = (int)AssociationType.HasMember;

            if (Request.FolderEntryUUID.Count > 0)
            {
                //using UUID
                foreach (var f in Request.FolderEntryUUID)
                    uuids.Add(f.UUID.ToString());

                folder_matches = from folder in db.Folders
                                    from uid in uuids
                                    where folder.UUID == uid
                                    select folder;
                    
            }
            else
            {
                //using UniqueId
                foreach (var f in Request.FolderUniqueId)
                    uuids.Add(f.UUID.ToString());


                folder_matches = from folder in db.Folders
                                    from uid in uuids
                                    where folder.UniqueID == uid
                                    select folder;

            }

                association_matches = from assoc in db.Associations
                                        from folder in folder_matches
                                        where assoc.TypeAsInt == type_has_member
                                        && assoc.SourceUUID == folder.UUID
                                        select assoc;

                document_matches = from doc in db.Documents
                                    from assoc in association_matches
                                    where doc.UUID == assoc.TargetUUID
                                    select doc;


            //Filter docs for FormatCodes And ConfidentialityCodes
            document_matches = FilterForCodes(Request, document_matches);

            LogMessageEvent(string.Format("Found {0} documents", document_matches.Count()));
            LogMessageEvent(string.Format("Found {0} associations", association_matches.Count()));
            LogMessageEvent(string.Format("Found {0} folders", folder_matches.Count()));
            
            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass)
            {
                //join the results on PatientID,  if result > 1 then records are not from single patient
                var res = folder_matches.Join(document_matches, f => f.PatientID, d => d.PatientID, (f, d) => new {patientId = f.PatientID});
                var patientIds = res.GroupBy(s => s.patientId);
                if (1 < patientIds.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }

            return ReturnResults(Request, db, document_matches, null, folder_matches, association_matches);
        }

        private XdsQueryResponse HandleGetFoldersForDocument(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Folders and Documents");

            IQueryable<XdsFolder> folder_matches;

            XdsDataBase db = new XdsDataBase();
            
            //validation
            if (String.IsNullOrEmpty(Request.DocumentEntryEntryUUID.ToString()) && String.IsNullOrEmpty(Request.DocumentEntryUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.DocumentEntryEntryUUID.Count() && (0 != Request.DocumentEntryUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");

            int type_has_member = (int)AssociationType.HasMember;
            List<String> uuids = new List<String>();


            if (0 != Request.DocumentEntryEntryUUID.Count())
            {
                foreach (var o in Request.DocumentEntryEntryUUID)
                    uuids.Add(o.UUID.ToString());

            }
            else
            {
                IQueryable<XdsDocument> docs;
                List<string> uniquesIds = new List<string>();

                foreach (var o in Request.DocumentEntryUniqueId)
                    uniquesIds.Add(o.UUID.ToString());


                //Convert uniqueids to uuids
                docs = from d in db.Documents
                       from u in uniquesIds
                       where d.UniqueID == u
                       select d;

                foreach (var doc in docs)
                    uuids.Add(doc.UUID.ToString());
            }

            folder_matches = from association in db.Associations
                                from uid in uuids
                                from folder in db.Folders
                                where association.TypeAsInt == type_has_member
                                && association.TargetUUID == uid
                                && association.SourceUUID == folder.UUID
                                select folder;

            LogMessageEvent(string.Format("Found {0} folders", folder_matches.Count()));

            return ReturnResults(Request, db, null, null, folder_matches, null);
        }

        private XdsQueryResponse HandleGetFolders(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Folders");

            IQueryable<XdsFolder> matches;
            XdsDataBase db = new XdsDataBase();
            
            if (String.IsNullOrEmpty(Request.FolderEntryUUID.ToString()) && String.IsNullOrEmpty(Request.FolderUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.FolderEntryUUID.Count() && (0 != Request.FolderUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");


            if (0 != Request.FolderEntryUUID.Count())
            {
                List<String> uuids = new List<String>();
                foreach (var o in Request.FolderEntryUUID)
                    uuids.Add(o.UUID.ToString());

                matches = from folder in db.Folders
                            from uid in uuids
                            where uid == folder.UUID
                            select folder;
            }
            else
            {

                List<String> uids = new List<String>();
                foreach (var o in Request.FolderUniqueId)
                    uids.Add(o.UUID.ToString());

                matches = from folder in db.Folders
                            from uid in uids
                            where uid == folder.UniqueID
                            select folder;
            }

            LogMessageEvent(string.Format("Found {0} folders", matches.Count()));

            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass && 1 < matches.Count())
            {
                var query = matches.GroupBy(s => s.PatientID);
                if (1 != query.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }

            return ReturnResults(Request, db, null, null, matches, null);
        }

        private XdsQueryResponse HandleFindFolders(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Find Folders");

            IQueryable<XdsFolder> folder_matches;
            XdsDataBase db = new XdsDataBase();
            
            //validation
            if (String.IsNullOrEmpty(Request.PatientId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing PatientId");

            if (String.IsNullOrEmpty(Request.FolderStatus.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing Folder Status");

            int folder_status = (int)Request.FolderStatus;

            folder_matches = from   folder in db.Folders
                             where folder.PatientID == Request.PatientId 
                                //&& folder.LastUpdateTime < Request.FolderLastUpdateTimeTo
                                //&& folder.LastUpdateTime > Request.FolderLastUpdateTimeFrom
                               // && folder.StatusAsInt == folder_status
                                select folder;

            LogMessageEvent(string.Format("Found {0} folders", folder_matches.Count()));
            
            //Filter for LastUpdateTime
            if (Request.FolderLastUpdateTimeFrom != null && Request.FolderLastUpdateTimeTo != null)
                folder_matches = folder_matches.Where(f => f.LastUpdateTime < Request.FolderLastUpdateTimeTo && f.LastUpdateTime > Request.FolderLastUpdateTimeFrom);

            //Filter For Codes
            IEnumerable<XdsFolder> linqQuery = folder_matches.AsEnumerable();
            foreach (var constraint in Request.FolderCodeSet)
                linqQuery = linqQuery.Where(f => MatchCodeListInList(f.CodeList, constraint));


            return ReturnResults(Request, db, null, null, linqQuery, null);
        }

        private XdsQueryResponse HandleGetRelatedDocuments(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Related Documents");

            //Find ASsocaiations
            IQueryable<XdsAssociation> association_matches;
            IQueryable<XdsDocument>    doc_matches;
            XdsDataBase db = new XdsDataBase();
            
            //validation
            if (String.IsNullOrEmpty(Request.DocumentEntryEntryUUID.ToString()) && String.IsNullOrEmpty(Request.DocumentEntryUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.DocumentEntryEntryUUID.Count() && (0 != Request.DocumentEntryUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");


            List<String> uuids = new List<String>();
            foreach (var o in Request.DocumentEntryEntryUUID)
                uuids.Add(o.UUID.ToString());


            int assoc_type = (int)Request.AssociationTypes[0];


            association_matches = from association in db.Associations
                                    from doc in db.Documents
                                    from uid in uuids
                                    where ( uid == association.SourceUUID  && association.TargetUUID == doc.UUID
                                        || uid == association.TargetUUID  && association.SourceUUID == doc.UUID)
                                        && (association.TypeAsInt == assoc_type)
                                    select association;


            doc_matches = from doc in db.Documents
                            from assocation in association_matches
                            where (assocation.SourceUUID == doc.UUID || assocation.TargetUUID == doc.UUID)
                            select doc;

            LogMessageEvent(string.Format("Found {0} associations", association_matches.Count()));
            LogMessageEvent(string.Format("Found {0} documents", doc_matches.Count()));

            return ReturnResults(Request, db, doc_matches, null, null, association_matches);
        }

        private XdsQueryResponse HandleGetDocuments(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Documents");
            var requestedUUID = "";
            IQueryable<XdsDocument> matches;
            XdsDataBase db = new XdsDataBase();
            WebServiceEndpoint hssRegistry = new WebServiceEndpoint(registryURI);
            XdsAudit.RegistryStoredQuery(atnaTest, Request.QueryType.ToString(), hssRegistry, new XdsPatient(), Request, new XdsQueryResponse());

            if (String.IsNullOrEmpty(Request.DocumentEntryEntryUUID.ToString()) && String.IsNullOrEmpty(Request.DocumentEntryUniqueId.ToString()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID or UniqueId");
            else if (0 != Request.DocumentEntryEntryUUID.Count() && (0 != Request.DocumentEntryUniqueId.Count()))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryParamNumber, "Both EntryUUID and UniqueId present in request.");


            if (0 != Request.DocumentEntryEntryUUID.Count())
            {
                List<String> uuids = new List<String>();
                foreach (var o in Request.DocumentEntryEntryUUID)
                {
                    uuids.Add(o.UUID.ToString());
                    requestedUUID = o.UUID.ToString();
                }

                IQueryable<XdsDocument> documents = db.Documents;

                matches = from document in documents
                            from uid in uuids
                            where uid == document.UUID
                            select document;
            }
            else
            {

                List<String> uids = new List<String>();
                foreach (var o in Request.DocumentEntryUniqueId)
                {
                    uids.Add(o.UUID.ToString());
                    requestedUUID = o.UUID.ToString();
                }
                    
                matches = from document in db.Documents
                            from uid in uids
                            where uid == document.UniqueID
                            select document;
            }

            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(string.Format(DateTime.Now.ToString("HH:mm:ss.fff") + ": Found {0} documents - {1}", matches.Count(), requestedUUID));

            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass && 1 < matches.Count())
            {
                var query = matches.GroupBy(s => s.PatientID);
                if (1 != query.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }
            

            return ReturnResults(Request, db, matches, null, null, null);
        }

        private XdsQueryResponse HandleGetSubmissionSets(XdsQueryRequest Request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Subbmissionsets");
            WebServiceEndpoint hssRegistry = new WebServiceEndpoint(registryURI);
            XdsAudit.RegistryStoredQuery(atnaTest, Request.QueryType.ToString(), hssRegistry, new XdsPatient(), Request, new XdsQueryResponse());
            XdsDataBase db = new XdsDataBase();
            
            // validation
            if (0 == Request.AssociationUUID.Count)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing EntryUUID");

            //DB query
            IQueryable<XdsAssociation>   associations   = db.Associations;
            IQueryable<XdsSubmissionSet> submissionSets = db.SubmissionSets;

            // apply filters
            IQueryable<XdsSubmissionSet> submissionset_matches;
            
            List<String> strUuids = new List<string>();
            foreach (var obj in Request.AssociationUUID)
                strUuids.Add(obj.UUID.ToString());

            submissionset_matches = from   submissionSet in submissionSets
                                    from   association   in associations
                                    from   uuid1         in strUuids
                                    where  submissionSet.UUID == association.SourceUUID && association.TargetUUID == uuid1 && association.TypeAsInt == (int)AssociationType.HasMember
                                    select submissionSet;

            LogMessageEvent(string.Format("Found {0} submission sets", submissionset_matches.Count()));

            //Do we need to verify the PatientIds are all same
            if (Request.QueryReturnType == QueryReturnType.LeafClass && 1 < submissionset_matches.Count())
            {
                var query = submissionset_matches.GroupBy(s => s.PatientID);
                if (1 != query.Count())
                {
                    //TODO add new error code.....
                    return new XdsQueryResponse(XdsErrorCode.GeneralException, "Not Single Patient");
                }
            }

            return ReturnResults(Request, db, null, submissionset_matches, null, null);
        }

        private XdsQueryResponse HandleGetAssociations(XdsQueryRequest request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get Associations");

            XdsQueryResponse response = new XdsQueryResponse();

            // validation
            if (0 == request.AssociationUUID.Count)
            return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing Association UID");

            using (XdsDataBase db = new XdsDataBase())
            {
                IQueryable<XdsAssociation> association_matches;
                
                List<string> uuids = new List<string>();
                foreach (var s in request.AssociationUUID)
                    uuids.Add(s.UUID.ToString());
                
                association_matches =  from a in db.Associations
                                       from uid in uuids
                                       where  a.SourceUUID == uid || a.TargetUUID == uid
                                       select a;

                foreach (var assoc in association_matches.AsEnumerable())
                    response.ReturnedAssociations.Add(assoc);
           
                LogMessageEvent(string.Format("Found {0} associations", association_matches.Count()));
            }


            return response;
        }

        //This has changed!!!!!
        XdsRegistryResponse server_RegisterReceived(XdsSubmissionSet SubmissionSet, XdsRequestInfo test)
        {
            LogMessageEvent("--- --- ---");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Register Document Request Received");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Login audit event logged...");
            XdsAudit.UserAuthentication(atnaTest, true);

            string nhsNumber = SubmissionSet.Documents[0].ReferenceIdList.Strings[1].ToString();

            XdsPatient myPatient = new XdsPatient();
            myPatient.CompositeId = SubmissionSet.PatientInfo.CompositeId;
            List<string> StudyUIDList = new List<string>();
            StudyUIDList.Add(SubmissionSet.UniqueID);
            XdsAudit.PHIImport(atnaTest, SubmissionSet.SourceID, StudyUIDList, myPatient);
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": RegisterReceived Import audit event logged...");
            try
            {
                XdsRegistryResponse resp = new XdsRegistryResponse();
                SubmissionSet.ReplaceSymbolicNames();
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": PatientId - " + SubmissionSet.PatientID);

                //check that Authority Domain of patient matches with that of Registry
                if (SubmissionSet.PatientInfo.ID_Root != authDomain)
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Authority Domains do not match...");
                    resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                    resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSUnknownCommunity, "Authority Domains do not match", "");
                    return resp;
                }

                //chq Author
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Checking Authors...");
                int authorCount = 0;
                foreach(var auth in SubmissionSet.Authors)
                {
                    authorCount++;
                }

                if(authorCount == 0)
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                    XdsAudit.UserAuthentication(atnaTest, false);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing Author classifications");
                    resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                    resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing Author Classifications", "");
                    return resp;
                }
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Authors check complete...");
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Checking document metadata...");
                foreach(var doc in SubmissionSet.Documents)
                {
                    XdsCode classCodeExists = doc.ClassCode;

                    doc.VersionInfo = "1";
                    //doc.ClassCode = new XdsCode("myCode", "MyCode", new XdsInternationalString("myCode"));
                    
                    if (classCodeExists == null)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing ClassCode Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode Classification");
                        return resp;
                    }
                    else
                    {
                        string classCode1 = doc.ClassCode.CodeMeaning.AllValues;
                        if (classCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from ClassCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodeMeaning value");
                            return resp;
                        }
                        string classCode2 = doc.ClassCode.CodeValue;
                        if (classCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from ClassCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodeValue value");
                            return resp;
                        }
                        string classCode3 = doc.ClassCode.CodingScheme;
                        if (classCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from ClassCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ClassCode.CodingScheme value");
                            return resp;
                        }
                    }

                    int countConfCode = doc.ConfidentialityCodes.Count;
                    if (countConfCode == 0)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing ConfidentialityCode Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode Classification");
                        return resp;
                    }
                    else
                    {
                        string confCode1 = doc.ConfidentialityCodes[0].CodeMeaning.AllValues;
                        if (confCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from ConfidentialityCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodeMeaning value");
                            return resp;
                        }
                        string confCode2 = doc.ConfidentialityCodes[0].CodeValue;
                        if (confCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from ConfidentialityCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodeValue value");
                            return resp;
                        }
                        string confCode3 = doc.ConfidentialityCodes[0].CodingScheme;
                        if (confCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from ConfidentialityCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ConfidentialityCode.CodingScheme value");
                            return resp;
                        }
                    }

                    XdsCode formatCodeExists = doc.FormatCode;
                    if (formatCodeExists == null)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing FormatCode Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode Classification");
                        return resp;
                    }
                    else
                    {
                        string formatCode1 = doc.FormatCode.CodeMeaning;
                        if (formatCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from FormatCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodeMeaning value");
                            return resp;
                        }
                        string formatCode2 = doc.FormatCode.CodeValue;
                        if (formatCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from FormatCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodeValue value");
                            return resp;
                        }
                        string formatCode3 = doc.FormatCode.CodingScheme;
                        if (formatCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from FormatCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing FormatCode.CodingScheme value");
                            return resp;
                        }
                    }

                    XdsCode healthCareCodeExists = doc.HealthCareFacilityTypeCode;
                    if (healthCareCodeExists == null)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing HealthCareFacility Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacility Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacility Classification");
                        return resp;
                    }
                    else
                    {
                        string healthCareCode1 = doc.HealthCareFacilityTypeCode.CodeMeaning;
                        if (healthCareCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from HealthCareFacilityTypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodeMeaning value");
                            return resp;
                        }
                        string healthCareCode2 = doc.HealthCareFacilityTypeCode.CodeValue;
                        if (healthCareCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from HealthCareFacilityTypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodeValue value");
                            return resp;
                        }
                        string healthCareCode3 = doc.HealthCareFacilityTypeCode.CodingScheme;
                        if (healthCareCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from HealthCareFacilityTypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing HealthCareFacilityTypeCode.CodingScheme value");
                            return resp;
                        }
                    }

                    XdsCode practiceSettingCode = doc.PracticeSettingCode;
                    if (practiceSettingCode == null)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing PracticeSettingCode Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode Classification");
                        return resp;
                    }
                    else
                    {
                        string practiceSettingCode1 = doc.PracticeSettingCode.CodeMeaning;
                        if (practiceSettingCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from PracticeSettingCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodeMeaning value");
                            return resp;
                        }
                        string practiceSettingCode2 = doc.PracticeSettingCode.CodeValue;
                        if (practiceSettingCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from PracticeSettingCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodeValue value");
                            return resp;
                        }
                        string practiceSettingCode3 = doc.PracticeSettingCode.CodingScheme;
                        if (practiceSettingCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from PracticeSettingCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing PracticeSettingCode.CodingScheme value");
                            return resp;
                        }
                    }

                    XdsCode typeCodeExists = doc.TypeCode;
                    if (typeCodeExists == null)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing TypeCode Classification...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode Classification", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode Classification");
                        return resp;
                    }
                    else
                    {
                        string typeCode1 = doc.TypeCode.CodeMeaning;
                        if (typeCode1 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from TypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodeMeaning value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodeMeaning value");
                            return resp;
                        }
                        string typeCode2 = doc.TypeCode.CodeValue;
                        if (typeCode2 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from TypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodeValue value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodeValue value");
                            return resp;
                        }
                        string typeCode3 = doc.TypeCode.CodingScheme;
                        if (typeCode3 == "")
                        {
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                            XdsAudit.UserAuthentication(atnaTest, false);
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from TypeCode details...");
                            resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                            resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodingScheme value", "");
                            //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing TypeCode.CodingScheme value");
                            return resp;
                        }
                    }
                }
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Document metadata checks complete...");
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Checking SubmissionSet data...");
                XdsCode contentTypeExists = SubmissionSet.ContentType;
                if (contentTypeExists == null)
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                    XdsAudit.UserAuthentication(atnaTest, false);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing ContentType Classification...");
                    resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                    resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType Classification", "");
                    //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType Classification");
                    return resp;
                }
                else
                {
                    string contentType1 = SubmissionSet.ContentType.CodeMeaning.AllValues;
                    if (contentType1 == "")
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeMeaning from ContentType details...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodeMeaning value", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodeMeaning value");
                        return resp;
                    }
                    string contentType2 = SubmissionSet.ContentType.CodeValue;
                    if (contentType2 == "")
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodeValue from ContentType details...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodeValue value", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodeValue value");
                        return resp;
                    }
                    string contentType3 = SubmissionSet.ContentType.CodingScheme;
                    if (contentType3 == "")
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                        XdsAudit.UserAuthentication(atnaTest, false);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet rejected - missing CodingScheme from ContentType details...");
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodingScheme value", "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSRegistryMetadataError, "missing ContentType.CodingScheme value");
                        return resp;
                    }
                }
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet data checks complete...");

                using (XdsDataBase dbSub = new XdsDataBase())
                {
                    int subCount = dbSub.MySubscriptions.Count();
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Checking subscriptions (" + subCount + ")...");
                    bool subscriptionMatched = false;

                    foreach (var sub in dbSub.MySubscriptions)
                    {
                        //Only consider subscriptions where termination datetime is current
                        DateTime terminationDateTime = sub.TerminationTime;
                        if (terminationDateTime > DateTime.Now)
                        {
                            string patId = sub.patientId;
                            if (patId == SubmissionSet.PatientID)
                            {
                                //check for cancer type match
                                string examName = SubmissionSet.Documents[0].Title;

                                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Matched subscription for " + SubmissionSet.PatientID + "...");
                                subscriptionMatched = true;
                                //raise notification and send
                                XdsDomain domain = new XdsDomain();
                                //domain.NotificationRecipientEndpoint = new WebServiceEndpoint(notificationRecipient);
                                string notifyEndPoint = "http://" + sub.ConsumerReferenceAddress + "/";
                                domain.NotificationRecipientEndpoint = new WebServiceEndpoint(notifyEndPoint);
                                XdsNotifyRequest request = new XdsNotifyRequest();
                                request.Address = "http://NotificationBrokerServer/Subscription";
                                Guid guid = Guid.NewGuid();
                                request.SubscriptionId = guid.ToString();
                                //request.SubscriptionId = "382dcdc7-8888-9999-8888-48fd83bca938";
                                request.ProducerReference = "http://producerReference.com";
                                string notificationType = sub.TopicDialect;
                                //string notificationType = "ihe:SubmissionSetMetadata";


                                switch (notificationType)
                                {
                                    case "SubsriptionFullDocument":
                                        {
                                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Type - SubscriptionFullDocument");
                                            request.Topic = XdsObjects.Enums.QueryType.SubsriptionFullDocument;
                                            string docUniqueId = SubmissionSet.UniqueID;
                                            //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": repositoryPath - " + repositoryPath + docUniqueId);
                                            //XdsDocument doc = new XdsDocument(@"C:\HSSRepository\" + docUniqueId);
                                            XdsDocument doc = new XdsDocument(repositoryPath + docUniqueId);
                                            string patName = SubmissionSet.Documents[0].SourcePatientDetails.CompositeName;
                                            string patientId = SubmissionSet.Documents[0].SourcePatientDetails.CompositeId;
                                            XdsPatient patient = new XdsPatient(patName);
                                            patient.CompositeId = patientId;
                                            doc.PatientInfo = patient;
                                            doc.SourcePatientDetails = patient;
                                            request.Message.DocumentsToNotify.Add(doc);
                                            break;
                                        }
                                    case "SubscriptionMinimalDocument":
                                        {
                                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Type - SubscriptionMinimalDocument");
                                            request.Topic = XdsObjects.Enums.QueryType.SubscriptionMinimalDocument;
                                            XdsDocument doc = new XdsDocument();
                                            doc.RepositoryUniqueId = repositoryId;
                                            //doc.HomeCommunityID = "1.1.1.1.1";
                                            doc.UniqueID = SubmissionSet.UUID;
                                            request.Message.DocumentsToNotify.Add(doc);
                                            break;
                                        }
                                }
                                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Notification sent to - " + notifyEndPoint);
                                domain.Notify(request);
                            }
                        }
                    }
                    if (subscriptionMatched == false)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": No subscriptions matched...");
                    }
                }

                using (XdsDataBase db = new XdsDataBase())
                {
                    //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": My Audit5");
                    // Validate Patient ID
                    //if (!db.Patients.Any(patient => patient.PatientId == SubmissionSet.PatientInfo.CompositeId))
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Validating patient...");
                    if(!db.Patients.Any(patient => patient.PatientId == SubmissionSet.PatientInfo.CompositeId))
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Unknown patient - " + SubmissionSet.PatientInfo.CompositeId);
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                        resp.AddError(XdsObjects.Enums.XdsErrorCode.XDSUnknownPatientId, SubmissionSet.PatientInfo.CompositeId, "");
                        //return new XdsRegistryResponse(XdsObjects.Enums.XdsErrorCode.XDSUnknownPatientId, SubmissionSet.PatientInfo.CompositeId);
                        return resp;
                    }
                    else
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Patient exists in Registry...");
                    }

                    

                    //XdsPatient p = db.Patients.Where(patient => patient.CompositeId == SubmissionSet.PatientInfo.CompositeId).First();
                    // TODO - lots of other validation as per IHE rules

                    
                    // set status to available for all documents, folders and the submission set itself

                    SubmissionSet.AvailabilityStatus = AvailabilityStatusCode.Approved;
                    //SubmissionSet.VersionInfo = "";
                    /*foreach (var s in SubmissionSets)
                    {
                        db.Entry(s).Collection(p => p.Authors).Load();
                        db.Entry(s).Reference(p => p.ContentType).Load();
                        s.PatientInfo.ID_Type = "";
                        s.PatientInfo.ID_Domain = "";
                        s.VersionInfo = "";
                        resp.ReturnedSubmissionSets.Add(s);
                    }*/
                    //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet.SourceID - " + SubmissionSet.ContentTyp);
                    //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Audit 3");
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet.SourceID - " + SubmissionSet.SourceID);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet.UniqueID - " + SubmissionSet.UniqueID);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": SubmissionSet.UUID - " + SubmissionSet.UUID);
                    bool foundReplace = false;
                    bool foundAppend = false;
                    bool foundTrans = false;
                    foreach(var ass in SubmissionSet.Associations)
                    {
                        if(ass.Type == AssociationType.Replace)
                        {
                            foundReplace = true;
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Association Type - Replace");
                            break;
                        }
                        else if(ass.Type == AssociationType.Append)
                        {
                            foundAppend = true;
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Association Type - Replace");
                            break;
                        }
                        else if (ass.Type == AssociationType.Transform)
                        {
                            foundTrans = true;
                            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Association Type - Replace");
                            break;
                        }
                    }
                    if (foundReplace == false && foundAppend == false && foundTrans == false)
                    {
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Association Type - Original");
                    }

                    int docCounter = 0;
                    foreach (var d in SubmissionSet.Documents)
                    {
                        docCounter++;
                        d.AvailabilityStatus = AvailabilityStatusCode.Approved;
                        string uid = d.UniqueID;
                        string uuid = d.UUID;
                        string mimeType = d.MimeType;
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Document(" + docCounter + ") UniqueId - " + uid);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Document(" + docCounter + ") UUID - " + uuid);
                        LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Document(" + docCounter + ") mime type - " + mimeType);
                        //d.PatientInfo = SubmissionSet.PatientInfo;

                        d.VersionInfo = "1";
                        //SubmissionSet.VersionInfo = "1";

                        d.Title = new XdsInternationalString("");

                        if (SubmissionSet.Title == null)
                        {
                            SubmissionSet.Title = new XdsInternationalString("");
                        }

                        if (d.Comments == null)
                            d.Comments = new XdsInternationalString();
                    }

                    //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Audit 4");
                    foreach (var d in SubmissionSet.Folders)
                    {
                        d.AvailabilityStatus = AvailabilityStatusCode.Approved;

                        if (d.Comments == null)
                            d.Comments = new XdsInternationalString();
                    }

                    if (resp.Errors.ErrorList.Count > 0)
                    {
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                    }
                    else
                    {
                        resp.Status = XdsObjects.Enums.RegistryResponseStatus.Success;

                        //Set lastUpdateTime on all associated folders
                        foreach (var f in SubmissionSet.Folders)
                        {
                            f.LastUpdateTime = DateTime.Now;
                        }

                        if (SubmissionSet.Comments == null)
                            SubmissionSet.Comments = new XdsInternationalString("");

                        if(SubmissionSet.Title == null)
                        {
                            SubmissionSet.Title = new XdsInternationalString("");
                        }

                        SubmissionSet.Title = new XdsInternationalString("");
                        SubmissionSet.VersionInfo = "";

                        //db.Patients.Attach(SubmissionSet.PatientInfo);
                        db.SubmissionSets.Add(SubmissionSet);

                        //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Audit 5");


                        db.SaveChanges();
                        //LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Audit 6");
                        //Existing folders
                        //Find folders by  from assoc
                        var folder_matches = from assoc in SubmissionSet.Associations
                                             from folder in db.Folders
                                             where assoc.SourceUUID == folder.UUID
                                             select folder;

                        foreach (var f in folder_matches)
                        {
                            f.LastUpdateTime = DateTime.Now;
                        }


                    }
                    
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                    XdsAudit.UserAuthentication(atnaTest, false);
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Returning registry response...");
                    return resp;
                }
            }
            catch (Exception ex)
            {
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": User Logout audit event logged...");
                XdsAudit.UserAuthentication(atnaTest, false);
                XdsRegistryResponse response = new XdsRegistryResponse();
                LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ":" + ex.InnerException.Message);
                response.Status = XdsObjects.Enums.RegistryResponseStatus.Failure;
                response.AddError(XdsObjects.Enums.XdsErrorCode.GeneralException, ex.InnerException.Message, "");
                //return new XdsRegistryResponse(ex, XdsObjects.Enums.XdsErrorCode.GeneralException);
                return response;
            }
        }

        #endregion

        XdsQueryResponse HandleFindDocumentQuery(XdsQueryRequest request)
        {
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Find Document Query for Patient - " + request.PatientId);
            XdsAudit.UserAuthentication(atnaTest, true);
            XdsPatient myPatient = new XdsPatient();
            myPatient.CompositeId = request.PatientId;
            WebServiceEndpoint hssRegistry = new WebServiceEndpoint(registryURI);
            XdsAudit.RegistryStoredQuery(atnaTest, request.QueryType.ToString(), hssRegistry, myPatient, request, new XdsQueryResponse());
            using (XdsDataBase db = new XdsDataBase())
            {
                // validation
                if (request.DocumentEntryStatus == AvailabilityStatusCode.None)
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": XDSDocumentEntryStatus is missing...");
                    return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "$XDSDocumentEntryStatus is missing in FindDocuments query");
                }

                if (String.IsNullOrEmpty(request.PatientId))
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Missing Patient ID...");
                    return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing Patient ID");
                }

                if (!db.Patients.Any(p => p.PatientId == request.PatientId))
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Unknown Patient ID...");
                    return new XdsQueryResponse(XdsErrorCode.XDSUnknownPatientId, DummyContext);
                }

                var Documents = DocumentQuery(request, db);
                LogMessageEvent(string.Format("Found {0} documents", Documents.Count()));
                XdsAudit.UserAuthentication(atnaTest, false);
                return ReturnResults(request, db, Documents, null, null, null);
            }
        }


        XdsQueryResponse HandleFindSubmissionSetQuery(XdsQueryRequest request)
        {
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Find Submissionset Query");

            using (XdsDataBase db = new XdsDataBase())
            {

                if (String.IsNullOrEmpty(request.PatientId))
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Missing Patient ID...");
                    return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing Patient ID");
                }

                if (!db.Patients.Any(p => p.PatientId == request.PatientId))
                {// No patient found
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Unknown Patient ID...");
                    return new XdsQueryResponse(XdsErrorCode.XDSUnknownPatientId, DummyContext);
                }

                if (request.SubmissionSetStatus == AvailabilityStatusCode.None)
                {
                    LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": XDSSubmissionSetStatus is missing in FindDocuments query...");
                    return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "$XDSSubmissionSetStatus is missing in FindSubmissionSet query");
                }
                
                var SubmissionSets = SubmissionSetQuery(request, db);
                LogMessageEvent(string.Format("Found {0} subbmission sets", SubmissionSets.Count()));
                return ReturnResults(request, db, null, SubmissionSets, null, null);
            }
        }

        XdsQueryResponse HandleGetAll(XdsQueryRequest request)
        {
            //logDateTime = DateTime.Now.ToString("dd/MM/yyy HH:mm:ss.fff");
            LogMessageEvent(DateTime.Now.ToString("HH:mm:ss.fff") + ": Handling Get All");

            XdsDataBase db = new XdsDataBase();
         
            //Validation
            if (String.IsNullOrEmpty(request.PatientId))
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "Missing Patient ID");
            if (!db.Patients.Any(p => p.PatientId == request.PatientId)) // No patient found
                return new XdsQueryResponse(XdsErrorCode.XDSUnknownPatientId, DummyContext);
            if (request.DocumentEntryStatus == AvailabilityStatusCode.None)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "$XDSDocumentEntryStatus is missing in GetAll query");
            if (request.SubmissionSetStatus == AvailabilityStatusCode.None)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "$XDSSubmissionSetStatus is missing in GetAll query");
            if (request.FolderStatus == AvailabilityStatusCode.None)
                return new XdsQueryResponse(XdsErrorCode.XDSStoredQueryMissingParam, "$XDSFolderStatus is missing in GetAll query");

            //Query
            var submission_sets = SubmissionSetQuery(request, db);
            var documents = DocumentQuery(request, db);

            var folders = from f in db.Folders
                          where f.PatientID == request.PatientId && f.AvailabilityStatusAsInt == (int) request.FolderStatus
                          select f;

            var associations = (from a in db.Associations
                               from f in folders
                               from s in submission_sets
                               from d in documents
                               where (a.TargetUUID == f.UUID || a.SourceUUID == f.UUID)
                               || (a.TargetUUID == d.UUID || a.SourceUUID == d.UUID)
                               || (a.TargetUUID == s.UUID || a.SourceUUID == s.UUID)
                               select a).Distinct();

            LogMessageEvent(string.Format("Found {0} documents", documents.Count()));
            LogMessageEvent(string.Format("Found {0} associations", associations.Count()));
            LogMessageEvent(string.Format("Found {0} folders", folders.Count()));
            LogMessageEvent(string.Format("Found {0} submission sets", submission_sets.Count()));

            return ReturnResults(request, db, documents, submission_sets, folders, associations);
        }

        #region High Level Shared methods
        
        private static IQueryable<XdsDocument> DocumentQuery(XdsQueryRequest request, XdsDataBase db)
        {
            IQueryable<XdsDocument> source = db.Documents;

            // decide what other data is needed for querying
            if (request.DocumentEntryTypeCodes.Count > 0)
                source = source.Include(x => x.TypeCode);
            if (request.DocumentEntryConfidentialityCodeSet.Count > 0)
                source = source.Include(x => x.ConfidentialityCodes);

            var efMatchingDocuments = from  d in source
                                      where d.PatientID == request.PatientId
                                      && ((int)request.DocumentEntryStatus & d.AvailabilityStatusAsInt) > 0
                                      select d;

            efMatchingDocuments = FilterForTimeParams(request, efMatchingDocuments);

            efMatchingDocuments = FilterForCodes(request, efMatchingDocuments);

            return efMatchingDocuments;
        }

        private static IQueryable<XdsDocument> FilterForCodes(XdsQueryRequest request, IQueryable<XdsDocument> efMatchingDocuments)
        {
            IEnumerable<XdsDocument> linqQuery = efMatchingDocuments.AsEnumerable();

            if (request.DocumentEntryClassCodes.Count > 0)
                linqQuery = linqQuery.Where(x => MatchCodeInList(x.ClassCode, request.DocumentEntryClassCodes));

            if (request.DocumentEntryTypeCodes.Count > 0)
                linqQuery = linqQuery.Where(x => MatchCodeInList(x.TypeCode, request.DocumentEntryTypeCodes));

            if (request.DocumentEntryFormatCodes.Count > 0)
                linqQuery = linqQuery.Where(x => MatchCodeInList(x.FormatCode, request.DocumentEntryFormatCodes));

            foreach (var constraint in request.DocumentEntryConfidentialityCodeSet)
            {
                linqQuery = linqQuery.Where(x => MatchCodeListInList(x.ConfidentialityCodes, constraint));
            }

            foreach (var constraint in request.DocumentEntryEventCodeSet)
            {
                linqQuery = linqQuery.Where(x => MatchCodeListInList(x.EventCodeList, constraint));
            }
            return linqQuery.AsQueryable();
        }

        private static IQueryable<XdsDocument> FilterForTimeParams(XdsQueryRequest request, IQueryable<XdsDocument> efMatchingDocuments)
        {
            if (request.DocumentEntryCreationTimeFrom != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.CreationTime >= request.DocumentEntryCreationTimeFrom);

            if (request.DocumentEntryCreationTimeTo != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.CreationTime <= request.DocumentEntryCreationTimeTo);

            if (request.DocumentEntryServiceStartTimeFrom != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.ServiceStartTime >= request.DocumentEntryServiceStartTimeFrom);

            if (request.DocumentEntryServiceStartTimeTo != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.ServiceStartTime >= request.DocumentEntryServiceStartTimeTo);

            if (request.DocumentEntryServiceStopTimeFrom != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.ServiceStopTime >= request.DocumentEntryServiceStopTimeFrom);

            if (request.DocumentEntryServiceStopTimeTo != null)
                efMatchingDocuments = efMatchingDocuments.Where(x => x.ServiceStopTime >= request.DocumentEntryServiceStopTimeTo);

            return efMatchingDocuments;
        }

        private static IQueryable<XdsSubmissionSet> SubmissionSetQuery(XdsQueryRequest request, XdsDataBase db)
        {
            IQueryable<XdsSubmissionSet> source = db.SubmissionSets;

            // needed for author query
            if (request.SubmissionSetAuthorPerson != null)
            {
                source = source.Include(x => x.Authors);
            }

            var efSubmissionSets = from d in source
                                   where d.PatientID == request.PatientId
                                   && ((int)request.SubmissionSetStatus & d.AvailabilityStatusAsInt) > 0
                                   select d;

            if (request.SubmissionSetSourceId != null)
                efSubmissionSets = efSubmissionSets.Where(x => request.SubmissionSetSourceId.Contains(x.SourceID));

            if (request.SubmissionSetSubmissionTimeFrom != null)
                efSubmissionSets = efSubmissionSets.Where(x => x.SubmissionTime <= request.SubmissionSetSubmissionTimeFrom);

            if (request.SubmissionSetSubmissionTimeTo != null)
                efSubmissionSets = efSubmissionSets.Where(x => x.SubmissionTime <= request.SubmissionSetSubmissionTimeTo);



            // above this line is EF queries (simple only)
            // filters below use client evaluation
            var linqSubmissionSets = efSubmissionSets;

            if (request.SubmissionSetAuthorPerson != null)
                linqSubmissionSets = linqSubmissionSets.Where(x => x.HasAuthor(request.SubmissionSetAuthorPerson));

            return linqSubmissionSets;
        }

        private static XdsQueryResponse ReturnResults(XdsQueryRequest request, XdsDataBase db,
            IEnumerable<XdsDocument> Documents,
            IEnumerable<XdsSubmissionSet> SubmissionSets,
            IEnumerable<XdsFolder> Folders,
            IEnumerable<XdsAssociation> Associations)
        {

            XdsQueryResponse resp = new XdsQueryResponse();

            if (Documents == null)
                Documents = new List<XdsDocument>();
            if (SubmissionSets == null)
                SubmissionSets = new List<XdsSubmissionSet>();
            if (Folders == null)
                Folders = new List<XdsFolder>();
            if (Associations == null)
                Associations = new List<XdsAssociation>();

            if (request.QueryReturnType == QueryReturnType.ObjectRef)
            {
                if (Documents.Count() + SubmissionSets.Count() + Folders.Count() + Associations.Count() > MaxObjectRefResults) // too many results
                    return new XdsQueryResponse(new Exception("Too many results"), XdsErrorCode.XDSTooManyResults);

                foreach (var d in Documents)
                    resp.ReturnedObjectRefs.Add(new XdsObjectRef(d.UUID));
                foreach (var d in SubmissionSets)
                    resp.ReturnedObjectRefs.Add(new XdsObjectRef(d.UUID));
                foreach (var d in Folders)
                    resp.ReturnedObjectRefs.Add(new XdsObjectRef(d.UUID));
                foreach (var d in Associations)
                    resp.ReturnedObjectRefs.Add(new XdsObjectRef(d.UUID));
                
                resp.Status = RegistryResponseStatus.Success;
                return resp;
            }
            else
            {
                if (Documents.Count() > MaxLeafClassResults) // too many results
                    return new XdsQueryResponse(new Exception("Too many results"), XdsErrorCode.XDSTooManyResults);

                // This may strill need more sub-data to be loaded
                foreach (var d in Documents)
                {
                    // load related - this is NOT necessarily the most efficient way!
                    db.Entry(d).Collection(p => p.Authors).Load();
                    db.Entry(d).Collection(p => p.ConfidentialityCodes).Load();
                    db.Entry(d).Collection(p => p.EventCodeList).Load();

                    db.Entry(d).Reference(p => p.ClassCode).Load();
                    db.Entry(d).Reference(p => p.TypeCode).Load();
                    db.Entry(d).Reference(p => p.FormatCode).Load();
                    db.Entry(d).Reference(p => p.HealthCareFacilityTypeCode).Load();
                    db.Entry(d).Reference(p => p.PracticeSettingCode).Load();

                    d.PatientInfo.ID_Type = "";
                    d.PatientInfo.ID_Domain = "";

                    d.VersionInfo = "1";
                    resp.ReturnedDocuments.Add(d);
                }
                foreach (var d in SubmissionSets)
                {
                    db.Entry(d).Collection(p => p.Authors).Load();
                    db.Entry(d).Reference(p => p.ContentType).Load();

                    d.PatientInfo.ID_Type = "";
                    d.PatientInfo.ID_Domain = "";

                    resp.ReturnedSubmissionSets.Add(d);
                    d.VersionInfo = "1";
                }
                foreach (var d in Folders)
                {
                    d.PatientInfo.ID_Type = "";
                    d.PatientInfo.ID_Domain = "";
                    resp.ReturnedFolders.Add(d);
                }
                foreach (var a in Associations)
                {
                    resp.ReturnedAssociations.Add(a);
                }

                resp.Status = RegistryResponseStatus.Success;
                return resp;
            }
        }
        #endregion



        #region utilities
        private static bool MatchCodeListInList(List<XdsCode> stored, List<XdsCode> constraint)
        {
            foreach (var code in stored)
            {
                if (MatchCodeInList(code, constraint))
                    return true;
            }
            return false;
        }

        private static bool MatchCodeInList(XdsCode code, List<XdsCode> list)
        {
            if (code == null)
                return false;

            if (list.Count == 0)
                return true;

            foreach (var c in list)
            {
                if (c.CodingScheme == code.CodingScheme && c.CodeValue == code.CodeValue)
                    return true;
            }
            return false;
        }


        #endregion


    }

    static class Extensions
    {
        public static bool HasAuthor(this XdsSubmissionSet s, string Pattern)
        {
            // this implements SQL "LIKE" syntax    
            var matcher = new Regex(@"\A"
                + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\")
                    .Replace(Pattern, ch => @"\" + ch).Replace('_', '.')
                    .Replace("%", ".*")
                + @"\z", RegexOptions.Singleline);

            foreach (var a in s.Authors)
            {

                if (matcher.IsMatch(a.Name))
                    return true;
            }
            return false;
        }
    }
}

