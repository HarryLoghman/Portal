using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using MyLeagueLibrary.Models;

namespace MyLeagueLibrary
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static AutochargeContent SelectAutochargeContentByOrder(MyLeagueEntities entity, long subscriberId)
        {
            AutochargeContent autochargeContent = null;
            var lastContentSubscriberReceived = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId).OrderByDescending(o => o.Id).FirstOrDefault();
            if (lastContentSubscriberReceived == null)
            {
                var lastContent = new AutochargeContentsSendedToUser();
                lastContent.SubscriberId = subscriberId;
                lastContent.AutochargeContentId = autochargeContent.Id;
                lastContent.DateAdded = DateTime.Now;
                entity.AutochargeContentsSendedToUsers.Add(lastContent);
                autochargeContent = entity.AutochargeContents.FirstOrDefault();
            }
            else
            {
                var nextAutochargeContent = entity.AutochargeContents.FirstOrDefault(o => o.Id > lastContentSubscriberReceived.AutochargeContentId);
                if (nextAutochargeContent != null)
                {
                    var lastContent = new AutochargeContentsSendedToUser();
                    lastContent.SubscriberId = subscriberId;
                    lastContent.AutochargeContentId = nextAutochargeContent.Id;
                    lastContent.DateAdded = DateTime.Now;
                    entity.AutochargeContentsSendedToUsers.Add(lastContent);
                    autochargeContent = nextAutochargeContent;
                }
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static AutochargeContent SelectAutochargeContentByDate(MyLeagueEntities entity, long subscriberId)
        {
            var today = DateTime.Now.Date;
            var autochargesAlreadyInQueue = entity.AutochargeMessagesBuffers.Where(o => DbFunctions.TruncateTime(o.DateAddedToQueue) == today).Select(o => o.Id).ToList();

            var autochargeContent = entity.AutochargeContents.FirstOrDefault(o => DbFunctions.TruncateTime(o.SendDate) == today && !autochargesAlreadyInQueue.Contains(o.Id));
            if (autochargeContent != null)
            {
                var lastContent = new AutochargeContentsSendedToUser();
                lastContent.SubscriberId = subscriberId;
                lastContent.AutochargeContentId = autochargeContent.Id;
                lastContent.DateAdded = DateTime.Now;
                entity.AutochargeContentsSendedToUsers.Add(lastContent);
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static void RemoveSubscriberLeagues(long? subscriberId)
        {
            using (var entity = new MyLeagueEntities())
            {
                var subscriberLeagus = entity.SubscribersLeagues.Where(o => o.SubscriberId == subscriberId);
                entity.SubscribersLeagues.RemoveRange(subscriberLeagus);
                entity.SaveChanges();
            }
        }

        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new MyLeagueEntities())
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

        public static List<ServiceRechargeKeyword> GetServiceRechargeKeywords()
        {
            using (var entity = new MyLeagueEntities())
            {
                var rechargeKeywords = entity.ServiceRechargeKeywords.ToList();
                return rechargeKeywords;
            }
        }

        public static bool CheckIfUserWantsChargeCode(string content, List<ServiceRechargeKeyword> serviceRehcargeKeywords)
        {
            foreach (var serviceRechargeKeyword in serviceRehcargeKeywords)
            {
                if (content == serviceRechargeKeyword.Keyword)
                    return true;
            }
            return false;
        }
    }
}