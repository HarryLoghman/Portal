﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using SharedLibrary.Models;
using JhoobinPorShetabLibrary.Models;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace JhoobinPorShetabLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static long OtpLog(string mobileNumber, string otpType, string userMessage)
        {
            try
            {
                using (var entity = new JhoobinPorShetabEntities())
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
        public static void OtpLogUpdate(long otpId, string returnValue)
        {
            try
            {
                using (var entity = new JhoobinPorShetabEntities())
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

        public static MessageObject SetImiChargeInfo(JhoobinPorShetabEntities entity, ImiChargeCode imiChargeCode, MessageObject message, int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
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
        public static MessageObject InvalidContentWhenSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenNotSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertMessageToQueue(MessageObject message)
        {
            using (var entity = new JhoobinPorShetabEntities())
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

        public static MessageObject SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscribedUserSendKeyword").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceHelp(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertBulkMessagesToQueue(List<MessageObject> messages)
        {
            using (var entity = new JhoobinPorShetabEntities())
            {
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

        public static string AddAutochargeHeaderAndFooter(string autochargeContent)
        {
            try
            {
                using (var entity = new JhoobinPorShetabEntities())
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

        public static MessageObject UserHasActiveSinglecharge(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "UserHasActiveSinglecharge").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void SetOffReason(Subscriber subscriber, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            using (var entity = new JhoobinPorShetabEntities())
            {
                var offReason = new ServiceOffReason();
                offReason.SubscriberId = subscriber.Id;
                offReason.Reason = message.Content;
                entity.ServiceOffReasons.Add(offReason);
                entity.SaveChanges();
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
                InsertMessageToQueue(message);
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

        public static ImiChargeCode GetImiChargeObjectFromPrice(int price, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            using (var entity = new JhoobinPorShetabEntities())
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

        public static string GetImiChargeKeyFromPrice(int price)
        {
            using (var entity = new JhoobinPorShetabEntities())
            {
                var imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                return imiChargeCode.ChargeKey;
            }
        }

        public static MessageObject InvalidContentWhenNotSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void OffReasonResponse(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(message);
        }

        public static List<OnDemandMessagesBuffer> GetUnprocessedOnDemandMessages(JhoobinPorShetabEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).Take(readSize).ToList();
        }

        public static List<EventbaseMessagesBuffer> GetUnprocessedEventbaseMessages(JhoobinPorShetabEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static List<AutochargeMessagesBuffer> GetUnprocessedAutochargeMessages(JhoobinPorShetabEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static void SetEventbaseStatus(long eventbaseId)
        {
            using (var entity = new JhoobinPorShetabEntities())
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

        public static void AddEventbaseMessagesToQueue(EventbaseContent eventbaseContent, long aggregatorId)
        {
            var serviceCode = "JhoobinPorShetab";
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
            using (var entity = new JhoobinPorShetabEntities())
            {
                var messages = new List<MessageObject>();
                var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(eventbaseContent.Price, null);
                foreach (var subscriber in subscribers)
                {
                    var content = eventbaseContent.Content;
                    content = HandleSpecialStrings(content, eventbaseContent.Point, subscriber.MobileNumber, serviceId.Value);
                    var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, eventbaseContent.Point, null, eventbaseContent.Price);
                    messages.Add(message);
                }
                logs.Info("Evenbase messages count:" + messages.Count());
                InsertBulkMessagesToQueue(messages);
                eventbaseContent.IsAddedToSendQueueFinished = true;
                entity.Entry(eventbaseContent).State = EntityState.Modified;
                CreateMonitoringItem(eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, subscribers.Count(), null);
                entity.SaveChanges();
            }
        }

        public static void CreateMonitoringItem(long? contentId, SharedLibrary.MessageHandler.MessageType messageType, int totalMessages, int? tag)
        {
            using (var entity = new JhoobinPorShetabEntities())
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

        public static MessageObject SetImiChargeInfo(MessageObject message, int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            var imiChargeObj = GetImiChargeObjectFromPrice(price, subscriberState);
            message.ImiChargeCode = imiChargeObj.ChargeCode;
            message.ImiChargeKey = imiChargeObj.ChargeKey;
            message.ImiMessageType = messageType;
            message.Price = price;
            return message;
        }

        public static async Task SendMesssagesToPardisImi(JhoobinPorShetabEntities entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                var SPID = "RESA";
                SharedLibrary.PardisImiServiceReference.SMS[] smsList = new SharedLibrary.PardisImiServiceReference.SMS[messagesCount];
                for (int index = 0; index < messagesCount; index++)
                {
                    smsList[index] = new SharedLibrary.PardisImiServiceReference.SMS()
                    {
                        Index = index + 1,
                        Addresses = "98" + messages[index].MobileNumber.TrimStart('0'),
                        ShortCode = "98" + serviceAdditionalInfo["shortCode"],
                        Message = messages[index].Content,
                        ChargeCode = messages[index].ImiChargeKey,
                        SubUnsubMoMessage = messages[index].SubUnSubMoMssage,
                        SubUnsubType = messages[index].SubUnSubType
                    };
                }
                var pardisClient = new SharedLibrary.PardisImiServiceReference.MTSoapClient();
                var pardisResponse = pardisClient.SendSMS(SPID, smsList);
                if (pardisResponse.Rows.Count == 0)
                {
                    logs.Info("SendMessagesToPardisImi does not return response there must be somthing wrong with the parameters");
                    foreach (var sms in smsList)
                    {
                        logs.Info("Index: " + sms.Index);
                        logs.Info("Addresses: " + sms.Addresses);
                        logs.Info("ShortCode: " + sms.ShortCode);
                        logs.Info("Message: " + sms.Message);
                        logs.Info("ChargeCode: " + sms.ChargeCode);
                        logs.Info("SubUnsubMoMessage: " + sms.SubUnsubMoMessage);
                        logs.Info("SubUnsubType: " + sms.SubUnsubType);
                        logs.Info("++++++++++++++++++++++++++++++++++");
                    }
                    return;
                }
                for (int index = 0; index < messagesCount; index++)
                {
                    messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                    messages[index].ReferenceId = pardisResponse.Rows[index]["Correlator"].ToString();
                    messages[index].SentDate = DateTime.Now;
                    messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    if (messages[index].MessagePoint > 0)
                        SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                    entity.Entry(messages[index]).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMesssagesToPardisImi: " + e);
            }
        }

        public static Singlecharge SendSinglechargeMesssageToPardisImi(MessageObject message, long installmentId = 0)
        {
            if (message == null)
                return null;
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var SPID = "RESA";
                var sms = new SharedLibrary.PardisImiSinglechargeServiceReference.SMS()
                {
                    Address = "98" + message.MobileNumber.TrimStart('0'),
                    ShortCode = "98" + message.ShortCode,
                    ChargeCode = message.ImiChargeKey,
                };

                var pardisClient = new SharedLibrary.PardisImiSinglechargeServiceReference.ServiceSoapClient();
                var pardisResponse = pardisClient.SingleCharge(SPID, sms);
                if (pardisResponse == null)
                {
                    logs.Info("response returend from pardis singlecharge is null");
                    return null;
                }

                if (pardisResponse[2] == "1")
                    singlecharge.IsSucceeded = true;
                else
                    singlecharge.IsSucceeded = false;

                singlecharge.Description = pardisResponse[2];
                singlecharge.ReferenceId = pardisResponse[0];
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToPardisImi: " + e);
            }
            try
            {
                if (singlecharge.IsSucceeded == null)
                    singlecharge.IsSucceeded = false;
                if (singlecharge.ReferenceId == null)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                if (installmentId != 0)
                    singlecharge.InstallmentId = installmentId;

                using (var entity = new JhoobinPorShetabEntities())
                {
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToPardisImi on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<Dictionary<string, string>> SendSingleMessageToTelepromo(HttpClient client, string url)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            try
            {
                using (var response = client.GetAsync(new Uri(url)).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        XDocument xmlResult = XDocument.Parse(httpResult);
                        result["status"] = xmlResult.Root.Descendants("status").Select(e => e.Value).FirstOrDefault();
                        result["message"] = xmlResult.Root.Descendants("message").Select(e => e.Value).FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
            }
            return result;
        }

        public static async Task SendMesssagesToHub(JhoobinPorShetabEntities entityDisposeIt, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                await Task.Delay(10); // for making it async
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                using (var entity = new JhoobinPorShetabEntities())
                {
                    var aggregatorUsername = serviceAdditionalInfo["username"];
                    var aggregatorPassword = serviceAdditionalInfo["password"];
                    var from = serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    var subUnsubXmlStringList = new List<string>();

                    XmlDocument doc = new XmlDocument();
                    XmlElement root = doc.CreateElement("xmsrequest");
                    XmlElement userid = doc.CreateElement("userid");
                    XmlElement password = doc.CreateElement("password");
                    XmlElement action = doc.CreateElement("action");
                    XmlElement body = doc.CreateElement("body");
                    XmlElement type = doc.CreateElement("type");
                    type.InnerText = "oto";
                    body.AppendChild(type);
                    XmlElement serviceid = doc.CreateElement("serviceid");
                    serviceid.InnerText = serviceId;
                    body.AppendChild(serviceid);

                    foreach (var message in messages)
                    {
                        if (message.ImiChargeKey == "UnSubscription" || message.ImiChargeKey == "Register" || message.ImiChargeKey == "Renewal")
                        {
                            XmlDocument doc1 = new XmlDocument();
                            XmlElement root1 = doc1.CreateElement("xmsrequest");
                            XmlElement userid1 = doc1.CreateElement("userid");
                            XmlElement password1 = doc1.CreateElement("password");
                            XmlElement action1 = doc1.CreateElement("action");
                            XmlElement body1 = doc1.CreateElement("body");
                            XmlElement serviceid1 = doc1.CreateElement("serviceid");
                            XmlElement mobile1 = doc1.CreateElement("mobile");
                            XmlElement smsid = doc1.CreateElement("smsid");
                            XmlElement subUnSub;
                            serviceid1.InnerText = serviceId;
                            body1.AppendChild(serviceid1);

                            mobile1.InnerText = message.MobileNumber;
                            body1.AppendChild(mobile1);

                            smsid.InnerText = "-1";
                            body1.AppendChild(smsid);


                            if (message.ImiChargeKey == "UnSubscription")
                            {
                                subUnSub = doc1.CreateElement("sendgoodbye");
                                action1.InnerText = "vasremovemember";
                            }
                            else
                            {
                                subUnSub = doc1.CreateElement("sendwellcome");
                                action1.InnerText = "vasaddmember";
                            }
                            subUnSub.InnerText = "0";
                            body1.AppendChild(subUnSub);

                            userid1.InnerText = aggregatorUsername;
                            password1.InnerText = aggregatorPassword;

                            doc1.AppendChild(root1);
                            root1.AppendChild(userid1);
                            root1.AppendChild(password1);
                            root1.AppendChild(action1);
                            root1.AppendChild(body1);

                            subUnsubXmlStringList.Add(doc1.OuterXml);
                        }
                        XmlElement recipient = doc.CreateElement("recipient");
                        recipient.InnerText = message.Content;
                        body.AppendChild(recipient);

                        XmlAttribute mobile = doc.CreateAttribute("mobile");
                        recipient.Attributes.Append(mobile);
                        mobile.InnerText = message.MobileNumber;

                        XmlAttribute originator = doc.CreateAttribute("originator");
                        originator.InnerText = from;
                        recipient.Attributes.Append(originator);

                        XmlAttribute cost = doc.CreateAttribute("cost");
                        cost.InnerText = message.Price.ToString();
                        recipient.Attributes.Append(cost);

                        //XmlAttribute type1 = doc.CreateAttribute("type");
                        //type1.InnerText = "250";
                        //recipient.Attributes.Append(type1);
                    }

                    userid.InnerText = aggregatorUsername;
                    password.InnerText = aggregatorPassword;
                    action.InnerText = "smssend";
                    //
                    doc.AppendChild(root);
                    root.AppendChild(userid);
                    root.AppendChild(password);
                    root.AppendChild(action);
                    root.AppendChild(body);
                    //
                    string stringedXml = doc.OuterXml;
                    SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                    foreach (var subUnsubStringXml in subUnsubXmlStringList)
                    {
                        string subUnsubResponse = hubClient.XmsRequest(subUnsubStringXml).ToString();
                    }
                    string response = hubClient.XmsRequest(stringedXml).ToString();
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(response);
                    XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                    foreach (XmlNode error in OK)
                    {
                        if (error.InnerText != "" && error.InnerText != "ok")
                        {
                            logs.Error("Error in sending message to Hub");
                        }
                        else
                        {
                            var i = 0;
                            XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                            foreach (XmlNode xn in xnList)
                            {
                                string responseCode = (xn.Attributes["status"].Value).ToString();
                                if (responseCode == "40")
                                {
                                    messages[i].ReferenceId = xn.InnerText;
                                    messages[i].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                    if (messages[i].MessagePoint > 0)
                                        SharedLibrary.MessageHandler.SetSubscriberPoint(messages[i].MobileNumber, messages[i].ServiceId, messages[i].MessagePoint);
                                }
                                else
                                {
                                    messages[i].ReferenceId = "failed:" + responseCode;
                                    messages[i].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                }
                                messages[i].SentDate = DateTime.Now;
                                messages[i].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                entity.Entry(messages[i]).State = EntityState.Modified;
                                i++;
                            }
                            entity.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMessagesToHub: " + e);
            }
        }

        public static async Task<Singlecharge> SendSinglechargeMesssageToHub(MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var aggregatorUsername = serviceAdditionalInfo["username"];
                var aggregatorPassword = serviceAdditionalInfo["password"];
                var from = serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("xmsrequest");
                XmlElement userid = doc.CreateElement("userid");
                XmlElement password = doc.CreateElement("password");
                XmlElement action = doc.CreateElement("action");
                XmlElement body = doc.CreateElement("body");
                XmlElement serviceid = doc.CreateElement("serviceid");
                serviceid.InnerText = serviceId;
                body.AppendChild(serviceid);

                XmlElement recipient = doc.CreateElement("recipient");
                body.AppendChild(recipient);

                XmlAttribute mobile = doc.CreateAttribute("mobile");
                recipient.Attributes.Append(mobile);
                mobile.InnerText = message.MobileNumber;

                XmlAttribute originator = doc.CreateAttribute("originator");
                originator.InnerText = from;
                recipient.Attributes.Append(originator);

                XmlAttribute cost = doc.CreateAttribute("cost");
                cost.InnerText = (message.Price * 10).ToString();
                recipient.Attributes.Append(cost);

                //XmlAttribute type1 = doc.CreateAttribute("type");
                //type1.InnerText = "250";
                //recipient.Attributes.Append(type1);

                userid.InnerText = aggregatorUsername;
                password.InnerText = aggregatorPassword;
                action.InnerText = "singlecharge";
                //
                doc.AppendChild(root);
                root.AppendChild(userid);
                root.AppendChild(password);
                root.AppendChild(action);
                root.AppendChild(body);
                //
                string stringedXml = doc.OuterXml;
                SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                logs.Info(stringedXml);
                string response = hubClient.XmsRequest(stringedXml).ToString();
                logs.Info(response);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response);
                XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                foreach (XmlNode error in OK)
                {
                    if (error.InnerText != "" && error.InnerText != "ok")
                    {
                        logs.Error("Error in Singlecharge using Hub");
                    }
                    else
                    {
                        var i = 0;
                        XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                        foreach (XmlNode xn in xnList)
                        {
                            string responseCode = (xn.Attributes["status"].Value).ToString();
                            if (responseCode == "40")
                            {
                                singlecharge.IsSucceeded = false;
                                singlecharge.Description = responseCode;
                                singlecharge.ReferenceId = xn.InnerText;
                            }
                            else
                            {
                                singlecharge.IsSucceeded = false;
                                singlecharge.Description = responseCode;
                                //singlecharge.ReferenceId = xn.InnerText;
                            }
                            i++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToHub: " + e);
            }
            try
            {
                if (singlecharge.IsSucceeded == null)
                    singlecharge.IsSucceeded = false;
                if (singlecharge.ReferenceId == null)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                if (installmentId != 0)
                    singlecharge.InstallmentId = installmentId;

                using (var entity = new JhoobinPorShetabEntities())
                {
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToHub on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task SendMesssagesToTelepromo(JhoobinPorShetabEntities entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                //var url = "http://10.20.9.159:8600" + "/samsson-sdp/transfer/send?";
                //var url = "http://10.20.9.135:8600" + "/samsson-sdp/transfer/send?";
				var url= SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                using (var client = new HttpClient())
                {
                    foreach (var message in messages)
                    {
                        var to = "98" + message.MobileNumber.TrimStart('0');
                        var messageContent = message.Content;
                        var messageId = Guid.NewGuid().ToString();
                        var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                , sc, username, password, from, serviceId, to, messageContent, messageId);
                        if (message.ImiChargeKey != "FREE")
                            urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                        
                        var result = new Dictionary<string, string>();
                        result["status"] = "";
                        result["message"] = "";
                        try
                        {
                            result = await SendSingleMessageToTelepromo(client, urlWithParameters);
                        }
                        catch (Exception e)
                        {
                            logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
                        }

                        if (result["status"] == "0")
                        {
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                            message.ReferenceId = messageId;
                            message.SentDate = DateTime.Now;
                            message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            if (message.MessagePoint > 0)
                                SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
                            entity.Entry(message).State = EntityState.Modified;
                        }
                        else
                        {
                            logs.Info("SendMesssagesToTelepromo MobileNumber:" + message.MobileNumber + " Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            message.ReferenceId = "error status:" + result["status"];
                            message.SentDate = DateTime.Now;
                            message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMessagesToTelepromo: " + e);
            }
        }

        public static async Task<Singlecharge> SendSinglechargeMesssageToTelepromo(MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                //var url = "http://10.20.9.159:8600" + "/samsson-sdp/transfer/charge?";
                //var url = "http://10.20.9.135:8600" + "/samsson-sdp/transfer/charge?";
				var url= SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo, SharedLibrary.HelpfulFunctions.enumServersActions.charge);
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                using (var client = new HttpClient())
                {
                    var to = "98" + message.MobileNumber.TrimStart('0');
                    var messageContent = "InAppPurchase";
                    var messageId = Guid.NewGuid().ToString();
                    var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                            , sc, username, password, from, serviceId, to, messageContent, messageId);
                    urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                    var result = new Dictionary<string, string>();
                    result["status"] = "";
                    result["message"] = "";
                    result = await SendSingleMessageToTelepromo(client, urlWithParameters);
                    if (result["status"] == "0" && result["message"].Contains("description=ACCEPTED"))
                        singlecharge.IsSucceeded = true;
                    else
                        singlecharge.IsSucceeded = false;

                    singlecharge.Description = result["message"];
                    singlecharge.ReferenceId = messageId;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToTelepromo: " + e);
            }
            try
            {
                if (singlecharge.IsSucceeded == null)
                    singlecharge.IsSucceeded = false;
                if (singlecharge.ReferenceId == null)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                if (installmentId != 0)
                    singlecharge.InstallmentId = installmentId;

                using (var entity = new JhoobinPorShetabEntities())
                {
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSinglechargeMesssageToTelepromo on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task SendMesssagesToHamrahvas(JhoobinPorShetabEntities entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            List<string> mobileNumbers = new List<string>();
            List<string> contents = new List<string>();
            List<string> shortCodes = new List<string>();
            List<int> serviceIds = new List<int>();
            List<int> ImiMessageType = new List<int>();
            List<int> ImiChargeCode = new List<int>();
            List<string> messageIds = new List<string>();

            entity.Configuration.AutoDetectChangesEnabled = false;
            foreach (var message in messages)
            {
                mobileNumbers.Add(message.MobileNumber);
                contents.Add(message.Content);
                shortCodes.Add(serviceAdditionalInfo["shortCode"]);
                ImiMessageType.Add(message.ImiMessageType);
                ImiChargeCode.Add(message.ImiChargeCode);
                serviceIds.Add(Convert.ToInt32(serviceAdditionalInfo["aggregatorServiceId"]));
                //messageIds.Add(message.Id.ToString());
            }
            try
            {
                //var hamrahClient = new HamrahServiceReference.SmsBufferSoapClient("SmsBufferSoap");
                //var hamrahResult = hamrahClient.MessageListUploadWithServiceId(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], mobileNumbers.ToArray(), contents.ToArray(), shortCodes.ToArray(), serviceIds.ToArray(), ImiMessageType.ToArray(), ImiChargeCode.ToArray()/*, messageIds.ToArray()*/);
                //if (mobileNumbers.Count > 1 && hamrahResult.Length == 1)
                //{
                //    logs.Info(" Hamrah Webservice returned an error: " + hamrahResult[0]);
                //    return;
                //}
                //else
                //{
                //    int cntr = 0;
                //    foreach (var item in messages)
                //    {
                //        if (hamrahResult[cntr].ToLower().Contains("success"))
                //        {
                //            if (item.MessagePoint != 0)
                //            {
                //                var subscriber = entity.SubscribersPoints.Where(o => o.MobileNumber == item.MobileNumber).FirstOrDefault();
                //                if (subscriber != null)
                //                {
                //                    subscriber.Point += item.MessagePoint.GetValueOrDefault();
                //                    entity.Entry(subscriber).State = EntityState.Modified;
                //                }
                //            }
                //            item.ProcessStatus = (int)MessageHandler.ProcessStatus.Success;
                //        }
                //        else
                //        {
                //            if (!String.IsNullOrEmpty(hamrahResult[cntr]))
                //                logs.Info("Error!!: " + hamrahResult[cntr]);
                //            else
                //                logs.Info("Error");
                //                item.ProcessStatus = (int)MessageHandler.ProcessStatus.Failed;
                //        }
                //        item.ReferenceId = hamrahResult[cntr].Length > 50 ? hamrahResult[cntr].Substring(0, 49) : hamrahResult[cntr];
                //        item.SentDate = DateTime.Now;
                //        entity.MessagesBuffers.Attach(item);
                //        entity.Entry(item).State = EntityState.Modified;
                //        cntr++;
                //    }
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exception in Calling Hamrah Webservice", e);
            }
            foreach (var item in messages)
            {
                item.ProcessStatus = 3;
                entity.Entry(item).State = EntityState.Modified;
            }
            entity.SaveChanges();
            logs.Info(" Send function ended ");
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