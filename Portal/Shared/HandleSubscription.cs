using System;
using System.Data.Entity;
using System.Linq;
using Portal.Models;

namespace Portal.Shared
{
    public class HandleSubscription
    {
        public static ServiceStatusForSubscriberState HandleSubscriptionContent(MessageObject message, Service service, bool isUserWantsToUnsubscribe)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            message.MessageType = (int)MessageHandler.MessageType.OnDemand;

            if (isUserWantsToUnsubscribe == true)
                serviceStatusForSubscriberState = Unsubscribe(message, service);
            else
                serviceStatusForSubscriberState = Subscribe(message, service);
            return serviceStatusForSubscriberState;
        }

        public static ServiceStatusForSubscriberState Subscribe(MessageObject message, Service service)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            Subscriber subscriber;
            using (var entity = new PortalEntities())
            {
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == service.Id).FirstOrDefault();
            }
            if (subscriber == null)
                serviceStatusForSubscriberState = AddNewSubscriberToService(message, service);
            else if (subscriber.DeactivationDate == null)
                serviceStatusForSubscriberState = ServiceStatusForSubscriberState.InvalidContentWhenSubscribed;
            else
                serviceStatusForSubscriberState = ActivateServiceForSubscriber(subscriber, message.Content);
            return serviceStatusForSubscriberState;
        }

        public static long? GetSubscriberId(string mobileNumber, long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                var subscriberId = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).Select(o => o.Id).FirstOrDefault();
                if (subscriberId == null)
                    return null;
                else
                    return subscriberId;
            }
        }

        public static Subscriber GetSubscriber(string mobileNumber, long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                var subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).FirstOrDefault();
                return subscriber;
            }
        }


        private static ServiceStatusForSubscriberState ActivateServiceForSubscriber(Subscriber subscriber, string onKeyword)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.OffKeyword = null;
                subscriber.OffMethod = null;
                subscriber.DeactivationDate = null;
                subscriber.PersianDeactivationDate = null;
                subscriber.OnKeyword = onKeyword;
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
            }
            return ServiceStatusForSubscriberState.Activated;
        }

        private static ServiceStatusForSubscriberState AddNewSubscriberToService(MessageObject message, Service service)
        {
            using (var entity = new PortalEntities())
            {
                var newSubscriber = new Subscriber();
                newSubscriber.MobileNumber = message.MobileNumber;
                newSubscriber.OnKeyword = message.Content;
                newSubscriber.OnMethod = "keyword";
                newSubscriber.ServiceId = service.Id;
                newSubscriber.ActivationDate = DateTime.Now;
                newSubscriber.PersianActivationDate = Shared.Date.GetPersianDate();
                newSubscriber.MobileOperator = message.MobileOperator;
                newSubscriber.OperatorPlan = message.OperatorPlan;
                newSubscriber.SubscriberUniqueId = CreateUniqueId();
                entity.Subscribers.Add(newSubscriber);
                entity.SaveChanges();

                AddSubscriberToSubscriberPointsTable(newSubscriber, service);

                AddToSubscriberHistory(message.MobileNumber, message.ShortCode, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null);
            }
            return ServiceStatusForSubscriberState.Activated;
        }

        private static string CreateUniqueId()
        {
            Random random = new Random();
            var unqiueId = random.Next(10000000, 99999999).ToString();
            using (var entity = new PortalEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var subsriber = entity.Subscribers.FirstOrDefault(o => o.SubscriberUniqueId == unqiueId);
                if (subsriber != null)
                    unqiueId = CreateUniqueId();
            }
            return unqiueId;
        }

        private static void AddSubscriberToSubscriberPointsTable(Subscriber newSubscriber, Service service)
        {
            using (var entity = new PortalEntities())
            {
                var subscriberPoint = new SubscribersPoint();
                subscriberPoint.ServiceId = service.Id;
                subscriberPoint.SubscriberId = newSubscriber.Id;
                subscriberPoint.Point = 0;
                entity.SubscribersPoints.Add(subscriberPoint);
                entity.SaveChanges();
            }
        }

        private static void AddToSubscriberHistory(string mobileNumber, string shortCode, Service service, ServiceStatusForSubscriberState subscriberState, WhoChangedSubscriberState whoChangedSubscriberState, string invalidContent)
        {
            using (var entity = new PortalEntities())
            {
                var subscriberHistory = new SubscribersHistory();
                subscriberHistory.MobileNumber = mobileNumber;
                subscriberHistory.Date = DateTime.Now;
                subscriberHistory.ServiceName = service.Name;
                subscriberHistory.ServiceId = service.Id;
                subscriberHistory.ServiceStatusForSubscriber = (int)subscriberState;
                subscriberHistory.ShortCode = shortCode;
                subscriberHistory.Time = DateTime.Now.TimeOfDay;
                subscriberHistory.WhoChangedSubscriberStatus = (int)whoChangedSubscriberState;
                subscriberHistory.MCIServiceId = 0;
                subscriberHistory.InvalidContent = invalidContent;
                entity.SubscribersHistories.Add(subscriberHistory);
                entity.SaveChanges();
            }
        }

        public static ServiceStatusForSubscriberState Unsubscribe(MessageObject message, Service service)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            Subscriber subscriber;
            using (var entity = new PortalEntities())
            {
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == service.Id && o.DeactivationDate == null).FirstOrDefault();
            }
            if (subscriber == null)
                serviceStatusForSubscriberState = ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed;
            else
                serviceStatusForSubscriberState = DeactivateServiceForSubscriber(message, service, subscriber);

            return serviceStatusForSubscriberState;
        }

        private static ServiceStatusForSubscriberState DeactivateServiceForSubscriber(MessageObject message, Service service, Subscriber subscriber)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.DeactivationDate = DateTime.Now;
                subscriber.PersianDeactivationDate = Shared.Date.GetPersianDate();
                subscriber.OffKeyword = message.Content;
                subscriber.OffMethod = "keyword";
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
                AddToSubscriberHistory(message.MobileNumber, message.ShortCode, service, ServiceStatusForSubscriberState.Deactivated, WhoChangedSubscriberState.User, null);
            }
            return ServiceStatusForSubscriberState.Deactivated;
        }

        public enum ServiceStatusForSubscriberState
        {
            Deactivated = 0,
            Activated = 1,
            Unspecified = 2,
            InvalidContentWhenNotSubscribed = 3,
            InvalidContentWhenSubscribed = 4,

        }

        public enum WhoChangedSubscriberState
        {
            User = 0,
            IntegratedPanel = 1
        }
    }
}