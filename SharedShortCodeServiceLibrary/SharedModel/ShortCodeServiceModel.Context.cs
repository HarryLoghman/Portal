﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharedShortCodeServiceLibrary.SharedModel
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public partial class ShortCodeServiceEntities : DbContext
    {
        public ShortCodeServiceEntities(string connectionStringNameInAppConfig)
            : base("name=" + connectionStringNameInAppConfig)
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
        public virtual DbSet<InstallmentCycle> InstallmentCycles { get; set; }
        public virtual DbSet<MessagesArchive> MessagesArchives { get; set; }
        public virtual DbSet<MessagesMonitoring> MessagesMonitorings { get; set; }
        public virtual DbSet<OnDemandMessagesBuffer> OnDemandMessagesBuffers { get; set; }
        public virtual DbSet<Otp> Otps { get; set; }
        public virtual DbSet<PointsTable> PointsTables { get; set; }
        public virtual DbSet<ServiceOffReason> ServiceOffReasons { get; set; }
        public virtual DbSet<ServicesRealtimeStatistic> ServicesRealtimeStatistics { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<Singlecharge> Singlecharges { get; set; }
        public virtual DbSet<SinglechargeArchive> SinglechargeArchives { get; set; }
        public virtual DbSet<SinglechargeInstallment> SinglechargeInstallments { get; set; }
        public virtual DbSet<SinglechargeInstallmentArchive> SinglechargeInstallmentArchives { get; set; }
        public virtual DbSet<SingleChargeTiming> SingleChargeTimings { get; set; }
        public virtual DbSet<SinglechargeWaiting> SinglechargeWaitings { get; set; }
        public virtual DbSet<SubscribersAdditionalInfo> SubscribersAdditionalInfoes { get; set; }
        public virtual DbSet<TimedTempMessagesBuffer> TimedTempMessagesBuffers { get; set; }
        public virtual DbSet<MessagesTemplate> MessagesTemplates { get; set; }
        public virtual DbSet<ServiceCommand> ServiceCommands { get; set; }
    }
}
