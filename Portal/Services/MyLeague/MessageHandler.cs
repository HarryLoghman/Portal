using System;
using System.Collections.Generic;
using System.Linq;
using Portal.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Portal.Services.MyLeague
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static MessageObject InvalidContentWhenSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, Shared.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertMessageToQueue(MessageObject message)
        {
            using (var entity = new MyLeagueEntities())
            {
                if (message.MessageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                {
                    var messageBuffer = CreateAutochargeMessageBuffer(message);
                    entity.AutochargeMessagesBuffers.Add(messageBuffer);
                }
                else if (message.MessageType == (int)Shared.MessageHandler.MessageType.EventBase)
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

        public static void InsertBulkMessagesToQueue(List<MessageObject> messages)
        {
            using (var entity = new MyLeagueEntities())
            {
                int counter = 0;
                foreach (var message in messages)
                {
                    if (message.MessageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                    {
                        var messageBuffer = CreateAutochargeMessageBuffer(message);
                        entity.AutochargeMessagesBuffers.Add(messageBuffer);
                    }
                    else if (message.MessageType == (int)Shared.MessageHandler.MessageType.EventBase)
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

        public static void SetOffReason(Subscriber subscriber, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            using(var entity = new MyLeagueEntities())
            {
                var offReason = new ServiceOffReason();
                offReason.SubscriberId = subscriber.Id;
                offReason.Reason = message.Content;
                entity.ServiceOffReasons.Add(offReason);
                entity.SaveChanges();
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, Shared.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
                InsertMessageToQueue(message);
            }
        }

        public static string HandleSpecialStrings(string content, int point, long subscriberId, long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                if (content.Contains("{DPOINT}"))
                {
                    point = Shared.MessageHandler.GetSubscriberPoint(subscriberId, serviceId);
                    content = content.Replace("{DPOINT}", point.ToString());
                }
                if (content.Contains("{WPOINT}"))
                {
                    point = Shared.MessageHandler.GetSubscriberPoint(subscriberId, null);
                    content = content.Replace("{WPOINT}", point.ToString());
                }
            }
            return content;
        }

        public static OnDemandMessagesBuffer CreateOnDemandMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = Shared.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new OnDemandMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? Shared.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = Shared.Date.GetPersianDate(DateTime.Now);
            return messageBuffer;
        }

        public static EventbaseMessagesBuffer CreateEventbaseMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = Shared.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new EventbaseMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? Shared.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = Shared.Date.GetPersianDate(DateTime.Now);
            return messageBuffer;
        }

        public static AutochargeMessagesBuffer CreateAutochargeMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = Shared.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new AutochargeMessagesBuffer();
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? Shared.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = Shared.Date.GetPersianDate(DateTime.Now);
            return messageBuffer;
        }

        public static ImiChargeCode GetImiChargeObjectFromPrice(int price, Shared.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            using (var entity = new MyLeagueEntities())
            {
                ImiChargeCode imiChargeCode;
                if (subscriberState == null)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                else if(subscriberState == Shared.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Register");
                else if (subscriberState == Shared.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
                else if (subscriberState == Shared.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
                else
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Free");
                return imiChargeCode;
            }
        }

        public static string GetImiChargeKeyFromPrice(int price)
        {
            using (var entity = new MyLeagueEntities())
            {
                var imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                return imiChargeCode.ChargeKey;
            }
        }

        public static MessageObject InvalidContentWhenNotSubscribed(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, Shared.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void OffReasonResponse(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, Shared.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(message);
        }

        public static List<OnDemandMessagesBuffer> GetUnprocessedOnDemandMessages(MyLeagueEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend).Take(readSize).ToList();
        }

        public static List<EventbaseMessagesBuffer> GetUnprocessedEventbaseMessages(MyLeagueEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static List<AutochargeMessagesBuffer> GetUnprocessedAutochargeMessages(MyLeagueEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static void SetEventbaseStatus(long eventbaseId)
        {
            using (var entity = new MyLeagueEntities())
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
            var serviceCode = "MyLeague";
            var serviceId = Shared.ServiceHandler.GetServiceId(serviceCode);
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
                    subscribers = (from s in entity.Subscribers join r in entity.ReceievedMessages on s.MobileNumber equals r.MobileNumber where s.ServiceId == serviceId && s.DeactivationDate == null && (DbFunctions.TruncateTime(r.ReceivedTime).Value >= dateDiffrence && DbFunctions.TruncateTime(r.ReceivedTime).Value <= today) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator  = s.MobileOperator}).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
            }
            if (subscribers == null)
            {
                logs.Info("There is no subscribers for service with code of: " + serviceCode);
                return;
            }
            using (var entity = new MyLeagueEntities())
            {
                var leagueSubscribers = (from s in subscribers join l in entity.SubscribersLeagues on s.Id equals l.SubscriberId where l.LeagueId == eventbaseContent.LeagueId select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
                var messages = new List<MessageObject>();
                var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(eventbaseContent.Price, null);
                foreach (var subscriber in leagueSubscribers)
                {
                    eventbaseContent.Content = HandleSpecialStrings(eventbaseContent.Content, eventbaseContent.Point, subscriber.Id, serviceId.Value);
                    var message = Shared.MessageHandler.CreateMessage(subscriber, eventbaseContent.Content, eventbaseContent.Id, Shared.MessageHandler.MessageType.EventBase, Shared.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, eventbaseContent.Point, null);
                    messages.Add(message);
                }
                InsertBulkMessagesToQueue(messages);
                eventbaseContent.IsAddedToSendQueueFinished = true;
                entity.Entry(eventbaseContent).State = EntityState.Modified;
                CreateMonitoringItem(eventbaseContent.Id, Shared.MessageHandler.MessageType.EventBase, leagueSubscribers.Count(), null);
                entity.SaveChanges();
            }
        }

        public static void CreateMonitoringItem(long? contentId, Shared.MessageHandler.MessageType messageType, int totalMessages, int? tag)
        {
            using (var entity = new MyLeagueEntities())
            {
                var monitoringItem = new MessagesMonitoring();
                monitoringItem.ContentId = contentId;
                monitoringItem.MessageType = (int)messageType;
                monitoringItem.TotalMessages = totalMessages;
                monitoringItem.TotalFailed = 0;
                monitoringItem.TotalSuccessfulySended = 0;
                monitoringItem.TotalWithoutCharge = 0;
                monitoringItem.DateCreated = DateTime.Now;
                monitoringItem.PersianDateCreated = Shared.Date.GetPersianDate(DateTime.Now);
                monitoringItem.Status = (int)Shared.MessageHandler.ProcessStatus.InQueue;
                monitoringItem.Tag = tag;
                entity.MessagesMonitorings.Add(monitoringItem);
                entity.SaveChanges();
            }
        }

        public static MessageObject SetImiChargeInfo(MessageObject message, int price, int messageType, Shared.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            var imiChargeObj = GetImiChargeObjectFromPrice(price, subscriberState);
            message.ImiChargeCode = imiChargeObj.ChargeCode;
            message.ImiChargeKey = imiChargeObj.ChargeKey;
            message.ImiMessageType = messageType;
            return message;
        }

        public static async Task SendMesssagesToPardisImi(MyLeagueEntities entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                var SPID = "RESA";
                PardisImiServiceReference.SMS[] smsList = new PardisImiServiceReference.SMS[messagesCount];
                for (int index = 0; index < messagesCount; index++)
                {
                    smsList[index] = new PardisImiServiceReference.SMS()
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
                var pardisClient = new PardisImiServiceReference.MTSoapClient();
                var pardisResponse = pardisClient.SendSMS(SPID, smsList);
                if(pardisResponse.Rows.Count == 0)
                {
                    logs.Info("SendMessagesToPardisImi does not return response there must be somthing wrong with the parameters");
                    return;
                }
                for (int index = 0; index < messagesCount; index++)
                {
                    messages[index].ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.Success;
                    messages[index].ReferenceId = pardisResponse.Rows[index]["Correlator"].ToString();
                    entity.Entry(messages[index]).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMesssagesToPardisImi: " + e);
            }
        }
        public static async Task SendMesssagesToHamrahvas(MyLeagueEntities entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
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
    }
}