﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DambelLibrary.Models;
using DambelLibrary;
using System.Data.Entity;

namespace DehnadDambelService
{
    public class SinglechargeQueue
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void ProcessQueue()
        {
            try
            {
                RemoveUsersFromSinglechargeWaitingList();
                //SendWarningToSinglechargeUsersInQueue();
                //ChargeUsersFromSinglechargeQueue();
                //SendRenewalWarningToSinglechargeUsersInQueue();
                //RenewSinglechargeInstallmentQueue();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue ProcessQueue : ", e);
            }
        }

        private void RemoveUsersFromSinglechargeWaitingList()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new DambelEntities())
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings/*.Where(o => DbFunctions.AddHours(o.DateAdded, 2) <= now)*/.ToList();
                    if (QueueList.Count == 0)
                        return;
                    var mobileNumbers = QueueList.Select(o => o.MobileNumber).ToList();
                    foreach (var item in QueueList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        entity.SinglechargeWaitings.Remove(item);
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();

                    var maxChargeLimit = SinglechargeInstallmentClass.maxChargeLimit;
                    string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                    var serviceCode = Properties.Settings.Default.ServiceCode;
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                    int installmentListCount = mobileNumbers.Count;
                    var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                    SinglechargeInstallmentClass.InstallmentJob(maxChargeLimit, 0, 0, serviceCode, chargeCodes, mobileNumbers, installmentListCount, installmentListTakeSize, serviceAdditionalInfo);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue RemoveUsersFromSinglechargeWaitingList : ", e);
            }
        }

        private void SendRenewalWarningToSinglechargeUsersInQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new DambelEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeInstallments.Where(o => DbFunctions.AddDays(o.DateCreated, 29) < now && now < DbFunctions.AddDays(o.DateCreated, 30) && o.IsUserCanceledTheInstallment == false && o.IsRenewalMessageSent != true).ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Dambel");
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                    if (serviceInfo == null)
                    {
                        logs.Info("serviceInfo is null in SendRenewalWarningToSinglechargeUsersInQueue!");
                        return;
                    }
                    var shortCode = serviceInfo.ShortCode;
                    foreach (var item in QueueList)
                    {
                        var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(item.MobileNumber, serviceId.GetValueOrDefault());
                        var content = entity.MessagesTemplates.FirstOrDefault(o => o.Title == "RenewalSinglechargeMessage").Content;
                        var imiChargeObject = DambelLibrary.MessageHandler.GetImiChargeObjectFromPrice(0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeObject, serviceInfo.AggregatorId, 0, null, imiChargeObject.Price);
                        DambelLibrary.MessageHandler.InsertMessageToQueue(message);
                        item.IsRenewalMessageSent = true;
                        entity.Entry(item).State = EntityState.Modified;
                        if (batchSaveCounter > 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        batchSaveCounter++;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue SendRenewalWarningToSinglechargeUsersInQueue : ", e);
            }
        }


        private void SendWarningToSinglechargeUsersInQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new DambelEntities())
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings.Where(o => DbFunctions.AddDays(o.DateAdded, 2) < now && o.IsLastDayWarningSent == false).ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Dambel");
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
                        var imiChargeObject = DambelLibrary.MessageHandler.GetImiChargeObjectFromPrice(0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                        var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, content, 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeObject, serviceInfo.AggregatorId, 0, null, imiChargeObject.Price);
                        DambelLibrary.MessageHandler.InsertMessageToQueue(message);
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

        private void RenewSinglechargeInstallmentQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new DambelEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeInstallments.Where(o => now > DbFunctions.AddDays(o.DateCreated, 30) && o.DateCreated > DbFunctions.AddDays(now, -32) && o.IsUserCanceledTheInstallment != true && o.IsRenewd != true).ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Dambel");
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                    if (serviceInfo == null)
                    {
                        logs.Info("serviceInfo is null in RenewSinglechargeInstallmentQueue!");
                        return;
                    }
                    var shortCode = serviceInfo.ShortCode;
                    foreach (var item in QueueList)
                    {
                        var isMobileExists = entity.SinglechargeInstallments.Where(o => o.MobileNumber == item.MobileNumber && o.IsUserCanceledTheInstallment != true && o.DateCreated < now && o.DateCreated > DbFunctions.AddDays(now, -2)).FirstOrDefault();
                        if (isMobileExists != null)
                            continue;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }

                        var installment = new SinglechargeInstallment();
                        installment.MobileNumber = item.MobileNumber;
                        installment.TotalPrice = item.TotalPrice;
                        installment.IsExceededDailyChargeLimit = false;
                        installment.IsFullyPaid = false;
                        installment.PricePayed = 0;
                        installment.DateCreated = DateTime.Now;
                        installment.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                        installment.PriceTodayCharged = 0;
                        installment.IsUserDailyChargeBalanced = false;
                        installment.IsUserCanceledTheInstallment = false;
                        installment.IsRenewalMessageSent = false;
                        installment.IsRenewd = false;
                        entity.SinglechargeInstallments.Add(installment);
                        item.IsRenewd = true;
                        entity.Entry(item).State = EntityState.Modified;
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeQueue RenewSinglechargeInstallmentQueue : ", e);
            }
        }

        private void ChargeUsersFromSinglechargeQueue()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                using (var entity = new DambelEntities())
                {

                    var chargeCodes = entity.ImiChargeCodes.ToList();
                    var now = DateTime.Now;
                    var QueueList = entity.SinglechargeWaitings/*.Where(o => DbFunctions.AddHours(o.DateAdded, 2) <= now)*/.ToList();
                    var serviceId = SharedLibrary.ServiceHandler.GetServiceId("Dambel");
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

                        var installment = new SinglechargeInstallment();
                        installment.MobileNumber = message.MobileNumber;
                        installment.TotalPrice = item.Price;
                        installment.IsExceededDailyChargeLimit = false;
                        installment.IsFullyPaid = false;
                        installment.PricePayed = 0;
                        installment.DateCreated = DateTime.Now;
                        installment.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                        installment.PriceTodayCharged = 0;
                        installment.IsUserDailyChargeBalanced = false;
                        installment.IsUserCanceledTheInstallment = false;
                        installment.IsRenewalMessageSent = false;
                        installment.IsRenewd = false;
                        entity.SinglechargeInstallments.Add(installment);
                        //}
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
