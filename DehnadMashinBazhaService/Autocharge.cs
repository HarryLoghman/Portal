using System;
using SharedLibrary.Models;
using MashinBazhaLibrary.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadMashinBazhaService
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
            var currentPersianDate = SharedLibrary.Date.GetPersianDate(DateTime.Now).Replace("/", "-");
            var autochargeTimeTable = GetAutochargeTimeTable();
            foreach (var item in autochargeTimeTable)
            {
                var sendTime = TimeSpan.Parse(item.SendTime);
                var SendEndTime = sendTime + TimeSpan.Parse("00:00:05");
                if (currentTime > sendTime && currentTime < SendEndTime)
                {
                    using (var entity = new MashinBazhaEntities())
                    {
                        entity.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, item.Tag, currentPersianDate, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, null);
                    }
                }
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
                using (var entity = new MashinBazhaEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var autochargeTimeTable = GetAutochargeTimeTable();
                    var carBrandsList = MashinBazhaLibrary.ContentManager.GetCarBrandsList();
                    if (carBrandsList == null)
                        return;
                    foreach (var item in autochargeTimeTable)
                    {
                        int tag = item.Tag;
                        var messages = new List<MessageObject>();
                        foreach(var carBrand in carBrandsList)
                        {
                            var subscribersForCarBrand = (from m in entity.SubscribersCarBrands where m.CarBrandId == carBrand.Id select m.Subscriberid);
                            var carBrandSubscribers = (from s in subscribers where subscribersForCarBrand.Contains(s.Id) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
                            foreach (var subscriber in carBrandSubscribers)
                            {
                                var autochargeContent = MashinBazhaLibrary.ContentManager.PrepareLatestMashinBazhaContent(subscriber.Id, carBrand.Id);
                                if (autochargeContent == null)
                                    continue;
                                autochargeContent.Content = MashinBazhaLibrary.MessageHandler.AddAutochargeHeaderAndFooter(autochargeContent.Content);
                                autochargeContent.Content = MashinBazhaLibrary.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.MobileNumber, serviceId.Value);
                                var imiChargeObject = MashinBazhaLibrary.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price, null);
                                var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag, imiChargeObject.Price);
                                messages.Add(message);
                            }
                        }
                        var subscribersCarBrand = (from m in entity.SubscribersCarBrands select m.Subscriberid);
                        var subscribersWithNoCarBrandSelected = (from s in subscribers where !subscribersCarBrand.Contains(s.Id) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
                        foreach (var subscriber in subscribersWithNoCarBrandSelected)
                        {
                            var autochargeContent = MashinBazhaLibrary.ContentManager.PrepareLatestMashinBazhaContent(subscriber.Id, carBrandsList.FirstOrDefault().Id);
                            if (autochargeContent == null)
                                continue;
                            autochargeContent.Content = MashinBazhaLibrary.MessageHandler.AddAutochargeHeaderAndFooter(autochargeContent.Content);
                            autochargeContent.Content = MashinBazhaLibrary.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.MobileNumber, serviceId.Value);
                            var imiChargeObject = MashinBazhaLibrary.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price, null);
                            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag, imiChargeObject.Price);
                            messages.Add(message);
                        }

                        MashinBazhaLibrary.MessageHandler.InsertBulkMessagesToQueue(messages);
                        MashinBazhaLibrary.MessageHandler.CreateMonitoringItem(null, SharedLibrary.MessageHandler.MessageType.AutoCharge, messages.Count, tag);
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
                using (var entity = new MashinBazhaEntities())
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