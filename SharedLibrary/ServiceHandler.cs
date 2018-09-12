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

        public static Dictionary<string, string> GetAdditionalServiceInfoForSendingMessage(long serviceId, string aggregatorName)
        {
            var sendInfoDic = new Dictionary<string, string>();
            using (var entity = new PortalEntities())
            {
                var aggregatorInfo = entity.Aggregators.FirstOrDefault(o => o.AggregatorName == aggregatorName);
                sendInfoDic["username"] = aggregatorInfo.AggregatorUsername;
                sendInfoDic["password"] = aggregatorInfo.AggregatorPassword;
                var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorId == aggregatorInfo.Id && o.ServiceId == serviceId);
                sendInfoDic["shortCode"] = serviceInfo.ShortCode;
                sendInfoDic["serviceId"] = serviceId.ToString();
                sendInfoDic["aggregatorId"] = serviceInfo.AggregatorId.ToString();
                sendInfoDic["aggregatorServiceId"] = serviceInfo.AggregatorServiceId.ToString();
            }
            return sendInfoDic;
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

        public static bool CheckIfUserSendsSubscriptionKeyword(string content, Service service)
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

        public static Service GetServiceFromServiceCode(string serviceCode)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.Services.FirstOrDefault(o => o.ServiceCode == serviceCode);
                if (service != null)
                    return service;
                else
                    return null;
            }
        }

        public static Service GetServiceFromServiceId(long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
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
            public int priceChargedToday { get; set; }
            public int priceChargedYesterday { get; set; }
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

        public static dynamic ChargeMtnSubscriber(
           DateTime timeStartProcessMtnInstallment, DateTime timeAfterEntity, DateTime timeAfterWhere,
           dynamic entity, dynamic singlecharge, dynamic timingTable, ThrottleMTN v_throttle, string serviceName
            , MessageObject message, bool isRefund, bool isInAppPurchase
           , string aggregatorServiceId, int installmentCycleNumber, int loopNo, int threadNumber
           , DateTime timeLoop, long installmentId = 0)
        {

            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeAfterXML = null;
            Nullable<DateTime> timeBeforeHTTPClient = null;
            Nullable<DateTime> timeBeforeSendMTNClient = null;
            Nullable<DateTime> timeAfterSendMTNClient = null;
            Nullable<DateTime> timeBeforeReadStringClient = null;
            Nullable<DateTime> timeAfterReadStringClient = null;
            string guidStr = Guid.NewGuid().ToString();
            bool internalServerError;
            WebExceptionStatus status;
            string httpResult;

            var startTime = DateTime.Now;
            string charge = "";
            var spId = "980110006379";
            var mobile = "98" + message.MobileNumber.TrimStart('0');
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            int rialedPrice = message.Price.Value * 10;
            var referenceCode = Guid.NewGuid().ToString();

            if (isRefund == true)
                charge = "refundAmount";
            else
                charge = "chargeAmount";

            var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
, timeStamp, mobile, rialedPrice, referenceCode, charge, aggregatorServiceId, spId);
            try
            {
                //timeBeforeHTTPClient = DateTime.Now;

                DateTime timeLimitToSend = DateTime.Now.AddSeconds(-1);

                while (DateTime.Now > timeLimitToSend)
                {
                    timeLimitToSend = v_throttle.throttleRequests(serviceName, mobile, guidStr);

                    payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
                    , timeStamp, mobile, rialedPrice, referenceCode, charge, aggregatorServiceId, spId);
                    if (DateTime.Now > timeLimitToSend)
                        logs.Info("TimePassed:" + DateTime.Now.ToString("HH:mm:ss,fff") + "-" + timeLimitToSend.ToString("HH:mm:ss,fff"));
                }

                timeBeforeSendMTNClient = DateTime.Now;

                httpResult = SharedLibrary.UsefulWebApis.sendPostWithWebRequest(url, payload, out internalServerError, out status);
                timeAfterSendMTNClient = DateTime.Now;
                singlecharge.MobileNumber = message.MobileNumber;
                singlecharge.ThreadNumber = threadNumber;
                singlecharge.CycleNumber = installmentCycleNumber;
                singlecharge.ReferenceId = referenceCode;

                if (status == WebExceptionStatus.Success || internalServerError)
                {

                    timeBeforeReadStringClient = DateTime.Now;
                    //string httpResult = response.Content.ReadAsStringAsync().Result;
                    timeAfterReadStringClient = DateTime.Now;

                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(httpResult);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local");
                    XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns1:chargeAmountResponse", manager);
                    if (successNode != null)
                    {
                        singlecharge.IsSucceeded = true;
                    }
                    else
                    {
                        singlecharge.IsSucceeded = false;

                        manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                        XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                        foreach (XmlNode fault in faultNode)
                        {
                            XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                            XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                            singlecharge.Description = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                        }
                    }
                }
                else
                {
                    singlecharge.IsSucceeded = false;
                    singlecharge.Description = status.ToString();
                }
                timeAfterXML = DateTime.Now;


            }
            catch (Exception e)
            {
                logs.Info(payload);
                logs.Error("Exception in ChargeMtnSubscriber: " + e);
            }
            try
            {
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "IsSucceeded") != true)
                    singlecharge.IsSucceeded = false;
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                if (isRefund == true)
                    singlecharge.Price = message.Price.GetValueOrDefault() * -1;
                else
                    singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                if (installmentId != 0)
                    singlecharge.InstallmentId = installmentId;

                singlecharge.IsCalledFromInAppPurchase = isInAppPurchase;

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                singlecharge.ProcessTimeInMilliSecond = (int)duration.TotalMilliseconds;
                entity.Singlecharges.Add(singlecharge);

                timingTable.cycleNumber = installmentCycleNumber;
                timingTable.loopNo = loopNo;
                timingTable.threadNumber = threadNumber;
                timingTable.mobileNumber = message.MobileNumber;
                timingTable.guid = guidStr;
                timingTable.timeAfterReadStringClient = timeAfterReadStringClient;
                timingTable.timeAfterSendMTNClient = timeAfterSendMTNClient;
                timingTable.timeAfterXML = timeAfterXML;
                timingTable.timeBeforeHTTPClient = timeBeforeHTTPClient;
                timingTable.timeBeforeReadStringClient = timeBeforeReadStringClient;
                timingTable.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                timingTable.timeCreate = timeLoop;
                timingTable.timeFinish = singlecharge.DateCreated;
                timingTable.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                timingTable.timeStartProcessMtnInstallment = timeStartProcessMtnInstallment;
                timingTable.timeAfterWhere = timeAfterWhere;
                timingTable.timeAfterEntity = timeAfterEntity;
                entity.SingleChargeTimings.Add(timingTable);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeMtnSubscriber on saving values to db: " + e);
            }
            return singlecharge;
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

        public static List<Subscriber> GetServiceActiveSubscribersFromServiceId(long serviceId)
        {
            List<Subscriber> subscriberList = new List<Subscriber>();
            using (var entity = new PortalEntities())
            {
                try
                {
                    subscriberList = entity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceActiveSubscribersFromServiceId: " + e);
                }
                return subscriberList;
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

        public static bool CheckIfUserSendedUnsubscribeContentToShortCode(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content == offKeyword)
                    return true;
            }
            return false;
        }

        public static List<Service> GetServicesThatUserSubscribedOnShortCode(string mobileNumber, string shortCode)
        {
            List<Service> servicesThatUserSubscribedOnShortCode = new List<Service>();
            using (var entity = new PortalEntities())
            {
                try
                {
                    servicesThatUserSubscribedOnShortCode = (from s in entity.Services
                                                             join sub in entity.Subscribers on s.Id equals sub.ServiceId
                                                             join sInfo in entity.ServiceInfoes on s.Id equals sInfo.ServiceId
                                                             where sub.MobileNumber == mobileNumber && sub.DeactivationDate == null && sInfo.ShortCode == shortCode
                                                             select new { ServiceId = s.Id, ServiceOnKeywords = s.OnKeywords, ServiceName = s.Name })
                                                   .AsEnumerable()
                                                   .Select(o => new Service { Id = o.ServiceId, Name = o.ServiceName, OnKeywords = o.ServiceOnKeywords })
                                                   .ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServicesThatUserSubscriberd: " + e);
                }
                return servicesThatUserSubscribedOnShortCode;
            }
        }

        public static dynamic GetServiceImiChargeCodes(dynamic entity)
        {
            try
            {
                return ((IEnumerable<dynamic>)entity.ImiChargeCodes).ToList();
            }
            catch (System.Exception e)
            {
                logs.Error("Error in GetServiceImiChargeCodes: " + e);
            }
            return new List<dynamic>();
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

        public static string GetSubscriberOnKeyword(long subscriberId)
        {
            using (var entity = new PortalEntities())
            {
                try
                {
                    var onKeyword = entity.Subscribers.FirstOrDefault(o => o.Id == subscriberId).OnKeyword;
                    return onKeyword;
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceInfoFromServiceId: " + e);
                    return null;
                }
            }
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
    }
}