using System;
using SharedLibrary.Models;
using DanestanehLibrary.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadDanestanehService
{
    class Autocharge
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Job()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var insertTime = TimeSpan.Parse(Properties.Settings.Default.InsertAutochargeMessageInQueueTime);
                var insertEndTime = TimeSpan.Parse(Properties.Settings.Default.InsertAutochargeMessageInQueueEndTime);
                if (time > insertTime && time < insertEndTime)
                    InsertLeveledAutochargeMessagesToQueue();

                SendAutochargeOnTime();
            }
            catch (Exception e)
            {
                logs.Error("Error in Autocharge Job:" + e);
            }
        }

        private void SendAutochargeOnTime()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentPersianDate = SharedLibrary.Date.GetPersianDate(DateTime.Now).Replace("/", "-");
            var autochargeTimeTable = GetAutochargeTimeTable();
            foreach (var item in autochargeTimeTable)
            {
                var sendTime = TimeSpan.Parse(item.SendTime);
                var SendEndTime = sendTime + TimeSpan.Parse("00:00:05");
                if (currentTime > sendTime && currentTime < SendEndTime)
                {
                    using (var entity = new DanestanehEntities())
                    {
                        entity.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, item.Tag, currentPersianDate, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, null);
                    }
                }
            }
        }


        public void InsertLeveledAutochargeMessagesToQueue()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var aggregatorName = Properties.Settings.Default.AggregatorName;
                var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
                var serviceInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                var subscribers = SharedLibrary.ServiceHandler.GetServiceActiveSubscribersFromServiceId(serviceId.Value);
                using (var entity = new DanestanehEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)entity).ObjectContext;
                    objCtx.ExecuteStoreCommand("TRUNCATE TABLE [Danestaneh].[dbo].[LeveledAutochargeContentDailyList]");

                    int tag = 1;
                    var messages = new List<MessageObject>();
                    foreach (var subscriber in subscribers)
                    {
                        var message = DanestanehLibrary.ContentManager.LeveledAutochargeMessage(subscriber, serviceId.Value,serviceInfo["shortCode"], tag, aggregatorId, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue);
                        if (message != null)
                            messages.Add(message);
                    }
                    DanestanehLibrary.MessageHandler.InsertBulkMessagesToQueue(messages);
                    DanestanehLibrary.MessageHandler.CreateMonitoringItem(null, SharedLibrary.MessageHandler.MessageType.AutoCharge, messages.Count, tag);
                }
            }
            catch (Exception e)
            {
                logs.Info(" Exception in Autocharge thread occurred ");
                logs.Error(" Exception in Autocharge thread occurred: ", e);
            }
        }

        public void InsertAutochargeMessagesToQueue()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var aggregatorName = Properties.Settings.Default.AggregatorName;
                var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
                var subscribers = SharedLibrary.ServiceHandler.GetServiceActiveSubscribersFromServiceId(serviceId.Value);
                using (var entity = new DanestanehEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var autochargeTimeTable = GetAutochargeTimeTable();
                    foreach (var item in autochargeTimeTable)
                    {
                        int tag = item.Tag;
                        var messages = new List<MessageObject>();
                        foreach (var subscriber in subscribers)
                        {
                            var autochargeContent = DanestanehLibrary.ServiceHandler.SelectAutochargeContentByDate(entity, subscriber.Id);
                            if (autochargeContent == null)
                                continue;
                            autochargeContent.Content = DanestanehLibrary.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.MobileNumber, serviceId.Value);
                            var imiChargeObject = DanestanehLibrary.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price, null);
                            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag);
                            messages.Add(message);
                        }
                        DanestanehLibrary.MessageHandler.InsertBulkMessagesToQueue(messages);
                        DanestanehLibrary.MessageHandler.CreateMonitoringItem(null, SharedLibrary.MessageHandler.MessageType.AutoCharge, messages.Count, tag);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Info(" Exception in Autocharge thread occurred ");
                logs.Error(" Exception in Autocharge thread occurred: ", e);
            }
        }

        private static List<AutochargeTimeTable> GetAutochargeTimeTable()
        {
            var autochargeTimeTable = new List<AutochargeTimeTable>();
            try
            {
                using (var entity = new DanestanehEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    autochargeTimeTable = entity.AutochargeTimeTables.ToList();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetAutochargeTimeTable:" + e);
            }
            return autochargeTimeTable;
        }
    }
}