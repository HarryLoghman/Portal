using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;

namespace SharedShortCodeServiceLibrary
{
    public class Subscribers
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void AddSubscriptionPointIfItsFirstTime(string connectionStringNameInAppConfig, string mobileNumber, long serviceId)
        {
            try
            {
                var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(mobileNumber, serviceId);
                var subscriberAdditionalInfo = GetSubscriberAdditionalInfo(connectionStringNameInAppConfig, subscriberId.Value);
                if (subscriberAdditionalInfo.IsSubscriberReceivedSubscriptionPoint == true)
                    return;
                var pointsTable = GetPointsTableValues(connectionStringNameInAppConfig);
                var subscriptionPoint = pointsTable.Where(o => o.Title == "SubscriptionPoint").FirstOrDefault();
                if (subscriptionPoint != null)
                {
                    SharedLibrary.MessageHandler.SetSubscriberPoint(mobileNumber, serviceId, subscriptionPoint.Point);
                    using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
                    {
                        entity.SubscribersAdditionalInfoes.Attach(subscriberAdditionalInfo);
                        subscriberAdditionalInfo.IsSubscriberReceivedSubscriptionPoint = true;
                        entity.Entry(subscriberAdditionalInfo).State = System.Data.Entity.EntityState.Modified;
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddSubscriptionPointIfItsFirstTime: " + e);
            }
        }


        public static List<PointsTable> GetPointsTableValues(string connectionStringNameInAppConfig)
        {
            var pointsTable = new List<PointsTable>();
            try
            {
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
                {
                    pointsTable = entity.PointsTables.ToList();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetPointsTableValues: " + e);
            }
            return pointsTable;
        }

        public static SubscribersAdditionalInfo GetSubscriberAdditionalInfo(string connectionStringNameInAppConfig, long subscriberId)
        {
            var subscriberAdditionalInfo = new SubscribersAdditionalInfo();
            try
            {
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

        public static void CreateSubscriberAdditionalInfo(string connectionStringNameInAppConfig, string mobileNumber, long serviceId)
        {
            try
            {
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

        public static void SetIsSubscriberSendedOffReason(string connectionStringNameInAppConfig, long subscriberId, bool value)
        {
            try
            {
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

    }
}