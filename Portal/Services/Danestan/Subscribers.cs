using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Services.Danestan
{
    public class Subscribers
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void AddSubscriptionPointIfItsFirstTime(string mobileNumber, long serviceId)
        {
            try
            {
                var subscriberId = Shared.HandleSubscription.GetSubscriberId(mobileNumber, serviceId);
                var subscriberAdditionalInfo = GetSubscriberAdditionalInfo(subscriberId.Value);
                if (subscriberAdditionalInfo.IsSubscriberReceivedSubscriptionPoint == true)
                    return;
                var pointsTable = GetPointsTableValues();
                var subscriptionPoint = pointsTable.Where(o => o.Title == "SubscriptionPoint").FirstOrDefault();
                Shared.MessageHandler.SetSubscriberPoint(subscriberId.Value, serviceId, subscriptionPoint.Point);
                using (var entity = new DanestanEntities())
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

        public static void AddSubscriptionOffReasonPoint(long subscriberId, long serviceId)
        {
            try
            {
                var subscriberAdditionalInfo = GetSubscriberAdditionalInfo(subscriberId);
                if (subscriberAdditionalInfo.IsSubscriberReceivedOffReasonPoint == true)
                    return;
                var pointsTable = GetPointsTableValues();
                var subscribtionPoint = pointsTable.Where(o => o.Title == "OffReasonPoint").FirstOrDefault();
                Shared.MessageHandler.SetSubscriberPoint(subscriberId, serviceId, subscribtionPoint.Point);
                using (var entity = new DanestanEntities())
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
                using (var entity = new DanestanEntities())
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
                using (var entity = new DanestanEntities())
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
                using (var entity = new DanestanEntities())
                {
                    var subscriberId = Shared.HandleSubscription.GetSubscriberId(mobileNumber, serviceId);
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
    }
}