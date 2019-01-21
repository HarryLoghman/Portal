using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SharedLibrary;
using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System.Net.Http;

namespace Tabriz2018Library
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static AutochargeContent SelectAutochargeContentByOrder(Tabriz2018Entities entity, long subscriberId)
        {
            AutochargeContent autochargeContent = null;
            var lastContentSubscriberReceived = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId).OrderByDescending(o => o.Id).FirstOrDefault();
            if (lastContentSubscriberReceived == null)
            {
                AddToAutochargeContentSendedToUserTable(subscriberId, autochargeContent.Id);
                autochargeContent = entity.AutochargeContents.FirstOrDefault();
            }
            else
            {
                var nextAutochargeContent = entity.AutochargeContents.FirstOrDefault(o => o.Id > lastContentSubscriberReceived.AutochargeContentId);
                if (nextAutochargeContent != null)
                {
                    AddToAutochargeContentSendedToUserTable(subscriberId, nextAutochargeContent.Id);
                    autochargeContent = nextAutochargeContent;
                }
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static void AddToAutochargeContentSendedToUserTable(long subscriberId, long autochargeId)
        {
            try
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var lastContent = new AutochargeContentsSendedToUser();
                    lastContent.SubscriberId = subscriberId;
                    lastContent.AutochargeContentId = autochargeId;
                    lastContent.DateAdded = DateTime.Now;
                    entity.AutochargeContentsSendedToUsers.Add(lastContent);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddToAutochargeContentSendedToUserTable: " + e);
            }
        }

        public static void AddToAutochargeContentSendedToUserTable(long subscriberId, long autochargeId, long mobileId)
        {
            try
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var lastContent = new AutochargeContentsSendedToUser();
                    lastContent.SubscriberId = subscriberId;
                    lastContent.AutochargeContentId = autochargeId;
                    lastContent.MobileId = mobileId;
                    lastContent.DateAdded = DateTime.Now;
                    entity.AutochargeContentsSendedToUsers.Add(lastContent);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddToAutochargeContentSendedToUserTable: " + e);
            }
        }

        public static AutochargeContent SelectAutochargeContentByDate(Tabriz2018Entities entity, long subscriberId)
        {
            var today = DateTime.Now.Date;
            var autochargesAlreadyInQueue = entity.AutochargeMessagesBuffers.Where(o => DbFunctions.TruncateTime(o.DateAddedToQueue) == today).Select(o => o.Id).ToList();

            var autochargeContent = entity.AutochargeContents.FirstOrDefault(o => DbFunctions.TruncateTime(o.SendDate) == today && !autochargesAlreadyInQueue.Contains(o.Id));
            if (autochargeContent != null)
            {
                AddToAutochargeContentSendedToUserTable(subscriberId, autochargeContent.Id);
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static AutochargeContent SelectAutochargeContent(Tabriz2018Entities entity, long subscriberId)
        {
            var today = DateTime.Now.Date;
            var autochargesAlreadyInQueue = entity.AutochargeMessagesBuffers.Where(o => DbFunctions.TruncateTime(o.DateAddedToQueue) == today).Select(o => o.Id).ToList();

            var autochargeContent = entity.AutochargeContents.FirstOrDefault(o => DbFunctions.TruncateTime(o.SendDate) == today && !autochargesAlreadyInQueue.Contains(o.Id));
            if (autochargeContent != null)
            {
                AddToAutochargeContentSendedToUserTable(subscriberId, autochargeContent.Id);
            }
            entity.SaveChanges();
            return autochargeContent;
        }


        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new Tabriz2018Entities())
            {
                return entity.MessagesTemplates.ToList();
            }
        }

        public static bool CheckIfUserWantsToUnsubscribe(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content.Contains(offKeyword))
                    return true;
            }
            return false;
        }

        public static void InfromMapfaIntegratedPanel(HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState
            , MessageObject message, vw_servicesServicesInfo service, string keyword)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                    string url = "http://10.20.22.18/getdata.aspx?" + "num=" + message.MobileNumber + "&sid=" + serviceInfo.AggregatorServiceId;
                    if (serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        url += "&st=1";
                    else
                        url += "&st=0";
                    url += "&dt=" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    url += "&SubUnsubMessage=" + keyword;
                    using (var response = client.GetAsync(new Uri(url)).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in InfromMapfaIntegratedPanel: " + e);
            }
        }

        public static void RemoveSubscriberMobiles(long? subscriberId)
        {
            using (var entity = new Tabriz2018Entities())
            {
                var subscriberMobiles = entity.SubscribersMobiles.Where(o => o.Subscriberid == subscriberId);
                entity.SubscribersMobiles.RemoveRange(subscriberMobiles);
                entity.SaveChanges();
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