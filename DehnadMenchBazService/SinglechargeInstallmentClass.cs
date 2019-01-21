using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models.ServiceModel;

using System.Data.Entity;
using System.Threading;

namespace DehnadMenchBazService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int maxChargeLimit = 1000;
        public void ProcessInstallment()
        {
            FakeInstallmentJob();
            //if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 7)
            //    return;
            //else
            //    InstallmentJob();

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
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    entity.SinglechargeInstallments.Where(o => o.IsUserDailyChargeBalanced == true).ToList().ForEach(o => o.IsUserDailyChargeBalanced = false);
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
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsUserDailyChargeBalanced == false && o.IsUserCanceledTheInstallment == false).ToList();
                    foreach (var installment in installmentList)
                    {
                        if (batchSaveCounter >= 500)
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
                            if (installment.PriceTodayCharged > maxChargeLimit)
                                installment.PriceTodayCharged = maxChargeLimit;
                            installment.PricePayed += maxChargeLimit - installment.PriceTodayCharged;
                        }
                        installment.PriceTodayCharged = 0;
                        installment.IsExceededDailyChargeLimit = false;
                        installment.IsUserDailyChargeBalanced = true;
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

        public static void FakeInstallmentJob()
        {
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            logs.Info("InstallmentJob start!");
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false).ToList();
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", "Telepromo");
                    foreach (var installment in installmentList)
                    {
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        installment.PriceTodayCharged = 1000;
                        installment.PricePayed += 1000;
                        installment.IsExceededDailyChargeLimit = true;
                        if (installment.PricePayed > installment.TotalPrice)
                            installment.IsFullyPaid = true;
                        entity.Entry(installment).State = EntityState.Modified;
                        batchSaveCounter++;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
            logs.Info("InstallmentJob ended!");
        }

        private void InstallmentJob()
        {
            try
            {
                var today = DateTime.Now.Date;
                int batchSaveCounter = 0;
                logs.Info("InstallmentJob start!");
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                {
                    var chargeCodes = entity.ImiChargeCodes.Where(o => o.Price <= maxChargeLimit).ToList();
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false).ToList();
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", "Telepromo");
                    foreach (var installment in installmentList)
                    {
                        if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        var priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.InstallmentId == installment.Id && DbFunctions.TruncateTime(o.DateCreated).Value == today).ToList().Sum(o => o.Price);
                        if (priceUserChargedToday >= maxChargeLimit)
                        {
                            bool isUserCanceledTheInstallment = entity.SinglechargeInstallments.AsNoTracking().FirstOrDefault(o => o.Id == installment.Id).IsUserCanceledTheInstallment;
                            if (isUserCanceledTheInstallment == true)
                            {
                                installment.IsUserCanceledTheInstallment = true;
                                installment.CancelationDate = DateTime.Now;
                                installment.PersianCancelationDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            }
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
                        if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                        {
                            if (message.Price == 400)
                            {
                                SetMessagePrice(message, chargeCodes, 300);
                                response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode,message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 200);
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 100);
                                        response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                        if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                        {
                                            SetMessagePrice(message, chargeCodes, 50);
                                            response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                        }
                                    }
                                }
                            }
                            else if (message.Price == 300)
                            {
                                SetMessagePrice(message, chargeCodes, 200);
                                response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 100);
                                    response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 50);
                                        response = SharedShortCodeServiceLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(Properties.Settings.Default.ServiceCode, message, serviceAdditionalInfo, installment.Id).Result;
                                    }
                                }
                            }
                            else if (message.Price == 200)
                            {
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
                            bool isUserCanceledTheInstallment = entity.SinglechargeInstallments.AsNoTracking().FirstOrDefault(o => o.Id == installment.Id).IsUserCanceledTheInstallment;
                            if (isUserCanceledTheInstallment == true)
                            {
                                installment.IsUserCanceledTheInstallment = true;
                                installment.CancelationDate = DateTime.Now;
                                installment.PersianCancelationDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            }
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
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
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
    }
}
