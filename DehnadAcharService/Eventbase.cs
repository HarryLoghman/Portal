using System;
using System.Linq;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DehnadAcharService
{
    class Eventbase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertEventbaseMessagesToQueue()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var eventbaseContent = entity.EventbaseContents.FirstOrDefault(o => o.IsAddingMessagesToSendQueue == true && o.IsAddedToSendQueueFinished == false);
                    if (eventbaseContent == null)
                        return;
                    if (eventbaseContent.Content == null || eventbaseContent.Content.Trim() == "")
                        return;
                    var aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                    var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                    SharedShortCodeServiceLibrary.MessageHandler.AddEventbaseMessagesToQueue(Properties.Settings.Default.ServiceCode 
                        ,Properties.Settings.Default.ServiceCode, eventbaseContent, aggregatorId);
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in Eventbase thread occurred: ", e);
            }
        }
        public void InsertBulkMessagesToEventBaseQueue()
        {
            try
            {
                using (var portalEntity = new PortalEntities())
                {
                    portalEntity.Configuration.AutoDetectChangesEnabled = false;
                    var serviceCode = Properties.Settings.Default.ServiceCode;
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(serviceCode);
                    var bulkContent = portalEntity.BulkLists.Where(o => o.ServiceCode == serviceCode && o.IsDone == false).ToList();
                    if (bulkContent == null)
                        return;
                    foreach (var item in bulkContent)
                    {
                        try
                        {
                            List<string> mobileNumbers;
                            if (item.MobileNumbersList.Contains(','))
                                mobileNumbers = item.MobileNumbersList.Split(',').ToList();
                            else
                                mobileNumbers = item.MobileNumbersList.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            //var existingSubscribers = portalEntity.Subscribers.Where(o => o.ServiceId == service.Id && o.DeactivationDate == null).Select(o => o.MobileNumber).ToList();
                            //var finalMobileNumbers = mobileNumbers.Where(o => !existingSubscribers.Any(e => o.Contains(e))).ToList();
                            var imiChargeObject = SharedShortCodeServiceLibrary.MessageHandler.GetImiChargeObjectFromPrice(Properties.Settings.Default.ServiceCode , 0, null);
                            var aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                            var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                            var rnd = new Random();
                            long contentId = rnd.Next(10000, 99999);

                            int takeSize = 10000;
                            int[] take = new int[(mobileNumbers.Count() / takeSize) + 1];
                            int[] skip = new int[(mobileNumbers.Count() / takeSize) + 1];
                            skip[0] = 0;
                            take[0] = takeSize;
                            for (int i = 1; i < take.Length; i++)
                            {
                                take[i] = takeSize;
                                skip[i] = skip[i - 1] + takeSize;
                            }

                            List<Task> TaskList = new List<Task>();
                            for (int i = 0; i < take.Length; i++)
                            {

                                var chunkedmobileNumbersList = mobileNumbers.Skip(skip[i]).Take(take[i]).ToList();
                                TaskList.Add(ProcessSubscribersListChunk(chunkedmobileNumbersList, item.Message, imiChargeObject, service.Id, aggregatorId, i, contentId));
                            }
                            Task.WaitAll(TaskList.ToArray());
                            SharedShortCodeServiceLibrary.MessageHandler.CreateMonitoringItem(Properties.Settings.Default.ServiceCode , contentId, SharedLibrary.MessageHandler.MessageType.EventBase, mobileNumbers.Count(), null);
                        }
                        catch (Exception e)
                        {
                            logs.Error("InsertBulkMessagesToEventBaseQueue one of the items", e);
                        }
                        item.IsDone = true;
                        portalEntity.Entry(item).State = System.Data.Entity.EntityState.Modified;
                        portalEntity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error(" Exception in InsertBulkMessagesToEventBaseQueue: ", e);
            }
        }

        public static async Task ProcessSubscribersListChunk(List<string> chunkedMobileNumbersList, string eventbaseContent
            , SharedLibrary.Models.ServiceModel.ImiChargeCode imiChargeObject, long serviceId, long aggregatorId, int taskId, long contentId)
        {
            logs.Info("ProcessSubscribersListChunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            await Task.Delay(10); // for making it async
            try
            {
                var counter = 1;
                var subscriber = new Subscriber();
                var messages = new List<MessageObject>();
                foreach (var mobileNumber in chunkedMobileNumbersList)
                {
                    if (counter > 1000)
                    {
                        SharedShortCodeServiceLibrary.MessageHandler.InsertBulkMessagesToQueue(Properties.Settings.Default.ServiceCode , messages);
                        messages.Clear();
                        counter = 1;
                    }
                    subscriber.MobileNumber = mobileNumber.Trim();
                    subscriber.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(subscriber.MobileNumber);
                    if (subscriber.MobileNumber == "Invalid Mobile Number")
                        continue;
                    subscriber.Id = 0;
                    subscriber.OperatorPlan = 0;
                    subscriber.MobileOperator = 1;
                    subscriber.ServiceId = serviceId;
                    var content = eventbaseContent;
                    if (content.Contains("{MSISDN}"))
                    {
                        content = content.Replace("{MSISDN}", subscriber.MobileNumber);
                    }
                    var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, contentId, SharedLibrary.MessageHandler.MessageType.EventBase, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, 0, null, 0);
                    messages.Add(message);
                    counter++;
                }
                SharedShortCodeServiceLibrary.MessageHandler.InsertBulkMessagesToQueue(Properties.Settings.Default.ServiceCode, messages);
            }
            catch (Exception e)
            {
                logs.Error("Exception in AddEventbaseMessagesToQueue ProcessSubscribersListChunk: ", e);
            }
        }
    }
}
