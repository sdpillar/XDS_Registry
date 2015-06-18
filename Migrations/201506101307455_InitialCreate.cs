namespace XdsRegistry.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MySubscriptions",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        TerminationTime = c.DateTime(nullable: false, storeType: "datetime2"),
                        SubscriptionDate = c.DateTime(nullable: false, storeType: "datetime2"),
                        patientId = c.String(),
                        ConsumerReferenceAddress = c.String(),
                        TopicDialect = c.String(),
                        CancerType = c.String(),
                        Query_Id = c.Guid(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.XdsSubscriptionRequests", t => t.Query_Id)
                .Index(t => t.Query_Id);
            
            CreateTable(
                "dbo.XdsSubscriptionRequests",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        ConsumerReferenceAddress = c.String(),
                        TopicDialect = c.String(),
                        InitialTerminationTime = c.DateTime(nullable: false),
                        Query_Id = c.Guid(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.XdsQueryRequests", t => t.Query_Id)
                .Index(t => t.Query_Id);
            
            CreateTable(
                "dbo.XdsQueryRequests",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        DocumentEntryCreationTimeFrom = c.DateTime(),
                        DocumentEntryCreationTimeTo = c.DateTime(),
                        DocumentEntryServiceStartTimeFrom = c.DateTime(),
                        DocumentEntryServiceStartTimeTo = c.DateTime(),
                        DocumentEntryServiceStopTimeFrom = c.DateTime(),
                        DocumentEntryServiceStopTimeTo = c.DateTime(),
                        SubmissionSetSubmissionTimeFrom = c.DateTime(),
                        SubmissionSetSubmissionTimeTo = c.DateTime(),
                        SubmissionSetAuthorPerson = c.String(),
                        FolderLastUpdateTimeFrom = c.DateTime(),
                        FolderLastUpdateTimeTo = c.DateTime(),
                        HomeCommunityID = c.String(),
                        PatientId = c.String(),
                        SubmissionSetEntryUUID_UUID = c.String(maxLength: 128),
                        SubmissionSetUniqueId_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.XdsObjectRefs", t => t.SubmissionSetEntryUUID_UUID)
                .ForeignKey("dbo.XdsObjectRefs", t => t.SubmissionSetUniqueId_UUID)
                .Index(t => t.SubmissionSetEntryUUID_UUID)
                .Index(t => t.SubmissionSetUniqueId_UUID);
            
            CreateTable(
                "dbo.XdsCodes",
                c => new
                    {
                        ID = c.Guid(nullable: false),
                        CodingScheme = c.String(),
                        CodeValue = c.String(),
                        CodeMeaning_AllValues = c.String(),
                        XdsQueryRequest_Id = c.Guid(),
                        XdsQueryRequest_Id1 = c.Guid(),
                        XdsQueryRequest_Id2 = c.Guid(),
                        XdsQueryRequest_Id3 = c.Guid(),
                        XdsQueryRequest_Id4 = c.Guid(),
                        XdsQueryRequest_Id5 = c.Guid(),
                        XdsDocument_UUID = c.String(maxLength: 128),
                        XdsDocument_UUID1 = c.String(maxLength: 128),
                        XdsFolder_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id1)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id2)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id3)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id4)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id5)
                .ForeignKey("dbo.XdsDocuments", t => t.XdsDocument_UUID)
                .ForeignKey("dbo.XdsDocuments", t => t.XdsDocument_UUID1)
                .ForeignKey("dbo.XdsFolders", t => t.XdsFolder_UUID)
                .Index(t => t.XdsQueryRequest_Id)
                .Index(t => t.XdsQueryRequest_Id1)
                .Index(t => t.XdsQueryRequest_Id2)
                .Index(t => t.XdsQueryRequest_Id3)
                .Index(t => t.XdsQueryRequest_Id4)
                .Index(t => t.XdsQueryRequest_Id5)
                .Index(t => t.XdsDocument_UUID)
                .Index(t => t.XdsDocument_UUID1)
                .Index(t => t.XdsFolder_UUID);
            
            CreateTable(
                "dbo.XdsObjectRefs",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        HomeCommunityID = c.String(),
                        XdsQueryRequest_Id = c.Guid(),
                        XdsQueryRequest_Id1 = c.Guid(),
                        XdsQueryRequest_Id2 = c.Guid(),
                        XdsQueryRequest_Id3 = c.Guid(),
                        XdsQueryRequest_Id4 = c.Guid(),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id1)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id2)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id3)
                .ForeignKey("dbo.XdsQueryRequests", t => t.XdsQueryRequest_Id4)
                .Index(t => t.XdsQueryRequest_Id)
                .Index(t => t.XdsQueryRequest_Id1)
                .Index(t => t.XdsQueryRequest_Id2)
                .Index(t => t.XdsQueryRequest_Id3)
                .Index(t => t.XdsQueryRequest_Id4);
            
            CreateTable(
                "dbo.Patients",
                c => new
                    {
                        PatientId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.PatientId);
            
            CreateTable(
                "dbo.XdsSubmissionSets",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        Structured = c.Boolean(nullable: false),
                        IntendedRecipients_AllValues = c.String(),
                        UniqueID = c.String(),
                        SourceID = c.String(),
                        HomeCommunityID = c.String(),
                        PatientID = c.String(),
                        SubmissionTime = c.DateTime(nullable: false),
                        AvailabilityStatusAsInt = c.Int(nullable: false),
                        Comments_AllValues = c.String(),
                        Title_AllValues = c.String(),
                        LogicalId = c.String(),
                        VersionInfo = c.String(),
                        ContentType_ID = c.Guid(),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsCodes", t => t.ContentType_ID)
                .Index(t => t.ContentType_ID);
            
            CreateTable(
                "dbo.XdsAuthors",
                c => new
                    {
                        ID = c.Guid(nullable: false),
                        Name = c.String(),
                        Institution_AllValues = c.String(),
                        Role_AllValues = c.String(),
                        Speciality_AllValues = c.String(),
                        XdsSubmissionSet_UUID = c.String(maxLength: 128),
                        XdsDocument_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.XdsSubmissionSets", t => t.XdsSubmissionSet_UUID)
                .ForeignKey("dbo.XdsDocuments", t => t.XdsDocument_UUID)
                .Index(t => t.XdsSubmissionSet_UUID)
                .Index(t => t.XdsDocument_UUID);
            
            CreateTable(
                "dbo.XdsDocuments",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        LegalAuthenticator = c.String(),
                        PatientID = c.String(),
                        SourcePatientDetails_Address = c.String(),
                        SourcePatientDetails_SingleName = c.String(),
                        SourcePatientDetails_GivenName = c.String(),
                        SourcePatientDetails_FamilyName = c.String(),
                        SourcePatientDetails_MiddleName = c.String(),
                        SourcePatientDetails_Prefix = c.String(),
                        SourcePatientDetails_Suffix = c.String(),
                        SourcePatientDetails_ID_Root = c.String(),
                        SourcePatientDetails_ID_Root_Name = c.String(),
                        SourcePatientDetails_ID_Extension = c.String(),
                        SourcePatientDetails_ID_Domain = c.String(),
                        SourcePatientDetails_ID_Type = c.String(),
                        SourcePatientDetails_CompositeId = c.String(),
                        SourcePatientDetails_DOB = c.DateTime(),
                        SourcePatientDetails_Sex = c.String(),
                        SourcePatientDetails_PhoneNumber = c.String(),
                        AlreadyExists = c.Boolean(nullable: false),
                        RepositoryUniqueId = c.String(),
                        MimeType = c.String(),
                        Title_AllValues = c.String(),
                        CreationTime = c.DateTime(nullable: false),
                        Data = c.Binary(),
                        ServiceStartTime = c.DateTime(nullable: false, storeType: "datetime2"),
                        ReferenceIdList_AllValues = c.String(),
                        ServiceStopTime = c.DateTime(nullable: false, storeType: "datetime2"),
                        UniqueID = c.String(),
                        LanguageCode = c.String(),
                        Hash = c.String(),
                        Size = c.Int(nullable: false),
                        AvailabilityStatusAsInt = c.Int(nullable: false),
                        Comments_AllValues = c.String(),
                        HomeCommunityID = c.String(),
                        Uri = c.String(),
                        LogicalId = c.String(),
                        VersionInfo = c.String(),
                        DocumentRelationship_UUID = c.String(maxLength: 128),
                        ClassCode_ID = c.Guid(),
                        TypeCode_ID = c.Guid(),
                        FormatCode_ID = c.Guid(),
                        HealthCareFacilityTypeCode_ID = c.Guid(),
                        PracticeSettingCode_ID = c.Guid(),
                        XdsSubmissionSet_UUID = c.String(maxLength: 128),
                        XdsFolder_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsRelationships", t => t.DocumentRelationship_UUID)
                .ForeignKey("dbo.XdsCodes", t => t.ClassCode_ID)
                .ForeignKey("dbo.XdsCodes", t => t.TypeCode_ID)
                .ForeignKey("dbo.XdsCodes", t => t.FormatCode_ID)
                .ForeignKey("dbo.XdsCodes", t => t.HealthCareFacilityTypeCode_ID)
                .ForeignKey("dbo.XdsCodes", t => t.PracticeSettingCode_ID)
                .ForeignKey("dbo.XdsSubmissionSets", t => t.XdsSubmissionSet_UUID)
                .ForeignKey("dbo.XdsFolders", t => t.XdsFolder_UUID)
                .Index(t => t.DocumentRelationship_UUID)
                .Index(t => t.ClassCode_ID)
                .Index(t => t.TypeCode_ID)
                .Index(t => t.FormatCode_ID)
                .Index(t => t.HealthCareFacilityTypeCode_ID)
                .Index(t => t.PracticeSettingCode_ID)
                .Index(t => t.XdsSubmissionSet_UUID)
                .Index(t => t.XdsFolder_UUID);
            
            CreateTable(
                "dbo.XdsRelationships",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        ParentUuid = c.String(),
                        Description = c.String(),
                        Reason_ID = c.Guid(),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsCodes", t => t.Reason_ID)
                .Index(t => t.Reason_ID);
            
            CreateTable(
                "dbo.XdsSlots",
                c => new
                    {
                        ID = c.Guid(nullable: false),
                        Name = c.String(),
                        AllValues = c.String(),
                        XdsDocument_UUID = c.String(maxLength: 128),
                        XdsFolder_UUID = c.String(maxLength: 128),
                        XdsAssociation_UUID = c.String(maxLength: 128),
                        XdsSubmissionSet_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.XdsDocuments", t => t.XdsDocument_UUID)
                .ForeignKey("dbo.XdsFolders", t => t.XdsFolder_UUID)
                .ForeignKey("dbo.XdsAssociations", t => t.XdsAssociation_UUID)
                .ForeignKey("dbo.XdsSubmissionSets", t => t.XdsSubmissionSet_UUID)
                .Index(t => t.XdsDocument_UUID)
                .Index(t => t.XdsFolder_UUID)
                .Index(t => t.XdsAssociation_UUID)
                .Index(t => t.XdsSubmissionSet_UUID);
            
            CreateTable(
                "dbo.XdsFolders",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        AlreadyExists = c.Boolean(nullable: false),
                        Title_AllValues = c.String(),
                        Comments_AllValues = c.String(),
                        UniqueID = c.String(),
                        PatientID = c.String(),
                        AvailabilityStatusAsInt = c.Int(nullable: false),
                        LastUpdateTime = c.DateTime(),
                        HomeCommunityID = c.String(),
                        XdsSubmissionSet_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsSubmissionSets", t => t.XdsSubmissionSet_UUID)
                .Index(t => t.XdsSubmissionSet_UUID);
            
            CreateTable(
                "dbo.XdsAssociations",
                c => new
                    {
                        UUID = c.String(nullable: false, maxLength: 128),
                        SourceUUID = c.String(),
                        TargetUUID = c.String(),
                        TypeAsInt = c.Int(nullable: false),
                        SubmissionSetStatus = c.String(),
                        XdsSubmissionSet_UUID = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.UUID)
                .ForeignKey("dbo.XdsSubmissionSets", t => t.XdsSubmissionSet_UUID)
                .Index(t => t.XdsSubmissionSet_UUID);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.XdsAssociations", new[] { "XdsSubmissionSet_UUID" });
            DropIndex("dbo.XdsFolders", new[] { "XdsSubmissionSet_UUID" });
            DropIndex("dbo.XdsSlots", new[] { "XdsSubmissionSet_UUID" });
            DropIndex("dbo.XdsSlots", new[] { "XdsAssociation_UUID" });
            DropIndex("dbo.XdsSlots", new[] { "XdsFolder_UUID" });
            DropIndex("dbo.XdsSlots", new[] { "XdsDocument_UUID" });
            DropIndex("dbo.XdsRelationships", new[] { "Reason_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "XdsFolder_UUID" });
            DropIndex("dbo.XdsDocuments", new[] { "XdsSubmissionSet_UUID" });
            DropIndex("dbo.XdsDocuments", new[] { "PracticeSettingCode_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "HealthCareFacilityTypeCode_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "FormatCode_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "TypeCode_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "ClassCode_ID" });
            DropIndex("dbo.XdsDocuments", new[] { "DocumentRelationship_UUID" });
            DropIndex("dbo.XdsAuthors", new[] { "XdsDocument_UUID" });
            DropIndex("dbo.XdsAuthors", new[] { "XdsSubmissionSet_UUID" });
            DropIndex("dbo.XdsSubmissionSets", new[] { "ContentType_ID" });
            DropIndex("dbo.XdsObjectRefs", new[] { "XdsQueryRequest_Id4" });
            DropIndex("dbo.XdsObjectRefs", new[] { "XdsQueryRequest_Id3" });
            DropIndex("dbo.XdsObjectRefs", new[] { "XdsQueryRequest_Id2" });
            DropIndex("dbo.XdsObjectRefs", new[] { "XdsQueryRequest_Id1" });
            DropIndex("dbo.XdsObjectRefs", new[] { "XdsQueryRequest_Id" });
            DropIndex("dbo.XdsCodes", new[] { "XdsFolder_UUID" });
            DropIndex("dbo.XdsCodes", new[] { "XdsDocument_UUID1" });
            DropIndex("dbo.XdsCodes", new[] { "XdsDocument_UUID" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id5" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id4" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id3" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id2" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id1" });
            DropIndex("dbo.XdsCodes", new[] { "XdsQueryRequest_Id" });
            DropIndex("dbo.XdsQueryRequests", new[] { "SubmissionSetUniqueId_UUID" });
            DropIndex("dbo.XdsQueryRequests", new[] { "SubmissionSetEntryUUID_UUID" });
            DropIndex("dbo.XdsSubscriptionRequests", new[] { "Query_Id" });
            DropIndex("dbo.MySubscriptions", new[] { "Query_Id" });
            DropForeignKey("dbo.XdsAssociations", "XdsSubmissionSet_UUID", "dbo.XdsSubmissionSets");
            DropForeignKey("dbo.XdsFolders", "XdsSubmissionSet_UUID", "dbo.XdsSubmissionSets");
            DropForeignKey("dbo.XdsSlots", "XdsSubmissionSet_UUID", "dbo.XdsSubmissionSets");
            DropForeignKey("dbo.XdsSlots", "XdsAssociation_UUID", "dbo.XdsAssociations");
            DropForeignKey("dbo.XdsSlots", "XdsFolder_UUID", "dbo.XdsFolders");
            DropForeignKey("dbo.XdsSlots", "XdsDocument_UUID", "dbo.XdsDocuments");
            DropForeignKey("dbo.XdsRelationships", "Reason_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "XdsFolder_UUID", "dbo.XdsFolders");
            DropForeignKey("dbo.XdsDocuments", "XdsSubmissionSet_UUID", "dbo.XdsSubmissionSets");
            DropForeignKey("dbo.XdsDocuments", "PracticeSettingCode_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "HealthCareFacilityTypeCode_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "FormatCode_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "TypeCode_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "ClassCode_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsDocuments", "DocumentRelationship_UUID", "dbo.XdsRelationships");
            DropForeignKey("dbo.XdsAuthors", "XdsDocument_UUID", "dbo.XdsDocuments");
            DropForeignKey("dbo.XdsAuthors", "XdsSubmissionSet_UUID", "dbo.XdsSubmissionSets");
            DropForeignKey("dbo.XdsSubmissionSets", "ContentType_ID", "dbo.XdsCodes");
            DropForeignKey("dbo.XdsObjectRefs", "XdsQueryRequest_Id4", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsObjectRefs", "XdsQueryRequest_Id3", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsObjectRefs", "XdsQueryRequest_Id2", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsObjectRefs", "XdsQueryRequest_Id1", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsObjectRefs", "XdsQueryRequest_Id", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsFolder_UUID", "dbo.XdsFolders");
            DropForeignKey("dbo.XdsCodes", "XdsDocument_UUID1", "dbo.XdsDocuments");
            DropForeignKey("dbo.XdsCodes", "XdsDocument_UUID", "dbo.XdsDocuments");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id5", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id4", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id3", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id2", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id1", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsCodes", "XdsQueryRequest_Id", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.XdsQueryRequests", "SubmissionSetUniqueId_UUID", "dbo.XdsObjectRefs");
            DropForeignKey("dbo.XdsQueryRequests", "SubmissionSetEntryUUID_UUID", "dbo.XdsObjectRefs");
            DropForeignKey("dbo.XdsSubscriptionRequests", "Query_Id", "dbo.XdsQueryRequests");
            DropForeignKey("dbo.MySubscriptions", "Query_Id", "dbo.XdsSubscriptionRequests");
            DropTable("dbo.XdsAssociations");
            DropTable("dbo.XdsFolders");
            DropTable("dbo.XdsSlots");
            DropTable("dbo.XdsRelationships");
            DropTable("dbo.XdsDocuments");
            DropTable("dbo.XdsAuthors");
            DropTable("dbo.XdsSubmissionSets");
            DropTable("dbo.Patients");
            DropTable("dbo.XdsObjectRefs");
            DropTable("dbo.XdsCodes");
            DropTable("dbo.XdsQueryRequests");
            DropTable("dbo.XdsSubscriptionRequests");
            DropTable("dbo.MySubscriptions");
        }
    }
}
