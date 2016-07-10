using System;
using Portal.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadDanestanService
{
    class Autocharge
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void InsertAutochargeMessagesToQueue()
        {
            try
            {
                var time = DateTime.Now.TimeOfDay;
                var insertTime = TimeSpan.Parse(Properties.Resources.InsertAutochargeMessageInQueueTime);
                var insertEndTime = TimeSpan.Parse(Properties.Resources.InsertAutochargeMessageInQueueEndTime);
                if (time < insertTime && time > insertEndTime)
                    return;
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
                        var messages = new List<MessageObject>();
                        foreach (var subscriber in subscribers)
                        {
                            var autochargeContent = Portal.Services.Danestan.ServiceHandler.SelectAutochargeContentByDate(entity, subscriber.Id);
                            if (autochargeContent == null)
                                continue;
                            autochargeContent.Content = Portal.Services.Danestan.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.Id);
                            var imiChargeCode = Portal.Services.Danestan.MessageHandler.GetImiChargeCodeFromPrice(autochargeContent.Price);
                            var message = Portal.Shared.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, Portal.Shared.MessageHandler.MessageType.AutoCharge, Portal.Shared.MessageHandler.ProcessStatus.InQueue, 0, imiChargeCode, aggregatorId, autochargeContent.Point);
                            messages.Add(message);
                        }
                        Portal.Services.Danestan.MessageHandler.InsertBulkMessagesToQueue(messages);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Info(" Exception in Autocharge thread occurred ");
                logs.Error(" Exception in Autocharge thread occurred: ", e);
            }
        }
    }
}