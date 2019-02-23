using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using SharedLibrary.Models;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using static SharedShortCodeServiceLibrary.HandleMo;
using SharedLibrary.Models.ServiceModel;

namespace SharedShortCodeServiceLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static long OtpLog(string connectionStringNameInAppConfig, string mobileNumber, string otpType, string userMessage)
        {
            try
            {
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

        public static MessageObject SetImiChargeInfo(SharedServiceEntities entity, ImiChargeCode imiChargeCode, MessageObject message, int price, int messageType, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState? subscriberState)
        {
            if (subscriberState == null && price > 0)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price);
            else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Activated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Register");
            else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Deactivated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
            else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Renewal)
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
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenNotSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertMessageToQueue(string connectionStringNameInAppConfig, MessageObject message)
        {
            if (!string.IsNullOrEmpty(message.ReceivedFrom) && message.ReceivedFrom.Contains("FromFtp"))
                //do not create message for those users come from ftp
                return;
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscribedUserSendKeyword").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceSubscriptionHelp(string connectionStringNameInAppConfig, MessageObject message
            , List<SharedLibrary.Models.ServiceModel.MessagesTemplate> messagesTemplate)
        {
            message = SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceHelp(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertBulkMessagesToQueue(string connectionStringNameInAppConfig, List<MessageObject> messages)
        {
            
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                int counter = 0;
                foreach (var message in messages)
                {
                    if (!string.IsNullOrEmpty(message.ReceivedFrom) && message.ReceivedFrom.Contains("FromFtp"))
                        //do not create message for those users come from ftp
                        continue;
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
                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "UserHasActiveSinglecharge").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void SetOffReason(string connectionStringNameInAppConfig, Subscriber subscriber, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
            {
                var offReason = new ServiceOffReason();
                offReason.SubscriberId = subscriber.Id;
                offReason.Reason = message.Content;
                entity.ServiceOffReasons.Add(offReason);
                entity.SaveChanges();
                message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Unspecified);
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
                    var sub = SharedLibrary.SubscriptionHandler.GetSubscriber(mobileNumber, serviceId);
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
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.SubscriptionHandler.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
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
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.SubscriptionHandler.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static async Task<Singlecharge> SendSinglechargeMesssageToTelepromo(string connectionStringInAppConfig,  MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                //var url = "http://10.20.9.159:8600" + "/samsson-sdp/transfer/charge?";
                //var url = "http://10.20.9.135:8600" + "/samsson-sdp/transfer/charge?";
                var url = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromo, SharedLibrary.HelpfulFunctions.enumServersActions.charge);
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

                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(connectionStringInAppConfig))
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
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.SubscriptionHandler.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static ImiChargeCode GetImiChargeObjectFromPrice(string connectionStringNameInAppConfig, int price, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState? subscriberState)
        {
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
            {
                ImiChargeCode imiChargeCode;
                if (subscriberState == null && price > 0)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Activated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Register");
                else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Deactivated)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
                else if (subscriberState == SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Renewal)
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
                else
                    imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price && o.Description == "Free");
                return imiChargeCode;
            }
        }

        public static string GetImiChargeKeyFromPrice(string connectionStringNameInAppConfig, int price)
        {
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
            {
                var imiChargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.Price == price);
                return imiChargeCode.ChargeKey;
            }
        }

        public static MessageObject InvalidContentWhenNotSubscribed(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void OffReasonResponse(string connectionStringNameInAppConfig, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SubscriberSendedOffReason").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(connectionStringNameInAppConfig, message);
        }

        public static List<OnDemandMessagesBuffer> GetUnprocessedOnDemandMessages(SharedServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).Take(readSize).ToList();
        }

        public static List<EventbaseMessagesBuffer> GetUnprocessedEventbaseMessages(SharedServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static List<AutochargeMessagesBuffer> GetUnprocessedAutochargeMessages(SharedServiceEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).Take(readSize).ToList();
        }

        public static void SetEventbaseStatus(string connectionStringNameInAppConfig, long eventbaseId)
        {
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

        //public static void AddEventbaseMessagesToQueue(string connectionStringNameInAppConfig, string serviceCode, EventbaseContent eventbaseContent, long aggregatorId)
        //{
        //    //var serviceCode = "Tamly500";
        //    var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
        //    if (serviceId == null)
        //    {
        //        logs.Info("There is no service with code of : " + serviceCode);
        //        return;
        //    }
        //    var messagePoint = eventbaseContent.Point;
        //    var today = DateTime.Now.Date;
        //    var dateDiffrence = DateTime.Now.AddDays(-eventbaseContent.SubscriberNotSendedMoInDays).Date;

        //    IEnumerable<Subscriber> subscribers;
        //    using (var entity = new PortalEntities())
        //    {
        //        if (eventbaseContent.SubscriberNotSendedMoInDays == 0)
        //            subscribers = entity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).ToList();
        //        else
        //            subscribers = (from s in entity.Subscribers join r in entity.ReceivedMessagesArchives on s.MobileNumber equals r.MobileNumber where s.ServiceId == serviceId && s.DeactivationDate == null && (DbFunctions.TruncateTime(r.ReceivedTime).Value >= dateDiffrence && DbFunctions.TruncateTime(r.ReceivedTime).Value <= today) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
        //    }
        //    if (subscribers == null)
        //    {
        //        logs.Info("There is no subscribers for service with code of: " + serviceCode);
        //        return;
        //    }
        //    logs.Info("Eventbase subscribers count:" + subscribers.Count());
        //    using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
        //    {
        //        var messages = new List<MessageObject>();
        //        var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(connectionStringNameInAppConfig, eventbaseContent.Price, null);
        //        foreach (var subscriber in subscribers)
        //        {
        //            var content = eventbaseContent.Content;
        //            content = HandleSpecialStrings(content, eventbaseContent.Point, subscriber.MobileNumber, serviceId.Value);
        //            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, eventbaseContent.Point, null, eventbaseContent.Price);
        //            messages.Add(message);
        //        }
        //        logs.Info("Evenbase messages count:" + messages.Count());
        //        InsertBulkMessagesToQueue(connectionStringNameInAppConfig, messages);
        //        eventbaseContent.IsAddedToSendQueueFinished = true;
        //        entity.Entry(eventbaseContent).State = EntityState.Modified;
        //        CreateMonitoringItem(connectionStringNameInAppConfig, eventbaseContent.Id, SharedLibrary.MessageHandler.MessageType.EventBase, subscribers.Count(), null);
        //        entity.SaveChanges();
        //    }
        //}

        public static void CreateMonitoringItem(string connectionStringNameInAppConfig, long? contentId, SharedLibrary.MessageHandler.MessageType messageType, int totalMessages, int? tag)
        {
            using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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

        public static MessageObject SetImiChargeInfo(string connectionStringNameInAppConfig, MessageObject message, int price, int messageType, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState? subscriberState)
        {
            var imiChargeObj = GetImiChargeObjectFromPrice(connectionStringNameInAppConfig, price, subscriberState);
            message.ImiChargeCode = imiChargeObj.ChargeCode;
            message.ImiChargeKey = imiChargeObj.ChargeKey;
            message.ImiMessageType = messageType;
            message.Price = price;
            return message;
        }

        public static string PrepareSubscriptionMessage(List<MessagesTemplate> messagesTemplate, SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState serviceStatusForSubscriberState, int isCampaignActive)
        {
            string content = null;
            switch (serviceStatusForSubscriberState)
            {
                case SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Deactivated:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "OffMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignOffMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Activated:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignWelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Renewal:
                    if (isCampaignActive == (int)CampaignStatus.Suspend || isCampaignActive == (int)CampaignStatus.Deactive)
                        content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    else
                        content = messagesTemplate.Where(o => o.Title == "CampaignWelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                case SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                default:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
            }
            return content;
        }

        public static async Task<Singlecharge> SendSinglechargeMesssageToHub(string connectionStringNameInAppConfig, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
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

                using (var entity = new SharedServiceEntities(connectionStringNameInAppConfig))
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
        public enum MessageType
        {
            OnDemand = 1,
            EventBase = 2,
            AutoCharge = 3,
        }

        public enum ProcessStatus
        {
            InQueue = 1,
            TryingToSend = 2,
            Success = 3,
            Failed = 4,
            Finished = 5,
            Paused = 6,
        }

        public enum MobileOperators
        {
            Mci = 1,
            Irancell = 2,
            Rightel = 3,
            TCT = 4
        }

        public enum OperatorPlan
        {
            Unspecified = 0,
            Postpaid = 1,
            Prepaid = 2
        }

        public enum MapfaChannels
        {
            SMS = 1,
            USSD = 2,
            MMS = 3,
            IVR = 4,
            ThreeG = 5
        }

        public enum BulkStatus
        {
            Enabled,
            Disabled,
            Stopped,
            Paused,
            Running,
            FinishedByTime,
            FinishedAll
        }

        public enum BulkFileType
        {
            file = 0,
            sqlTable = 1,
            list = 2,
            upload = 3
        }
    }
}