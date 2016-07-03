using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Portal.Services.Danestan
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void InsertMessageToQueue(Message message)
        {
            using (var entity = new DanestanEntities())
            {
                if(message.AggregatorId == 0)
                    message.AggregatorId = Shared.MessageHandler.GetAggregatorId(message);

                var messageBuffer = new MessagesBuffer();
                messageBuffer.Content = message.Content;
                messageBuffer.ContentId = message.ContentId;
                messageBuffer.ImiChargeCode = message.ImiChargeCode;
                messageBuffer.ImiMessageType = message.ImiMessageType;
                messageBuffer.MobileNumber = message.MobileNumber;
                messageBuffer.MessageType = message.MessageType;
                messageBuffer.ProcessStatus = message.ProcessStatus;
                messageBuffer.ServiceId = message.ServiceId;
                messageBuffer.DateAddedToQueue = DateTime.Now;
                messageBuffer.AggregatorId = message.AggregatorId;
                messageBuffer.SubscriberId = Shared.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                messageBuffer.PersianDateAddedToQueue = Shared.Date.GetPersianDate(DateTime.Now);
                messageBuffer.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.TryingToSend;
                entity.MessagesBuffers.Add(messageBuffer);
                entity.SaveChanges();
            }
        }

        public static void InvalidContentWhenNotSubscribed(Message message, List<MessagesTemplate> messagesTemplate)
        {
            if (message.MobileOperator == (int)Shared.MessageHandler.MobileOperators.Mci)
                message = Shared.MessageHandler.SetImiChargeCode(message, 0, 0);
            message.Content = messagesTemplate.Where(o => o.Title == "InvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            InsertMessageToQueue(message);
        }

        public static List<MessagesBuffer> GetUnprocessedMessages(DanestanEntities entity, int readSize)
        {
            var today = DateTime.Now.Date;
            return entity.MessagesBuffers.Where(o => o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend && DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today).OrderBy(o => o.MessageType).Take(readSize).ToList();
        }

        public static async Task SendMesssagesToHamrahvas(DanestanEntities entity, List<MessagesBuffer> messages, Dictionary<string, string> serviceAdditionalInfo)
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
                ImiMessageType.Add(message.ImiMessageType.GetValueOrDefault());
                ImiChargeCode.Add(message.ImiChargeCode.GetValueOrDefault());
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
            entity.SaveChanges();
            logs.Info(" Send function ended ");
        }
    }
}