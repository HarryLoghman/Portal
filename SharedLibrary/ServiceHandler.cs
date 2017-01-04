using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;

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
                sendInfoDic["serviceId"] = service.Id.ToString();
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
                                                   .Select( o => new Service { Id = o.ServiceId, Name = o.ServiceName, OnKeywords = o.ServiceOnKeywords })
                                                   .ToList();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in GetServicesThatUserSubscriberd: " + e);
                }
                return servicesThatUserSubscribedOnShortCode;
            }
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
                "off",
                "of",
                "cancel",
                "stop",
                "laghv",
                "lagv",
                "khamoosh",
                "خاموش",
                "لغو",
                "کنسل",
                "توقف",
                "پایان"
            };
            return offContents;
        }
    }
}