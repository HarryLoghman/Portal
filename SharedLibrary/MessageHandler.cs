using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using SharedLibrary.Models.ServiceModel;

namespace SharedLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SaveReceivedMessage(MessageObject message)
        {
            using (var entity = new PortalEntities())
            {
                try
                {
                    var mo = new ReceievedMessage()
                    {
                        MobileNumber = message.MobileNumber,
                        ShortCode = message.ShortCode,
                        ReceivedTime = DateTime.Now,
                        PersianReceivedTime = Date.GetPersianDateTime(DateTime.Now),
                        MessageId = message.MessageId,
                        Content = (message.Content == null) ? " " : message.Content,
                        IsProcessed = false,
                        IsReceivedFromIntegratedPanel = (message.IsReceivedFromIntegratedPanel == null) ? false : message.IsReceivedFromIntegratedPanel,
                        ReceivedFromSource = message.ReceivedFromSource,
                        ReceivedFrom = message.ReceivedFrom
                    };
                    entity.ReceievedMessages.Add(mo);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SaveReceivedMessage: " + e);
                }
            }
        }

        public static long GetAggregatorId(MessageObject message)
        {
            using (var entity = new PortalEntities())
                return entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode).FirstOrDefault().AggregatorId;
        }

        public static long GetAggregatorIdFromConfig(string AggregatorName)
        {
            using (var entity = new PortalEntities())
                return entity.Aggregators.Where(o => o.AggregatorName == AggregatorName).FirstOrDefault().Id;
        }

        public static void SaveDeliveryStatus(DeliveryObject deliveryObj)
        {
            using (var entity = new PortalEntities())
            {
                var delivery = new Delivery();
                delivery.ReferenceId = deliveryObj.ReferenceId;
                delivery.Status = deliveryObj.Status;
                delivery.Description = deliveryObj.ErrorMessage;
                delivery.DeliveryTime = DateTime.Now;
                delivery.IsProcessed = false;
                delivery.AggregatorId = deliveryObj.AggregatorId;
                entity.Deliveries.Add(delivery);
                entity.SaveChanges();
            }
        }

        public static MessageObject ValidateMessage(MessageObject message, List<SharedLibrary.Models.OperatorsPrefix> prefixs)
        {
            message.ShortCode = ValidateShortCode(message.ShortCode);
            message.Content = NormalizeContent(message.Content);
            message = GetSubscriberOperatorInfo(message, prefixs);
            return message;
        }

        public static string NormalizeContent(string content)
        {
            if (content == null)
                content = "";
            else
            {
                content = content.Trim();
                content = Regex.Replace(content, @"\s+", " ");
                content = content.ToLower();
                content = content.Replace('ك', 'ک');
                content = content.Replace('ي', 'ی');
                content = content.Replace("‏۱", "1");
                content = content.Replace('۱', '1');
                content = content.Replace('١', '1');
                content = content.Replace('٢', '2');
                content = content.Replace('۲', '2');
                content = content.Replace('۳', '3');
                content = content.Replace('٣', '3');
                content = content.Replace("‏۳", "3");
                content = content.Replace("‏‏٣", "3");
                content = content.Replace("‏۴", "4");
                content = content.Replace('۴', '4');
                content = content.Replace('٤', '4');
                content = content.Replace("‏۵", "5");
                content = content.Replace('۵', '5');
                content = content.Replace('٥', '5');
                content = content.Replace('۶', '6');
                content = content.Replace("‏۶", "6");
                content = content.Replace('٦', '6');
                content = content.Replace('٧', '7');
                content = content.Replace('۷', '7');
                content = content.Replace('٨', '8');
                content = content.Replace('۸', '8');
                content = content.Replace('۹', '9');
                content = content.Replace('٩', '9');
                content = content.Replace('٠', '0');
                content = content.Replace('۰', '0');
            }
            return content;
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
            }
            return content;
        }

        public static bool IsInBlackList(string mobileNumber, long serviceId)
        {
            bool result = false;
            try
            {
                using (var entity = new PortalEntities())
                {
                    var mobile = entity.BlackLists.Where(o => o.MobileNumber == mobileNumber).ToList();
                    if (mobile.FirstOrDefault(o => o.ServiceId == 0 || o.ServiceId == serviceId) != null)
                        result = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsInBlackList: ", e);
            }
            return result;
        }

        public static MessageObject InvalidContentWhenNotSubscribed(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(entity, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static long GetServiceIdFromUserMessage(string content, string shortCode)
        {
            long serviceId = 0;
            try
            {
                using (var entity = new PortalEntities())
                {
                    var serviceKeywords = entity.ServiceKeywords.FirstOrDefault(o => o.Keyword == content && o.ShortCode == shortCode);
                    if (serviceKeywords != null)
                        serviceId = serviceKeywords.ServiceId;
                    else
                    {
                        var services = (from s in entity.Services join sInfo in entity.ServiceInfoes on s.Id equals sInfo.ServiceId where sInfo.ShortCode == shortCode select new { ServiceId = s.Id, ServiceOnKeywords = s.OnKeywords }).AsEnumerable().ToList();
                        foreach (var service in services)
                        {
                            var serviceOnKeywords = service.ServiceOnKeywords.Split(',');
                            foreach (var onKeyword in serviceOnKeywords)
                            {
                                var trimmedOnKeyword = onKeyword.Trim();
                                if (content == trimmedOnKeyword)
                                    return service.ServiceId;
                                else
                                {
                                    var offkewords = SharedLibrary.ServiceHandler.ServiceOffKeywords();
                                    foreach (var offkeyword in offkewords)
                                    {
                                        if (content.Contains(offkeyword) && content.Contains(trimmedOnKeyword))
                                            return service.ServiceId;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetServiceIdFromUserMessage: " + e);
            }
            return serviceId;
        }

        public static string ValidateShortCode(string shortCode)
        {
            shortCode = shortCode.Replace(" ", "");
            if (shortCode.StartsWith("+"))
                shortCode = shortCode.TrimStart('+');
            if (shortCode.StartsWith("98"))
                shortCode = shortCode.Remove(0, 2);
            return shortCode;
        }

        public static string ValidateNumber(string mobileNumber)
        {
            if (mobileNumber == null)
                mobileNumber = "Invalid Mobile Number";
            if (mobileNumber.StartsWith("+"))
                mobileNumber = mobileNumber.TrimStart('+');
            if (mobileNumber.StartsWith("98"))
                mobileNumber = mobileNumber.Remove(0, 2);
            if (!mobileNumber.StartsWith("0"))
                mobileNumber = "0" + mobileNumber;
            if (!mobileNumber.StartsWith("09") || mobileNumber.Length != 11)
                mobileNumber = "Invalid Mobile Number";

            return mobileNumber;
        }

        public static string ValidateLandLineNumber(string number)
        {
            if (number == null)
                number = "Invalid Number";
            if (number.StartsWith("+"))
                number = number.TrimStart('+');
            if (number.StartsWith("98"))
                number = number.Remove(0, 2);
            if (!number.StartsWith("0"))
                number = "0" + number;
            if (number.StartsWith("09") || number.Length > 11)
                number = "Invalid Number";

            return number;
        }
        public static MessageObject GetSubscriberOperatorInfo(MessageObject message, List<SharedLibrary.Models.OperatorsPrefix> operatorPrefixes)
        {
            int mobileOperator;
            int operatorPlan;
            GetSubscriberOperatorInfo(message.MobileNumber, out mobileOperator, out operatorPlan, operatorPrefixes);

            message.MobileOperator = mobileOperator;
            message.OperatorPlan = operatorPlan;
            //foreach (var operatorPrefixe in operatorPrefixes)
            //{
            //    if (message.MobileNumber.StartsWith(operatorPrefixe.Prefix))
            //    {
            //        message.MobileOperator = operatorPrefixe.OperatorId;
            //        message.OperatorPlan = operatorPrefixe.OperatorPlan;
            //        break;
            //    }
            //    else
            //    {
            //        message.MobileOperator = (int)MobileOperators.Mci;
            //        message.OperatorPlan = (int)OperatorPlan.Prepaid;
            //    }
            //}
            return message;
        }

        public static void GetSubscriberOperatorInfo(string mobileNumber, out int mobileOperator, out int operatorPlan, List<SharedLibrary.Models.OperatorsPrefix> operatorPrefixes)
        {
            mobileOperator = (int)MobileOperators.Mci;
            operatorPlan = (int)OperatorPlan.Prepaid;
            foreach (var operatorPrefixe in operatorPrefixes)
            {
                if (mobileNumber.StartsWith(operatorPrefixe.Prefix))
                {
                    mobileOperator = operatorPrefixe.OperatorId;
                    operatorPlan = operatorPrefixe.OperatorPlan;
                    break;
                }

            }

        }

        public static MessageObject CreateMessage(Subscriber subscriber, string content, long contentId, MessageType messageType, ProcessStatus processStatus, int ImiMessageType, dynamic ImiChargeObject, long AggregatorId, int messagePoint, int? tag, int price)
        {
            var message = new MessageObject();
            message.Content = content;
            message.MobileNumber = subscriber.MobileNumber;
            message.SubscriberId = subscriber.Id;
            message.ContentId = contentId;
            message.MessageType = (int)messageType;
            message.ProcessStatus = (int)processStatus;
            message.ServiceId = subscriber.ServiceId;
            message.OperatorPlan = subscriber.OperatorPlan;
            message.MobileOperator = subscriber.MobileOperator;
            message.ImiMessageType = ImiMessageType;
            message.ImiChargeCode = ImiChargeObject.ChargeCode;
            message.ImiChargeKey = ImiChargeObject.ChargeKey;
            message.AggregatorId = AggregatorId;
            message.Point = messagePoint;
            message.Tag = tag;
            message.Price = price;
            return message;
        }

        public static MessageObject CreateMessageFromMessageBuffer(long? subscriberId, string mobileNumber, long serviceId, string content, long? contentId, MessageType messageType, ProcessStatus processStatus, int? ImiMessageType, int? imiChargeCode, string imiChargeKey, long AggregatorId, int messagePoint, int? tag, byte? subUnSubType, string subUnSubMoMessage, int price)
        {
            var message = new MessageObject();
            message.Content = content;
            message.MobileNumber = mobileNumber;
            message.SubscriberId = subscriberId;
            message.ContentId = contentId;
            message.MessageType = (int)messageType;
            message.ProcessStatus = (int)processStatus;
            message.ServiceId = serviceId;
            message.ImiMessageType = ImiMessageType;
            message.ImiChargeCode = imiChargeCode;
            message.ImiChargeKey = imiChargeKey;
            message.AggregatorId = AggregatorId;
            message.Point = messagePoint;
            message.Tag = tag;
            message.SubUnSubType = subUnSubType;
            message.SubUnSubMoMssage = subUnSubMoMessage;
            message.Price = price;
            return message;
        }
        public static int GetSubscriberPoint(string mobileNumber, long? serviceId)
        {
            int point = 0;
            using (var entity = new PortalEntities())
            {
                if (serviceId != null)
                    point = entity.SubscribersPoints.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).Point;
                else
                    point = entity.SubscribersPoints.Where(o => o.MobileNumber == mobileNumber).Select(o => o.Point).DefaultIfEmpty(0).Sum();
            }
            return point;
        }

        public static void SetSubscriberPoint(string mobileNumber, long serviceId, int point)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var pointObj = entity.SubscribersPoints.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                    pointObj.Point += point;
                    entity.Entry(pointObj).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SetSubscriberPoint: ", e);
            }
        }

        public static MessageObject SetImiChargeInfo(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity, MessageObject message
            , int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            SharedLibrary.Models.ServiceModel.ImiChargeCode imiChargeCode = new Models.ServiceModel.ImiChargeCode();
            if (subscriberState == null && price > 0)
                imiChargeCode = (entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price);
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                imiChargeCode = (entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Register");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                imiChargeCode = (entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                imiChargeCode = (entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
            else
                imiChargeCode = (entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Free");

            if (imiChargeCode != null)
            {
                message.ImiChargeCode = imiChargeCode.ChargeCode;
                message.ImiChargeKey = imiChargeCode.ChargeKey;
                message.ImiMessageType = messageType;
                message.Price = price;
            }
            return message;
        }

        public static long OtpLog(vw_servicesServicesInfo service, string mobileNumber, string otpType, string userMessage)
        {
            try
            {
                using (var entity = new Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
                {
                    var otpLog = new Models.ServiceModel.Otp()
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

        public static void OtpLogUpdate(vw_servicesServicesInfo service, long otpId, string returnValue)
        {
            try
            {
                using (var entity = new Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
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
        public static MessageObject SendServiceSubscriptionHelp(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity, MessageObject message
            , List<SharedLibrary.Models.ServiceModel.MessagesTemplate> messagesTemplate)
        {
            message = SetImiChargeInfo(entity, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = messagesTemplate.Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static void InsertMessageToQueue(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity, MessageObject message)
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

        public static Models.ServiceModel.OnDemandMessagesBuffer CreateOnDemandMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new Models.ServiceModel.OnDemandMessagesBuffer();
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

        public static Models.ServiceModel.EventbaseMessagesBuffer CreateEventbaseMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new Models.ServiceModel.EventbaseMessagesBuffer();
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

        public static Models.ServiceModel.AutochargeMessagesBuffer CreateAutochargeMessageBuffer(MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            var messageBuffer = new Models.ServiceModel.AutochargeMessagesBuffer();
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceEntity"></param>
        /// <param name="messageType"></param>
        /// <param name="readSize">0 means all</param>
        /// <returns></returns>
        public static IEnumerable GetUnprocessedMessages(SharedLibrary.Models.ServiceModel.SharedServiceEntities serviceEntity
            , MessageType messageType, int readSize)
        {
            var today = DateTime.Now.Date;
            var maxRetryCount = SharedLibrary.MessageSender.retryCountMax;
            var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
            var retryTimeOut = DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute);
            serviceEntity.Configuration.AutoDetectChangesEnabled = false;

            if (readSize != 0)
            {
                if (messageType == MessageType.AutoCharge)
                    return serviceEntity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).Take(readSize).ToList();
                else if (messageType == MessageType.EventBase)
                    return serviceEntity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).Take(readSize).ToList();
                else if (messageType == MessageType.OnDemand)
                    return serviceEntity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).Take(readSize).ToList();
            }
            else
            {
                if (messageType == MessageType.AutoCharge)
                    return serviceEntity.AutochargeMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).ToList();
                else if (messageType == MessageType.EventBase)
                    return serviceEntity.EventbaseMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).ToList();
                else if (messageType == MessageType.OnDemand)
                    return serviceEntity.OnDemandMessagesBuffers.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                    && (o.RetryCount == null || o.RetryCount <= maxRetryCount)
                    && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                    && o.bulkId == null).ToList();
            }
            return null;
        }

        public static dynamic GetUnprocessedMessagesOld(Type entityType, MessageType messageType, int readSize)
        {
            var today = DateTime.Now.Date;
            var maxRetryCount = SharedLibrary.MessageSender.retryCountMax;
            var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
            var retryTimeOut = DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute);
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;

                if (messageType == MessageType.AutoCharge)
                    return ((IEnumerable<dynamic>)entity.AutochargeMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == MessageType.EventBase)
                    return ((IEnumerable<dynamic>)entity.EventbaseMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == MessageType.OnDemand)
                    return ((IEnumerable<dynamic>)entity.OnDemandMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else
                    return new List<dynamic>();
            }
        }
        public static void SendSelectedMessages(SharedLibrary.Models.vw_servicesServicesInfo service
            , List<SharedLibrary.Models.ServiceModel.AutochargeMessagesBuffer> messages)
        {
            Aggregators.Aggregator agg = SharedVariables.fnc_getAggregator(service.aggregatorName);
            foreach (var message in messages)
                agg.sb_sendMessage(service, message.Id, message.MobileNumber, SharedLibrary.MessageHandler.MessageType.AutoCharge
                    , SharedLibrary.MessageSender.retryCountMax
                    , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey);
        }
        public static void SendSelectedMessages(SharedLibrary.Models.vw_servicesServicesInfo service
            , List<SharedLibrary.Models.ServiceModel.EventbaseMessagesBuffer> messages)
        {
            Aggregators.Aggregator agg = SharedVariables.fnc_getAggregator(service.aggregatorName);
            foreach (var message in messages)
                agg.sb_sendMessage(service, message.Id, message.MobileNumber, SharedLibrary.MessageHandler.MessageType.EventBase
                    , SharedLibrary.MessageSender.retryCountMax
                    , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey);
        }

        public static void SendSelectedMessages(SharedLibrary.Models.vw_servicesServicesInfo service
            , List<SharedLibrary.Models.ServiceModel.OnDemandMessagesBuffer> messages)
        {
            Aggregators.Aggregator agg = SharedVariables.fnc_getAggregator(service.aggregatorName);
            foreach (var message in messages)
                agg.sb_sendMessage(service, message.Id, message.MobileNumber, SharedLibrary.MessageHandler.MessageType.OnDemand
                    , SharedLibrary.MessageSender.retryCountMax
                    , message.Content, message.DateAddedToQueue, message.Price, message.ImiChargeKey);
        }

        public static void SendSelectedMessagesOld(Type entityType, dynamic messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (((IEnumerable<dynamic>)messages).Count() == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                var chunkedMessages = ((IEnumerable<dynamic>)messages).Skip(skip[i]).Take(take[i]).ToList();
                if (aggregatorName == "Hamrahvas")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHamrahvas(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisImi")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisImi(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "Telepromo")
                //{
                //    if (serviceAdditionalInfo["shortCode"] == "3072428" || serviceAdditionalInfo["shortCode"] == "307251"
                //        || serviceAdditionalInfo["shortCode"] == "307251-" || serviceAdditionalInfo["shortCode"] == "307236")
                //        TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromoJSON(entityType, chunkedMessages, serviceAdditionalInfo));
                //    else TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromo(entityType, chunkedMessages, serviceAdditionalInfo));
                //}
                else if (aggregatorName == "Hub")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHub(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisPlatform")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisPlatform(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "MobinOneMapfa")
                //    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisPlatform(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "MTN")
                //    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMtn(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "MobinOne")
                //    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMobinOne(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "MciDirect")
                //    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMci(entityType, chunkedMessages, serviceAdditionalInfo));
                //else if (aggregatorName == "TelepromoMapfa")
                //    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromoMapfa(entityType, chunkedMessages, serviceAdditionalInfo));
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static Dictionary<string, int[]> CalculateServiceSendMessageThreadNumbers(int readSize, int takeSize)
        {
            if (readSize < takeSize)
            {
                int[] takes = new int[1];
                int[] skips = new int[1];
                skips[0] = 0;
                takes[0] = takeSize;
                return new Dictionary<string, int[]>() { { "take", takes }, { "skip", skips } };
            }
            int[] take = new int[(readSize / takeSize) + 1];
            int[] skip = new int[(readSize / takeSize) + 1];
            skip[0] = 0;
            take[0] = takeSize;
            for (int i = 1; i < take.Length; i++)
            {
                take[i] = takeSize;
                skip[i] = skip[i - 1] + takeSize;
            }
            return new Dictionary<string, int[]>() { { "take", take }, { "skip", skip } };
        }

        public static Dictionary<string, int[]> CalculateServiceSendMessageThreadNumbersByTps(int readSize, int threadNumbers)
        {
            if (readSize <= threadNumbers)
            {
                int[] takes = new int[1];
                int[] skips = new int[1];
                skips[0] = 0;
                takes[0] = threadNumbers;
                return new Dictionary<string, int[]>() { { "take", takes }, { "skip", skips } };
            }
            var takeSize = readSize / threadNumbers;

            int[] take = new int[threadNumbers];
            int[] skip = new int[threadNumbers];
            skip[0] = 0;
            take[0] = takeSize;
            for (int i = 1; i < take.Length; i++)
            {
                take[i] = takeSize;
                skip[i] = skip[i - 1] + takeSize;
            }
            return new Dictionary<string, int[]>() { { "take", take }, { "skip", skip } };
        }

        public static string CreateMCISoapEnvelopeString(dynamic message, string shortCode
            , int price, string imiChargeKey, string urlDelivery)
        {
            string correlator = MessageSender.fnc_getCorrelator(shortCode, message, true);
            string mobileNumber = message.MobileNumber;
            string messageContent = message.Content;

            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode;
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');
            string xmlString = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v4_0/local"">
                           <soapenv:Header/>
                           <soapenv:Body>
                              <loc:sendSms>
                        <loc:addresses>{0}</loc:addresses>
                                 <loc:senderName>{1}</loc:senderName>", mobileNumber, shortCode);
            if (price != 0)
            {
                xmlString += string.Format(@"
                            <loc:charging>
                                <description></description>
                                <currency>RLS</currency>
                                <amount>{0}</amount>
                                <code>{1}</code>
                             </loc:charging>", price + "0", imiChargeKey);
            }
            xmlString += string.Format(@"
                        <loc:message> {0} </loc:message>
                            <loc:receiptRequest>
                                         <endpoint>{2}</endpoint>
                                    <interfaceName>SMS</interfaceName>
                                    <correlator>{1}</correlator>
                                    </loc:receiptRequest>
                                  </loc:sendSms>
                                </soapenv:Body>
                              </soapenv:Envelope>", messageContent, correlator, urlDelivery);
            return xmlString;
        }

        public static string CreateTelepromoJsonString(string username, string password
            , string operatorServiceId, string shortCode, dynamic message, string description
            , string chargeCode, string amount, string currency
            , string isFree, string serviceName)
        {
            string mobileNumber = message.MobileNumber;
            string messageContent = message.Content;
            string correlator = MessageSender.fnc_getCorrelator(shortCode, message.DateAddedToQueue.Ticks, true);

            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode;
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');

            Dictionary<string, string> dic = new Dictionary<string, string>()
                            {
                                { "username" , username }
                                ,{ "password" , password }
                                ,{"serviceid" , operatorServiceId }
                                ,{"shortcode" , shortCode }
                                ,{ "msisdn" , mobileNumber }
                                ,{"description" , description }
                                ,{"chargecode" ,chargeCode }
                                ,{"amount" , amount }
                                ,{"currency" , currency }
                                ,{"message",  messageContent }
                                ,{"is_free",isFree }
                                ,{"correlator" , correlator }
                                ,{ "servicename" , serviceName }
                            };
            string json = JsonConvert.SerializeObject(dic);
            return json;
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

        public enum MessageSourceType
        {
            sms = 0,
            app = 1,
            landing = 2
        }
    }
}