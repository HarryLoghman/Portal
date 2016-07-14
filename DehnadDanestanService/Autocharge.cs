﻿using System;
using Portal.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadDanestanService
{
    class Autocharge
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Job()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var insertTime = TimeSpan.Parse(Properties.Resources.InsertAutochargeMessageInQueueTime);
                var insertEndTime = TimeSpan.Parse(Properties.Resources.InsertAutochargeMessageInQueueEndTime);
                if (time < insertTime && time > insertEndTime)
                    InsertAutochargeMessagesToQueue();

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
            var currentPersianDate = Portal.Shared.Date.GetPersianDate(DateTime.Now).Replace("/","-");
            var autochargeTimeTable = GetAutochargeTimeTable();
            foreach (var item in autochargeTimeTable)
            {
                var SendEndTime = item.SendTime + TimeSpan.Parse("00:00:05");
                if (currentTime > item.SendTime && currentTime < SendEndTime)
                {
                    using (var entity = new DanestanEntities())
                    {
                        entity.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.AutoCharge, null, item.Tag, currentPersianDate, (int)Portal.Shared.MessageHandler.ProcessStatus.InQueue, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend);
                    }
                }
            }
        }

        public void InsertAutochargeMessagesToQueue()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var aggregatorName = Properties.Resources.AggregatorName;
                var aggregatorId = Portal.Shared.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                var serviceCode = Properties.Resources.ServiceCode;
                var portalEntity = new PortalEntities();
                var serviceId = portalEntity.Services.Where(o => o.ServiceCode == serviceCode).FirstOrDefault().Id;
                var subscribers = portalEntity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).ToList();
                portalEntity.Dispose();
                using (var entity = new DanestanEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var numberOfAutochargeMessagesPerDay = Convert.ToInt32(Properties.Resources.NumberOfAutochargeMessagesPerDay);
                    for (int i = 1; i <= numberOfAutochargeMessagesPerDay; i++)
                    {
                        int tag = i;
                        var messages = new List<MessageObject>();
                        foreach (var subscriber in subscribers)
                        {
                            var autochargeContent = Portal.Services.Danestan.ServiceHandler.SelectAutochargeContentByDate(entity, subscriber.Id);
                            if (autochargeContent == null)
                                continue;
                            autochargeContent.Content = Portal.Services.Danestan.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.Id, serviceId);
                            var imiChargeObject = Portal.Services.Danestan.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price);
                            var message = Portal.Shared.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, Portal.Shared.MessageHandler.MessageType.AutoCharge, Portal.Shared.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag);
                            messages.Add(message);
                        }
                        Portal.Services.Danestan.MessageHandler.InsertBulkMessagesToQueue(messages);
                        Portal.Services.Danestan.MessageHandler.CreateMonitoringItem(null, Portal.Shared.MessageHandler.MessageType.AutoCharge, messages.Count, tag);
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
                using (var entity = new DanestanEntities())
                {
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