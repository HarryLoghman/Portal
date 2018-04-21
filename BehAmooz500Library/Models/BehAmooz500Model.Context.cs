﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BehAmooz500Library.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class BehAmooz500Entities : DbContext
    {
        public BehAmooz500Entities()
            : base("name=BehAmooz500Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<AutochargeContent> AutochargeContents { get; set; }
        public virtual DbSet<AutochargeContentsSendedToUser> AutochargeContentsSendedToUsers { get; set; }
        public virtual DbSet<AutochargeHeaderFooter> AutochargeHeaderFooters { get; set; }
        public virtual DbSet<AutochargeMessagesBuffer> AutochargeMessagesBuffers { get; set; }
        public virtual DbSet<AutochargeTimeTable> AutochargeTimeTables { get; set; }
        public virtual DbSet<DailyStatistic> DailyStatistics { get; set; }
        public virtual DbSet<EventbaseContent> EventbaseContents { get; set; }
        public virtual DbSet<EventbaseMessagesBuffer> EventbaseMessagesBuffers { get; set; }
        public virtual DbSet<ImiChargeCode> ImiChargeCodes { get; set; }
        public virtual DbSet<MessagesArchive> MessagesArchives { get; set; }
        public virtual DbSet<MessagesMonitoring> MessagesMonitorings { get; set; }
        public virtual DbSet<MessagesTemplate> MessagesTemplates { get; set; }
        public virtual DbSet<OnDemandMessagesBuffer> OnDemandMessagesBuffers { get; set; }
        public virtual DbSet<PointsTable> PointsTables { get; set; }
        public virtual DbSet<ServiceOffReason> ServiceOffReasons { get; set; }
        public virtual DbSet<ServicesRealtimeStatistic> ServicesRealtimeStatistics { get; set; }
        public virtual DbSet<Singlecharge> Singlecharges { get; set; }
        public virtual DbSet<SinglechargeArchive> SinglechargeArchives { get; set; }
        public virtual DbSet<SinglechargeInstallment> SinglechargeInstallments { get; set; }
        public virtual DbSet<SinglechargeInstallmentArchive> SinglechargeInstallmentArchives { get; set; }
        public virtual DbSet<SinglechargeLiveStatu> SinglechargeLiveStatus { get; set; }
        public virtual DbSet<SinglechargeWaiting> SinglechargeWaitings { get; set; }
        public virtual DbSet<SubscribersAdditionalInfo> SubscribersAdditionalInfoes { get; set; }
        public virtual DbSet<TimedTempMessagesBuffer> TimedTempMessagesBuffers { get; set; }
        public virtual DbSet<vw_SentMessages> vw_SentMessages { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<vw_Singlecharge> vw_Singlecharge { get; set; }
        public virtual DbSet<InstallmentCycle> InstallmentCycles { get; set; }
        public virtual DbSet<Otp> Otps { get; set; }
    
        public virtual int AggregateDailyStatistics(Nullable<System.DateTime> miladiDate, string serviceCode)
        {
            var miladiDateParameter = miladiDate.HasValue ?
                new ObjectParameter("MiladiDate", miladiDate) :
                new ObjectParameter("MiladiDate", typeof(System.DateTime));
    
            var serviceCodeParameter = serviceCode != null ?
                new ObjectParameter("ServiceCode", serviceCode) :
                new ObjectParameter("ServiceCode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("AggregateDailyStatistics", miladiDateParameter, serviceCodeParameter);
        }
    
        public virtual int ArchiveMessages()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ArchiveMessages");
        }
    
        public virtual int ChangeMessageStatus(Nullable<int> messageType, Nullable<long> contentId, Nullable<int> tag, string persianDate, Nullable<int> currentStatus, Nullable<int> desiredProcessStatus, Nullable<long> monitoringId)
        {
            var messageTypeParameter = messageType.HasValue ?
                new ObjectParameter("MessageType", messageType) :
                new ObjectParameter("MessageType", typeof(int));
    
            var contentIdParameter = contentId.HasValue ?
                new ObjectParameter("ContentId", contentId) :
                new ObjectParameter("ContentId", typeof(long));
    
            var tagParameter = tag.HasValue ?
                new ObjectParameter("Tag", tag) :
                new ObjectParameter("Tag", typeof(int));
    
            var persianDateParameter = persianDate != null ?
                new ObjectParameter("PersianDate", persianDate) :
                new ObjectParameter("PersianDate", typeof(string));
    
            var currentStatusParameter = currentStatus.HasValue ?
                new ObjectParameter("CurrentStatus", currentStatus) :
                new ObjectParameter("CurrentStatus", typeof(int));
    
            var desiredProcessStatusParameter = desiredProcessStatus.HasValue ?
                new ObjectParameter("DesiredProcessStatus", desiredProcessStatus) :
                new ObjectParameter("DesiredProcessStatus", typeof(int));
    
            var monitoringIdParameter = monitoringId.HasValue ?
                new ObjectParameter("MonitoringId", monitoringId) :
                new ObjectParameter("MonitoringId", typeof(long));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ChangeMessageStatus", messageTypeParameter, contentIdParameter, tagParameter, persianDateParameter, currentStatusParameter, desiredProcessStatusParameter, monitoringIdParameter);
        }
    
        public virtual int RetryUndeliveredMessages()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("RetryUndeliveredMessages");
        }
    
        public virtual ObjectResult<string> SinglechargeLiveStatuses(string serviceCode)
        {
            var serviceCodeParameter = serviceCode != null ?
                new ObjectParameter("ServiceCode", serviceCode) :
                new ObjectParameter("ServiceCode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<string>("SinglechargeLiveStatuses", serviceCodeParameter);
        }
    }
}
