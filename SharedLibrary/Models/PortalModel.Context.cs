﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharedLibrary.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class PortalEntities : DbContext
    {
        public PortalEntities()
            : base("name=PortalEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Aggregator> Aggregators { get; set; }
        public virtual DbSet<AspNetRole> AspNetRoles { get; set; }
        public virtual DbSet<AspNetRoleUserAccess> AspNetRoleUserAccesses { get; set; }
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual DbSet<AspNetUsersRolesServiceAccess> AspNetUsersRolesServiceAccesses { get; set; }
        public virtual DbSet<BlackList> BlackLists { get; set; }
        public virtual DbSet<Operator> Operators { get; set; }
        public virtual DbSet<OperatorsPlan> OperatorsPlans { get; set; }
        public virtual DbSet<OperatorsPrefix> OperatorsPrefixs { get; set; }
        public virtual DbSet<ParidsShortCode> ParidsShortCodes { get; set; }
        public virtual DbSet<Referral> Referrals { get; set; }
        public virtual DbSet<ServersAction> ServersActions { get; set; }
        public virtual DbSet<serviceCyclesNew> serviceCyclesNews { get; set; }
        public virtual DbSet<ServiceInfo> ServiceInfoes { get; set; }
        public virtual DbSet<ServiceKeyword> ServiceKeywords { get; set; }
        public virtual DbSet<Service> Services { get; set; }
        public virtual DbSet<SinglechargeDelivery> SinglechargeDeliveries { get; set; }
        public virtual DbSet<Subscriber> Subscribers { get; set; }
        public virtual DbSet<SubscribersPoint> SubscribersPoints { get; set; }
        public virtual DbSet<TempReferralData> TempReferralDatas { get; set; }
        public virtual DbSet<VerifySubscriber> VerifySubscribers { get; set; }
        public virtual DbSet<vw_servicesServicesInfo> vw_servicesServicesInfo { get; set; }
        public virtual DbSet<JhoobinSetting> JhoobinSettings { get; set; }
        public virtual DbSet<SubscribersHistory> SubscribersHistories { get; set; }
        public virtual DbSet<Delivery> Deliveries { get; set; }
        public virtual DbSet<BulkList> BulkLists { get; set; }
        public virtual DbSet<vw_AspNetRoles> vw_AspNetRoles { get; set; }
        public virtual DbSet<vw_AspNetUserRoles> vw_AspNetUserRoles { get; set; }
        public virtual DbSet<vw_AspNetUsers> vw_AspNetUsers { get; set; }
        public virtual DbSet<vw_DehnadAllServicesStatistics> vw_DehnadAllServicesStatistics { get; set; }
        public virtual DbSet<Audit> Audits { get; set; }
        public virtual DbSet<chargeInfo> chargeInfoes { get; set; }
        public virtual DbSet<RealtimeStatisticsFor2GServices> RealtimeStatisticsFor2GServices { get; set; }
        public virtual DbSet<RealtimeStatisticsFor3GServices> RealtimeStatisticsFor3GServices { get; set; }
        public virtual DbSet<RequestsLog> RequestsLogs { get; set; }
        public virtual DbSet<ReceievedMessage> ReceievedMessages { get; set; }
        public virtual DbSet<vw_ReceivedMessages> vw_ReceivedMessages { get; set; }
        public virtual DbSet<MCISingleChargeFtpFile> MCISingleChargeFtpFiles { get; set; }
        public virtual DbSet<App> Apps { get; set; }
        public virtual DbSet<MCIFtpLastState> MCIFtpLastStates { get; set; }
        public virtual DbSet<MobinOneFtp> MobinOneFtps { get; set; }
        public virtual DbSet<ServersIP> ServersIPs { get; set; }
        public virtual DbSet<Bulk> Bulks { get; set; }
    
        public virtual int ArchiveReceivedMessages()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ArchiveReceivedMessages");
        }
    
        public virtual int sp_DehnadAll2GServicesStatistics(string serviceCode)
        {
            var serviceCodeParameter = serviceCode != null ?
                new ObjectParameter("ServiceCode", serviceCode) :
                new ObjectParameter("ServiceCode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_DehnadAll2GServicesStatistics", serviceCodeParameter);
        }
    
        public virtual int sp_DehnadAll3GServicesStatistics(string serviceCode)
        {
            var serviceCodeParameter = serviceCode != null ?
                new ObjectParameter("ServiceCode", serviceCode) :
                new ObjectParameter("ServiceCode", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_DehnadAll3GServicesStatistics", serviceCodeParameter);
        }
    
        public virtual ObjectResult<GetUserLog_Result> GetUserLog(string userId, string mobileNumber)
        {
            var userIdParameter = userId != null ?
                new ObjectParameter("userId", userId) :
                new ObjectParameter("userId", typeof(string));
    
            var mobileNumberParameter = mobileNumber != null ?
                new ObjectParameter("MobileNumber", mobileNumber) :
                new ObjectParameter("MobileNumber", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetUserLog_Result>("GetUserLog", userIdParameter, mobileNumberParameter);
        }
    
        public virtual ObjectResult<getUserAvailableServices_Result> getUserAvailableServices(string userId)
        {
            var userIdParameter = userId != null ?
                new ObjectParameter("userId", userId) :
                new ObjectParameter("userId", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<getUserAvailableServices_Result>("getUserAvailableServices", userIdParameter);
        }
    
        public virtual ObjectResult<sp_getSubscriberServices_Result> sp_getSubscriberServices(string userId, string mobileNumber, Nullable<System.DateTime> chargePriceFrom)
        {
            var userIdParameter = userId != null ?
                new ObjectParameter("userId", userId) :
                new ObjectParameter("userId", typeof(string));
    
            var mobileNumberParameter = mobileNumber != null ?
                new ObjectParameter("mobileNumber", mobileNumber) :
                new ObjectParameter("mobileNumber", typeof(string));
    
            var chargePriceFromParameter = chargePriceFrom.HasValue ?
                new ObjectParameter("chargePriceFrom", chargePriceFrom) :
                new ObjectParameter("chargePriceFrom", typeof(System.DateTime));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<sp_getSubscriberServices_Result>("sp_getSubscriberServices", userIdParameter, mobileNumberParameter, chargePriceFromParameter);
        }
    
        public virtual int sp_RequestsRulesChecker(Nullable<long> requestId, Nullable<System.DateTime> regdate, string sourceIP, string requestParams, string destinationIP, string methodName, ObjectParameter action, ObjectParameter actionMessage)
        {
            var requestIdParameter = requestId.HasValue ?
                new ObjectParameter("requestId", requestId) :
                new ObjectParameter("requestId", typeof(long));
    
            var regdateParameter = regdate.HasValue ?
                new ObjectParameter("regdate", regdate) :
                new ObjectParameter("regdate", typeof(System.DateTime));
    
            var sourceIPParameter = sourceIP != null ?
                new ObjectParameter("sourceIP", sourceIP) :
                new ObjectParameter("sourceIP", typeof(string));
    
            var requestParamsParameter = requestParams != null ?
                new ObjectParameter("requestParams", requestParams) :
                new ObjectParameter("requestParams", typeof(string));
    
            var destinationIPParameter = destinationIP != null ?
                new ObjectParameter("destinationIP", destinationIP) :
                new ObjectParameter("destinationIP", typeof(string));
    
            var methodNameParameter = methodName != null ?
                new ObjectParameter("methodName", methodName) :
                new ObjectParameter("methodName", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("sp_RequestsRulesChecker", requestIdParameter, regdateParameter, sourceIPParameter, requestParamsParameter, destinationIPParameter, methodNameParameter, action, actionMessage);
        }
    
        public virtual ObjectResult<sp_MCIFtpLastState_getAsync_Result> sp_MCIFtpLastState_getAsync(Nullable<long> serviceId, Nullable<System.DateTime> datetimeNow, Nullable<int> syncFtpOldItemsInMins, Nullable<int> syncFtpWaitTimeInMins, Nullable<int> syncDBWaitTimeInMins, Nullable<int> syncChargedTriedNDaysBefore)
        {
            var serviceIdParameter = serviceId.HasValue ?
                new ObjectParameter("serviceId", serviceId) :
                new ObjectParameter("serviceId", typeof(long));
    
            var datetimeNowParameter = datetimeNow.HasValue ?
                new ObjectParameter("datetimeNow", datetimeNow) :
                new ObjectParameter("datetimeNow", typeof(System.DateTime));
    
            var syncFtpOldItemsInMinsParameter = syncFtpOldItemsInMins.HasValue ?
                new ObjectParameter("SyncFtpOldItemsInMins", syncFtpOldItemsInMins) :
                new ObjectParameter("SyncFtpOldItemsInMins", typeof(int));
    
            var syncFtpWaitTimeInMinsParameter = syncFtpWaitTimeInMins.HasValue ?
                new ObjectParameter("SyncFtpWaitTimeInMins", syncFtpWaitTimeInMins) :
                new ObjectParameter("SyncFtpWaitTimeInMins", typeof(int));
    
            var syncDBWaitTimeInMinsParameter = syncDBWaitTimeInMins.HasValue ?
                new ObjectParameter("SyncDBWaitTimeInMins", syncDBWaitTimeInMins) :
                new ObjectParameter("SyncDBWaitTimeInMins", typeof(int));
    
            var syncChargedTriedNDaysBeforeParameter = syncChargedTriedNDaysBefore.HasValue ?
                new ObjectParameter("SyncChargedTriedNDaysBefore", syncChargedTriedNDaysBefore) :
                new ObjectParameter("SyncChargedTriedNDaysBefore", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<sp_MCIFtpLastState_getAsync_Result>("sp_MCIFtpLastState_getAsync", serviceIdParameter, datetimeNowParameter, syncFtpOldItemsInMinsParameter, syncFtpWaitTimeInMinsParameter, syncDBWaitTimeInMinsParameter, syncChargedTriedNDaysBeforeParameter);
        }
    }
}
