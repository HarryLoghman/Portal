using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace Tabriz2018Library
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            var mobilesList = GetMobilesList();
            if (Regex.IsMatch(message.Content, @"^[1-9]"))
                message.Content = "+" + message.Content;
            if (message.Content.Contains("+"))
                message = AddMobileToSubsriber(subscriber, message, mobilesList, messagesTemplate);
            else if (message.Content.Contains("-"))
                message = RemoveMobileFromSubsriber(subscriber, message, mobilesList, messagesTemplate);
            else
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            if (message.Content != null)
                MessageHandler.InsertMessageToQueue(message);
        }

        private static MessageObject RemoveMobileFromSubsriber(Subscriber subscriber, MessageObject message, List<MobilesList> mobilesList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("-", "");
            long mobileId = 0;
            foreach (var item in mobilesList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.MobileName)
                {
                    mobileId = item.Id;
                    break;
                }
            }
            if (mobileId != 0)
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var subscriberMobile = entity.SubscribersMobiles.FirstOrDefault(o => o.Subscriberid == subscriber.Id && o.MobileId == mobileId);
                    if (subscriberMobile == null)
                    {
                        message.Content = "شما عضو خبرنامه موبایل " + mobilesList.Where(o => o.Id == mobileId).FirstOrDefault().MobileName + " نیستید";
                    }
                    else
                    {
                        entity.SubscribersMobiles.Remove(subscriberMobile);
                        entity.SaveChanges();
                        message.Content = "دریافت خبرهای موبایل " + mobilesList.Where(o => o.Id == mobileId).FirstOrDefault().MobileName + " برای شما غیر فعال گردید";
                    }
                }
            }
            else
            {
                message = MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
            }
            return message;
        }

        private static MessageObject AddMobileToSubsriber(Subscriber subscriber, MessageObject message, List<MobilesList> mobilesList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("+", "");
            long mobileId = 0;
            foreach (var item in mobilesList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.MobileName)
                {
                    mobileId = item.Id;
                    break;
                }
            }
            if (mobileId != 0)
            {
                using (var entity = new Tabriz2018Entities())
                {
                    var isSubscriberMobileExits = entity.SubscribersMobiles.FirstOrDefault(o => o.Subscriberid == subscriber.Id && o.MobileId == mobileId);
                    if (isSubscriberMobileExits == null)
                    {
                        var mobile = new SubscribersMobile();
                        mobile.Subscriberid = subscriber.Id;
                        mobile.MobileId = mobileId;
                        entity.SubscribersMobiles.Add(mobile);
                        entity.SaveChanges();
                        SendWelcomeToMobileMessage(message, mobileId);
                    }
                }
                message = PrepareLatestMobileContent(message, subscriber.Id, mobileId);
            }
            else
            {
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            }
            return message;
        }

        private static void SendWelcomeToMobileMessage(MessageObject message, long mobileId)
        {
            using (var entity = new Tabriz2018Entities())
            {
                var welcomeMessage = message;
                var mobileName = entity.MobilesLists.FirstOrDefault(o => o.Id == mobileId).MobileName;
                welcomeMessage.Content = "به خبرنامه " + mobileName + " خوش آمدید.";
                welcomeMessage = MessageHandler.SetImiChargeInfo(welcomeMessage, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                MessageHandler.InsertMessageToQueue(welcomeMessage);
            }
        }

        public static MessageObject PrepareLatestMobileContent(MessageObject message, long subscriberId, long mobileId)
        {
            AutochargeContent content = new AutochargeContent();
            using (var entity = new Tabriz2018Entities())
            {
                var autochargeContentsSendedToUser = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId && o.MobileId == mobileId).OrderByDescending(o => o.Id).FirstOrDefault();
                if (autochargeContentsSendedToUser == null)
                {
                    content = entity.AutochargeContents.Where(o => o.MobileId == mobileId).FirstOrDefault();
                    if (content == null)
                    {
                        message.Content = null;
                        return message;
                    }
                }
                else
                {
                    content = entity.AutochargeContents.Where(o => o.MobileId == mobileId && o.Id > autochargeContentsSendedToUser.AutochargeContentId).FirstOrDefault();
                    if (content == null)
                    {
                        message.Content = null;
                        return message;
                    }
                }

                message = MessageHandler.SetImiChargeInfo(message, content.Price, 0, null);
                message.Content = content.Content;
                message.Point = content.Point;
                message.ContentId = content.Id;
                ServiceHandler.AddToAutochargeContentSendedToUserTable(subscriberId, content.Id, mobileId);
                return message;
            }
        }

        public static AutochargeContent PrepareLatestMobileContent(long subscriberId, long mobileId)
        {
            AutochargeContent content = new AutochargeContent();
            using (var entity = new Tabriz2018Entities())
            {
                var autochargeContentsSendedToUser = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId && o.MobileId == mobileId).OrderByDescending(o => o.Id).FirstOrDefault();
                if (autochargeContentsSendedToUser == null)
                {
                    content = entity.AutochargeContents.Where(o => o.MobileId == mobileId).FirstOrDefault();
                    return null;
                }
                else
                {
                    content = entity.AutochargeContents.Where(o => o.MobileId == mobileId && o.Id > autochargeContentsSendedToUser.AutochargeContentId).FirstOrDefault();
                    return null;
                }

                ServiceHandler.AddToAutochargeContentSendedToUserTable(subscriberId, content.Id, mobileId);
                return content;
            }
        }

        public static List<MobilesList> GetMobilesList()
        {
            using (var entity = new Tabriz2018Entities())
            {
                var mobilesList = entity.MobilesLists.ToList();
                return mobilesList;
            }
        }

        public static List<SubscribersMobile> GetSubscriberMobiles(long subscriberId)
        {
            using (var entity = new Tabriz2018Entities())
            {
                var subscriberMobiles = entity.SubscribersMobiles.Where(o => o.Subscriberid == subscriberId).ToList();
                return subscriberMobiles;
            }
        }
    }
}