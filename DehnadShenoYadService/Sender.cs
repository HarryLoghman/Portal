using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using ShenoYadLibrary.Models;
using System.Linq;
using System.Collections;
using System.Net.Http;
using System.Data.Entity;

namespace DehnadShenoYadService
{
    class Sender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SendHandler()
        {
            try
            {
                var today = DateTime.Now.Date;
                List<AutochargeMessagesBuffer> autochargeMessages;
                List<EventbaseMessagesBuffer> eventbaseMessages;
                List<OnDemandMessagesBuffer> onDemandMessages;
                int readSize = Convert.ToInt32(Properties.Settings.Default.ReadSize);
                int takeSize = Convert.ToInt32(Properties.Settings.Default.Take);
                bool retryNotDelieveredMessages = Properties.Settings.Default.RetryNotDeliveredMessages;
                string serviceCode = Properties.Settings.Default.ServiceCode;
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                using (var entity = new ShenoYadEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.OnDemand, 200)).OfType<OnDemandMessagesBuffer>().ToList();

                    //if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                    //{
                    //    TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                    //    var now = DateTime.Now.TimeOfDay;
                    //    if (now < retryEndTime)
                    //    {
                    //        entity.RetryUndeliveredMessages();
                    //    }
                    //}

                    SharedLibrary.MessageHandler.SendSelectedMessages(entity, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    if (DateTime.Now.Hour <= 20 && DateTime.Now.Hour >= 8)
                    {
                        autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.AutoCharge, 200)).OfType<AutochargeMessagesBuffer>().ToList();
                        eventbaseMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entity, SharedLibrary.MessageHandler.MessageType.EventBase, 200)).OfType<EventbaseMessagesBuffer>().ToList();
                        SharedLibrary.MessageHandler.SendSelectedMessages(entity, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                        SendSelectedMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }

        public static void SendSelectedMessages(dynamic messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (((IEnumerable)messages).Cast<dynamic>().Count() == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                var chunkedMessages = ((IEnumerable)messages).Cast<dynamic>().Skip(skip[i]).Take(take[i]).ToList();
                if (aggregatorName == "Telepromo")
                    TaskList.Add(SendMesssagesToTelepromo(chunkedMessages, serviceAdditionalInfo));
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static async Task SendMesssagesToTelepromo(dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            var retryCountMax = SharedLibrary.MessageSender.retryCountMax;
            var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
            using (var entity = new ShenoYadEntities())
            {
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var url = SharedLibrary.MessageSender.telepromoIp + "/samsson-sdp/transfer/send?";
                    
                    var sc = "Dehnad";
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var from = "98" + serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            if (message.RetryCount != null && message.RetryCount >= retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }
                            else if (message.DateLastTried != null && message.DateLastTried > DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute))
                                continue;
                            var to = "98" + message.MobileNumber.TrimStart('0');
                            var messageContent = message.Content;
                            var messageId = Guid.NewGuid().ToString();
                            var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                    , sc, username, password, from, serviceId, to, messageContent, messageId);
                            if (message.Price > 0)
                                urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);

                            var result = new Dictionary<string, string>();
                            result["status"] = "";
                            result["message"] = "";
                            try
                            {
                                result = await SharedLibrary.MessageSender.SendSingleMessageToTelepromo(client, urlWithParameters);
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
                                logs.Info("SendMesssagesToTelepromo Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                                if (message.RetryCount == null)
                                {
                                    message.RetryCount = 1;
                                    message.DateLastTried = DateTime.Now;
                                }
                                else
                                {
                                    if (message.RetryCount > retryCountMax)
                                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                    message.RetryCount += 1;
                                    message.DateLastTried = DateTime.Now;
                                }
                                entity.Entry(message).State = EntityState.Modified;
                            }
                        }
                        entity.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMessagesToTelepromo: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount == null)
                        {
                            message.RetryCount = 1;
                            message.DateLastTried = DateTime.Now;
                        }
                        else
                        {
                            if (message.RetryCount > retryCountMax)
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            message.RetryCount += 1;
                            message.DateLastTried = DateTime.Now;
                        }
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }
    }
}
