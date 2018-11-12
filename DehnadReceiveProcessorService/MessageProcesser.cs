using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DehnadReceiveProcessorService
{
    public class MessageProcesser
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int MaxRetryCount = 4;
        public static int RetryWaitTimeInSeconds = -50;
        public void Process()
        {
            try
            {
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in RecieveProcessor: " + e);
            }
        }

        public void PardisImiProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("PardisImi");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in PardisImiProcess: " + e);
            }
        }

        public void PardisPlatformProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("PardisPlatform");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                shortCodes.Add("2018");
                shortCodes.Add("20185");
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;
                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in PardisPlatformProcess: " + e);
            }
        }

        public void TelepromoProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("Telepromo");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut) && o.Content.Length != 4).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in TelepromoProcess: " + e);
            }
        }

        public void TelepromoOtpConfirmProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("Telepromo");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut) && o.Content.Length == 4).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in TelepromoOtpConfirmProcess: " + e);
            }
        }

        public void TelepromoMapfaProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("TelepromoMapfa");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in TelepromoMapfaProcess: " + e);
            }
        }

        public void HubProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("Hub");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in HubProcess: " + e);
            }
        }

        public void MciDirectProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("MciDirect");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
            }
            catch (Exception e)
            {
                logs.Error("Exeption in MciDirectProcess: " + e);
            }
        }

        public void IrancellProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("MTN");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in IrancellProcess: " + e);
            }
        }

        public void MobinOneProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("MobinOne");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in MobinOneProcess: " + e);
            }
        }

        public void MobinOneMapfaProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("MobinOneMapfa");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in MobinOneMapfaProcess: " + e);
            }
        }

        public void SamssonTciProcess()
        {
            try
            {
                var aggeragatorId = SharedLibrary.ServiceHandler.GetAggregatorIdFromAggregatorName("SamssonTci");
                var shortCodes = SharedLibrary.ServiceHandler.GetShortCodesFromAggregatorId(aggeragatorId);
                var receivedMessages = new List<ReceievedMessage>();
                var NumberOfConcurrentMessagesToProcess = Convert.ToInt32(Properties.Settings.Default.NumberOfConcurrentMessagesToProcess);
                using (var db = new PortalEntities())
                {
                    var retryTimeOut = DateTime.Now.AddSeconds(RetryWaitTimeInSeconds);
                    receivedMessages = db.ReceievedMessages.Where(o => o.IsProcessed == false && shortCodes.Contains(o.ShortCode) && (o.RetryCount == null || o.RetryCount <= MaxRetryCount) && (o.LastRetryDate == null || o.LastRetryDate < retryTimeOut)).OrderBy(o => o.ReceivedTime).GroupBy(o => o.MobileNumber).Select(o => o.FirstOrDefault()).ToList();
                }
                if (receivedMessages.Count == 0)
                    return;

                for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                {
                    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                    List<Task> TaskList = new List<Task>();
                    foreach (var message in receivedChunk)
                    {
                        TaskList.Add(HandleReceivedMessage(message));
                    }
                    Task.WaitAll(TaskList.ToArray());
                }
                //for (int i = 0; i < receivedMessages.Count; i += NumberOfConcurrentMessagesToProcess)
                //{
                //    var receivedChunk = receivedMessages.Skip(i).Take(NumberOfConcurrentMessagesToProcess).ToList();
                //    Parallel.ForEach(receivedChunk, receivedMessage =>
                //    {
                //        HandleReceivedMessage(receivedMessage);
                //    });
                //}
            }
            catch (Exception e)
            {
                logs.Error("Exeption in SamssonTciProcess: " + e);
            }
        }

        public static async Task HandleReceivedMessage(ReceievedMessage receivedMessage)
        {
            await Task.Delay(10).ConfigureAwait(false);
            var message = new MessageObject();
            message.MobileNumber = receivedMessage.MobileNumber;
            message.ShortCode = receivedMessage.ShortCode;
            message.Content = receivedMessage.Content;
            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
            message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
            message.IsReceivedFromIntegratedPanel = receivedMessage.IsReceivedFromIntegratedPanel;
            message.ReceivedFrom = receivedMessage.ReceivedFrom;
            message.IsReceivedFromWeb = receivedMessage.IsReceivedFromWeb;

            using (var entity = new PortalEntities())
            {
                bool isSucceeded = true;
                //if (message.ShortCode.StartsWith("2"))
                //{
                //    var pardisShortCode = entity.ParidsShortCodes.FirstOrDefault(o => o.ShortCode == message.ShortCode);
                //    if (pardisShortCode != null)
                //        message.ShortCode = entity.ServiceInfoes.FirstOrDefault(o => o.ServiceId == pardisShortCode.ServiceId).ShortCode;
                //}
                var serviceShortCodes = entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode);
                if (serviceShortCodes != null)
                {
                    isSucceeded = RouteUserToDesiredService(message, serviceShortCodes);
                }

                if (isSucceeded != true)
                {
                    receivedMessage.IsProcessed = false;
                    receivedMessage.LastRetryDate = DateTime.Now;
                    if (receivedMessage.RetryCount == null)
                        receivedMessage.RetryCount = 1;
                    else
                        receivedMessage.RetryCount += 1;
                    if (receivedMessage.RetryCount > MaxRetryCount)
                        receivedMessage.IsProcessed = true;
                }
                else
                    receivedMessage.IsProcessed = true;
                entity.Entry(receivedMessage).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
        }

        private static bool RouteUserToDesiredService(MessageObject message, IQueryable<ServiceInfo> serviceShortCodes)
        {
            bool isSucceeded = true;
            try
            {
                using (var entity = new PortalEntities())
                {
                    long serviceId = 0;
                    message = SharedLibrary.MessageHandler.ValidateMessage(message, Service.prefix);

                    var isUserSendedGeneralUnsubscribeKeyword = SharedLibrary.ServiceHandler.CheckIfUserSendedUnsubscribeContentToShortCode(message.Content);
                    bool isTelepromo = serviceShortCodes.FirstOrDefault().AggregatorId == 5 ? true : false;
                    if (isUserSendedGeneralUnsubscribeKeyword == true && message.IsReceivedFromIntegratedPanel == false && serviceShortCodes.Count() > 1 && isTelepromo == false)
                    {
                        UnsubscribeUserOnAllServicesForShortCode(message);
                        //var servicesThatUserSubscribedOnShortCode = SharedLibrary.ServiceHandler.GetServicesThatUserSubscribedOnShortCode(message.MobileNumber, message.ShortCode);
                        //message.Content = SharedLibrary.MessageHandler.PrepareGeneralOffMessage(message, servicesThatUserSubscribedOnShortCode);
                        //if(serviceShortCodes.FirstOrDefault().AggregatorId == 2)
                        //    message = MyLeagueLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else if(serviceShortCodes.FirstOrDefault().AggregatorId == 5)
                        //    message = SoltanLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else if (serviceShortCodes.FirstOrDefault().AggregatorId == 6)
                        //    message = ShahreKalamehLibrary.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //else
                        //    message = Tabriz2018Library.MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        //serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                        //var serviceCode = entity.Services.FirstOrDefault(o => o.Id == serviceId).ServiceCode;
                        //SendMessageUsingServiceCode(serviceCode, message);
                        return isSucceeded;
                    }
                    else if (message.IsReceivedFromIntegratedPanel == true && isTelepromo == false)
                    {
                        var serviceInfo = entity.ServiceInfoes.FirstOrDefault(o => o.AggregatorServiceId == message.Content);
                        if (serviceInfo == null)
                            return isSucceeded;
                        message.Content = "off";
                        serviceId = serviceInfo.ServiceId;
                    }
                    else
                    {
                        if (serviceShortCodes.Count() == 1)
                            serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromShortCode(message.ShortCode);
                            if (serviceInfo.AggregatorId == 5)
                            {
                                var telepromoServices = SharedLibrary.ServiceHandler.GetAllServicesByAggregatorId(5);
                                if (message.ReceivedFrom.Contains("-New500-"))
                                {
                                    serviceId = telepromoServices.Where(o => o.ShortCode == message.ShortCode).OrderByDescending(o => o.ServiceId).Select(o => o.ServiceId).FirstOrDefault();
                                }
                                else
                                    serviceId = telepromoServices.Where(o => o.ShortCode == message.ShortCode).OrderBy(o => o.ServiceId).Select(o => o.ServiceId).FirstOrDefault();
                            }
                            else
                                serviceId = SharedLibrary.MessageHandler.GetServiceIdFromUserMessage(message.Content, message.ShortCode);
                        }
                        if (serviceId == 0)
                            serviceId = entity.ServiceInfoes.FirstOrDefault(o => o.ShortCode == message.ShortCode).ServiceId;
                    }
                    var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
                    if (service != null)
                    {
                        message.ServiceId = service.Id;
                        isSucceeded = ChooseService(message, service);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in RouteUserToDesiredService: " + e);
            }
            return isSucceeded;
        }

        private static void UnsubscribeUserOnAllServicesForShortCode(MessageObject message)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var serviceIds = entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode).Select(o => o.ServiceId);
                    var userSubscribedServices = entity.Subscribers.Where(o => o.MobileNumber == message.MobileNumber && serviceIds.Contains(o.ServiceId) && o.DeactivationDate == null).Select(o => o.ServiceId).ToList();
                    if (userSubscribedServices == null)
                    {
                        var service = entity.Services.FirstOrDefault(o => o.Id == serviceIds.FirstOrDefault());
                        ChooseService(message, service);
                    }
                    else
                    {
                        foreach (var serviceId in userSubscribedServices)
                        {
                            var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
                            ChooseService(message, service);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in UnsubscribeUserOnAllServicesForShortCode: " + e);
            }
        }

        private static bool ChooseService(MessageObject message, SharedLibrary.Models.Service service)
        {
            bool isSucceeded = true;
            //logs.Info(service.ServiceCode);
            try
            {
                if (service.ServiceCode == "MyLeague")
                    isSucceeded = MyLeagueLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Danestaneh")
                    isSucceeded = DanestanehLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Mobiliga")
                    isSucceeded = MobiligaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "MashinBazha")
                    isSucceeded = MashinBazhaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Soltan")
                    isSucceeded = SoltanLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Tabriz2018")
                    isSucceeded = Tabriz2018Library.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "SepidRood")
                    isSucceeded = SepidRoodLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Tirandazi")
                    isSucceeded = TirandaziLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "BimeKarbala")
                    isSucceeded = BimeKarbalaLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Boating")
                    isSucceeded = BoatingLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "DonyayeAsatir")
                    isSucceeded = DonyayeAsatirLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "ShahreKalameh")
                    isSucceeded = ShahreKalamehLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "BimeIran")
                    isSucceeded = BimeIranLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "JabehAbzar")
                {
                    JabehAbzarLibrary.HandleMo h = new JabehAbzarLibrary.HandleMo();
                    isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //isSucceeded = JabehAbzarLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Tamly")
                    isSucceeded = TamlyLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Tamly500")
                    isSucceeded = Tamly500Library.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "ShenoYad")
                    isSucceeded = ShenoYadLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "ShenoYad500")
                {
                    ShenoYad500Library.HandleMo h = new ShenoYad500Library.HandleMo();
                    isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //isSucceeded = (new ShenoYad500Library.HandleMo()).ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "FitShow")
                    isSucceeded = FitShowLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Takavar")
                    isSucceeded = TakavarLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "MenchBaz")
                    isSucceeded = MenchBazLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "AvvalPod")
                    isSucceeded = AvvalPodLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "AvvalPod500")
                    isSucceeded = AvvalPod500Library.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "AvvalYad")
                    isSucceeded = AvvalYadLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "BehAmooz500")
                    isSucceeded = BehAmooz500Library.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "IrancellTest")
                    isSucceeded = IrancellTestLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Soraty")
                    isSucceeded = SoratyLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "DefendIran")
                    isSucceeded = DefendIranLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "TahChin")
                    isSucceeded = TahChinLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Nebula")
                    isSucceeded = NebulaLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Dezhban")
                    isSucceeded = DezhbanLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "MusicYad")
                    isSucceeded = MusicYadLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Phantom")
                    isSucceeded = PhantomLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Medio")
                    isSucceeded = MedioLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Dambel")
                    isSucceeded = DambelLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "Aseman")
                    isSucceeded = AsemanLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Medad")
                    isSucceeded = MedadLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "PorShetab")
                    isSucceeded = PorShetabLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "TajoTakht")
                    isSucceeded = TajoTakhtLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "LahzeyeAkhar")
                    isSucceeded = LahzeyeAkharLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Hazaran")
                    isSucceeded = HazaranLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Halghe")
                    isSucceeded = HalgheLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Achar")
                {
                    AcharLibrary.HandleMo h = new AcharLibrary.HandleMo();
                    isSucceeded = h.ReceivedMessage(message, service).Result;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChooseService: " + service.ServiceCode + "," + message.MobileNumber + "," + e);
            }
            return isSucceeded;
        }

        private static void SendMessageUsingServiceCode(string serviceCode, MessageObject message)
        {
            try
            {
                if (serviceCode == "MyLeague")
                    MyLeagueLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Danestaneh")
                    DanestanehLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Mobiliga")
                    MobiligaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MashinBazha")
                    MashinBazhaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Soltan")
                    SoltanLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tabriz2018")
                    Tabriz2018Library.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "SepidRood")
                    SepidRoodLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tirandazi")
                    TirandaziLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "BimeKarbala")
                    BimeKarbalaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Boating")
                    BoatingLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "DonyayeAsatir")
                    DonyayeAsatirLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "ShahreKalameh")
                    ShahreKalamehLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "BimeIran")
                    BimeIranLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "JabehAbzar")
                    JabehAbzarLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Tamly")
                    TamlyLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "ShenoYad")
                    ShenoYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "FitShow")
                    FitShowLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Takavar")
                    TakavarLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MenchBaz")
                    MenchBazLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "AvvalPod")
                    AvvalPodLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "AvvalYad")
                    AvvalYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "IrancellTest")
                    IrancellTestLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Soraty")
                    SoratyLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "DefendIran")
                    DefendIranLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "TahChin")
                    TahChinLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Nebula")
                    NebulaLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Dezhban")
                    DezhbanLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "MusicYad")
                    MusicYadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Phantom")
                    PhantomLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Medio")
                    MedioLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Dambel")
                    DambelLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Aseman")
                    AsemanLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Medad")
                    MedadLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "PorShetab")
                    PorShetabLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "TajoTakht")
                    TajoTakhtLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "LahzeyeAkhar")
                    LahzeyeAkharLibrary.MessageHandler.InsertMessageToQueue(message);
                else if (serviceCode == "Hazaran")
                    HazaranLibrary.MessageHandler.InsertMessageToQueue(message);
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChooseService: " + e);
            }
        }
    }
}
