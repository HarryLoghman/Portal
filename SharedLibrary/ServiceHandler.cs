using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using System.Collections;

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
                    mobileNumbersList = entity.Subscribers.AsNoTracking().Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).Select(o=> o.MobileNumber).ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServiceActiveMobileNumbersFromServiceCode: " + e);
                }
                return mobileNumbersList;
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