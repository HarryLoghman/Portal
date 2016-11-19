using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using System.Text.RegularExpressions;

namespace SharedLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SaveReceivedMessage(MessageObject message)
        {
            using (var entity = new PortalEntities())
            {
                var mo = new ReceievedMessage()
                {
                    MobileNumber = message.MobileNumber,
                    ShortCode = message.ShortCode,
                    ReceivedTime = DateTime.Now,
                    PersianReceivedTime = Date.GetPersianDateTime(DateTime.Now),
                    MessageId = message.MessageId,
                    Content = message.Content,
                    IsProcessed = false,
                    IsReceivedFromIntegratedPanel = (message.IsReceivedFromIntegratedPanel == null) ? false : message.IsReceivedFromIntegratedPanel,
                    ReceivedFrom = message.ReceivedFrom
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

        public static void SaveDeliveryStatus(DeliveryObject deliveryObj)
        {
            using (var entity = new PortalEntities())
            {
                var delivery = new Delivery();
                delivery.ReferenceId = deliveryObj.PardisID;
                delivery.Status = deliveryObj.Status;
                delivery.Description = deliveryObj.ErrorMessage;
                delivery.DeliveryTime = DateTime.Now;
                entity.Deliveries.Add(delivery);
                entity.SaveChanges();
            }
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

        public static string PrepareGeneralOffMessage(MessageObject message, List<Service> servicesThatUserSubscribedOnShortCode)
        {
            if (servicesThatUserSubscribedOnShortCode.Count == 0)
                message.Content = "کاربر گرامی شما عضو هیچ سرویسی بر روی سرشماره " + message.ShortCode + " نمی باشید.";
            if (servicesThatUserSubscribedOnShortCode.Count == 1)
            {
                var offKeyword = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(servicesThatUserSubscribedOnShortCode[0].OnKeywords) + " off";
                message.Content = "کاربر گرامی شما عضو سرویس " + servicesThatUserSubscribedOnShortCode[0].Name + " می باشید." + Environment.NewLine + "برای غیر فعال سازی سرویس کلمه " + offKeyword + " را به شماره " + message.ShortCode + " ارسال نمایید.";
            }
            else
            {
                message.Content = "کاربر گرامی شما عضو سرویس های";
                foreach (var service in servicesThatUserSubscribedOnShortCode)
                {
                    message.Content += " " + service.Name + "،";
                }
                message.Content = message.Content.TrimEnd('،');
                message.Content += " می باشید." + Environment.NewLine + "برای غیر فعال سازی سرویس";
                foreach (var service in servicesThatUserSubscribedOnShortCode)
                {
                    var offKeyword = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(service.OnKeywords) + " off";
                    message.Content += " " + service.Name + " کلمه " + offKeyword + "، ";
                }
                message.Content = message.Content.TrimEnd(' ');
                message.Content = message.Content.TrimEnd('،');
                message.Content += " را به شماره " + message.ShortCode + " ارسال نمایید.";
            }
            return message.Content;
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

        private static MessageObject GetSubscriberOperatorInfo(MessageObject message)
        {
            using (var entities = new PortalEntities())
            {
                entities.Configuration.AutoDetectChangesEnabled = false;
                var operatorPrefixes = entities.OperatorsPrefixs.OrderByDescending(o => o.Prefix.Length).ToList();
                foreach (var operatorPrefixe in operatorPrefixes)
                {
                    if (message.MobileNumber.StartsWith(operatorPrefixe.Prefix))
                    {
                        message.MobileOperator = operatorPrefixe.OperatorId;
                        message.OperatorPlan = operatorPrefixe.OperatorPlan;
                        break;
                    }
                    else
                    {
                        message.MobileOperator = (int)MobileOperators.Mci;
                        message.OperatorPlan = (int)OperatorPlan.Prepaid;
                    }
                }
            }
            return message;
        }

        public static MessageObject CreateMessage(Subscriber subscriber, string content, long contentId, MessageType messageType, ProcessStatus processStatus, int ImiMessageType, dynamic ImiChargeObject, long AggregatorId, int messagePoint, int? tag)
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
            using (var entity = new PortalEntities())
            {
                var pointObj = entity.SubscribersPoints.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                pointObj.Point += point;
                entity.Entry(pointObj).State = System.Data.Entity.EntityState.Modified;
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

        public enum OperatorPlan
        {
            Unspecified = 0,
            Postpaid = 1,
            Prepaid = 2
        }
    }
}