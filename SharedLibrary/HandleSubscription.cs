using System;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using System.Collections.Generic;

namespace SharedLibrary
{
    public class HandleSubscription
    {
        public static ServiceStatusForSubscriberState HandleSubscriptionContent(MessageObject message, Service service, bool isUserWantsToUnsubscribe)
        {
            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            message.MessageType = (int)MessageHandler.MessageType.OnDemand;

            if (isUserWantsToUnsubscribe == true)
                serviceStatusForSubscriberState = Unsubscribe(message, service, serviceInfo);
            else
                serviceStatusForSubscriberState = Subscribe(message, service, serviceInfo);
            return serviceStatusForSubscriberState;
        }

        public static ServiceStatusForSubscriberState Subscribe(MessageObject message, Service service, ServiceInfo serviceInfo)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            Subscriber subscriber;
            using (var entity = new PortalEntities())
            {
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == service.Id).FirstOrDefault();
            }
            if (subscriber == null)
                serviceStatusForSubscriberState = AddNewSubscriberToService(message, service, serviceInfo);
            else if (subscriber.DeactivationDate == null)
                serviceStatusForSubscriberState = ServiceStatusForSubscriberState.InvalidContentWhenSubscribed;
            else
                serviceStatusForSubscriberState = ActivateServiceForSubscriber(message, subscriber, message.Content, service, serviceInfo);
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


        private static ServiceStatusForSubscriberState ActivateServiceForSubscriber(MessageObject message, Subscriber subscriber, string onKeyword, Service service, ServiceInfo serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.OffKeyword = null;
                subscriber.OffMethod = null;
                subscriber.DeactivationDate = null;
                subscriber.PersianDeactivationDate = null;
                subscriber.OnKeyword = onKeyword;
                if (message.IsReceivedFromIntegratedPanel != true && message.IsReceivedFromWeb != true)
                    subscriber.OnMethod = "keyword";
                else if (message.IsReceivedFromIntegratedPanel == true)
                    subscriber.OnMethod = "Integrated Panel";
                else
                    subscriber.OnMethod = "Web";
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
            }
            AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null, serviceInfo);
            return ServiceStatusForSubscriberState.Renewal;
        }

        private static ServiceStatusForSubscriberState AddNewSubscriberToService(MessageObject message, Service service, ServiceInfo serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                var newSubscriber = new Subscriber();
                newSubscriber.MobileNumber = message.MobileNumber;
                newSubscriber.OnKeyword = message.Content;
                if (message.IsReceivedFromIntegratedPanel != true && message.IsReceivedFromWeb != true)
                    newSubscriber.OnMethod = "keyword";
                else if (message.IsReceivedFromIntegratedPanel == true)
                    newSubscriber.OnMethod = "Integrated Panel";
                else
                    newSubscriber.OnMethod = "Web";
                newSubscriber.ServiceId = service.Id;
                newSubscriber.ActivationDate = DateTime.Now;
                newSubscriber.PersianActivationDate = Date.GetPersianDate();
                newSubscriber.MobileOperator = message.MobileOperator;
                newSubscriber.OperatorPlan = message.OperatorPlan;
                newSubscriber.SubscriberUniqueId = AssignUniqueId(newSubscriber.MobileNumber);
                entity.Subscribers.Add(newSubscriber);
                entity.SaveChanges();

                AddSubscriberToSubscriberPointsTable(newSubscriber, service);

                AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null, serviceInfo);
            }
            return ServiceStatusForSubscriberState.Activated;
        }

        public static string AssignUniqueId(string mobileNumber)
        {
            using (var entity = new PortalEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var subscriber = entity.Subscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber);
                string uniqueId = "";
                if (subscriber == null)
                    uniqueId = CreateUniqueId();
                else
                    uniqueId = subscriber.SubscriberUniqueId;
                return uniqueId;
            }
        }

        public static string CreateUniqueId()
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

        public static void AddSubscriberToSubscriberPointsTable(Subscriber newSubscriber, Service service)
        {
            using (var entity = new PortalEntities())
            {
                var subscriberPoint = new SubscribersPoint();
                subscriberPoint.ServiceId = service.Id;
                subscriberPoint.MobileNumber = newSubscriber.MobileNumber;
                subscriberPoint.Point = 0;
                entity.SubscribersPoints.Add(subscriberPoint);
                entity.SaveChanges();
            }
        }

        public static void AddToSubscriberHistory(MessageObject message, Service service, ServiceStatusForSubscriberState subscriberState, WhoChangedSubscriberState whoChangedSubscriberState, string invalidContent, ServiceInfo serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                int state;
                if (subscriberState == ServiceStatusForSubscriberState.Renewal)
                    state = (int)ServiceStatusForSubscriberState.Activated;
                else
                    state = (int)subscriberState;

                var subscriberHistory = new SubscribersHistory();
                subscriberHistory.MobileNumber = message.MobileNumber;
                subscriberHistory.Date = DateTime.Now;
                subscriberHistory.ServiceName = service.Name;
                subscriberHistory.ServiceId = service.Id;
                subscriberHistory.ServiceStatusForSubscriber = state;
                subscriberHistory.ShortCode = message.ShortCode;
                subscriberHistory.Time = DateTime.Now.TimeOfDay;
                if (message.IsReceivedFromIntegratedPanel != true && message.IsReceivedFromWeb != true)
                    subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.User;
                else if (message.IsReceivedFromIntegratedPanel == true)
                    subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.IntegratedPanel;
                else
                    subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.Web;

                subscriberHistory.AggregatorServiceId = serviceInfo.AggregatorServiceId;
                subscriberHistory.DateTime = DateTime.Now;
                subscriberHistory.PersianDateTime = Date.GetPersianDateTime();
                subscriberHistory.InvalidContent = invalidContent;
                subscriberHistory.AggregatorId = serviceInfo.AggregatorId;
                if (subscriberState == ServiceStatusForSubscriberState.Activated || subscriberState == ServiceStatusForSubscriberState.Renewal)
                    subscriberHistory.SubscriptionKeyword = message.Content;
                else
                    subscriberHistory.UnsubscriptionKeyword = message.Content;
                entity.SubscribersHistories.Add(subscriberHistory);
                entity.SaveChanges();
            }
        }

        public static ServiceStatusForSubscriberState Unsubscribe(MessageObject message, Service service, ServiceInfo serviceInfo)
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
                serviceStatusForSubscriberState = DeactivateServiceForSubscriber(message, service, subscriber, serviceInfo);

            return serviceStatusForSubscriberState;
        }

        private static ServiceStatusForSubscriberState DeactivateServiceForSubscriber(MessageObject message, Service service, Subscriber subscriber, ServiceInfo serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                subscriber.DeactivationDate = DateTime.Now;
                subscriber.PersianDeactivationDate = Date.GetPersianDate();
                subscriber.OffKeyword = message.Content;
                if (message.IsReceivedFromIntegratedPanel != true && message.IsReceivedFromWeb != true)
                    subscriber.OffMethod = "keyword";
                else if (message.IsReceivedFromIntegratedPanel == true)
                    subscriber.OffMethod = "Integrated Panel";
                else
                    subscriber.OffMethod = "Web";
                entity.Entry(subscriber).State = EntityState.Modified;
                entity.SaveChanges();
                AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Deactivated, WhoChangedSubscriberState.User, null, serviceInfo);
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
            Renewal = 5
        }

        public enum WhoChangedSubscriberState
        {
            User = 0,
            IntegratedPanel = 1,
            Web = 2
        }
    }
}