using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using System.Collections;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.Xml;

namespace SharedLibrary
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static Dictionary<string, string> GetAdditionalServiceInfoForSendingMessage(string serviceCode, string aggregatorName)
        {
            var sendInfoDic = new Dictionary<string, string>();
            using (var entity = new PortalEntities())
            {
                var aggregatorInfo = entity.Aggregators.FirstOrDefault(o => o.AggregatorName == aggregatorName);
                sendInfoDic["username"] = aggregatorInfo.AggregatorUsername;
                sendInfoDic["password"] = aggregatorInfo.AggregatorPassword;
                //sendInfoDic["aggregatorName"] = aggregatorInfo.AggregatorName;
                var service = entity.Services.FirstOrDefault(o => o.ServiceCode == serviceCode);
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorId == aggregatorInfo.Id && o.ServiceId == service.Id);
                sendInfoDic["shortCode"] = serviceInfo.ShortCode;
                sendInfoDic["serviceName"] = service.Name;
                sendInfoDic["serviceCode"] = service.ServiceCode;
                sendInfoDic["serviceId"] = service.Id.ToString();
                sendInfoDic["aggregatorId"] = serviceInfo.AggregatorId.ToString();
                sendInfoDic["aggregatorServiceId"] = serviceInfo.AggregatorServiceId.ToString();
                sendInfoDic["OperatorServiceId"] = (serviceInfo.OperatorServiceId == null) ? null : serviceInfo.OperatorServiceId;
            }
            return sendInfoDic;
        }

        public static List<Models.ParidsShortCode> GetPardisShortcodesFromServiceId(long serviceId)
        {
            using (var portalEntity = new PortalEntities())
            {
                return portalEntity.ParidsShortCodes.Where(o => o.ServiceId == serviceId).ToList();
            }
        }

        public static ServiceInfo GetServiceInfoFromAggregatorServiceId(string aggregatorServiceId)
        {
            using (var entity = new PortalEntities())
            {
                aggregatorServiceId = aggregatorServiceId.ToLower();
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorServiceId == aggregatorServiceId);
                return serviceInfo;
            }
        }

        public static ServiceInfo GetServiceInfoFromOperatorServiceId(string operatorServiceId)
        {
            using (var entity = new PortalEntities())
            {
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.OperatorServiceId == operatorServiceId);
                return serviceInfo;
            }
        }


        public static long GetAggregatorIdFromAggregatorName(string aggregatorName)
        {
            using (var entity = new PortalEntities())
            {
                var aggregatorId = entity.Aggregators.FirstOrDefault(o => o.AggregatorName == aggregatorName).Id;
                return aggregatorId;
            }
        }

        public static long? GetServiceId(string serviceCode)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.Services.FirstOrDefault(o => o.ServiceCode == serviceCode);
                if (service != null)
                    return service.Id;
                else
                    return null;
            }
        }

        public static bool CheckIfUserSendsSubscriptionKeyword(string content, vw_servicesServicesInfo service)
        {
            var serviceKeywords = service.OnKeywords.Split(',');
            foreach (var keyword in serviceKeywords)
            {
                if (content == keyword || content == keyword)
                    return true;
            }
            return false;
        }

        public static bool CheckIfUserWantsToUnsubscribe(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content.ToLower().Contains(offKeyword.ToLower()))
                    return true;
            }
            return false;
        }

        public static vw_servicesServicesInfo GetServiceFromServiceCode(string serviceCode)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.vw_servicesServicesInfo.FirstOrDefault(o => o.ServiceCode == serviceCode);
                if (service != null)
                    return service;
                else
                    return null;
            }
        }

        public static vw_servicesServicesInfo GetServiceFromServiceId(long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == serviceId);
                if (service != null)
                    return service;
                else
                    return null;
            }
        }

        public static ServiceInfo GetServiceInfoFromShortCode(string shortCode)
        {
            using (var entity = new PortalEntities())
            {
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == shortCode);
                if (serviceInfo != null)
                    return serviceInfo;
                else
                    return null;
            }
        }

        public static List<Subscriber> GetServiceActiveSubscribersFromServiceCode(string serviceCode)
        {
            List<Subscriber> subscriberList = new List<Subscriber>();
            using (var entity = new PortalEntities())
            {
                try
                {
                    var serviceId = entity.Services.FirstOrDefault(o => o.ServiceCode == serviceCode).Id;
                    subscriberList = entity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceActiveSubscribersFromServiceCode: " + e);
                }
                return subscriberList;
            }
        }

        public static List<string> GetServiceActiveMobileNumbersFromServiceCode(string serviceCode)
        {
            List<string> mobileNumbersList = new List<string>();
            using (var entity = new PortalEntities())
            {
                try
                {
                    var serviceId = entity.Services.AsNoTracking().FirstOrDefault(o => o.ServiceCode == serviceCode).Id;
                    mobileNumbersList = entity.Subscribers.AsNoTracking().Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).Select(o => o.MobileNumber).ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceActiveMobileNumbersFromServiceCode: " + e);
                }
                return mobileNumbersList;
            }
        }

        public class SubscribersAndCharges
        {
            public string mobileNumber { get; set; }
            public int? pricePaidToday { get; set; }
            public int? pricePaidYesterday { get; set; }
        }
        public static int GetActiveSubscribersAndChargesCount(string dbName, string serviceCode, int maxTries, int cycleNumber, int maxPrice
            , DateTime date, bool wipe, string wipeDescriptionSeparatedBySemicolon, Nullable<DateTime> lastExecutionTime, bool forciblyExecute)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.Services.AsNoTracking().Where(o => o.ServiceCode == serviceCode).FirstOrDefault();
                if (service == null) return 0;
                return GetActiveSubscribersAndChargesCount(dbName, service.Id, maxTries, cycleNumber, maxPrice, date, wipe, wipeDescriptionSeparatedBySemicolon
                    , lastExecutionTime, forciblyExecute);

            }
        }
        public static int GetActiveSubscribersAndChargesCount(string dbName, long serviceId, int maxTries, int cycleNumber, int maxPrice
            , DateTime date, bool wipe, string wipeDescriptionSeparatedBySemicolon, Nullable<DateTime> lastExecutionTime, bool forciblyExecute)
        {

            string yesterday = (DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");
            string str = "";
            /*'select count(*) '
            + ' from Subscribers s with(nolock) '
            + ' where '
            + ' (select count(*) from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber) < ' + cast(@maxTries as varchar(10))
            + ' and not exists(select 1 from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber and CycleNumber = ' + cast(@cycleNumber as varchar(10)) + ' )'
            + ' and isnull((select sum(Price) from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber and IsSucceeded = 1 ),0) < ' + cast(@maxPrice as varchar(100))
            + ' and mobileNumber not in (select mobileNumber from ' + @dbName + '.dbo.SinglechargeWaiting with(nolock))'
			+ ' and DeactivationDate is null and ServiceId = ' + cast(@serviceId as varchar(100))
			+ ' and activationDate is not null and dateAdd(minute,10,activationDate) <getdate()'*/


            str = "exec sp_getActiveSubscribersAndCharges " + dbName + "," + serviceId.ToString() + ","
                + maxTries.ToString() + "," + cycleNumber.ToString() + "," + maxPrice.ToString() + "," + "0"
                + ",Null"
                + "," + "'" + date.ToString("yyyy-MM-dd") + "'"
                + "," + (wipe ? "1" : "0")
                + "," + (string.IsNullOrEmpty(wipeDescriptionSeparatedBySemicolon) ? "Null" : "'" + wipeDescriptionSeparatedBySemicolon + "'")
                + ",1"
                + "," + (lastExecutionTime.HasValue ? "'" + lastExecutionTime.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" : "Null")
                + "," + (forciblyExecute ? "1" : "0");

            List<int> arr = null;
            using (PortalEntities portal = new PortalEntities())
            {
                portal.Database.CommandTimeout = 180;
                arr = portal.Database.SqlQuery<int>(str).ToList();
            }
            return arr[0];
        }
        public static List<SubscribersAndCharges> getActiveSubscribersForCharge(string dbName, long serviceId, DateTime date, int maxPrice, int maxTries, int cycleNumber, bool order
            , bool wipe, string wipeDescriptionSeparatedBySemicolon, Nullable<int> offset, Nullable<int> fetch)
        {

            string yesterday = (DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");
            string str = "";

            str = "exec sp_getActiveSubscribersAndChargesNew "
                + " '" + dbName + "'"
                + "," + serviceId.ToString()
                + "," + "'" + date.ToString("yyyy-MM-dd") + "'"
                + "," + maxPrice.ToString()
                + "," + maxTries.ToString()
                + "," + cycleNumber.ToString()
                + "," + (order ? "1" : "0")
                + "," + (wipe ? "1" : "0")
                + "," + (string.IsNullOrEmpty(wipeDescriptionSeparatedBySemicolon) ? "Null" : "'" + wipeDescriptionSeparatedBySemicolon + "'")
                + "," + (offset.HasValue ? offset.Value.ToString() : "Null")
                + "," + (fetch.HasValue ? fetch.Value.ToString() : "Null");

            List<SubscribersAndCharges> arr = null;
            using (PortalEntities portal = new PortalEntities())
            {
                portal.Database.CommandTimeout = 300;
                arr = portal.Database.SqlQuery<SubscribersAndCharges>(str).ToList();
            }
            return arr;
        }
        public static List<SubscribersAndCharges> GetActiveSubscribersAndCharges(string dbName, string serviceCode, int maxTries, int cycleNumber, int maxPrice, bool orderByChargeYesterday
            , Nullable<int> topRecord, DateTime date, bool wipe, string wipeDescriptionSeparatedBySemicolon, Nullable<DateTime> lastExecutionTime
            , bool forciblyExecute)
        {
            using (var entity = new PortalEntities())
            {

                var service = entity.Services.AsNoTracking().Where(o => o.ServiceCode == serviceCode).FirstOrDefault();
                if (service == null) return null;
                return GetActiveSubscribersAndCharges(dbName, service.Id, maxTries, cycleNumber, maxPrice, orderByChargeYesterday, topRecord
                    , date, wipe, wipeDescriptionSeparatedBySemicolon, lastExecutionTime, forciblyExecute);

            }
        }
        public static List<SubscribersAndCharges> GetActiveSubscribersAndCharges(string dbName, long serviceId, int maxTries, int cycleNumber, int maxPrice
            , bool orderByChargeYesterday, Nullable<int> topRecord, DateTime date, bool wipe, string wipeDescriptionSeparatedBySemicolon
            , Nullable<DateTime> lastExecutionTime, bool forciblyExecute)
        {

            string yesterday = (DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");
            string str = "";
            /*'select ' + (case when @topRecord is null then '' else ' top ' +cast(@topRecord as varchar(100)) end)
			+ '  * from '
            + '(select MobileNumber '
            + ', isnull((select sum(price) from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber and IsSucceeded = 1),0) pricePaiedToday '
            + ', isnull((select sum(price) from ' + @dbName + '.dbo.SinglechargeArchive with(nolock) where MobileNumber = s.MobileNumber and IsSucceeded = 1 and convert(date, datecreated) = ''' + cast(@yesterday as varchar(10))+ '''),0) pricePaiedYesterday '
            + ' from Subscribers s with(nolock) '
            + ' where '
            + ' (select count(*) from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber) < ' + cast(@maxTries as varchar(10))
            + ' and not exists(select 1 from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber and cycleNumber is not null and CycleNumber = ' + cast(@cycleNumber as varchar(10)) + ' )'
            + ' and isnull((select sum(Price) from ' + @dbName + '.dbo.Singlecharge with(nolock) where MobileNumber = s.MobileNumber and IsSucceeded = 1 ),0) < ' + cast(@maxPrice as varchar(100))
            + ' and mobileNumber not in (select mobileNumber from ' + @dbName + '.dbo.SinglechargeWaiting with(nolock))'
			+ ' and DeactivationDate is null and ServiceId = ' + cast(@serviceId as varchar(100))
			+ ' and activationDate is not null and dateAdd(minute,10,activationDate) <getdate()'
            + ' group by MobileNumber) t '
            + (case when isnull(@orderByChargeYesterday,0)=0 then '' else ' order by pricePaiedYesterday desc' end)*/


            str = "exec sp_getActiveSubscribersAndCharges " + dbName + "," + serviceId.ToString() + ","
                + maxTries.ToString() + "," + cycleNumber.ToString() + "," + maxPrice.ToString() + "," + (orderByChargeYesterday ? "1" : "0")
                + "," + (topRecord.HasValue ? topRecord.Value.ToString() : "Null")
                + "," + "'" + date.ToString("yyyy-MM-dd") + "'"
                + "," + (wipe ? "1" : "0")
                + "," + (string.IsNullOrEmpty(wipeDescriptionSeparatedBySemicolon) ? "Null" : "'" + wipeDescriptionSeparatedBySemicolon + "'")
                + ", 0"
                + "," + (lastExecutionTime.HasValue ? "'" + lastExecutionTime.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" : "Null")
                + "," + (forciblyExecute ? "1" : "0");


            List<SubscribersAndCharges> arr = null;
            using (PortalEntities portal = new PortalEntities())
            {

                portal.Database.CommandTimeout = 180;
                arr = portal.Database.SqlQuery<SubscribersAndCharges>(str).ToList();
            }
            return arr;
        }


        public static Models.ServiceModel.Singlecharge GetOTPRequestId(Models.ServiceModel.SharedServiceEntities entity, MessageObject message)
        {
            try
            {
                SharedLibrary.Models.ServiceModel.Singlecharge singlecharge;
                if (message.Price.HasValue)
                {
                    singlecharge = entity.Singlecharges.Where(o => o.MobileNumber == message.MobileNumber && o.Price == message.Price && o.Description == "SUCCESS-Pending Confirmation").OrderByDescending(o => o.DateCreated).FirstOrDefault();
                    if (singlecharge != null)
                        return singlecharge;
                    else
                        return null;
                }
                else
                {
                    singlecharge = entity.Singlecharges.Where(o => o.MobileNumber == message.MobileNumber && o.Description == "SUCCESS-Pending Confirmation").OrderByDescending(o => o.DateCreated).FirstOrDefault();
                    if (singlecharge != null)
                        return singlecharge;
                    else
                        return null;

                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetOTPRequestId: ", e);
            }
            return null;
        }

        public static int GetServiceActiveMobileNumbersCountFromServiceCode(string serviceCode)
        {
            int mobileNumbersCount = 0;
            using (var entity = new PortalEntities())
            {
                try
                {
                    var serviceId = entity.Services.AsNoTracking().FirstOrDefault(o => o.ServiceCode == serviceCode).Id;
                    mobileNumbersCount = entity.Subscribers.AsNoTracking().Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).Count();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceActiveMobileNumbersFromServiceCode: " + e);
                }
                return mobileNumbersCount;
            }
        }
        public static string GetShortCodeFromOperatorServiceId(string operatorServiceId)
        {
            using (var portalEntity = new PortalEntities())
            {
                return portalEntity.ServiceInfoes.Where(o => o.OperatorServiceId == operatorServiceId).FirstOrDefault().ShortCode;
            }
        }

        public static List<string> GetShortCodesFromAggregatorId(long aggregatorId)
        {
            using (var portalEntity = new PortalEntities())
            {
                return portalEntity.ServiceInfoes.Where(o => o.AggregatorId == aggregatorId).Select(o => o.ShortCode).ToList();
            }
        }

        
        public static bool IsUserVerifedTheSubscription(string mobileNumber, long serviceId, string keyword)
        {
            var result = false;
            using (var entity = new PortalEntities())
            {
                try
                {
                    var user = entity.VerifySubscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                    if (user == null)
                    {
                        var verify = new VerifySubscriber();
                        verify.MobileNumber = mobileNumber;
                        verify.ServiceId = serviceId;
                        verify.UsedKeyword = keyword;
                        entity.VerifySubscribers.Add(verify);
                    }
                    else if (user.UsedKeyword == keyword)
                        result = false;
                    else
                    {
                        entity.VerifySubscribers.Remove(user);
                        result = true;
                    }
                    entity.SaveChanges();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in IsUserVerifedTheSubscription: " + e);
                }
                return result;
            }
        }

        public static List<ServiceInfo> GetAllServicesByAggregatorId(long aggregatorId)
        {
            List<ServiceInfo> serviceInfoList = new List<ServiceInfo>();
            using (var entity = new PortalEntities())
            {
                try
                {
                    serviceInfoList = entity.ServiceInfoes.Where(o => o.AggregatorId == aggregatorId).ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetAllServicesByAggregatorId: " + e);
                }
                return serviceInfoList;
            }
        }

        public static string GetAggregatorNameFromServiceCode(string serviceCode)
        {
            string aggregatorName = ""; ;
            using (var entity = new PortalEntities())
            {
                try
                {
                    aggregatorName = entity.vw_servicesServicesInfo.Where(o => o.ServiceCode == serviceCode).Select(o => o.aggregatorName).FirstOrDefault();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetAllServicesByAggregatorId: " + e);
                }
                return aggregatorName;
            }
        }

        
        public static bool CheckIfUserSendedUnsubscribeContentToShortCode(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content == offKeyword)
                    return true;
            }
            return false;
        }

        public static JhoobinSetting GetJhoobinSettings()
        {
            JhoobinSetting settings = new JhoobinSetting();
            using (var entity = new PortalEntities())
            {
                try
                {
                    settings = entity.JhoobinSettings.FirstOrDefault();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetJhoobinSettings: " + e);
                }
                return settings;
            }
        }

        public static List<SharedLibrary.Models.ServiceModel.ImiChargeCode> GetServiceImiChargeCodes(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity)
        {
            try
            {
                return entity.ImiChargeCodes.ToList();
            }
            catch (System.Exception e)
            {
                logs.Error("Error in GetServiceImiChargeCodes: " + e);
            }
            return null;
        }

        public static ServiceInfo GetServiceInfoFromServiceId(long serviceId)
        {
            ServiceInfo serviceInfo;
            using (var entity = new PortalEntities())
            {
                try
                {
                    serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.ServiceId == serviceId);
                    return serviceInfo;
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceInfoFromServiceId: " + e);
                    return null;
                }
            }
        }

        public static string getFirstOnKeywordOfService(string serviceOnKeywords)
        {
            var onKeywords = serviceOnKeywords.Replace(" ", "").Split(',');
            return onKeywords[0];
        }

        public static string[] ServiceOffKeywords()
        {
            var offContents = new string[]
            {
                "Off",
                "خاموش",
            };
            return offContents;
        }

        public static List<Models.ServiceModel.MessagesTemplate> GetServiceMessagesTemplate(vw_servicesServicesInfo service)
        {
            using (var entity = new Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
            {
                return GetServiceMessagesTemplate(entity);
            }
        }
        public static List<Models.ServiceModel.MessagesTemplate> GetServiceMessagesTemplate(Models.ServiceModel.SharedServiceEntities entity)
        {
            return entity.MessagesTemplates.ToList();

        }
    }
}