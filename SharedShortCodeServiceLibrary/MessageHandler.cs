using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using SharedLibrary.Models;
using SharedShortCodeServiceLibrary.SharedModel;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using static SharedShortCodeServiceLibrary.HandleMo;

namespace SharedShortCodeServiceLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static long OtpLog(string connectionStringNameInAppConfig, string mobileNumber, string otpType, string userMessage)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    var otpLog = new Otp()
                    {
                        MobileNumber = mobileNumber,
                        UserMessage = userMessage,
                        Type = otpType,
                        DateCreated = DateTime.Now,
                    };
                    entity.Otps.Add(otpLog);
                    entity.SaveChanges();
                    return otpLog.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in OtpLog: ", e);
            }
            return 0;
        }
        public static void OtpLogUpdate(string connectionStringNameInAppConfig, long otpId, string returnValue)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    var otp = entity.Otps.FirstOrDefault(o => o.Id == otpId);
                    if (otp != null)
                    {
                        otp.ReturnValue = returnValue;
                        entity.Entry(otp).State = System.Data.Entity.EntityState.Modified;
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in OtpLogUpdate: ", e);
            }
        }

        public static MessageObject SetImiChargeInfo(ShortCodeServiceEntities entity, ImiChargeCode imiChargeCode, MessageObject message, int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            if (subscriberState == null && price > 0)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price);
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Register");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
            else
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Free");

            if (imiChargeCode != null)
            {
                message.ImiChargeCode = imiChargeCode.ChargeCode;
                message.ImiChargeKey = imiChargeCode.ChargeKey;
                message.ImiMessageType = messageType;
                message.Price = price;
            }
            return message;
        }

        public static MessageObject InvalidContentWhenSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenNotSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertMessageToQueue(string connectionStringNameInAppConfig, MessageObject message)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                message.Content = HandleSpecialStrings(message.Content, message.Point, message.MobileNumber, message.ServiceId);
                if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
                {
                    var messageBuffer = CreateAutochargeMessageBuffer(message);
                    entity.AutochargeMessagesBuffers.Add(messageBuffer);
                }
                else if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase)
                {
                    var messageBuffer = CreateEventbaseMessageBuffer(message);
                    entity.EventbaseMessagesBuffers.Add(messageBuffer);
                }
                else
                {
                    var messageBuffer = CreateOnDemandMessageBuffer(message);
                    entity.OnDemandMessagesBuffers.Add(messageBuffer);
                }
                entity.SaveChanges();
            }
        }

        public static MessageObject SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscribedUserSendKeyword").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceHelp(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertBulkMessagesToQueue(string connectionStringNameInAppConfig, List<MessageObject> messages)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                int counter = 0;
                foreach (var message in messages)
                {
                    if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
                    {
                        var messageBuffer = CreateAutochargeMessageBuffer(message);
                        entity.AutochargeMessagesBuffers.Add(messageBuffer);
                    }
                    else if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase)
                    {
                        var messageBuffer = CreateEventbaseMessageBuffer(message);
                        entity.EventbaseMessagesBuffers.Add(messageBuffer);
                    }
                    else
                    {
                        var messageBuffer = CreateOnDemandMessageBuffer(message);
                        entity.OnDemandMessagesBuffers.Add(messageBuffer);
                    }
                    if (counter > 1000)
                    {
                        counter = 0;
                        entity.SaveChanges();
                    }
                    counter++;
                }
                entity.SaveChanges();
            }
        }

        public static string AddAutochargeHeaderAndFooter(string connectionStringNameInAppConfig, string autochargeContent)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    var autochargeHeaderAndFooter = entity.AutochargeHeaderFooters.FirstOrDefault();
                    if (autochargeHeaderAndFooter != null)
                    {
                        if (autochargeHeaderAndFooter.Header != null && autochargeHeaderAndFooter.Header != "")
                            autochargeContent = autochargeHeaderAndFooter.Header + Environment.NewLine + autochargeContent;
                        if (autochargeHeaderAndFooter.Footer != null && autochargeHeaderAndFooter.Footer != "")
                            autochargeContent += Environment.NewLine + autochargeHeaderAndFooter.Footer;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddAutochargeHeaderAndFooter: " + e);
            }
            return autochargeContent;
        }

        public static MessageObject UserHasActiveSinglecharge(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "UserHasActiveSinglecharge").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void SetOffReason(string connectionStringNameInAppConfig, Subscriber subscriber, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                var offReason = new ServiceOffReason();
                offReason.SubscriberId = subscriber.Id;
                offReason.Reason = message.Content;
                entity.ServiceOffReasons.Add(offReason);
                entity.SaveChanges();
                message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
                InsertMessageToQueue(connectionStringNameInAppConfig, message);
            }
        }

        public static string HandleSpecialStrings(string content, int point, string mobileNumber, long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                if (content == null || content == "")
                    return content;
                if (content.Contains("{SPOINT}"))
                {
                    var subscriberPoint = SharedLibrary.MessageHandler.GetSubscriberPoint(mobileNumber, serviceId);
                    subscriberPoint += point;
                    content = content.Replace("{SPOINT}", subscriberPoint.ToString());
                }
                if (content.Contains("{TPOINT}"))
                {
                    var subscriberPoint = SharedLibrary.MessageHandler.GetSubscriberPoint(mobileNumber, null);
                    subscriberPoint += point;
                    content = content.Replace("{TPOINT}", subscriberPoint.ToString());
                }
                if (content.Contains("{REFERRALCODE}"))
                {
                    var subId = "1";
                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(mobileNumber, serviceId);
                    if (sub != null)
                        subId = sub.SpecialUniqueId;
                    content = content.Replace("{REFERRALCODE}", subId);
                }
            }
            return content;
        }

        public static OnDemandMessagesBuffer CreateOnDemandMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new OnDemandMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessagePoint = message.Point;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = (message.SubUnSubType == null) ? 0 : message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static EventbaseMessagesBuffer CreateEventbaseMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new EventbaseMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessagePoint = message.Point;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = (message.SubUnSubType == null) ? 0 : message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static AutochargeMessagesBuffer CreateAutochargeMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new AutochargeMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessagePoint = message.Point;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = (message.SubUnSubType == null) ? 0 : message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static ImiChargeCode GetImiChargeObjectFromPrice(string connectionStringNameInAppConfig, int price, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                ImiChargeCode imiChargeCode;
                if (subscriberState == null && price > 0)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Register");
                else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
                else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
                else
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Free");
                return imiChargeCode;
            }
        }

        public static string GetImiChargeKeyFromPrice(string connectionStringNameInAppConfig, int price)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                var imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                return imiChargeCode.ChargeKey;
            }
        }

        public static MessageObject InvalidContentWhenNotSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void OffReasonResponse(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(connectionStringNameInAppConfig, message);
        }

        public static List<OnDemandMessagesBuffer> GetUnprocessedOnDemandMessages(ShortCodeServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).Take(readSize).ToList();
        }

        public static List<EventbaseMessagesBuffer> GetUnprocessedEventbaseMessages(ShortCodeServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static List<AutochargeMessagesBuffer> GetUnprocessedAutochargeMessages(ShortCodeServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static void SetEventbaseStatus(string connectionStringNameInAppConfig, long eventbaseId)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.Id == eventbaseId);
                if (eventbaseContent != null)
                {
                    eventbaseContent.IsAddedToSendQueueFinished = true;
                    entity.Entry(eventbaseContent).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
        }

        public static void AddEventbaseMessagesToQueue(string connectionStringNameInAppConfig, string serviceCode, EventbaseContent eventbaseContent, long aggregatorId)
        {
            //var serviceCode = "Tamly500";
            var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
            if (serviceId == null)
            {
                logs.Info("There is no service with code of : " + serviceCode);
                return;
            }
            var messagePoint = eventbaseContent.Point;
            var today = DateTime.Now.Date;
            var dateDiffrence = DateTime.Now.AddDays(-eventbaseContent.SubscriberNotSendedMoInDays).Date;

            IEnumerable<Subscriber> subscribers;
            using (var entity = new PortalEntities())
            {
                if (eventbaseContent.SubscriberNotSendedMoInDays == 0)
                    subscribers = entity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).ToList();
                else
                    subscribers = (from s in entity.Subscribers join r in entity.ReceivedMessagesArchives on s.MobileNumber equals r.MobileNumber where s.ServiceId == serviceId && s.DeactivationDate == null && (DbFunctions.TruncateTime(r.ReceivedTime).Value >= dateDiffrence && DbFunctions.TruncateTime(r.ReceivedTime).Value <= today) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
            }
            if (subscribers == null)
            {
                logs.Info("There is no subscribers for service with code of: " + serviceCode);
                return;
            }
            logs.Info("Eventbase subscribers count:" + subscribers.Count());
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                var messages = new List<MessageObject>();
                var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(connectionStringNameInAppConfig, eventbaseContent.Price, null);
                foreach (var subscriber in subscribers)
                {
                    var content = eventbaseContent.Content;
                    content = HandleSpecialStrings(content, eventbaseContent.Point, subscriber.MobileNumber, serviceId.Value);
                    var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, eventbaseContent.Point, null, eventbaseContent.Price);
                    messages.Add(message);
                }
                logs.Info("Evenbase messages count:" + messages.Count());
                InsertBulkMessagesToQueue(connectionStringNameInAppConfig, messages);
                eventbaseContent.IsAddedToSendQueueFinished = true;
                entity.Entry(eventbaseContent).State = EntityState.Modified;
                CreateMonitoringItem(connectionStringNameInAppConfig, eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, subscribers.Count(), null);
                entity.SaveChanges();
            }
        }

        public static void CreateMonitoringItem(string connectionStringNameInAppConfig, long? contentId, SharedLibrary.MessageHandler.MessageType messageType, int totalMessages, int? tag)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                var monitoringItem = new MessagesMonitoring();
                monitoringItem.ContentId = contentId;
                monitoringItem.MessageType = (int)messageType;
                monitoringItem.TotalMessages = totalMessages;
                monitoringItem.TotalFailed = 0;
                monitoringItem.TotalSuccessfulySended = 0;
                monitoringItem.TotalWithoutCharge = 0;
                monitoringItem.DateCreated = DateTime.Now;
                monitoringItem.PersianDateCreated = SharedLibrary.Date.GetPersianDate(DateTime.Now);
                monitoringItem.Status = (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue;
                monitoringItem.Tag = tag;
                entity.MessagesMonitorings.Add(monitoringItem);
                entity.SaveChanges();
            }
        }

        public static MessageObject SetImiChargeInfo(string connectionStringNameInAppConfig, MessageObject message, int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            var imiChargeObj = GetImiChargeObjectFromPrice(connectionStringNameInAppConfig, price, subscriberState);
            message.ImiChargeCode = imiChargeObj.ChargeCode;
            message.ImiChargeKey = imiChargeObj.ChargeKey;
            message.ImiMessageType = messageType;
            message.Price = price;
            return message;
        }

        public static string PrepareSubscriptionMessage(List<MessagesTemplate> messagesTemplate, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState, int isCampaignActive)
        {
            string content = null;
            switch (serviceStatusForSubscriberState)
            {
                case SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "OffMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignOffMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignWelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignWelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                default:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
            }
            return content;
        }
    }
}