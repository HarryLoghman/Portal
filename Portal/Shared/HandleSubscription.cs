using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Shared;

namespace Portal.Shared
{
    public class HandleSubscription
    {
        public static ServiceStatusForSubscriberState HandleSubscriptionContent(Message message, Service serviceInfo)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            message.ImiChargeCode = 0;
            message.ImiMessageType = 21;
            message.MessageType = (int)MessageHandler.MessageType.OnDemand;

            if (OffKeywords().Contains(message.Content))
                serviceStatusForSubscriberState = Unsubscribe(message, serviceInfo);
            else
                serviceStatusForSubscriberState = Subscribe(message, serviceInfo);
            return serviceStatusForSubscriberState;
        }

        public static ServiceStatusForSubscriberState Subscribe(Message message, Service serviceInfo)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            using (var entity = new PortalEntities())
            {
                var subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == serviceInfo.Id).FirstOrDefault();
                if (subscriber == null)
                    serviceStatusForSubscriberState = AddNewSubscriberToService(message, serviceInfo);
                else if (subscriber.DeactivationDate == null)
                    AlreadySubsribed(message, serviceInfo);
                else
                    serviceStatusForSubscriberState = ActivateServiceForSubscriber(message, subscriber, serviceInfo);
            }
            return serviceStatusForSubscriberState;
        }

        private static void AlreadySubsribed(Message message, Service serviceInfo)
        {
            MessageHandler.InvalidContentWhenSubscribed(message, serviceInfo);
        }

        private static ServiceStatusForSubscriberState ActivateServiceForSubscriber(Message message, Subscriber subscriber, Service serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.OffKeyword = null;
                subscriber.OffMethod = null;
                entity.Subscribers.Attach(subscriber);
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
            }
            message.Content = serviceInfo.WelcomeMessage;
            MessageHandler.InsertMessageToQueue(message);
            return ServiceStatusForSubscriberState.Activated;
        }

        private static ServiceStatusForSubscriberState AddNewSubscriberToService(Message message, Service serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                var newSubscriber = new Subscriber();
                newSubscriber.MobileNumber = message.MobileNumber;
                newSubscriber.OnKeyword = message.Content;
                newSubscriber.OnMethod = "keyword";
                newSubscriber.ServiceId = serviceInfo.Id;
                newSubscriber.ActivationDate = DateTime.Now;
                newSubscriber.PersianActivationDate = Shared.Date.GetPersianDate();
                newSubscriber.MobileOperator = message.MobileOperator;
                newSubscriber.OperatorPlan = message.OperatorPlan;
                entity.Subscribers.Add(newSubscriber);
                entity.SaveChanges();

                AddToSubscriberHistory(message.MobileNumber, serviceInfo, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null);

                message.Content = serviceInfo.WelcomeMessage;
                MessageHandler.InsertMessageToQueue(message);
            }
            return ServiceStatusForSubscriberState.Activated;
        }

        private static void AddToSubscriberHistory(string mobileNumber, Service serviceInfo, ServiceStatusForSubscriberState subscriberState, WhoChangedSubscriberState whoChangedSubscriberState, string invalidContent)
        {
            using (var entity = new PortalEntities())
            {
                var subscriberHistory = new SubscribersHistory();
                subscriberHistory.MobileNumber = mobileNumber;
                subscriberHistory.Date = DateTime.Now;
                subscriberHistory.ServiceName = serviceInfo.Name;
                subscriberHistory.ServiceId = serviceInfo.Id;
                subscriberHistory.ServiceStatusForSubscriber = (int)subscriberState;
                subscriberHistory.ShortCode = serviceInfo.ShortCode;
                subscriberHistory.Time = DateTime.Now.TimeOfDay;
                subscriberHistory.WhoChangedSubscriberStatus = (int)whoChangedSubscriberState;
                subscriberHistory.MCIServiceId = 0;
                subscriberHistory.InvalidContent = invalidContent;
                entity.SubscribersHistories.Add(subscriberHistory);
                entity.SaveChanges();
            }
        }

        public static ServiceStatusForSubscriberState Unsubscribe(Message message, Service serviceInfo)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            using (var entity = new PortalEntities())
            {
                
                var subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == serviceInfo.Id && o.DeactivationDate != null).FirstOrDefault();
                if (subscriber == null)
                    UserNotSubscribedToService(message, serviceInfo);
                else
                    serviceStatusForSubscriberState = DeactivateServiceForSubscriber(message, serviceInfo, subscriber);
            }
            return serviceStatusForSubscriberState;
        }

        private static void UserNotSubscribedToService(Message message, Service serviceInfo)
        {
            MessageHandler.InvalidContentWhenNotSubscribed(message, serviceInfo);
        }

        private static ServiceStatusForSubscriberState DeactivateServiceForSubscriber(Message message, Service serviceInfo, Subscriber subscriber)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.DeactivationDate = DateTime.Now;
                subscriber.PersianDeactivationDate = Shared.Date.GetPersianDate();
                subscriber.OffKeyword = message.Content;
                subscriber.OffMethod = "keyword";
                entity.Subscribers.Attach(subscriber);
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
                AddToSubscriberHistory(message.MobileNumber, serviceInfo, ServiceStatusForSubscriberState.Deactivated, WhoChangedSubscriberState.User, null);

                message.Content = serviceInfo.LeaveMessage;
                MessageHandler.InsertMessageToQueue(message);
            }
            return ServiceStatusForSubscriberState.Deactivated;
        }

        private static string[] OffKeywords()
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

        public enum ServiceStatusForSubscriberState
        {
            Deactivated = 0,
            Activated = 1,
            Unspecified = 2
        }

        public enum WhoChangedSubscriberState
        {
            User = 0,
        }
    }
}