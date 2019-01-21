using SharedLibrary.Models;
using DanestanehLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace DanestanehLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void HandleContent(MessageObject message, vw_servicesServicesInfo service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            if (Regex.IsMatch(message.Content, @"^[1-3]"))
                message = GetContent(message);
            else
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            if (message.Content != null)
                MessageHandler.InsertMessageToQueue(message);
        }

        public static MessageObject GetContent(MessageObject message)
        {
            var content = Convert.ToInt32(message.Content);
            message.Content = null;
            try
            {
                using (var entity = new DanestanehEntities())
                {
                    var autochargeId = entity.LeveledAutochargeContentDailyLists.FirstOrDefault(o => o.SubscriberId == message.SubscriberId && o.Index == content).AutochargeContentId;
                    var autocharge = entity.AutochargeContents.FirstOrDefault(o => o.Id == autochargeId);
                    message.Content = autocharge.Content;
                    message.Point = autocharge.Point;
                    message = MessageHandler.SetImiChargeInfo(message, autocharge.Price, 0, null);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Eventbase thread occurred: ", e);
            }
            return message;
        }

        public static MessageObject LeveledAutochargeMessage(Subscriber subscriber, long serviceId, string shortCode ,int? tag, long aggregatorId, SharedLibrary.MessageHandler.MessageType messageType, SharedLibrary.MessageHandler.ProcessStatus processStatus)
        {
            var autochargeContent = DanestanehLibrary.ServiceHandler.PrepareLeveledAutochargeContentDailyListForSubscriber(subscriber.Id, 3);
            if (autochargeContent == "")
                return null;
            var autochargeContentId = 0;
            var point = 0;
            var price = 100;
            autochargeContent = DanestanehLibrary.MessageHandler.AddAutochargeHeaderAndFooter(autochargeContent);
            autochargeContent = DanestanehLibrary.MessageHandler.HandleSpecialStrings(autochargeContent, point, subscriber.MobileNumber, serviceId);
            var imiChargeObject = DanestanehLibrary.MessageHandler.GetImiChargeObjectFromPrice(100, null);
            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent, autochargeContentId, messageType, processStatus, 0, imiChargeObject, aggregatorId, point, tag, imiChargeObject.Price);
            message.ShortCode = shortCode;
            return message;
        }

        public static void RemoveLeveledAutochargeContentDailyListForSubscriber(long subscriberId)
        {
            try
            {
                using (var entity = new DanestanehEntities())
                {
                    var autochargeContents = entity.LeveledAutochargeContentDailyLists.Where(o => o.SubscriberId == subscriberId);
                    foreach (var autochargeContent in autochargeContents)
                    {
                        var item = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId && o.AutochargeContentId == autochargeContent.AutochargeContentId).FirstOrDefault();
                        entity.AutochargeContentsSendedToUsers.Remove(item);
                        entity.LeveledAutochargeContentDailyLists.Remove(autochargeContent);
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in RemoveLeveledAutochargeContentDailyListForSubscriber occurred: ", e);
            }
        }
    }
}