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
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int maxChargeLimit = 400;
        public void ProcessInstallment()
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 10)
                InstallmentDailyBalance();
            else
                InstallmentJob();
            if (DateTime.Now.Hour == 5 || DateTime.Now.Hour == 6 || DateTime.Now.Hour == 7)
            {
                ResetUserDailyChargeBalanceValue();
            }
        }

        private void DeactivateChargingUsersAfter30Days()
        {
            try
            {
                using (var entity = new BimeIranEntities())
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

        private void ResetUserDailyChargeBalanceValue()
        {
            try
            {
                using (var entity = new BimeIranEntities())
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

        private void InstallmentDailyBalance()
        {
            try
            {
                int batchSaveCounter = 0;
                using (var entity = new BimeIranEntities())
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

        private void InstallmentJob()
        {
            try
            {
                var today = DateTime.Now.Date;
                int batchSaveCounter = 0;
                logs.Info("InstallmentJob start!");
                using (var entity = new BimeIranEntities())
                {
                    var chargeCodes = entity.ImiChargeCodes.Where(o => o.Price <= maxChargeLimit).ToList();
                    var installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false).ToList();
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
                            installment.IsExceededDailyChargeLimit = true;
                            entity.Entry(installment).State = EntityState.Modified;
                            batchSaveCounter += 1;
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment.MobileNumber;
                        message.ShortCode = "3071171";

                        message = ChooseSinglechargePrice(message, chargeCodes, priceUserChargedToday);
                        var response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                        if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                        {
                            if (message.Price == 400)
                            {
                                SetMessagePrice(message, chargeCodes, 300);
                                response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                {
                                    SetMessagePrice(message, chargeCodes, 200);
                                    response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                    if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 100);
                                        response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                        if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                        {
                                            SetMessagePrice(message, chargeCodes, 50);
                                            response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                        }
                                    }
                                }
                            }
                            else if (message.Price == 300)
                            {
                                SetMessagePrice(message, chargeCodes, 200);
                                response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                {
                                    SetMessagePrice(message, chargeCodes, 100);
                                    response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                    if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 50);
                                        response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                                    }
                                }
                            }
                            else if (message.Price == 200)
                            {
                                SetMessagePrice(message, chargeCodes, 100);
                                response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                if (response.IsSucceeded == false && response.Description.Contains("Insufficient balance"))
                                {
                                    SetMessagePrice(message, chargeCodes, 50);
                                    response = BimeIranLibrary.MessageHandler.SendSinglechargeMesssageToPardisImi(message, installment.Id);
                                }
                            }
                        }
                        if (response.IsSucceeded == true)
                        {
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
