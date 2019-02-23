using System;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;
using System.Data.Entity.Validation;

namespace SharedLibrary
{
    public class SubscriptionFtpHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static bool SubscribeFtp(string mobileNumber, DateTime activationDate
            , string onKeyword, long mobileOperator, long operatorPlan, vw_servicesServicesInfo service)
        {
            Subscriber entrySubscriber;
            using (var entityPortal = new PortalEntities())
            {
                entrySubscriber = entityPortal.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == service.Id).FirstOrDefault();
                if (entrySubscriber == null)
                {
                    if (AddNewSubscriberToService(mobileNumber, activationDate, onKeyword, mobileOperator, operatorPlan, service))
                        return true;
                }
                else
                {
                    if (ActivateServiceForSubscriber(mobileNumber, activationDate, onKeyword, mobileOperator, operatorPlan, entrySubscriber
                        , service)) return true;
                }
            }

            return false;
        }

        private static bool ActivateServiceForSubscriber(string mobileNumber, DateTime activationDate
            , string onKeyword, long mobileOperator, long operatorPlan, Subscriber subscriber
            , vw_servicesServicesInfo service)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    subscriber.ActivationDate = activationDate;
                    subscriber.PersianActivationDate = SharedLibrary.Date.GetPersianDate(activationDate);
                    subscriber.OffKeyword = null;
                    subscriber.OffMethod = null;
                    subscriber.DeactivationDate = null;
                    subscriber.PersianDeactivationDate = null;
                    subscriber.OnKeyword = onKeyword;
                    subscriber.SpecialUniqueId = null;
                    subscriber.OnMethod = "FtpSync";
                    subscriber.UserMessage = onKeyword;

                    entity.Entry(subscriber).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ActivateServiceForSubscriber: ", e);
                return false;
            }
            AddToSubscriberHistory(mobileNumber, onKeyword, null, service, true);
            return true;
        }



        private static bool AddNewSubscriberToService(string mobileNumber, DateTime activationDate
            , string onKeyword, long mobileOperator, long operatorPlan, vw_servicesServicesInfo service)
        {
            var entrySubscriber = new Subscriber();
            try
            {
                using (var entityPortal = new PortalEntities())
                {
                    entrySubscriber = new Subscriber();
                    entrySubscriber.ActivationDate = activationDate;
                    entrySubscriber.DeactivationDate = null;
                    entrySubscriber.MobileNumber = mobileNumber;
                    entrySubscriber.MobileOperator = mobileOperator;
                    entrySubscriber.OffKeyword = null;
                    entrySubscriber.OffMethod = null;
                    entrySubscriber.OnKeyword = onKeyword;
                    entrySubscriber.OnMethod = "FtpSync";
                    entrySubscriber.OperatorPlan = operatorPlan;
                    entrySubscriber.PersianActivationDate = Date.GetPersianDate(activationDate);
                    entrySubscriber.ServiceId = service.Id;
                    entrySubscriber.UserMessage = onKeyword;

                    entityPortal.Subscribers.Add(entrySubscriber);
                    entityPortal.SaveChanges();

                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddNewSubscriberToService: ", e);
                return false;
            }
            AddSubscriberToSubscriberPointsTable(entrySubscriber, service);
            AddToSubscriberHistory(mobileNumber, onKeyword, null, service, true);
            return true;
        }

        public static string CreateSpecialUniqueId()
        {
            Random random = new Random();
            var unqiueId = random.Next(10000000, 99999999).ToString();
            using (var entity = new PortalEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var subsriber = entity.Subscribers.FirstOrDefault(o => o.SpecialUniqueId == unqiueId);
                if (subsriber != null)
                    unqiueId = CreateSpecialUniqueId();
            }
            return unqiueId;
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

        public static void AddSubscriberToSubscriberPointsTable(Subscriber newSubscriber, vw_servicesServicesInfo service)
        {
            try
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
            catch (Exception e)
            {
                logs.Error("Exception in AddSubscriberToSubscriberPointsTable: ", e);
            }
        }

        public static void AddToSubscriberHistory(string mobileNumber, string onKeyword, string offKeyword, vw_servicesServicesInfo service
            , bool active)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    int state;
                    if (active)
                        state = (int)SubscriptionHandler.ServiceStatusForSubscriberState.Activated;
                    else
                        state = (int)SubscriptionHandler.ServiceStatusForSubscriberState.Deactivated;

                    var subscriberHistory = new SubscribersHistory();
                    subscriberHistory.MobileNumber = mobileNumber;
                    subscriberHistory.Date = DateTime.Now;
                    subscriberHistory.ServiceName = service.Name;
                    subscriberHistory.ServiceId = service.Id;
                    subscriberHistory.UserMessage = (string.IsNullOrEmpty(onKeyword) ? onKeyword : "FtpSync" + onKeyword);
                    subscriberHistory.UserMessageOff = (string.IsNullOrEmpty(offKeyword) ? offKeyword : "FtpSync" + offKeyword); ;
                    subscriberHistory.ServiceStatusForSubscriber = state;
                    subscriberHistory.ShortCode = service.ShortCode;
                    subscriberHistory.Time = DateTime.Now.TimeOfDay;
                    subscriberHistory.WhoChangedSubscriberStatus = (int)SharedLibrary.SubscriptionHandler.WhoChangedSubscriberState.Ftp;
                    subscriberHistory.AggregatorServiceId = service.AggregatorServiceId;
                    subscriberHistory.DateTime = DateTime.Now;
                    subscriberHistory.PersianDateTime = Date.GetPersianDateTime();
                    subscriberHistory.InvalidContent = null;
                    subscriberHistory.AggregatorId = service.AggregatorId;
                    if (active)
                        subscriberHistory.SubscriptionKeyword = onKeyword;
                    else
                        subscriberHistory.UnsubscriptionKeyword = offKeyword;
                    entity.SubscribersHistories.Add(subscriberHistory);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddToSubscriberHistory: ", e);
            }
        }

        public static bool UnsubscribeFtp(string mobileNumber, DateTime deActivationDate
            , string offKeyword, long mobileOperator, long operatorPlan, vw_servicesServicesInfo service)
        {
            Subscriber subscriber;
            using (var entity = new PortalEntities())
            {
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == service.Id && o.DeactivationDate == null).FirstOrDefault();
            }
            if (subscriber == null)
            {
                return false;
            }
            else
                return DeactivateServiceForSubscriber(mobileNumber, deActivationDate, offKeyword, mobileOperator, operatorPlan, service, subscriber);

        }

        private static bool DeactivateServiceForSubscriber(string mobileNumber, DateTime deactivationDate
            , string offKeyword, long mobileOperator, long operatorPlan, vw_servicesServicesInfo service, Subscriber subscriber)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    if (subscriber.ActivationDate > deactivationDate)
                    {
                        if (subscriber.ActivationDate > DateTime.Now)
                        {
                            deactivationDate = subscriber.ActivationDate.Value.AddSeconds(1);
                        }
                        else
                        {
                            deactivationDate = DateTime.Now;
                        }
                    }
                    subscriber.DeactivationDate = deactivationDate;
                    subscriber.PersianDeactivationDate = Date.GetPersianDate(deactivationDate);
                    subscriber.OffKeyword = offKeyword;
                    subscriber.UserMessageOff = subscriber.UserMessage;
                    subscriber.OffMethod = "FtpSync";
                    entity.Entry(subscriber).State = EntityState.Modified;
                    entity.SaveChanges();
                    AddToSubscriberHistory(mobileNumber, null, offKeyword, service, false);
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        logs.Error(string.Format("{0}:{1}", validationErrors.Entry.Entity.ToString(), validationError.ErrorMessage));
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DeactivateServiceForSubscriber:", e);
                return false;
            }
            return true;
        }

    }
}