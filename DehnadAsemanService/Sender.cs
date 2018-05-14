using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharedLibrary.Models;
using AsemanLibrary.Models;
using System.Linq;
using System.Collections;
using System.Net.Http;
using System.Data.Entity;
using System.Xml.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DehnadAsemanService
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

                Type entityType = typeof(AsemanEntities);

                autochargeMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.AutoCharge, readSize)).OfType<AutochargeMessagesBuffer>().ToList();
                eventbaseMessages = ((IEnumerable)GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType.EventBase, readSize)).OfType<EventbaseMessagesBuffer>().ToList();
                onDemandMessages = ((IEnumerable)SharedLibrary.MessageHandler.GetUnprocessedMessages(entityType, SharedLibrary.MessageHandler.MessageType.OnDemand, readSize)).OfType<OnDemandMessagesBuffer>().ToList();

                if (retryNotDelieveredMessages && autochargeMessages.Count == 0 && eventbaseMessages.Count == 0)
                {
                    TimeSpan retryEndTime = new TimeSpan(23, 30, 0);
                    var now = DateTime.Now.TimeOfDay;
                    if (now < retryEndTime)
                    {
                        using (var entity = new AsemanEntities())
                        {
                            entity.RetryUndeliveredMessages();
                        }
                    }
                }
                SharedLibrary.MessageHandler.SendSelectedMessages(entityType, onDemandMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                if (DateTime.Now.Hour < 21 && DateTime.Now.Hour > 7)
                {
                    SharedLibrary.MessageHandler.SendSelectedMessages(entityType, autochargeMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                    SendSelectedMessages(eventbaseMessages, skip, take, serviceAdditionalInfo, aggregatorName);
                }

            }
            catch (Exception e)
            {
                logs.Error("Error in SendHandler:" + e);
            }
        }

        public static dynamic GetUnprocessedMessages(SharedLibrary.MessageHandler.MessageType messageType, int readSize)
        {
            var today = DateTime.Now.Date;
            var maxRetryCount = SharedLibrary.MessageSender.retryCountMax;
            var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
            var retryTimeOut = DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute);
            using (var entity = new AsemanEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;

                if (messageType == SharedLibrary.MessageHandler.MessageType.AutoCharge)
                    return ((IEnumerable<dynamic>)entity.AutochargeMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == SharedLibrary.MessageHandler.MessageType.EventBase)
                    return ((IEnumerable<dynamic>)entity.EventbaseMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == SharedLibrary.MessageHandler.MessageType.OnDemand)
                    return ((IEnumerable<dynamic>)entity.OnDemandMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else
                    return new List<dynamic>();
            }
        }

        public static void SendSelectedMessages(List<EventbaseMessagesBuffer> messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (messages.Count() == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                var chunkedMessages = messages.Skip(skip[i]).Take(take[i]).ToList();
                if (aggregatorName == "Telepromo")
                    TaskList.Add(SendMesssagesToTelepromo(chunkedMessages, serviceAdditionalInfo));
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static async Task SendMesssagesToTelepromo(List<EventbaseMessagesBuffer> messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            await Task.Delay(10);
            using (var entity = new AsemanEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
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
                    Random rnd = new Random();
                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            if (message.RetryCount != null && message.RetryCount >= SharedLibrary.MessageSender.retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }

                            var to = "98" + message.MobileNumber.TrimStart('0');
                            var messageContent = message.Content;

                            var messageId = rnd.Next(1000000, 9999999).ToString();
                            var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                    , sc, username, password, from, serviceId, to, messageContent, messageId);
                            if (message.Price > 0)
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
                                    SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint.Value);
                                entity.Entry(message).State = EntityState.Modified;
                            }
                            else
                            {
                                logs.Info("SendMesssagesToTelepromo Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                                if (message.RetryCount > SharedLibrary.MessageSender.retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.DateLastTried = DateTime.Now;
                                message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
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

        public static async Task<Dictionary<string, string>> SendSingleMessageToTelepromo(HttpClient client, string url)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            result["transactionId"] = "";
            try
            {
                //HttpMethod method = HttpMethod.Head;
                //var httpRequest = new HttpRequestMessage() { RequestUri = new Uri(url), Method = method };
                //using (var response = client.SendAsync(httpRequest).Result)
                //{

                //}
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
                logs.Error("request:" + url);
                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
            }
            return result;
        }

        public static async Task SendBulkMesssagesToTelepromo(List<EventbaseMessagesBuffer> messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            await Task.Delay(10);
            using (var entity = new AsemanEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;

                    var url = SharedLibrary.MessageSender.telepromoIp + "/samsson-sdp/jtransfer/qsend?";
                    var sc = "Dehnad";
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var from = "98" + serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    Random rnd = new Random();
                    var contentList = new List<TelepromoBulkMessage>();
                    bool isExceptionOccured = false;
                    var responseString = "";
                    try
                    {
                        List<string> refrenceIds = new List<string>();
                        foreach (var item in messages)
                        {
                            var message = new TelepromoBulkMessage();
                            message.to = "98" + item.MobileNumber.TrimStart('0');
                            message.message = item.Content;
                            message.messageId = rnd.Next(1000000, 9999999).ToString();
                            refrenceIds.Add(message.messageId);
                            contentList.Add(message);
                        }
                        var contents = JsonConvert.SerializeObject(contentList);
                        var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}", sc, username, password, from, serviceId);
                        if (messages[0].Price > 0)
                            urlWithParameters += String.Format("&chargingCode={0}", messages[0].ImiChargeKey);

                        using (var client = new HttpClient())
                        {
                            var content = new StringContent(contents, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(urlWithParameters, content);
                            responseString = await response.Content.ReadAsStringAsync();
                        }
                        var regex = new Regex(Regex.Escape("\"message\":\""));
                        var correctedReponseString = regex.Replace(responseString, "\"message\":", 1);
                        correctedReponseString = correctedReponseString.Remove(correctedReponseString.Length - 2);
                        correctedReponseString += "}";
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(correctedReponseString);
                        int index = 0;
                        if (jsonResponse.status.ToString() == "0")
                        {
                            foreach (var item in jsonResponse.message)
                            {
                                //if (item.ToString() == "0")
                                //{
                                    logs.Info("bulk:" + messages[index].MobileNumber + "," + item.ToString());
                                    messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                    messages[index].ReferenceId = refrenceIds[index];
                                    messages[index].SentDate = DateTime.Now;
                                    messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                    if (messages[index].MessagePoint > 0)
                                        SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint.Value);
                                    entity.Entry(messages[index]).State = EntityState.Modified;
                                //}
                                //else
                                //{
                                //    logs.Info("5");
                                //    logs.Info("SendBulkMesssagesToTelepromo Message was not sended with status of: " + item.ToString());
                                //    if (messages[index].RetryCount > SharedLibrary.MessageSender.retryCountMax)
                                //        messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                //    messages[index].DateLastTried = DateTime.Now;
                                //    messages[index].RetryCount = messages[index].RetryCount == null ? 1 : messages[index].RetryCount + 1;
                                //    entity.Entry(messages[index]).State = EntityState.Modified;
                                //}
                                index++;
                            }
                        }
                        else
                        {
                            logs.Info("SendBulkMesssagesToTelepromo Message was not sended with status of: " + jsonResponse.status.ToString());
                            responseString = "";
                        }
                    }
                    catch (Exception e)
                    {
                        isExceptionOccured = true;
                        logs.Error("Exception in SendBulkMesssagesToTelepromo: " + e);
                    }

                    if (isExceptionOccured == true || responseString == "")
                    {
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
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendBulkMessagesToTelepromo: " + e);
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
