﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SepidRoodLibrary.Models;
using System.Text.RegularExpressions;
using SharedLibrary;

namespace SepidRoodLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            var content = message.Content;
            MessageObject lastEventbaseContent;
            if (message.Content == "2")
            {
                var isUserAlreadySubscribedToKeyword = IsUserAlreadySubscribedToKeyword(message.Content, subscriber.Id);
                if (isUserAlreadySubscribedToKeyword == false)
                {
                    message = ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriber.Id);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessageWith2Keyword").Select(o => o.Content).FirstOrDefault();
                    Subscribers.Add200TomanPoint(subscriber, service.Id);
                    MessageHandler.InsertMessageToQueue(message);
                    lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, content);
                    if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                    {
                        MessageHandler.InsertMessageToTimedTempQueue(lastEventbaseContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
                    }
                }
                else
                {
                    lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, content);
                    if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                    {
                        lastEventbaseContent.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                        MessageHandler.InsertMessageToQueue(lastEventbaseContent);
                    }
                }
            }
            else if (message.Content == "5")
            {
                var isUserAlreadySubscribedToKeyword = IsUserAlreadySubscribedToKeyword(message.Content, subscriber.Id);
                if (isUserAlreadySubscribedToKeyword == false)
                {
                    message = ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriber.Id);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessageWith5Keyword").Select(o => o.Content).FirstOrDefault();
                    Subscribers.Add500TomanPoint(subscriber, service.Id);
                    MessageHandler.InsertMessageToQueue(message);
                    lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, content);
                    if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                    {
                        MessageHandler.InsertMessageToTimedTempQueue(lastEventbaseContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
                    }
                }
                else
                {
                    lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, content);
                    if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                    {
                        lastEventbaseContent.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                        MessageHandler.InsertMessageToQueue(lastEventbaseContent);
                    }
                }
            }
            else if (message.Content == "22")
            {
                message = ContentWith2000Price(message, messagesTemplate);
                Subscribers.Add2000TomanPoint(subscriber, service.Id);
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "55")
            {
                message = ContentWith5000Price(message, messagesTemplate);
                Subscribers.Add5000TomanPoint(subscriber, service.Id);
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "")
            {
                message = MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
            }
        }

        public static bool IsUserAlreadySubscribedToKeyword(string content, long subscriberId)
        {
            try
            {
                using (var entity = new SepidRoodEntities())
                {
                    var keyword = entity.SubscriptionKeywords.FirstOrDefault(o => o.Keyword == content);
                    var subscriberKeyword = entity.SusbcribersSubscriptionKeywords.FirstOrDefault(o => o.SubscriberId == subscriberId && o.SubscriptionKeywordId == keyword.Id);
                    if (subscriberKeyword == null)
                        return false;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in checkUserAlreadySubscribedToKeyword:" + e);
            }
            return true;
        }

        public static MessageObject ContentWith2000Price(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 2000, 0, null);
            message.Content = messagesTemplate.Where(o => o.Title == "ContentWith2000Price").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject ContentWith5000Price(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 5000, 0, null);
            message.Content = messagesTemplate.Where(o => o.Title == "ContentWith5000Price").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static List<SubscriptionKeyword> GetSubscriptionKeywordsList()
        {
            using (var entity = new SepidRoodEntities())
            {
                var subscriptionKeywords = entity.SubscriptionKeywords.ToList();
                return subscriptionKeywords;
            }
        }

        public static MessageObject GetLastEventbaseContent(Subscriber subscriber, long serviceId, string shortCode, int? tag, long aggregatorId, SharedLibrary.MessageHandler.MessageType messageType, SharedLibrary.MessageHandler.ProcessStatus processStatus, string messageContent)
        {
            try
            {
                using (var entity = new SepidRoodEntities())
                {
                    var subscriberKeyword = entity.SubscriptionKeywords.FirstOrDefault(o => o.Keyword == messageContent);
                    var eventbaseContent = entity.EventbaseContents.Where(o => o.SubscriptionKeywordId == subscriberKeyword.Id).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                    if (eventbaseContent == null)
                        return null;
                    eventbaseContent.Content = MessageHandler.HandleSpecialStrings(eventbaseContent.Content, eventbaseContent.Point, subscriber.MobileNumber, serviceId);
                    var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(eventbaseContent.Price, null);
                    var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, eventbaseContent.Content, eventbaseContent.Id, messageType, processStatus, 0, imiChargeObject, aggregatorId, eventbaseContent.Point, null);
                    message.ShortCode = shortCode;
                    return message;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetLastEventbaseContent: " + e);
            }
            return null;
        }
    }
}