﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using DanestanehLibrary.Models;

namespace DanestanehLibrary
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static AutochargeContent SelectAutochargeContentByOrder(DanestanehEntities entity, long subscriberId)
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
                using (var entity = new DanestanehEntities())
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

        public static AutochargeContent SelectAutochargeContentByDate(DanestanehEntities entity, long subscriberId)
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


        public static string PrepareLeveledAutochargeContentDailyListForSubscriber(long subscriberId, int numberofContents)
        {
            var content = "";
            try
            {
                using (var entity = new DanestanehEntities())
                {
                    var allAutochargeContentsSubsriberReceived = (from c in entity.AutochargeContentsSendedToUsers where c.SubscriberId == subscriberId select c.AutochargeContentId);
                    var autochargeContents = (from c in entity.AutochargeContents where !allAutochargeContentsSubsriberReceived.Contains(c.Id) orderby c.Id ascending select new { AutochargeContentId = c.Id, Title = c.Title, Content = c.Content }).Take(numberofContents).ToList();
                    int index = 1;
                    foreach (var autochargeContent in autochargeContents)
                    {
                        var level = new LeveledAutochargeContentDailyList();
                        level.AutochargeContentId = autochargeContent.AutochargeContentId;
                        level.SubscriberId = subscriberId;
                        level.Index = index;
                        entity.LeveledAutochargeContentDailyLists.Add(level);
                        entity.SaveChanges();
                        AddToAutochargeContentSendedToUserTable(subscriberId, autochargeContent.AutochargeContentId);
                        content += index + ". " + autochargeContent.Title + Environment.NewLine;
                        index++;
                    }
                    
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in PrepareLeveledAutochargeContentDailyListForSubscriber: " + e);
            }
            return content;
        }


        public static List<MessagesTemplate> GetServiceMessagesTemplate()
        {
            using (var entity = new DanestanehEntities())
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
    }
}