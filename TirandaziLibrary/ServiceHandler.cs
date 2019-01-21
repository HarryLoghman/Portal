using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using TirandaziLibrary.Models;

namespace TirandaziLibrary
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static AutochargeContent SelectAutochargeContentByOrder(TirandaziEntities entity, long subscriberId)
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

        public static MessageObject AddSubscriberToSubscriptionKeywords(MessageObject message, long subscriberId)
        {
            try
            {
                using (var entity = new TirandaziEntities())
                {
                    var keyword = entity.SubscriptionKeywords.FirstOrDefault(o => o.Keyword == message.Content);
                    var subscriberKeyword = entity.SusbcribersSubscriptionKeywords.FirstOrDefault(o => o.SubscriberId == subscriberId && o.SubscriptionKeywordId == keyword.Id);
                    if(subscriberKeyword == null)
                    {
                        subscriberKeyword = new SusbcribersSubscriptionKeyword();
                        subscriberKeyword.SubscriberId = subscriberId;
                        subscriberKeyword.SubscriptionKeywordId = keyword.Id;
                        entity.SusbcribersSubscriptionKeywords.Add(subscriberKeyword);
                        entity.SaveChanges();
                    }
                     
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TryToAddUserToGroup2: ", e);
            }
            return message;
        }

        public static AutochargeContent SelectAutochargeContentByDate(TirandaziEntities entity, long subscriberId)
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


        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new TirandaziEntities())
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

        public static void RemoveSubscriberSubscriptionKeywords(long? subscriberId)
        {
            using (var entity = new TirandaziEntities())
            {
                var subscriberKeywords = entity.SusbcribersSubscriptionKeywords.Where(o => o.SubscriberId == subscriberId);
                if (subscriberKeywords != null)
                {
                    entity.SusbcribersSubscriptionKeywords.RemoveRange(subscriberKeywords);
                    entity.SaveChanges();
                }
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