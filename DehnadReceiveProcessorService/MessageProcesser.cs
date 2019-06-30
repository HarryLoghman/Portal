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
            message.ReceivedFromSource = receivedMessage.ReceivedFromSource;
            message.ReceiveTime = receivedMessage.ReceivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

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
                {
                    receivedMessage.description = message.description;
                    receivedMessage.ReceivedFrom = message.ReceivedFrom;
                    receivedMessage.IsProcessed = true;
                }
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
                    //var service = entity.Services.FirstOrDefault(o => o.Id == serviceId);
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceId);
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
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceIds.FirstOrDefault());
                        ChooseService(message, service);
                    }
                    else
                    {
                        foreach (var serviceId in userSubscribedServices)
                        {
                            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceId);
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
        private static bool ChooseService(MessageObject message, SharedLibrary.Models.vw_servicesServicesInfo service)
        {
            bool isSucceeded = true;
            //logs.Info(service.ServiceCode);
            try
            {
                //if (service.ServiceCode == "MyLeague")
                //    isSucceeded = MyLeagueLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Danestaneh")
                //    isSucceeded = DanestanehLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Mobiliga")
                //    isSucceeded = MobiligaLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "MashinBazha")
                //    isSucceeded = MashinBazhaLibrary.HandleMo.ReceivedMessage(message, service);
                if (service.ServiceCode == "Soltan")
                {
                    isSucceeded = SharedVariables.prp_soltanLibrary.ReceivedMessage(message, service).Result;
                    //JabehAbzarLibrary.HandleMo h = new JabehAbzarLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //else if (service.ServiceCode == "Tabriz2018")
                //    isSucceeded = Tabriz2018Library.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "SepidRood")
                //    isSucceeded = SepidRoodLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Tirandazi")
                //    isSucceeded = TirandaziLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "BimeKarbala")
                //    isSucceeded = BimeKarbalaLibrary.HandleMo.ReceivedMessage(message, service);
                //else if (service.ServiceCode == "Boating")
                //    isSucceeded = BoatingLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "DonyayeAsatir")
                {
                    isSucceeded = SharedVariables.prp_donyayeAsatirLibrary.ReceivedMessage(message, service).Result;
                    //JabehAbzarLibrary.HandleMo h = new JabehAbzarLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "ShahreKalameh")
                {
                    isSucceeded = SharedVariables.prp_shahreKalamehLibrary.ReceivedMessage(message, service).Result;
                    //JabehAbzarLibrary.HandleMo h = new JabehAbzarLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //else if (service.ServiceCode == "BimeIran")
                //    isSucceeded = BimeIranLibrary.HandleMo.ReceivedMessage(message, service);
                else if (service.ServiceCode == "JabehAbzar")
                {
                    isSucceeded = SharedVariables.prp_jabehAbzarLibrary.ReceivedMessage(message, service).Result;
                    //JabehAbzarLibrary.HandleMo h = new JabehAbzarLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //isSucceeded = JabehAbzarLibrary.HandleMo.ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "Tamly")
                {
                    isSucceeded = SharedVariables.prp_tamlyLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Tamly500")
                {
                    isSucceeded = SharedVariables.prp_tamly500Library.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "ShenoYad")
                {
                    isSucceeded = SharedVariables.prp_shenoYadLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "ShenoYad500")
                {
                    isSucceeded = SharedVariables.prp_shenoYad500Library.ReceivedMessage(message, service).Result;
                    //ShenoYad500Library.HandleMo h = new ShenoYad500Library.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                //isSucceeded = (new ShenoYad500Library.HandleMo()).ReceivedMessage(message, service).Result;
                else if (service.ServiceCode == "FitShow")
                {
                    isSucceeded = SharedVariables.prp_FitshowLibrary.ReceivedMessage(message, service).Result;
                }

                else if (service.ServiceCode == "Takavar")
                {
                    isSucceeded = SharedVariables.prp_takavarLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "MenchBaz")
                {
                    isSucceeded = SharedVariables.prp_menchBazLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "AvvalPod")
                {
                    isSucceeded = SharedVariables.prp_avvalPodLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "AvvalPod500")
                {
                    isSucceeded = SharedVariables.prp_avvalPod500Library.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "AvvalYad")
                {
                    isSucceeded = SharedVariables.prp_avvalYadLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "BehAmooz500")
                {
                    isSucceeded = SharedVariables.prp_behAmooz500Library.ReceivedMessage(message, service).Result;
                }
                //else if (service.ServiceCode == "IrancellTest")
                //{
                //    isSucceeded = IrancellTestLibrary.HandleMo.ReceivedMessage(message, service).res;
                //}
                else if (service.ServiceCode == "Soraty")
                {
                    isSucceeded = SharedVariables.prp_soratyBazLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "DefendIran")
                {
                    isSucceeded = SharedVariables.prp_defendIranLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "TahChin")
                {
                    isSucceeded = SharedVariables.prp_tahChinLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Nebula")
                {
                    isSucceeded = SharedVariables.prp_nebulaLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Dezhban")
                {
                    isSucceeded = SharedVariables.prp_dezhbanLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "MusicYad")
                {
                    isSucceeded = SharedVariables.prp_musicYadLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Phantom")
                {
                    isSucceeded = SharedVariables.prp_phantomLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Medio")
                {
                    isSucceeded = SharedVariables.prp_medioLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Dambel")
                {
                    isSucceeded = SharedVariables.prp_dambelLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Aseman")
                {
                    isSucceeded = SharedVariables.prp_asemanLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Medad")
                {
                    isSucceeded = SharedVariables.prp_medadLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "PorShetab")
                {
                    isSucceeded = SharedVariables.prp_porShetabLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "TajoTakht")
                {
                    isSucceeded = SharedVariables.prp_tajoTakhtLibrary.ReceivedMessage(message, service).Result;
                }

                else if (service.ServiceCode == "LahzeyeAkhar")
                {
                    isSucceeded = SharedVariables.prp_lahzeyeAkharLibrary.ReceivedMessage(message, service).Result;
                }

                else if (service.ServiceCode == "Hazaran")
                {
                    isSucceeded = SharedVariables.prp_hazaranLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Halghe")
                {
                    isSucceeded = SharedVariables.prp_halgheLibrary.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Achar")
                {
                    isSucceeded = SharedVariables.prp_acharLibrary.ReceivedMessage(message, service).Result;
                    //AcharLibrary.HandleMo h = new AcharLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode == "Hoshang")
                {
                    isSucceeded = SharedVariables.prp_hoshangLibrary.ReceivedMessage(message, service).Result;
                    //AcharLibrary.HandleMo h = new AcharLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
                else if (service.ServiceCode.ToLower() == "ChassisBoland".ToLower())
                {
                    isSucceeded = SharedVariables.prp_chassisBolandLibrary.ReceivedMessage(message, service).Result;
                    //AcharLibrary.HandleMo h = new AcharLibrary.HandleMo();
                    //isSucceeded = h.ReceivedMessage(message, service).Result;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChooseService: " + service.ServiceCode + "," + message.MobileNumber + "," + e);
            }
            return isSucceeded;
        }

        //private static void SendMessageUsingServiceCode(string serviceCode, MessageObject message)
        //{
        //    try
        //    {
        //        List<string> serviceCodes = new List<string> { "MyLeague", "Danestaneh" , "Mobiliga" , "MashinBazha" , "Soltan"
        //        ,"Tabriz2018" ,"SepidRood", "Tirandazi" , "BimeKarbala" ,"Boating" ,"DonyayeAsatir" ,"ShahreKalameh" ,"BimeIran"
        //        ,"JabehAbzar" , "Tamly" , "ShenoYad" , "FitShow","Takavar","MenchBaz","AvvalPod","AvvalYad","IrancellTest"
        //        ,"Soraty" ,"DefendIran","TahChin","Nebula" ,"Dezhban","MusicYad" ,"Phantom" ,"Medio" , "Dambel" , "Aseman"
        //        ,"Medad","PorShetab","TajoTakht","LahzeyeAkhar","Hazaran"};
        //        if (serviceCodes.Any(o => o == serviceCode))
        //            SharedShortCodeServiceLibrary.MessageHandler.InsertMessageToQueue(serviceCode, message);
        //        //if (serviceCode == "MyLeague")
        //        //    MyLeagueLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Danestaneh")
        //        //    DanestanehLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Mobiliga")
        //        //    MobiligaLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "MashinBazha")
        //        //    MashinBazhaLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Soltan")
        //        //    SoltanLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Tabriz2018")
        //        //    Tabriz2018Library.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "SepidRood")
        //        //    SepidRoodLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Tirandazi")
        //        //    TirandaziLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "BimeKarbala")
        //        //    BimeKarbalaLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Boating")
        //        //    BoatingLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "DonyayeAsatir")
        //        //    DonyayeAsatirLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "ShahreKalameh")
        //        //    ShahreKalamehLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "BimeIran")
        //        //    BimeIranLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "JabehAbzar")
        //        //    JabehAbzarLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Tamly")
        //        //    TamlyLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "ShenoYad")
        //        //    ShenoYadLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "FitShow")
        //        //    FitShowLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Takavar")
        //        //    TakavarLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "MenchBaz")
        //        //    MenchBazLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "AvvalPod")
        //        //    AvvalPodLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "AvvalYad")
        //        //    AvvalYadLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "IrancellTest")
        //        //    IrancellTestLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Soraty")
        //        //    SoratyLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "DefendIran")
        //        //    DefendIranLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "TahChin")
        //        //    TahChinLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Nebula")
        //        //    NebulaLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Dezhban")
        //        //    DezhbanLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "MusicYad")
        //        //    MusicYadLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Phantom")
        //        //    PhantomLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Medio")
        //        //    MedioLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Dambel")
        //        //    DambelLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Aseman")
        //        //    AsemanLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Medad")
        //        //    MedadLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "PorShetab")
        //        //    PorShetabLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "TajoTakht")
        //        //    TajoTakhtLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "LahzeyeAkhar")
        //        //    LahzeyeAkharLibrary.MessageHandler.InsertMessageToQueue(message);
        //        //else if (serviceCode == "Hazaran")
        //        //    HazaranLibrary.MessageHandler.InsertMessageToQueue(message);
        //    }
        //    catch (Exception e)
        //    {
        //        logs.Error("Exception in ChooseService: " + e);
        //    }
        //}
    }
}
