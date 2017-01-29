using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoltanLibrary.Models;
using SoltanLibrary;
using System.Data.Entity;
using System.Threading;

namespace DehnadSoltanService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static int maxChargeLimit = 400;
        public void ProcessInstallment()
        {
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 10)
                return;
            else
                InstallmentJob();

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
                using (var entity = new SoltanEntities())
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
                using (var entity = new SoltanEntities())
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
                using (var entity = new SoltanEntities())
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
                            if (installment.DateCreated.AddDays(30) >= DateTime.Now)
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

        private void InstallmentJob()
        {
            try
            {
                logs.Info("InstallmentJob start!");



                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soltan", "Telepromo");
                List<SinglechargeInstallment> installmentList;
                List<ImiChargeCode> chargeCodes;
                using (var entity = new SoltanEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    chargeCodes = entity.ImiChargeCodes.Where(o => o.Price <= maxChargeLimit).ToList();
                    installmentList = entity.SinglechargeInstallments.Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false).ToList();
                }
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob ended!");
                    return;
                }
                int takeSize = 1000;

                int[] take = new int[(installmentList.Count / takeSize)];
                int[] skip = new int[(installmentList.Count / takeSize)];
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
                    using (var entity = new SoltanEntities())
                    {
                        var chunkedInstallmentList = installmentList.Skip(skip[i]).Take(take[i]).ToList();
                        TaskList.Add(ProcessInstallmentChunk(entity, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes));
                    }
                }
                Task.WaitAll(TaskList.ToArray());
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
        public static async Task ProcessInstallmentChunk(SoltanEntities entity, List<SinglechargeInstallment> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, List<ImiChargeCode> chargeCodes)
        {
            var today = DateTime.Now.Date;
            logs.Info("InstallmentJob Chunk started");
            int batchSaveCounter = 0;
            try
            {
                foreach (var installment in chunkedSingleChargeInstallment)
                {
                    if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 2)
                        break;
                    if (batchSaveCounter >= 1000)
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
                    message.ShortCode = serviceAdditionalInfo["shortCode"];

                    message = ChooseSinglechargePrice(message, chargeCodes, priceUserChargedToday);
                    var response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                    {
                        if (message.Price == 400)
                        {
                            SetMessagePrice(message, chargeCodes, 300);
                            response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                            if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                            {
                                SetMessagePrice(message, chargeCodes, 200);
                                response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 100);
                                    response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                    {
                                        SetMessagePrice(message, chargeCodes, 50);
                                        response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                                    }
                                }
                            }
                        }
                        else if (message.Price == 300)
                        {
                            SetMessagePrice(message, chargeCodes, 200);
                            response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                            if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                            {
                                SetMessagePrice(message, chargeCodes, 100);
                                response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                                {
                                    SetMessagePrice(message, chargeCodes, 50);
                                    response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                                }
                            }
                        }
                        else if (message.Price == 200)
                        {
                            SetMessagePrice(message, chargeCodes, 100);
                            response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
                            if (response.IsSucceeded == false && response.Description.Contains("Billing  Failed"))
                            {
                                SetMessagePrice(message, chargeCodes, 50);
                                response = SoltanLibrary.MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo, installment.Id).Result;
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
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment ProcessInstallment: ", e);
            }
            entity.SaveChanges();
            logs.Info("InstallmentJob Chunk ended");
        }
    }
}
