using System;
using System.Collections.Generic;
using System.Linq;
using Portal.Models;

namespace Portal.Shared
{
    public class MessageHandler
    {
        public static void SaveReceivedMessage(MessageObject message)
        {
            using (var entity = new PortalEntities())
            {
                var mo = new ReceievedMessage()
                {
                    MobileNumber = message.MobileNumber,
                    ShortCode = message.ShortCode,
                    ReceivedTime = DateTime.Now,
                    MessageId = message.MessageId,
                    Content = message.Content,
                    IsProcessed = false
                };
                entity.ReceievedMessages.Add(mo);
                entity.SaveChanges();
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
        
        public static string PrepareSubscriptionMessage(List<MessagesTemplate> messagesTemplate, HandleSubscription.ServiceStatusForSubscriberState serviceStatusForSubscriberState)
        {
            string content = null;
            switch (serviceStatusForSubscriberState)
            {
                case HandleSubscription.ServiceStatusForSubscriberState.Deactivated:
                    content = messagesTemplate.Where(o => o.Title == "OffMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case HandleSubscription.ServiceStatusForSubscriberState.Activated:
                    content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                    break;
                case HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                case HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
                default:
                    content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    break;
            }
            return content;
        }

        public static MessageObject ValidateMessage(MessageObject message)
        {
            message.ShortCode = ValidateShortCode(message.ShortCode);
            message.Content = NormalizeContent(message.Content);
            message = GetSubscriberOperatorInfo(message);
            return message;
        }

        private static string NormalizeContent(string content)
        {
            if (content == null)
                content = "";
            else
            {
                content = content.Replace(" ", "").ToLower();
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

        private static MessageObject GetSubscriberOperatorInfo(MessageObject message)
        {
            using (var entities = new PortalEntities())
            {
                var operatorPrefixes = entities.OperatorsPrefixs.OrderByDescending(o => o.Prefix.Length).ToList();
                foreach (var operatorPrefixe in operatorPrefixes)
                {
                    if (message.MobileNumber.StartsWith(operatorPrefixe.Prefix))
                    {
                        message.MobileOperator = operatorPrefixe.OperatorId;
                        message.OperatorPlan = operatorPrefixe.OperatorPlan;
                        break;
                    }
                }
                if(message.MobileOperator == 0)
                    Shared.PortalException.Throw("Invalid Operator for Mobile Number: " + message.MobileNumber);
            }
            return message;
        }

        public static MessageObject CreateMessage(Subscriber subscriber, string content, long contentId, MessageType messageType, ProcessStatus processStatus, int ImiMessageType, ImiChargeCode ImiChargeObject, long AggregatorId, int messagePoint, int? tag)
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
            return message;
        }

        public static int GetSubscriberPoint(long subscriberId, long? serviceId)
        {
            int point = 0;
            using (var entity = new PortalEntities())
            {
                if (serviceId != null)
                    point = entity.SubscribersPoints.FirstOrDefault(o => o.SubscriberId == subscriberId && o.ServiceId == serviceId).Point;
                else
                    point = entity.SubscribersPoints.Where(o => o.SubscriberId == subscriberId).Select(o => o.Point).DefaultIfEmpty(0).Sum();
            }
            return point;
        }

        public static void HandleReceivedMessage(ReceievedMessage receivedMessage)
        {
            var message = new MessageObject();
            message.MobileNumber = receivedMessage.MobileNumber;
            message.ShortCode = receivedMessage.ShortCode;
            message.Content = receivedMessage.Content;
            message.ProcessStatus = (int)ProcessStatus.TryingToSend;
            message.MessageType = (int)MessageType.OnDemand;

            using (var entity = new PortalEntities())
            {
                var serviceShortCodes = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode);
                if (serviceShortCodes == null)
                    Portal.Shared.PortalException.Throw("Invalid service short code : " + message.ShortCode);
                var service = entity.Services.FirstOrDefault(o => o.Id == serviceShortCodes.ServiceId);
                if (service == null)
                    Portal.Shared.PortalException.Throw("Invalid service for: " + message.ShortCode);
                message = Portal.Shared.MessageHandler.ValidateMessage(message);
                message.ServiceId = service.Id;

                //if (service.ServiceCode == "RPS")
                //    Portal.Services.RockPaperScissor.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Danestan")
                    Portal.Services.Danestan.HandleMo.ReceivedMessage(message, service);
                receivedMessage.IsProcessed = true;
                entity.Entry(receivedMessage).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
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
            Rightel = 3
        }
    }
}