using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BimeIranLibrary.Models;
using BimeIranLibrary;
using System.Data.Entity;

namespace DehnadBimeIranService
{
    public class Reminder
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Process()
        {
            try
            {
                SendInsuranceNumbersToUsers();
                //SendWarningToUsers();
                CancelExpiredInsurances();
                RenewInsurance();
                
            }
            catch (Exception e)
            {
                logs.Error("Exception in Reminder Process : ", e);
            }
        }

        private void CancelExpiredInsurances()
        {
            using (var entity = new BimeIranEntities())
            {
                try
                {
                    var now = DateTime.Now;
                    if (DateTime.Now.Hour > 8 && DateTime.Now.Hour < 21)
                    {
                        int batchSaveCounter = 0;
                        var serviceId = SharedLibrary.ServiceHandler.GetServiceId("BimeIran");
                        var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                        var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(0, null);
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("BimeIran", "Telepromo");
                        var aggregatorName = Properties.Settings.Default.AggregatorName;
                        var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                        var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
                        var cancelList = entity.InsuranceInfoes.Where(o => o.IsUserRequestedInsuranceCancelation != true && o.IsCancelationSendedToInsuranceCompany != true && o.NextRenewalDate.Value < now).ToList();
                        foreach (var item in cancelList)
                        {
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(item.MobileNumber, serviceId.Value);
                            bool isCanceled = BimeIranLibrary.ContentManager.CancelUserInsuranceIfExists(subscriber);
                            if (isCanceled)
                            {
                                var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, "", 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeCodes.FirstOrDefault(), aggregatorId, 0, null, 0);
                                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                                message.Content = messagesTemplate.Where(o => o.Title == "InsuranceExpired").Select(o => o.Content).FirstOrDefault();
                                MessageHandler.InsertMessageToQueue(message);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in Reminder CancelExpiredInsurances : ", e);
                }
            }
        }

        private void RenewInsurance()
        {
            using (var entity = new BimeIranEntities())
            {
                try
                {
                    if (DateTime.Now.Hour > 8 && DateTime.Now.Hour < 21)
                    {
                        int batchSaveCounter = 0;
                        var serviceId = SharedLibrary.ServiceHandler.GetServiceId("BimeIran");
                        var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                        var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(0, null);
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("BimeIran", "Telepromo");
                        var aggregatorName = Properties.Settings.Default.AggregatorName;
                        var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                        var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
                        var now = DateTime.Now;
                        var renewList = entity.InsuranceInfoes.Where(o => o.IsUserRequestedInsuranceCancelation != true && o.IsCancelationSendedToInsuranceCompany != true && o.NextRenewalDate.Value.Date <= DbFunctions.AddDays(now.Date, 7) && o.NextRenewalDate.Value.Date >= now.Date).ToList();
                        foreach (var item in renewList)
                        {
                            if (batchSaveCounter > 500)
                            {
                                batchSaveCounter = 0;
                                entity.SaveChanges();
                            }
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(item.MobileNumber, serviceId.Value);
                            var additionalSubscriberInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriber.Id);
                            if (additionalSubscriberInfo.DateWarningSent.Value.Date == now.Date)
                                continue;

                            var imiChargeCode = BimeIranLibrary.ContentManager.SelectChargeCode(imiChargeCodes, item.InsuranceType, item.NextRenewalDate.Value);
                            var isSucceeded = TryToChargeUserForRenewal(imiChargeCode, subscriber, aggregatorId, serviceAdditionalInfo);

                            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, "", 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeCode, aggregatorId, 0, null, 0);
                            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                            if (isSucceeded)
                            {
                                var nextRenewalDate = BimeIranLibrary.ContentManager.GetNextRenewalDate(item.NextRenewalDate.Value);
                                item.NextRenewalDate = nextRenewalDate;
                                item.PersianNextRenewalDate = SharedLibrary.Date.GetPersianDateTime(nextRenewalDate);
                                additionalSubscriberInfo.DateWarningSent = null;
                                additionalSubscriberInfo.NumberOfWarningsSent = 0;
                                entity.Entry(item).State = EntityState.Modified;
                                message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeSuccessful").Select(o => o.Content).FirstOrDefault();

                            }
                            else
                            {
                                additionalSubscriberInfo.DateWarningSent = now;
                                if (additionalSubscriberInfo.NumberOfWarningsSent == null)
                                    additionalSubscriberInfo.NumberOfWarningsSent = 1;
                                else
                                    additionalSubscriberInfo.NumberOfWarningsSent++;
                                additionalSubscriberInfo.SubscriberLevel = 7;
                                var daysTillExpire = item.NextRenewalDate.Value.Subtract(now).TotalDays;
                                if (daysTillExpire == 7)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance7DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 6)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance6DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 5)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance5DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 4)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance4DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 3)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance3DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 2)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance2DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else if (daysTillExpire == 1)
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance1DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                                else
                                    message.Content = messagesTemplate.Where(o => o.Title == "Insurance0DaysTillExpire").Select(o => o.Content).FirstOrDefault();
                            }
                            entity.Entry(additionalSubscriberInfo).State = EntityState.Modified;
                            MessageHandler.InsertMessageToQueue(message);
                            batchSaveCounter++;
                        }
                        entity.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    entity.SaveChanges();
                    logs.Error("Exception in Reminder RenewInsurance : ", e);
                }
            }
        }

        private bool TryToChargeUserForRenewal(ImiChargeCode imiChargeCode, SharedLibrary.Models.Subscriber subscriber, long aggregatorId, Dictionary<string, string> serviceAdditionalInfo)
        {
            Singlecharge singlecharge = new Singlecharge();
            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, "Renewal", 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeCode, aggregatorId, 0, null, 0);
            message = MessageHandler.SetImiChargeInfo(message, imiChargeCode.Price, 0, null);
            singlecharge = MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo).Result;

            if (singlecharge.IsSucceeded == true)
                return true;
            else
                return false;
        }

        private void SendInsuranceNumbersToUsers()
        {
            try
            {
                int batchSaveCounter = 0;
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId("BimeIran");
                var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                var imiChargeObject = MessageHandler.GetImiChargeObjectFromPrice(0, null);
                var aggregatorName = Properties.Settings.Default.AggregatorName;
                var aggregatorId = SharedLibrary.MessageHandler.GetAggregatorIdFromConfig(aggregatorName);
                using (var entity = new BimeIranEntities())
                {
                    var users = entity.InsuranceInfoes.Where(o => o.InsuranceNo != null && o.IsInsuranceNumberSendedToUser != true).ToList();
                    if (users.Count > 0)
                    {
                        var messageContent = messagesTemplate.Where(o => o.Title == "InformInsuranceNumber").Select(o => o.Content).FirstOrDefault();
                        foreach (var user in users)
                        {
                            if (batchSaveCounter > 1000)
                            {
                                batchSaveCounter = 0;
                                entity.SaveChanges();
                            }
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(user.MobileNumber, serviceId.Value);
                            var message = SharedLibrary.MessageHandler.CreateMessage(subscriber, messageContent, 0, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, 0, imiChargeObject, aggregatorId, 0, null, 0);
                            MessageHandler.InsertMessageToQueue(message);
                            user.IsInsuranceNumberSendedToUser = true;
                            entity.Entry(user).State = EntityState.Modified;
                            batchSaveCounter++;
                        }
                        entity.SaveChanges();

                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Reminder SendInsuranceNumbersToUsers : ", e);
            }
        }

        private void SendWarningToUsers()
        {
            try
            {
                var today = DateTime.Now;
                int batchSaveCounter = 0;
                var serviceId = SharedLibrary.ServiceHandler.GetServiceId("BimeIran");
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId.GetValueOrDefault());
                if (serviceInfo == null)
                {
                    logs.Info("serviceInfo is null in SendWarningToUsers!");
                    return;
                }
                var shortCode = serviceInfo.ShortCode;
                using (var entity = new BimeIranEntities())
                {
                    var warningList = entity.SubscribersAdditionalInfoes.Where(o => (o.SubscriberLevel == 2 || o.SubscriberLevel == 3 || o.SubscriberLevel == 4) && o.DateWarningSent.Value.Date != today.Date).ToList();
                    foreach (var item in warningList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }

                        if ((item.SubscriberLevel == 2 || item.SubscriberLevel == 3) && item.NumberOfWarningsSent > 3)
                            continue;
                        if (item.SubscriberLevel == 6 && item.NumberOfWarningsSent > 7)
                        {
                            //Cancel user insurance
                            continue;
                        }
                        item.NumberOfWarningsSent++;
                        item.DateWarningSent = DateTime.Now;
                        entity.Entry(item).State = EntityState.Modified;
                        batchSaveCounter += 1;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Reminder SendWarningToUsers : ", e);
            }
        }
    }
}
