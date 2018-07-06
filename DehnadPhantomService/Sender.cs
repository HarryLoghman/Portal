using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using PhantomLibrary.Models;
using System.Linq;
using System.Collections;
using System.Data.Entity;

namespace DehnadPhantomService
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
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                string serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(readSize, takeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];


                onDemandMessages = ((IEnumerable)PhantomLibrary.MessageHandler.GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType.OnDemand, readSize)).OfType<OnDemandMessagesBuffer>().ToList();

                //if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                //{
                //    TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                //    var now = DateTime.Now.TimeOfDay;
                //    if (now < retryEndTime)
                //    {
                //        using (var entity = new PhantomEntities())
                //        {
                //            entity.RetryUndeliveredMessages();
                //        }
                //    }
                //}
                SendSelectedMessages(onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                if (DateTime.Now.Hour < 22 && DateTime.Now.Hour > 7)
                {
                    autochargeMessages = ((IEnumerable)PhantomLibrary.MessageHandler.GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType.AutoCharge, readSize)).OfType<AutochargeMessagesBuffer>().ToList();
                    eventbaseMessages = ((IEnumerable)PhantomLibrary.MessageHandler.GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType.EventBase, readSize)).OfType<EventbaseMessagesBuffer>().ToList();
                    SendSelectedMessages(autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    SendSelectedMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }

        public static void SendSelectedMessages(dynamic messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (((IEnumerable<dynamic>)messages).Count() == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                var chunkedMessages = ((IEnumerable<dynamic>)messages).Skip(skip[i]).Take(take[i]).ToList();
                if (aggregatorName == "Hamrahvas")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHamrahvas(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisImi")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisImi(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "Telepromo")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromo(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "Hub")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHub(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisPlatform")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisPlatform(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MobinOneMapfa")
                    TaskList.Add(SendMesssagesToPardisPlatform(chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MTN")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMtn(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MobinOne")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMobinOne(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MciDirect")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMci(null, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "TelepromoMapfa")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromoMapfa(null, chunkedMessages, serviceAdditionalInfo));
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static async Task SendMesssagesToPardisPlatform(dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            await Task.Delay(10);
            using (var entity = new PhantomEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = SharedLibrary.ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";

                    string[] mobileNumbers = new string[messagesCount];
                    string[] shortCodes = new string[messagesCount];
                    string[] messageContents = new string[messagesCount];
                    string[] aggregatorServiceIds = new string[messagesCount];
                    string[] udhs = new string[messagesCount];
                    string[] mclass = new string[messagesCount];

                    foreach (var message in messages)
                    {
                        if (message.RetryCount != null && message.RetryCount > SharedLibrary.MessageSender.retryCountMax)
                        {
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }
                    entity.SaveChanges();

                    for (int index = 0; index < messagesCount; index++)
                    {
                        mobileNumbers[index] = "98" + messages[index].MobileNumber.TrimStart('0');
                        shortCodes[index] = "98" + paridsShortCodes.FirstOrDefault(o => o.Price == messages[index].Price).ShortCode;
                        messageContents[index] = messages[index].Content;
                        aggregatorServiceIds[index] = paridsShortCodes.FirstOrDefault(o => o.Price == messages[index].Price).PardisServiceId;
                        udhs[index] = "";
                        mclass[index] = "";
                    }
                    mobileNumbers = mobileNumbers.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    shortCodes = shortCodes.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    messageContents = messageContents.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    aggregatorServiceIds = aggregatorServiceIds.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    long[] pardisResponse;
                    if (aggregatorId == "3")
                    {
                        using (var pardisClient = new SharedLibrary.PardisPlatformServiceReference.SendClient())
                        {
                            pardisResponse = pardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                        }
                    }
                    else
                    {
                        using (var mobinonePardisClient = new SharedLibrary.MobinOneMapfaSendServiceReference.SendClient())
                        {
                            pardisResponse = mobinonePardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                        }
                    }
                    logs.Info("pardis Response count: " + pardisResponse.Count());
                    if (pardisResponse == null || pardisResponse.Count() < messagesCount)
                    {
                        foreach (var item in pardisResponse)
                        {
                            logs.Info("paridsResposne when count < messageCount: " + item);
                        }
                        pardisResponse = new long[messagesCount];
                    }
                    for (int index = 0; index < messagesCount; index++)
                    {
                        if (pardisResponse[index] == null)
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        else if (pardisResponse[index] <= 100)
                        {
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            messages[index].ReferenceId = pardisResponse[index].ToString();
                            if (messages[index].MessagePoint > 0)
                                SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                        }
                        else
                        {
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                            messages[index].ReferenceId = pardisResponse[index].ToString();
                            if (messages[index].MessagePoint > 0)
                                SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                        }
                        messages[index].SentDate = DateTime.Now;
                        messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                        entity.Entry(messages[index]).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMesssagesToPardisPlatform: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount > SharedLibrary.MessageSender.retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }
    }
}
