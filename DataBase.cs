using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using XdsObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XdsObjects.Dsub;

namespace XdsRegistry
{
    public class XdsDataBase : DbContext
    {
        public DbSet<MySubscriptions> MySubscriptions { get; set; }
        public DbSet<XdsCode> Codes { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<XdsSubmissionSet> SubmissionSets { get; set; }
        public DbSet<XdsDocument> Documents { get; set; }
        public DbSet<XdsFolder> Folders { get; set; }
        public DbSet<XdsAssociation> Associations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<XdsObjectRef>().HasKey(t => t.UUID);
            modelBuilder.Entity<XdsQueryRequest>().Ignore(t => t.DocumentEntryAuthorPerson);
            modelBuilder.Entity<XdsQueryRequest>().Ignore(t => t.SubmissionSetSourceId);
            modelBuilder.Entity<XdsQueryRequest>().Ignore(t => t.DocumentEntryReferenceIdList);

            modelBuilder.Entity<XdsCode>().HasKey(t => t.ID);

            modelBuilder.Entity<XdsSubmissionSet>().HasKey(t => t.UUID);
            modelBuilder.Entity<XdsSubmissionSet>().HasMany(t => t.Documents);
            modelBuilder.Entity<XdsSubmissionSet>().HasMany(t => t.Associations);
            modelBuilder.Entity<XdsSubmissionSet>().HasMany(t => t.Folders);
            modelBuilder.Entity<XdsSubmissionSet>().Ignore(t => t.PatientInfo);

            modelBuilder.Entity<XdsDocument>().HasKey(t => t.UUID);
            modelBuilder.Entity<XdsDocument>().HasOptional(t => t.DocumentRelationship);
            modelBuilder.Entity<XdsDocument>().Property(t => t.ServiceStartTime).HasColumnType("datetime2");
            modelBuilder.Entity<XdsDocument>().Property(t => t.ServiceStopTime).HasColumnType("datetime2");
            modelBuilder.Entity<XdsDocument>().Ignore(t => t.PatientInfo);

            modelBuilder.ComplexType<XdsPatient>();

            modelBuilder.Entity<XdsAssociation>().HasKey(t => t.UUID);
                        
            modelBuilder.Entity<XdsFolder>().HasKey(t => t.UUID);
            modelBuilder.Entity<XdsFolder>().Ignore(t => t.PatientInfo);
            
            modelBuilder.Entity<XdsAuthor>().HasKey(t => t.ID);

            modelBuilder.Entity<XdsRelationship>().HasKey(t => t.UUID);

            modelBuilder.Entity<MySubscriptions>().HasKey(t => t.Id);

            modelBuilder.Entity<MySubscriptions>().Property(t => t.TerminationTime).HasColumnType("datetime2");
        }
    }

    public class Patient
    {
        public string PatientId { get; set; }

        public Patient(string id)
        {
            PatientId = id;
        }

    }

    public class MySubscriptions
    {
        public Guid Id { get; set; }
        public DateTime TerminationTime { get; set; }
        public String patientId { get; set; }
        public string ConsumerReferenceAddress { get; set; }
        public XdsSubscriptionRequest Query { get; set; }
        public string TopicDialect { get; set; }
        public string CancerType { get; set; }
    }

    //public class ContextInitializer : DropCreateDatabaseAlways<XdsDataBase>
    public class ContextInitializer : CreateDatabaseIfNotExists<XdsDataBase>
    {
        protected override void Seed(XdsDataBase context)
        {
            //context.Patients.Add(new Patient("e6b7e7213b14472^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            //context.Patients.Add(new Patient("736bce17003b4b2^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            //context.Patients.Add(new Patient("439e4498e02342c^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            //context.Patients.Add(new Patient("ea51ddf8a7be495^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            //context.Patients.Add(new Patient("18889^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            context.Patients.Add(new Patient("18887^^^&1.3.6.1.4.1.21367.2005.3.7&ISO"));
            //EA51DDF8A7BE495
            context.SaveChanges();
        }
    }

}
