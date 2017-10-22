using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SharedLibrary.Models;
using MusicYadLibrary.Models;

namespace MusicYadLibrary
{
    public class Subscribers
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void AddSubscriptionPointIfItsFirstTime(string mobileNumber, long serviceId)
        {
            try
            {
                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(mobileNumber, serviceId);
                var subscriberAdditionalInfo = GetSubscriberAdditionalInfo(subscriberId.Value);
                if (subscriberAdditionalInfo.IsSubscriberReceivedSubscriptionPoint == true)
                    return;
                var pointsTable = GetPointsTableValues();
                var subscriptionPoint = pointsTable.Where(o => o.Title == "SubscriptionPoint").FirstOrDefault();
                SharedLibrary.MessageHandler.SetSubscriberPoint(mobileNumber, serviceId, subscriptionPoint.Point);
                using (var entity = new MusicYadEntities())
                {
                    entity.SubscribersAdditionalInfoes.Attach(subscriberAdditionalInfo);
                    subscriberAdditionalInfo.IsSubscriberReceivedSubscriptionPoint = true;
                    entity.Entry(subscriberAdditionalInfo).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddSubscriptionPointIfItsFirstTime: " + e);
            }
        }

        public static void AddSubscriptionOffReasonPoint(Subscriber subscriber, long serviceId)
        {
            try
            {
                var subscriberAdditionalInfo = GetSubscriberAdditionalInfo(subscriber.Id);
                if (subscriberAdditionalInfo.IsSubscriberReceivedOffReasonPoint == true)
                    return;
                var pointsTable = GetPointsTableValues();
                var subscribtionPoint = pointsTable.Where(o => o.Title == "OffReasonPoint").FirstOrDefault();
                SharedLibrary.MessageHandler.SetSubscriberPoint(subscriber.MobileNumber, serviceId, subscribtionPoint.Point);
                using (var entity = new MusicYadEntities())
                {
                    entity.SubscribersAdditionalInfoes.Attach(subscriberAdditionalInfo);
                    subscriberAdditionalInfo.IsSubscriberReceivedOffReasonPoint = true;
                    entity.Entry(subscriberAdditionalInfo).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }

            }
            catch (Exception e)
            {
                logs.Error("Error in AddSubscriptionOffReasonPoint: " + e);
            }
        }

        public static List<PointsTable> GetPointsTableValues()
        {
            var pointsTable = new List<PointsTable>();
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    pointsTable = entity.PointsTables.ToList();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddSubscriptionPointIfItsFirstTime: " + e);
            }
            return pointsTable;
        }

        public static SubscribersAdditionalInfo GetSubscriberAdditionalInfo(long subscriberId)
        {
            var subscriberAdditionalInfo = new SubscribersAdditionalInfo();
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    subscriberAdditionalInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetSubscriberAdditionalInfo: " + e);
            }
            return subscriberAdditionalInfo;
        }

        public static void CreateSubscriberAdditionalInfo(string mobileNumber, long serviceId)
        {
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(mobileNumber, serviceId);
                    var subscriberAdditionalInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                    if (subscriberAdditionalInfo == null)
                    {
                        var subscriberAdditionalInfoObj = new SubscribersAdditionalInfo();
                        subscriberAdditionalInfoObj.SubscriberId = subscriberId.Value;
                        entity.SubscribersAdditionalInfoes.Add(subscriberAdditionalInfoObj);
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CreateSubscriberAdditionalInfo: " + e);
            }
        }

        public static void SetIsSubscriberSendedOffReason(long subscriberId, bool value)
        {
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    var subscriberAdditionalInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                    subscriberAdditionalInfo.IsSubscriberSendedOffReason = value;
                    entity.Entry(subscriberAdditionalInfo).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SetIsSubscriberSendedOffReason: " + e);
            }
        }

        public static bool GetIsSubscriberSendedOffReason(long subscriberId)
        {
            bool result = false;
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    var isSubscriberSendedOffReason = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId).IsSubscriberSendedOffReason;
                    if (isSubscriberSendedOffReason == true)
                        result = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetIsSubscriberSendedOffReason: " + e);
            }
            return result;
        }
    }
}