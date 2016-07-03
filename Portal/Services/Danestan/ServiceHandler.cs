using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.Danestan
{
    public class ServiceHandler
    {
        public static AutochargeContent SelectAutochargeContent(DanestanEntities entity, long subscriberId)
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

        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new DanestanEntities())
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