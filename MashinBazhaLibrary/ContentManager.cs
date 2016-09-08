using SharedLibrary.Models;
using MashinBazhaLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace MashinBazhaLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            var carBrandsList = GetCarBrandsList();
            if (Regex.IsMatch(message.Content, @"^[11-20]"))
                message.Content = "+" + message.Content;
            if (message.Content.Contains("+"))
                message = AddCarBrandToSubsriber(subscriber, message, carBrandsList, messagesTemplate);
            else if (message.Content.Contains("-"))
                message = RemoveCarBrandFromSubsriber(subscriber, message, carBrandsList, messagesTemplate);
            else
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            if (message.Content != null)
                MessageHandler.InsertMessageToQueue(message);
        }

        private static MessageObject RemoveCarBrandFromSubsriber(Subscriber subscriber, MessageObject message, List<CarBrandsList> carBarndsList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("-", "");
            long carBrandId = 0;
            foreach (var item in carBarndsList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.CarBrand)
                {
                    carBrandId = item.Id;
                    break;
                }
            }
            if (carBrandId != 0)
            {
                using (var entity = new MashinBazhaEntities())
                {
                    var subscriberCarBrand = entity.SubscribersCarBrands.FirstOrDefault(o => o.Subscriberid == subscriber.Id && o.CarBrandId == carBrandId);
                    if (subscriberCarBrand == null)
                    {
                        message.Content = "شما عضو خبرنامه ماشین " + carBarndsList.Where(o => o.Id == carBrandId).FirstOrDefault().CarBrand + " نیستید";
                    }
                    else
                    {
                        entity.SubscribersCarBrands.Remove(subscriberCarBrand);
                        entity.SaveChanges();
                        message.Content = "دریافت خبرهای ماشین " + carBarndsList.Where(o => o.Id == carBrandId).FirstOrDefault().CarBrand + " برای شما غیر فعال گردید";
                    }
                }
            }
            else
            {
                message = MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
            }
            return message;
        }

        private static MessageObject AddCarBrandToSubsriber(Subscriber subscriber, MessageObject message, List<CarBrandsList> carBrandsList, List<MessagesTemplate> messagesTemplate)
        {
            message.Content = message.Content.Replace("+", "");
            long carBrandId = 0;
            foreach (var item in carBrandsList)
            {
                if (message.Content == item.Number.ToString() || message.Content == item.CarBrand)
                {
                    carBrandId = item.Id;
                    break;
                }
            }
            if (carBrandId != 0)
            {
                using (var entity = new MashinBazhaEntities())
                {
                    var isSubscriberCarBrandExits = entity.SubscribersCarBrands.FirstOrDefault(o => o.Subscriberid == subscriber.Id && o.CarBrandId == carBrandId);
                    if (isSubscriberCarBrandExits == null)
                    {
                        var carBrand = new SubscribersCarBrand();
                        carBrand.Subscriberid = subscriber.Id;
                        carBrand.CarBrandId = carBrandId;
                        entity.SubscribersCarBrands.Add(carBrand);
                        entity.SaveChanges();
                    }
                    SendWelcomeToMashinBazhaMessage(message, carBrandId);
                }
                message = PrepareLatestMashinBazhaContent(message, subscriber.Id, carBrandId);
            }
            else
            {
                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
            }
            return message;
        }

        private static void SendWelcomeToMashinBazhaMessage(MessageObject message, long carBrandId)
        {
            using (var entity = new MashinBazhaEntities())
            {
                var welcomeMessage = message;
                var mobileName = entity.CarBrandsLists.FirstOrDefault(o => o.Id == carBrandId).CarBrand;
                welcomeMessage.Content = "به خبرنامه " + mobileName + " خوش آمدید.";
                welcomeMessage = MessageHandler.SetImiChargeInfo(welcomeMessage, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                MessageHandler.InsertMessageToQueue(welcomeMessage);
            }
        }

        public static MessageObject PrepareLatestMashinBazhaContent(MessageObject message, long subscriberId, long carBrandId)
        {
            AutochargeContent content = new AutochargeContent();
            using (var entity = new MashinBazhaEntities())
            {
                var autochargeContentsSendedToUser = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId && o.CarBrandId == carBrandId).OrderByDescending(o => o.Id).FirstOrDefault();
                if (autochargeContentsSendedToUser == null)
                {
                    content = entity.AutochargeContents.Where(o => o.CarBrandId == carBrandId).FirstOrDefault();
                    if (content == null)
                    {
                        message.Content = null;
                        return message;
                    }
                }
                else
                {
                    content = entity.AutochargeContents.Where(o => o.CarBrandId == carBrandId && o.Id > autochargeContentsSendedToUser.AutochargeContentId).FirstOrDefault();
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
                ServiceHandler.AddToAutochargeContentSendedToUserTable(subscriberId, content.Id, carBrandId);
                return message;
            }
        }

        public static AutochargeContent PrepareLatestMashinBazhaContent(long subscriberId, long carBrandId)
        {
            AutochargeContent content = new AutochargeContent();
            using (var entity = new MashinBazhaEntities())
            {
                var autochargeContentsSendedToUser = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId && o.CarBrandId == carBrandId).OrderByDescending(o => o.Id).FirstOrDefault();
                if (autochargeContentsSendedToUser == null)
                {
                    content = entity.AutochargeContents.Where(o => o.CarBrandId == carBrandId).FirstOrDefault();
                    return null;
                }
                else
                {
                    content = entity.AutochargeContents.Where(o => o.CarBrandId == carBrandId && o.Id > autochargeContentsSendedToUser.AutochargeContentId).FirstOrDefault();
                    return null;
                }

                ServiceHandler.AddToAutochargeContentSendedToUserTable(subscriberId, content.Id, carBrandId);
                return content;
            }
        }

        public static List<CarBrandsList> GetCarBrandsList()
        {
            using (var entity = new MashinBazhaEntities())
            {
                var carBrandsList = entity.CarBrandsLists.ToList();
                return carBrandsList;
            }
        }

        public static List<SubscribersCarBrand> GetSubscriberCarBrands(long subscriberId)
        {
            using (var entity = new MashinBazhaEntities())
            {
                var subscriberCarBrands = entity.SubscribersCarBrands.Where(o => o.Subscriberid == subscriberId).ToList();
                return subscriberCarBrands;
            }
        }
    }
}