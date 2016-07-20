using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Portal.Models;

namespace Portal.Services.MyLeague
{
    public class ServiceHandler
    {
        public static AutochargeContent SelectAutochargeContentByOrder(MyLeagueEntities entity, long subscriberId)
        {
            AutochargeContent autochargeContent = null;
            var lastContentSubscriberReceived = entity.LastAutochargeContentSendedToUsers.FirstOrDefault(o => o.SubscriberId == subscriberId);
            if (lastContentSubscriberReceived == null)
            {
                autochargeContent = entity.AutochargeContents.FirstOrDefault();
                var lastContent = new LastAutochargeContentSendedToUser();
                lastContent.SubscriberId = subscriberId;
                lastContent.AutochargeContentId = autochargeContent.Id;
                entity.LastAutochargeContentSendedToUsers.Add(lastContent);
            }
            else
            {
                var nextAutochargeContent = entity.AutochargeContents.FirstOrDefault(o => o.Id > lastContentSubscriberReceived.AutochargeContentId);
                if (nextAutochargeContent != null)
                {
                    autochargeContent = nextAutochargeContent;
                    lastContentSubscriberReceived.AutochargeContentId = nextAutochargeContent.Id;
                    entity.Entry(lastContentSubscriberReceived).State = System.Data.Entity.EntityState.Modified;
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
                var lastContent = new LastAutochargeContentSendedToUser();
                lastContent.SubscriberId = subscriberId;
                lastContent.AutochargeContentId = autochargeContent.Id;
                entity.LastAutochargeContentSendedToUsers.Add(lastContent);
            }
            entity.SaveChanges();
            return autochargeContent;
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
                "کنسل"
            };
            return offContents;
        }
    }
}