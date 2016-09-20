using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoltanLibrary.Models;
using SoltanLibrary;
using System.Data.Entity;

namespace DehnadSoltanService
{
    public class SinglechargeQueue
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessQueue()
        {
            try
            {
                SendWarningToSinglechargeUsersInQueue();
                ChargeUsersFromSinglechargeQueue();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue ProcessQueue : ", e);
            }
        }

        private void SendWarningToSinglechargeUsersInQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new SoltanEntities())
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings.Where(o => DbFunctions.AddDays(o.DateAdded, 2) < now && o.IsLastDayWarningSent == false).ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Soltan");
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                    if (serviceInfo == null)
                    {
                        logs.Info("serviceInfo is null in SendWarningToSinglechargeUsersInQueue!");
                        return;
                    }
                    var shortCode = serviceInfo.ShortCode;
                    foreach (var item in QueueList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(item.MobileNumber, serviceId.GetValueOrDefault());
                        var content = entity.MessagesTemplates.FirstOrDefault(o => o.Title == "OneDayRemainedTillSinglecharge").Content;
                        var imiChargeObject = SoltanLibrary.MessageHandler.GetImiChargeObjectFromPrice(0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeObject, serviceInfo.AggregatorId, 0, null);
                        SoltanLibrary.MessageHandler.InsertMessageToQueue(message);
                        item.IsLastDayWarningSent = true;
                        entity.Entry(item).State = EntityState.Modified;
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue SendWarningToSinglechargeUsersInQueue : ", e);
            }
        }

        private void ChargeUsersFromSinglechargeQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new SoltanEntities())
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings.Where(o => DbFunctions.AddDays(o.DateAdded, 3) <= now).ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Soltan");
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                    if (serviceInfo == null)
                    {
                        logs.Info("serviceInfo is null in ChargeUsersFromSinglechargeQueue!");
                        return;
                    }
                    var shortCode = serviceInfo.ShortCode;
                    foreach (var item in QueueList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = item.MobileNumber;
                        message.ImiChargeKey = chargeCodes.FirstOrDefault(o => o.Price == item.Price).ChargeKey;
                        message.ShortCode = shortCode;
                        message.Price = item.Price;
                        var singlecharge = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                        if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient balance"))
                        {
                            var installment = new SinglechargeInstallment();
                            installment.MobileNumber = message.MobileNumber;
                            installment.TotalPrice = message.Price.GetValueOrDefault();
                            installment.IsExceededDailyChargeLimit = false;
                            installment.IsFullyPaid = false;
                            installment.PricePayed = 0;
                            installment.DateCreated = DateTime.Now;
                            installment.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                            installment.PriceTodayCharged = 0;
                            installment.IsUserDailyChargeBalanced = false;
                            entity.SinglechargeInstallments.Add(installment);
                        }
                        entity.SinglechargeWaitings.Remove(item);
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue ChargeUsersFromSinglechargeQueue : ", e);
            }
        }
    }
}
