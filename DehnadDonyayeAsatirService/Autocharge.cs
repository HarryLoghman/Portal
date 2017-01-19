using System;
using SharedLibrary.Models;
using DonyayeAsatirLibrary.Models;
using System.Linq;
using System.Collections.Generic;

namespace DehnadDonyayeAsatirService
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
                    using (var entity = new DonyayeAsatirEntities())
                    {
                        entity.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, item.Tag, currentPersianDate, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, null);
                    }
                }
            }
        }

        public void InsertAutochargeMessagesToQueue()
        {
            //try
            //{
            //    var time = DateTime.Now.TimeOfDay;
            //    var aggregatorName = Properties.Settings.Default.AggregatorName;
            //    var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
            //    var serviceCode = Properties.Settings.Default.ServiceCode;
            //    var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode);
            //    var subscribers = SharedLibrary.ServiceHandler.GetServiceActiveSubscribersFromServiceId(serviceId.Value);
            //    using (var entity = new DonyayeAsatirEntities())
            //    {
            //        entity.Configuration.AutoDetectChangesEnabled = false;
            //        var autochargeTimeTable = GetAutochargeTimeTable();
            //        var mobilesList = DonyayeAsatirLibrary.ContentManager.GetMobilesList();
            //        if (mobilesList == null)
            //            return;
            //        foreach (var item in autochargeTimeTable)
            //        {
            //            int tag = item.Tag;
            //            var messages = new List<MessageObject>();
            //            foreach(var mobile in mobilesList)
            //            {
            //                var subscribersForMobile = (from m in entity.SubscribersMobiles where m.MobileId == mobile.Id select m.Subscriberid);
            //                var mobileSubscribers = (from s in subscribers where subscribersForMobile.Contains(s.Id) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
            //                foreach (var subscriber in mobileSubscribers)
            //                {
            //                    var autochargeContent = DonyayeAsatirLibrary.ContentManager.PrepareLatestMobileContent(subscriber.Id, mobile.Id);
            //                    if (autochargeContent == null)
            //                        continue;
            //                    autochargeContent.Content = DonyayeAsatirLibrary.MessageHandler.AddAutochargeHeaderAndFooter(autochargeContent.Content);
            //                    autochargeContent.Content = DonyayeAsatirLibrary.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.MobileNumber, serviceId.Value);
            //                    var imiChargeObject = DonyayeAsatirLibrary.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price, null);
            //                    var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag);
            //                    messages.Add(message);
            //                }
            //            }
            //            var subscribersMobiles = (from m in entity.SubscribersMobiles select m.Subscriberid);
            //            var subscribersWithNoMobileSelected = (from s in subscribers where !subscribersMobiles.Contains(s.Id) select new { MobileNumber = s.MobileNumber, Id = s.Id, ServiceId = s.ServiceId, OperatorPlan = s.OperatorPlan, MobileOperator = s.MobileOperator }).Distinct().AsEnumerable().Select(x => new Subscriber { Id = x.Id, MobileNumber = x.MobileNumber, ServiceId = x.ServiceId, OperatorPlan = x.OperatorPlan, MobileOperator = x.MobileOperator }).ToList();
            //            foreach (var subscriber in subscribersWithNoMobileSelected)
            //            {
            //                var autochargeContent = DonyayeAsatirLibrary.ContentManager.PrepareLatestMobileContent(subscriber.Id, mobilesList.FirstOrDefault().Id);
            //                if (autochargeContent == null)
            //                    continue;
            //                autochargeContent.Content = DonyayeAsatirLibrary.MessageHandler.AddAutochargeHeaderAndFooter(autochargeContent.Content);
            //                autochargeContent.Content = DonyayeAsatirLibrary.MessageHandler.HandleSpecialStrings(autochargeContent.Content, autochargeContent.Point, subscriber.MobileNumber, serviceId.Value);
            //                var imiChargeObject = DonyayeAsatirLibrary.MessageHandler.GetImiChargeObjectFromPrice(autochargeContent.Price, null);
            //                var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, autochargeContent.Content, autochargeContent.Id, SharedLibrary.MessageHandler.MessageType.AutoCharge, SharedLibrary.MessageHandler.ProcessStatus.InQueue, 0, imiChargeObject, aggregatorId, autochargeContent.Point, tag);
            //                messages.Add(message);
            //            }

            //            DonyayeAsatirLibrary.MessageHandler.InsertBulkMessagesToQueue(messages);
            //            DonyayeAsatirLibrary.MessageHandler.CreateMonitoringItem(null, SharedLibrary.MessageHandler.MessageType.AutoCharge, messages.Count, tag);
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    logs.Info(" Exception in Autocharge thread occurred ");
            //    logs.Error(" Exception in Autocharge thread occurred: ", e);
            //}
        }

        private static List<AutochargeTimeTable> GetAutochargeTimeTable()
        {
            var autochargeTimeTable = new List<AutochargeTimeTable>();
            try
            {
                using (var entity = new DonyayeAsatirEntities())
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