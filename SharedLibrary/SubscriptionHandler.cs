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
    public class SubscriptionHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static ServiceStatusForSubscriberState HandleSubscriptionContent(MessageObject message, vw_servicesServicesInfo service, bool isUserWantsToUnsubscribe)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            message.MessageType = (int)MessageHandler.MessageType.OnDemand;

            if (isUserWantsToUnsubscribe == true)
                serviceStatusForSubscriberState = Unsubscribe(message, service);
            else
                serviceStatusForSubscriberState = Subscribe(message, service);
            return serviceStatusForSubscriberState;
        }

        public static ServiceStatusForSubscriberState Subscribe(MessageObject message, vw_servicesServicesInfo service)
        {
            var serviceStatusForSubscriberState = ServiceStatusForSubscriberState.Unspecified;
            Subscriber subscriber;
            using (var entity = new PortalEntities())
            {
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && o.ServiceId == service.Id).FirstOrDefault();
            }
            if (subscriber == null)
                serviceStatusForSubscriberState = AddNewSubscriberToService(message, service);
            //else if (subscriber.DeactivationDate == null)
            //    serviceStatusForSubscriberState = ServiceStatusForSubscriberState.InvalidContentWhenSubscribed;
            else
                serviceStatusForSubscriberState = ActivateServiceForSubscriber(message, subscriber, message.Content, service);
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


        private static ServiceStatusForSubscriberState ActivateServiceForSubscriber(MessageObject message, Subscriber subscriber
            , string onKeyword, vw_servicesServicesInfo service)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    subscriber.ActivationDate = DateTime.Now;
                    subscriber.PersianActivationDate = SharedLibrary.Date.GetPersianDate(DateTime.Now);
                    subscriber.OffKeyword = null;
                    subscriber.OffMethod = null;
                    subscriber.DeactivationDate = null;
                    subscriber.PersianDeactivationDate = null;
                    subscriber.OnKeyword = onKeyword;
                    subscriber.SpecialUniqueId = null;
                    if (message.IsReceivedFromIntegratedPanel != true && (!message.ReceivedFromSource.HasValue || message.ReceivedFromSource.Value == 0))
                        subscriber.OnMethod = "keyword";
                    else if (message.IsReceivedFromIntegratedPanel == true)
                        subscriber.OnMethod = "Integrated Panel";
                    else
                        subscriber.OnMethod = "Web";
                    if (message.Content == null || (message.Content.Length == 1 && message.Content != "0") || message.Content.Length >= 10)
                    {
                        subscriber.UserMessage = message.Content;
                        message.UserMessage = message.Content;
                    }
                    else
                    {
                        var recievedMessages = entity.ReceievedMessages.Where(o => o.MobileNumber == message.MobileNumber && o.ShortCode == message.ShortCode).OrderByDescending(o => o.ReceivedTime).Skip(1).Take(10).ToList();
                        foreach (var item in recievedMessages)
                        {
                            if (item.Content.Length != 4)
                            {
                                subscriber.UserMessage = item.Content;
                                message.UserMessage = item.Content;
                                break;
                            }
                        }
                    }
                    entity.Entry(subscriber).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ActivateServiceForSubscriber: ", e);
            }
            AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null);
            return ServiceStatusForSubscriberState.Renewal;
        }

        public static void AddToTempReferral(string mobileNumber, long serviceId, string inviterUniqueId)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var temp = new TempReferralData();
                    temp.InviteeMobileNumber = mobileNumber;
                    temp.InviterUniqueId = inviterUniqueId;
                    temp.ServiceId = serviceId;
                    temp.DateCreated = DateTime.Now;
                    entity.TempReferralDatas.Add(temp);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddToTempReferral: ", e);
            }
        }

        private static ServiceStatusForSubscriberState AddNewSubscriberToService(MessageObject message, vw_servicesServicesInfo service)
        {
            var newSubscriber = new Subscriber();
            try
            {
                using (var entity = new PortalEntities())
                {
                    newSubscriber.MobileNumber = message.MobileNumber;
                    newSubscriber.OnKeyword = message.Content;
                    if (message.IsReceivedFromIntegratedPanel != true && (!message.ReceivedFromSource.HasValue || message.ReceivedFromSource.Value == 0))
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
                    //Random random = new Random();
                    //var subUniqueId = "";
                    //bool isUniqueIdAssigned = false;
                    //while (isUniqueIdAssigned == false)
                    //{
                    //    var uId = random.Next(10000000, 99999999).ToString();
                    //    var subscriber = entity.Subscribers.Where(o => o.SubscriberUniqueId == uId).Select(o => o.MobileNumber).FirstOrDefault();
                    //    if (subscriber == null)
                    //    {
                    //        subUniqueId = uId;
                    //        isUniqueIdAssigned = true;
                    //    }
                    //}
                    newSubscriber.SubscriberUniqueId = "";
                    newSubscriber.SpecialUniqueId = null;
                    if (message.Content == null || (message.Content.Length == 1 && message.Content != "0") || message.Content.Length >= 10)
                    {
                        newSubscriber.UserMessage = message.Content;
                        message.UserMessage = message.Content;
                    }
                    else
                    {
                        var recievedMessages = entity.ReceievedMessages.Where(o => o.MobileNumber == message.MobileNumber && o.ShortCode == message.ShortCode).OrderByDescending(o => o.ReceivedTime).Skip(1).Take(10).ToList();
                        foreach (var item in recievedMessages)
                        {
                            if (item.Content.Length != 4)
                            {
                                newSubscriber.UserMessage = item.Content;
                                message.UserMessage = item.Content;
                                break;
                            }
                        }
                    }
                    entity.Subscribers.Add(newSubscriber);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddNewSubscriberToService: ", e);
            }
            AddSubscriberToSubscriberPointsTable(newSubscriber, service);
            AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null);
            return ServiceStatusForSubscriberState.Activated;
        }

        public static bool IsSubscriberActive(string mobileNumber, string serviceIdString)
        {
            bool result = false;
            try
            {
                long serviceId = Convert.ToInt64(serviceIdString);
                using (var entity = new PortalEntities())
                {
                    var sub = entity.Subscribers.AsNoTracking().FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId && o.DeactivationDate == null);
                    if (sub == null)
                        result = false;
                    else
                        result = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsSubscriberActive:", e);
            }
            return true;// result;
        }

        /// <summary>
        /// create a special uniqueId for the mobilenumber in serviceid and save to DB
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <param name="serviceId"></param>
        /// <returns></returns>
        public static string CampaignUniqueId(string mobileNumber, long serviceId)
        {
            var result = "1";
            using (var entity = new PortalEntities())
            {
                using (DbContextTransaction scope = entity.Database.BeginTransaction())
                {
                    entity.Database.ExecuteSqlCommand("SELECT TOP 0 NULL FROM Portal.dbo.Subscribers WITH (TABLOCKX)");
                    var sub = entity.Subscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                    if (sub == null)
                        return result;
                    var specialUniqeId = "";
                    Random random = new Random();
                    bool isUniqueIdAssigned = false;
                    #region find uniqueID for user
                    while (isUniqueIdAssigned == false)
                    {

                        var unqiueId = random.Next(10000000, 99999999).ToString();
                        var subscriber = entity.Subscribers.FirstOrDefault(o => o.SpecialUniqueId == unqiueId && o.ServiceId == serviceId);
                        if (subscriber == null)
                        {
                            specialUniqeId = unqiueId;
                            isUniqueIdAssigned = true;
                        }
                    }
                    #endregion
                    result = specialUniqeId;
                    sub.SpecialUniqueId = specialUniqeId;
                    entity.Entry(sub).State = EntityState.Modified;
                    entity.SaveChanges();
                    scope.Commit();
                }
            }
            return result;
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

        public static void AddToSubscriberHistory(MessageObject message, vw_servicesServicesInfo service, ServiceStatusForSubscriberState subscriberState, WhoChangedSubscriberState whoChangedSubscriberState, string invalidContent)
        {
            try
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
                    subscriberHistory.UserMessage = message.UserMessage;
                    subscriberHistory.UserMessageOff = message.UserMessageOff;
                    subscriberHistory.ServiceStatusForSubscriber = state;
                    subscriberHistory.ShortCode = message.ShortCode;
                    subscriberHistory.Time = DateTime.Now.TimeOfDay;
                    if (message.IsReceivedFromIntegratedPanel != true && (!message.ReceivedFromSource.HasValue || message.ReceivedFromSource.Value == 0))
                        subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.User;
                    else if (message.IsReceivedFromIntegratedPanel == true)
                        subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.IntegratedPanel;
                    else
                        subscriberHistory.WhoChangedSubscriberStatus = (int)WhoChangedSubscriberState.Web;

                    subscriberHistory.AggregatorServiceId = service.AggregatorServiceId;
                    subscriberHistory.DateTime = DateTime.Now;
                    subscriberHistory.PersianDateTime = Date.GetPersianDateTime();
                    subscriberHistory.InvalidContent = invalidContent;
                    subscriberHistory.AggregatorId = service.AggregatorId;
                    if (subscriberState == ServiceStatusForSubscriberState.Activated || subscriberState == ServiceStatusForSubscriberState.Renewal)
                        subscriberHistory.SubscriptionKeyword = message.Content;
                    else
                        subscriberHistory.UnsubscriptionKeyword = message.Content;
                    entity.SubscribersHistories.Add(subscriberHistory);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddToSubscriberHistory: ", e);
            }
        }

        public static void AddReferral(string inviterUniqueId, string inviteeUniqueId)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var inviteeInviteInviterBefore = entity.Referrals.FirstOrDefault(o => o.InviterUniqueId == inviteeUniqueId && o.InviteeUniqueId == o.InviterUniqueId);
                    if (inviteeInviteInviterBefore != null)
                    {
                        entity.Referrals.Remove(inviteeInviteInviterBefore);
                        entity.SaveChanges();
                    }
                    var isReferralExists = entity.Referrals.FirstOrDefault(o => o.InviterUniqueId == inviterUniqueId && o.InviteeUniqueId == inviteeUniqueId);
                    if (isReferralExists == null)
                    {
                        var referral = new Referral();
                        referral.InviterUniqueId = inviterUniqueId;
                        referral.InviteeUniqueId = inviteeUniqueId;
                        referral.DateInvited = DateTime.Now;
                        referral.PersianDateInvited = SharedLibrary.Date.GetPersianDateTime();
                        entity.Referrals.Add(referral);
                        entity.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    logs.Error(string.Format("Error in AddReferral: Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        logs.Error(string.Format("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage));
                    }
                }
                throw;
            }
            catch (Exception e)
            {
                logs.Error("Error in AddReferral: " + e);
            }
        }

        public static Subscriber GetSubscriberBySpecialUniqueId(string specialUniqueId)
        {
            string result = "";
            try
            {
                using (var entity = new PortalEntities())
                {
                    var subscriber = entity.Subscribers.Where(o => o.SpecialUniqueId == specialUniqueId).FirstOrDefault();
                    if (subscriber != null)
                    {
                        return subscriber;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetSubscriberBySpecialUniqueId: ", e);
            }
            return null;
        }

        public static SubscribersHistory GetLastInsertedSubscriberHistory(string mobileNumber, long serviceId)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var subscriber = entity.SubscribersHistories.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).OrderByDescending(o => o.Id).FirstOrDefault();
                    return subscriber;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetLastInsertedSubscriberHistory: ", e);
            }
            return null;
        }

        public static string IsSubscriberInvited(string mobileNumber, long serviceId)
        {
            string result = "1";
            try
            {
                using (var entity = new PortalEntities())
                {
                    var temp = entity.TempReferralDatas.Where(o => o.InviteeMobileNumber == mobileNumber && o.ServiceId == serviceId).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                    if (temp != null)
                    {
                        result = temp.InviterUniqueId;
                        entity.TempReferralDatas.Remove(temp);
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsSubscriberInvited: ", e);
            }
            return result;
        }

        public static ServiceStatusForSubscriberState Unsubscribe(MessageObject message, vw_servicesServicesInfo service)
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

        private static ServiceStatusForSubscriberState DeactivateServiceForSubscriber(MessageObject message, vw_servicesServicesInfo service, Subscriber subscriber)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    subscriber.DeactivationDate = DateTime.Now;
                    subscriber.PersianDeactivationDate = Date.GetPersianDate();
                    subscriber.OffKeyword = message.Content;
                    subscriber.UserMessageOff = subscriber.UserMessage;
                    message.UserMessageOff = subscriber.UserMessageOff;
                    if (message.IsReceivedFromIntegratedPanel != true && (!message.ReceivedFromSource.HasValue || message.ReceivedFromSource == 0))
                        subscriber.OffMethod = "keyword";
                    else if (message.IsReceivedFromIntegratedPanel == true)
                        subscriber.OffMethod = "Integrated Panel";
                    else
                        subscriber.OffMethod = "Web";
                    entity.Entry(subscriber).State = EntityState.Modified;
                    entity.SaveChanges();
                    AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Deactivated, WhoChangedSubscriberState.User, null);
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
            }
            catch (Exception e)
            {
                logs.Error("Exception in DeactivateServiceForSubscriber:", e);
            }
            return ServiceStatusForSubscriberState.Deactivated;
        }
        public static void UnsubscribeUserFromTelepromoService(long serviceId, string mobileNumber)
        {
            //var url = "http://10.20.9.135:8600/samsson-sdp/pin/cancel?";
            //var sc = "Dehnad";
            //var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceId, "Telepromo");
            //var username = serviceAdditionalInfo["username"];
            //var password = serviceAdditionalInfo["password"];
            //var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
            //var msisdn = "98" + mobileNumber.TrimStart('0');
            //var urlWithParameters = url + String.Format("sc={0}&msisdn={1}&serviceId={2}&username={3}&password={4}", sc, msisdn, aggregatorServiceId, username, password);
            //logs.Info("telepromo cancel api request: " + urlWithParameters);
            //try
            //{
            //    using (var client = new HttpClient())
            //    {
            //        using (var response = client.GetAsync(new Uri(urlWithParameters)).Result)
            //        {
            //            if (response.IsSuccessStatusCode)
            //                logs.Info("telepromo cancel api response: " + response.Content.ReadAsStringAsync().Result);
            //            else
            //                logs.Info("telepromo cancel api response: Error " + response.StatusCode);
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    logs.Error("Exception in UnsubscribeUserFromTelepromoService: " + e);
            //}
        }

        public static bool consumeAppCharge(string requesterIP, string serviceCode, string appName, string mobileNumber, int? price
            , out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";


            if (!price.HasValue || price <= 0)
            {
                errorType = "Invalid Price";
                errorDescription = "";
                return false;
            }
            int totalCharged;
            int totalConsumed;

            int remain = getRemainAppCharge(serviceCode, appName, mobileNumber, out totalCharged, out totalConsumed, out errorType, out errorDescription);
            if (!string.IsNullOrEmpty(errorType))
            {
                return false;
            }
            if (price > remain)
            {
                errorType = "OverCharge";
                errorDescription = "Remain Charge is " + remain;
                return false;
            }
            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(serviceCode))
            {
                SharedLibrary.Models.ServiceModel.SingleChargeAppsConsume entryChargeApp = new Models.ServiceModel.SingleChargeAppsConsume();
                entryChargeApp.appName = appName;
                entryChargeApp.DateCreated = DateTime.Now;
                entryChargeApp.Description = null;
                entryChargeApp.MobileNumber = mobileNumber;
                entryChargeApp.PersianDateCreated = Date.GetPersianDate(DateTime.Now);
                entryChargeApp.Price = price.Value;
                entryChargeApp.requesterIP = requesterIP;


                entityService.SingleChargeAppsConsumes.Add(entryChargeApp);
                entityService.SaveChanges();
            }
            return true;
        }

        public static int getRemainAppCharge(string serviceCode, string appName, string mobileNumber
            , out int totalCharged, out int totalConsumed, out string errorType, out string errorDescription)
        {
            totalCharged = 0;
            totalConsumed = 0;
            errorType = "";
            errorDescription = "";

            mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumber);

            if (mobileNumber == "Invalid Mobile Number")
            {
                errorType = "Invalid Mobile Number";
                errorDescription = mobileNumber;
                return 0;
            }
            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
            if (service == null)
            {
                errorType = "Invalid Service Code";
                errorDescription = serviceCode;
                return 0;
            }

            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(serviceCode))
            {
                totalCharged = entityService.Singlecharges.Where(o => o.MobileNumber == mobileNumber && o.IsCalledFromInAppPurchase && o.IsSucceeded).Sum(o => (int?)o.Price) ?? 0;
                totalCharged += entityService.SinglechargeArchives.Where(o => o.MobileNumber == mobileNumber && o.IsCalledFromInAppPurchase && o.IsSucceeded).Sum(o => (int?)o.Price) ?? 0;
                totalConsumed = entityService.SingleChargeAppsConsumes.Where(o => o.MobileNumber == mobileNumber && o.appName == appName).Sum(o => (int?)o.Price) ?? 0;
            }
            return totalCharged - totalConsumed;
        }

        public static void getAppChargeDetail(string serviceCode, string appName, string mobileNumber
            , out Dictionary<DateTime, int> dicCharged, out Dictionary<DateTime, int> dicConsumed, out string errorType, out string errorDescription)
        {
            dicCharged = new Dictionary<DateTime, int>();
            dicConsumed = new Dictionary<DateTime, int>();
            errorType = "";
            errorDescription = "";

            mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumber);

            if (mobileNumber == "Invalid Mobile Number")
            {
                errorType = "Invalid Mobile Number";
                errorDescription = mobileNumber;
                return;
            }
            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
            if (service == null)
            {
                errorType = "Invalid Service Code";
                errorDescription = serviceCode;
                return;
            }
            
            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(serviceCode))
            {
                var f = (from o in entityService.Singlecharges
                         where o.MobileNumber == mobileNumber && o.IsCalledFromInAppPurchase && o.IsSucceeded
                         select new { o.DateCreated , o.Price})
                         .Union
                         (from o in entityService.SinglechargeArchives
                          where o.MobileNumber == mobileNumber && o.IsCalledFromInAppPurchase && o.IsSucceeded
                          select new { o.DateCreated, o.Price });
                dicCharged = f.OrderByDescending(o => o.DateCreated).ToDictionary(o => o.DateCreated, o => o.Price);
                //dicCharged = entityService.vw_Singlecharge.Where(o => o.MobileNumber == mobileNumber && o.IsCalledFromInAppPurchase && o.IsSucceeded).OrderByDescending(o => o.DateCreated).ToDictionary(o => o.DateCreated, o => o.Price);

                dicConsumed = entityService.SingleChargeAppsConsumes.Where(o => o.MobileNumber == mobileNumber && o.appName == appName).OrderByDescending(o => o.DateCreated).ToDictionary(o => o.DateCreated, o => o.Price);
            }

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
            Web = 2,
            Ftp = 3
        }
    }
}