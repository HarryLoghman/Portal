using System;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;

namespace SharedLibrary
{
    public class HandleSubscription
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
            //else if (subscriber.DeactivationDate == null)
            //    serviceStatusForSubscriberState = ServiceStatusForSubscriberState.InvalidContentWhenSubscribed;
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
                    if (message.IsReceivedFromIntegratedPanel != true && message.IsReceivedFromWeb != true)
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
            AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null, serviceInfo);
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

        private static ServiceStatusForSubscriberState AddNewSubscriberToService(MessageObject message, Service service, ServiceInfo serviceInfo)
        {
            var newSubscriber = new Subscriber();
            try
            {
                using (var entity = new PortalEntities())
                {
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
            AddToSubscriberHistory(message, service, ServiceStatusForSubscriberState.Activated, WhoChangedSubscriberState.User, null, serviceInfo);
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
                    result = specialUniqeId;
                    sub.SpecialUniqueId = specialUniqeId;
                    entity.Entry(sub).State = EntityState.Modified;
                    entity.SaveChanges();
                    scope.Commit();
                }
            }
            return result;
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

        public static string AssignSpecialUniqueId(string mobileNumber)
        {
            using (var entity = new PortalEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var subscriber = entity.Subscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber);
                string uniqueId = "";
                uniqueId = CreateSpecialUniqueId();
                return uniqueId;
            }
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

        public static void AddSubscriberToSubscriberPointsTable(Subscriber newSubscriber, Service service)
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

        public static void AddToSubscriberHistory(MessageObject message, Service service, ServiceStatusForSubscriberState subscriberState, WhoChangedSubscriberState whoChangedSubscriberState, string invalidContent, ServiceInfo serviceInfo)
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
                    var otherWayAroundExits = entity.Referrals.FirstOrDefault(o => o.InviterUniqueId == inviteeUniqueId && o.InviteeUniqueId == o.InviterUniqueId);
                    if (otherWayAroundExits != null)
                    {
                        entity.Referrals.Remove(otherWayAroundExits);
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

        public static string IsSubscriberInvited(string mobileNumber, long serviceId)
        {
            string result = "";
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
            try
            {
                using (var entity = new PortalEntities())
                {
                    subscriber.DeactivationDate = DateTime.Now;
                    subscriber.PersianDeactivationDate = Date.GetPersianDate();
                    subscriber.OffKeyword = message.Content;
                    subscriber.UserMessageOff = subscriber.UserMessage;
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

        public static void ChangeSubscriptionKeyword(Subscriber subscriber, string keyword)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var sub = entity.Subscribers.FirstOrDefault(o => o.Id == subscriber.Id);
                    sub.OnKeyword = keyword;
                    entity.Entry(sub).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChangeSubscriptionKeyword: " + e);
            }
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

        public static void UnsubscribeUserFromHubService(long serviceId, string mobileNumber)
        {
            //try
            //{
            //    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceId, "Hub");

            //    XmlDocument doc = new XmlDocument();
            //    XmlElement root = doc.CreateElement("xmsrequest");
            //    XmlElement userid = doc.CreateElement("userid");
            //    XmlElement password = doc.CreateElement("password");
            //    XmlElement action = doc.CreateElement("action");
            //    XmlElement body = doc.CreateElement("body");
            //    XmlElement recipient = doc.CreateElement("recipient");
            //    recipient.InnerText = mobileNumber;
            //    XmlElement serviceid = doc.CreateElement("serviceid");
            //    serviceid.InnerText = serviceAdditionalInfo["aggregatorServiceId"];
            //    body.AppendChild(serviceid);
            //    body.AppendChild(recipient);

            //    //Random random = new Random();
            //    //var randomNumber = random.Next(1000000, 9999999);
            //    //XmlAttribute doerId = doc.CreateAttribute("doerId");
            //    //doerId.InnerText = randomNumber.ToString();
            //    //recipient.Attributes.Append(doerId);

            //    userid.InnerText = serviceAdditionalInfo["username"];
            //    password.InnerText = serviceAdditionalInfo["password"];
            //    action.InnerText = "crm";

            //    doc.AppendChild(root);
            //    root.AppendChild(userid);
            //    root.AppendChild(password);
            //    root.AppendChild(action);
            //    root.AppendChild(body);
            //    string stringedXml = doc.OuterXml;
            //    logs.Info("hub cancel api request: " + stringedXml);
            //    SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();                
            //    string response = hubClient.XmsRequest(stringedXml).ToString();
            //    logs.Info("hub cancel api response: " + response);
            //}
            //catch (Exception e)
            //{
            //    logs.Error("Exception in UnsubscribeUserFromHubService: " + e);
            //}
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