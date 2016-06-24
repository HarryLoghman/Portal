using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;

namespace Portal.Shared
{
    public class MessageHandler
    {
        public static void SaveReceivedMessage(Message message)
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

        public static long GetAggregatorId(Message message)
        {
            using (var entity = new PortalEntities())
                return entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode).FirstOrDefault().AggregatorId;
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

        public static Message ValidateMessage(Message message)
        {
            message.MobileNumber = ValidateNumber(message.MobileNumber);
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

        private static string ValidateShortCode(string shortCode)
        {
            return shortCode.Replace(" ", "");
        }

        private static string ValidateNumber(string mobileNumber)
        {
            if (mobileNumber.StartsWith("+"))
                mobileNumber = mobileNumber.TrimStart('+');
            if (mobileNumber.StartsWith("98"))
                mobileNumber = mobileNumber.Remove(0, 2);
            if (!mobileNumber.StartsWith("0"))
                mobileNumber = "0" + mobileNumber;
            if (!mobileNumber.StartsWith("09") || mobileNumber.Length != 11)
                Shared.PortalException.Throw("Invalid Mobile Number: " + mobileNumber);

            return mobileNumber;
        }

        private static Message GetSubscriberOperatorInfo(Message message)
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
                    }
                }
                if(message.MobileOperator == null)
                    Shared.PortalException.Throw("Invalid Operator for Mobile Number: " + message.MobileNumber);
            }
            return message;
        }

        public static Message SetImIiChargeCode(Message message, int chargeCode, int messageType)
        {
            message.ImiChargeCode = chargeCode;
            message.ImiMessageType = messageType;
            return message;
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