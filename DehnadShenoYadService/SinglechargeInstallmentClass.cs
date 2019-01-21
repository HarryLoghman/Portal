using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models.ServiceModel;

using System.Data.Entity;
using System.Threading;

namespace DehnadShenoYadService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int maxChargeLimit = 400;
        public void ProcessInstallment(int installmentCycleNumber)
        {
            InstallmentJob(installmentCycleNumber);

            //if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 10)
            //    InstallmentDailyBalance();
            //else
            //    InstallmentJob();
            //if (DateTime.Now.Hour == 5 || DateTime.Now.Hour == 6 || DateTime.Now.Hour == 7)
            //{
            //    ResetUserDailyChargeBalanceValue();
            //}
        }

        private void DeactivateChargingUsersAfter30Days()
        {
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    var today = DateTime.Now;
                    entity.SinglechargeInstallments.Where(o => DbFunctions.AddDays(o.DateCreated, 30) < today).ToList().ForEach(o => o.IsFullyPaid = true);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentDailyBalance: ", e);
            }
        }

        public void ResetUserDailyChargeBalanceValue()
        {
            try
            {
                int batchSaveCounter = 0;
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var userDailyBalnace = entity.SinglechargeInstallments.Where(o => o.IsUserDailyChargeBalanced == true).ToList();
                    foreach (var item in userDailyBalnace)
                    {
                        if (batchSaveCounter >= 1000)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        item.IsUserDailyChargeBalanced = false;
                        entity.Entry(item).State = EntityState.Modified;
                        batchSaveCounter++;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentDailyBalance: ", e);
            }

        }

        public void InstallmentDailyBalance()
        {
            try
            {
                int batchSaveCounter = 0;
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsUserDailyChargeBalanced == false && o.IsUserCanceledTheInstallment == false).ToList();
                    var today = DateTime.Now.Date;
                    foreach (var installment in installmentList)
                    {
                        if (batchSaveCounter >= 1000)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        if ((installment.PriceTodayCharged + installment.PricePayed) >= installment.TotalPrice)
                        {
                            installment.IsFullyPaid = true;
                            installment.PricePayed = installment.TotalPrice;
                        }
                        else
                        {
                            if (installment.DateCreated.AddDays(30) < DateTime.Now)
                                installment.IsFullyPaid = true;
                        }
                        installment.PriceTodayCharged = 0;
                        installment.IsExceededDailyChargeLimit = false;
                        //installment.IsUserDailyChargeBalanced = true;
                        entity.Entry(installment).State = EntityState.Modified;
                        batchSaveCounter++;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentDailyBalance: ", e);
            }
        }

        private void InstallmentJob(int installmentCycleNumber)
        {
            try
            {
                logs.Info("InstallmentJob start!");
                string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("ShenoYad", aggregatorName);
                List<SinglechargeInstallment> installmentList;
                List<SharedLibrary.Models.Subscriber> subscribers = new List<SharedLibrary.Models.Subscriber>();
                List<ImiChargeCode> chargeCodes;
                logs.Info("installmentCycleNumber:" + installmentCycleNumber + " started");
                int takeSize;
                if (installmentCycleNumber == 1)
                {
                    using (var portalEntity = new SharedLibrary.Models.PortalEntities())
                    {
                        portalEntity.Configuration.AutoDetectChangesEnabled = false;
                        logs.Info("installmentCycleNumber 1 getting the postpaid subscribers list");
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("ShenoYad");
                        subscribers = portalEntity.Subscribers.Where(o => o.ServiceId == service.Id && o.DeactivationDate == null && o.OperatorPlan == 1).ToList();
                    }
                }
                else if (installmentCycleNumber == 2)
                {
                    using (var portalEntity = new SharedLibrary.Models.PortalEntities())
                    {
                        portalEntity.Configuration.AutoDetectChangesEnabled = false;
                        logs.Info("installmentCycleNumber 2 getting the prepaid subscribers list");
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("ShenoYad");
                        subscribers = portalEntity.Subscribers.Where(o => o.ServiceId == service.Id && o.DeactivationDate == null && o.OperatorPlan == 2).ToList();
                    }
                }
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    chargeCodes = entity.ImiChargeCodes.Where(o => o.Price <= maxChargeLimit).ToList();
                    if (installmentCycleNumber == 1)
                    {
                        takeSize = Properties.Settings.Default.FirstSingleChargeTakeSize;
                        logs.Info("installmentCycleNumber 1 getting the postpaid installment list");
                        installmentList = (from installment in entity.SinglechargeInstallments
                                           where installment.IsFullyPaid == false
                                           && installment.IsExceededDailyChargeLimit == false
                                           && installment.IsUserCanceledTheInstallment == false
                                           select installment).ToList();
                        installmentList = (from ins in installmentList
                                           where (from sub in subscribers select sub.MobileNumber).Contains(ins.MobileNumber)
                                           select ins).ToList();
                    }
                    else if (installmentCycleNumber == 2)
                    {
                        logs.Info("installmentCycleNumber 2 getting the prepaid installment list");
                        takeSize = Properties.Settings.Default.SecondSingleChargeTakeSize;
                        installmentList = (from installment in entity.SinglechargeInstallments
                                           where installment.IsFullyPaid == false
                                           && installment.IsExceededDailyChargeLimit == false
                                           && installment.IsUserCanceledTheInstallment == false
                                           select installment).ToList();
                        logs.Info("installmentCycleNumber 2 installmentlist count:" + installmentList.Count);
                        installmentList = (from ins in installmentList
                                           where (from sub in subscribers select sub.MobileNumber).Contains(ins.MobileNumber)
                                           select ins).ToList();
                        logs.Info("installmentCycleNumber 2 prepaid installmentlist count:" + installmentList.Count);
                    }
                    else
                    {
                        takeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                        installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false).ToList();
                    }
                }
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                int[] take = new int[(installmentList.Count / takeSize) + 1];
                int[] skip = new int[(installmentList.Count / takeSize) + 1];
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

                    var chunkedInstallmentList = installmentList.Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(ProcessInstallmentChunk(chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber));
                }
                Task.WaitAll(TaskList.ToArray());
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
        }

        private static SharedLibrary.Models.MessageObject ChooseSinglechargePrice(SharedLibrary.Models.MessageObject message, List<ImiChargeCode> chargeCodes, int priceUserChargedToday)
        {
            if (priceUserChargedToday == 0)
            {
                message = SetMessagePrice(message, chargeCodes, 400);
            }
            else if (priceUserChargedToday <= 100)
            {
                message = SetMessagePrice(message, chargeCodes, 300);
            }
            else if (priceUserChargedToday <= 200)
            {
                message = SetMessagePrice(message, chargeCodes, 200);
            }
            else if (priceUserChargedToday <= 300)
            {
                message = SetMessagePrice(message, chargeCodes, 100);
            }
            else if (priceUserChargedToday <= 350)
            {
                message = SetMessagePrice(message, chargeCodes, 50);
            }
            return message;
        }

        private static SharedLibrary.Models.MessageObject SetMessagePrice(SharedLibrary.Models.MessageObject message, List<ImiChargeCode> chargeCodes, int price)
        {
            var chargecode = chargeCodes.FirstOrDefault(o => o.Price == price);
            message.Price = chargecode.Price;
            message.ImiChargeKey = chargecode.ChargeKey;
            return message;
        }
        public async Task ProcessInstallmentChunk(List<SinglechargeInstallment> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, List<ImiChargeCode> chargeCodes, int taskId, int installmentCycleNumber)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            await Task.Delay(10); // for making it async
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 5)
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        var priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.InstallmentId == installment.Id && DbFunctions.TruncateTime(o.DateCreated).Value == today).ToList().Sum(o => o.Price);
                        if (priceUserChargedToday >= maxChargeLimit)
                        {
                            //bool isUserCanceledTheInstallment = entity.SinglechargeInstallments.AsNoTracking().FirstOrDefault(o => o.Id == installment.Id).IsUserCanceledTheInstallment;
                            //if (isUserCanceledTheInstallment == true)
                            //{
                            //    installment.IsUserCanceledTheInstallment = true;
                            //    installment.CancelationDate = DateTime.Now;
                            //    installment.PersianCancelationDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            //}
                            installment.IsExceededDailyChargeLimit = true;
                            entity.Entry(installment).State = EntityState.Modified;
                            batchSaveCounter += 1;
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment.MobileNumber;
                        message.ShortCode = serviceAdditionalInfo["shortCode"];

                        message = ChooseSinglechargePrice(message, chargeCodes, priceUserChargedToday);
                        var response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                        if (response.IsSucceeded == false && installmentCycleNumber == 1)
                            continue;
                        if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                        {
                            if (message.Price == 400)
                            {
                                if (installmentCycleNumber == 1 || installmentCycleNumber == 2)
                                    continue;
                                SetMessagePrice(message, chargeCodes, 300);
                                if (true == true) // TEMPORARY!!!!!!!
                                {
                                    response.IsSucceeded = false;
                                    response.Description = "Billing  Failed";
                                }
                                else
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode  , message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 200);
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode,message, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                    {
                                        continue; ////// TEMPORARY!!!!!!!
                                        SetMessagePrice(message, chargeCodes, 100);
                                        response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                        if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                        {
                                            if (installmentCycleNumber == 2)
                                                continue;
                                            SetMessagePrice(message, chargeCodes, 50);
                                            response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode,message, serviceAdditionalInfo, installment.Id).Result;
                                        }
                                    }
                                }
                            }
                            else if (message.Price == 300)
                            {
                                SetMessagePrice(message, chargeCodes, 200);
                                response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode,message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    continue; ////// TEMPORARY!!!!!!!
                                    SetMessagePrice(message, chargeCodes, 100);
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode,message, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 50);
                                        response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                    }
                                }
                            }
                            else if (message.Price == 200)
                            {
                                continue; ////// TEMPORARY!!!!!!!
                                SetMessagePrice(message, chargeCodes, 100);
                                response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 50);
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                }
                            }
                        }
                        if (response.IsSucceeded == true)
                        {
                            //bool isUserCanceledTheInstallment = entity.SinglechargeInstallments.AsNoTracking().FirstOrDefault(o => o.Id == installment.Id).IsUserCanceledTheInstallment;
                            //if (isUserCanceledTheInstallment == true)
                            //{
                            //    installment.IsUserCanceledTheInstallment = true;
                            //    installment.CancelationDate = DateTime.Now;
                            //    installment.PersianCancelationDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            //}
                            installment.PricePayed += message.Price.GetValueOrDefault();
                            installment.PriceTodayCharged += message.Price.GetValueOrDefault();
                            if (installment.PricePayed >= installment.TotalPrice)
                                installment.IsFullyPaid = true;
                            entity.Entry(installment).State = EntityState.Modified;
                        }
                        batchSaveCounter++;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment ProcessInstallmentChunk: ", e);
            }

            logs.Info("InstallmentJob Chunk ended: task: " + taskId);
        }
    }
}
