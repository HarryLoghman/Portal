using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class InstallmentHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static dynamic GetInstallmentList(dynamic entity)
        {
            return ((IEnumerable)entity.SinglechargeInstallments).Cast<dynamic>().Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false && o.IsRenewd != true).ToList();
        }

        public static void MapfaInstallmentJob(Type entityType, int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber, string serviceCode, dynamic chargeCodes, dynamic installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo, Type singlechargeType)
        {
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(installmentListCount, installmentListTakeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                List<Task> TaskList = new List<Task>();
                for (int i = 0; i < take.Length; i++)
                {
                    var chunkedInstallmentList = ((IEnumerable)installmentList).Cast<dynamic>().Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(MapfaProcessInstallmentChunk(entityType, maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, installmentInnerCycleNumber, singlechargeType));
                }
                Task.WaitAll(TaskList.ToArray());
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment MapfaInstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
        }

        public static void SamssonTciInstallmentJob(Type entityType, int maxChargeLimit, int installmentCycleNumber, string serviceCode, dynamic installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo, Type singlechargeType)
        {
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(installmentListCount, installmentListTakeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                List<Task> TaskList = new List<Task>();
                for (int i = 0; i < take.Length; i++)
                {
                    var chunkedInstallmentList = ((IEnumerable)installmentList).Cast<dynamic>().Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(SamssonTciProcessInstallmentChunk(entityType, maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, i, installmentCycleNumber, singlechargeType));
                }
                Task.WaitAll(TaskList.ToArray());
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment SamssonTciInstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
        }

        private static async Task MapfaProcessInstallmentChunk(Type entityType, int maxChargeLimit, dynamic chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, int installmentInnerCycleNumber, Type singlechargeType)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            dynamic singlecharge = Activator.CreateInstance(singlechargeType);
            dynamic reserverdSingleCharge = Activator.CreateInstance(singlechargeType);
            await Task.Delay(10); // for making it async
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if ((DateTime.Now.Hour == 23 && DateTime.Now.Minute > 57) && (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 15))
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        int priceUserChargedToday = ((IEnumerable)entity.Singlecharges).Cast<dynamic>().Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.IsApplicationInformed == false && o.DateCreated.Date == today.Date).ToList().Sum(o => o.Price);
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
                        message = SharedLibrary.InstallmentHandler.ChooseMapfaSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                        if (message.Price == 0)
                            continue;
                        var response = MessageSender.MapfaStaticPriceSinglecharge(entityType, singlechargeType, message, serviceAdditionalInfo, installment.Id).Result;
                        if (response.IsSucceeded == true)
                        {
                            installment.PricePayed += message.Price.GetValueOrDefault();
                            installment.PriceTodayCharged += message.Price.GetValueOrDefault();
                            if (installment.PriceTodayCharged >= maxChargeLimit)
                                installment.IsExceededDailyChargeLimit = true;
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
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }

            logs.Info("InstallmentJob Chunk task " + taskId + " ended");
        }

        private static async Task SamssonTciProcessInstallmentChunk(Type entityType, int maxChargeLimit, dynamic chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, int taskId, int installmentCycleNumber, Type singlechargeType)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            dynamic singlecharge = Activator.CreateInstance(singlechargeType);
            dynamic reserverdSingleCharge = Activator.CreateInstance(singlechargeType);
            await Task.Delay(10); // for making it async
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if ((DateTime.Now.Hour == 23 && DateTime.Now.Minute > 57) && (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 15))
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        int priceUserChargedToday = ((IEnumerable)entity.Singlecharges).Cast<dynamic>().Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.IsApplicationInformed == false && o.DateCreated.Date == today.Date).ToList().Sum(o => o.Price);
                        if (priceUserChargedToday >= maxChargeLimit)
                        {
                            installment.IsExceededDailyChargeLimit = true;
                            entity.Entry(installment).State = EntityState.Modified;
                            batchSaveCounter += 1;
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment.MobileNumber;
                        message.Token = installment.UserToken;
                        message.Price = installment.TotalPrice;
                        if (message.Price == 0)
                            continue;
                        var response = MessageSender.SamssonTciSinglecharge(entityType, singlechargeType, message, serviceAdditionalInfo, true, installment.Id).Result;
                        if (response.IsSucceeded == true)
                        {
                            installment.PricePayed += message.Price.GetValueOrDefault();
                            installment.PriceTodayCharged += message.Price.GetValueOrDefault();
                            if (installment.PriceTodayCharged >= maxChargeLimit)
                                installment.IsExceededDailyChargeLimit = true;
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
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }

            logs.Info("InstallmentJob Chunk task " + taskId + " ended");
        }

        public static int MtnInstallmentJob(Type entityType, int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber, string serviceCode, dynamic chargeCodes, dynamic installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo, Type singlechargeType, int income)
        {
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return 0;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(installmentListCount, installmentListTakeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                var TaskList = new List<Task<int>>();
                for (int i = 0; i < take.Length; i++)
                {
                    var chunkedInstallmentList = ((IEnumerable)installmentList).Cast<dynamic>().Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(MtnProcessInstallmentChunk(entityType, maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, installmentInnerCycleNumber, singlechargeType));
                }
                Task.WaitAll(TaskList.ToArray());
                income = TaskList.Select(o => o.Result).Sum();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment MtnInstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }

        private static async Task<int> MtnProcessInstallmentChunk(Type entityType, int maxChargeLimit, dynamic chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, int installmentInnerCycleNumber, Type singlechargeType)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            int income = 0;
            dynamic singlecharge = Activator.CreateInstance(singlechargeType);
            dynamic reserverdSingleCharge = Activator.CreateInstance(singlechargeType);
            await Task.Delay(10); // for making it async
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if ((DateTime.Now.Hour == 23 && DateTime.Now.Minute > 57) && (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 01))
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        //singlecharge = reserverdSingleCharge;
                        int priceUserChargedToday = ((IEnumerable)entity.Singlecharges).Cast<dynamic>().Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.IsApplicationInformed == false && o.DateCreated.Date == today.Date).ToList().Sum(o => o.Price);
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
                        message = SharedLibrary.InstallmentHandler.ChooseMtnSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                        if (installmentInnerCycleNumber == 1 && message.Price != 300)
                            continue;
                        else if (installmentInnerCycleNumber == 2 && message.Price >= 100)
                            message.Price = 100;
                        var response = MessageSender.ChargeMtnSubscriber(entityType, singlechargeType, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                        if (response.IsSucceeded == true)
                        {
                            income += message.Price.GetValueOrDefault();
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
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }
            logs.Info("InstallmentJob Chunk task " + taskId + " ended");
            return income;
        }

        public static SharedLibrary.Models.MessageObject ChooseMtnSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else if (priceUserChargedToday <= 200)
            {
                message.Price = 100;
            }
            else
                message.Price = 0;
            return message;
        }

        public static SharedLibrary.Models.MessageObject ChooseMapfaSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else
                message.Price = 0;
            return message;
        }

        public static SharedLibrary.Models.MessageObject SetMessagePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int price)
        {
            var chargecode = ((IEnumerable)chargeCodes).Cast<dynamic>().FirstOrDefault(o => o.Price == price);
            message.Price = chargecode.Price;
            message.ImiChargeKey = chargecode.ChargeKey;
            return message;
        }

        public static void InstallmentCycleToDb(Type entityType, Type installmentCycleType, int cycleNumber, long duration, int income)
        {
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    dynamic installmentCycle = Activator.CreateInstance(installmentCycleType);
                    installmentCycle.CycleNumber = cycleNumber;
                    installmentCycle.DateCreated = DateTime.Now;
                    installmentCycle.Duration = duration;
                    installmentCycle.Income = income;
                    entity.InstallmentCycles.Add(installmentCycle);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in InstallmentCycleToDb: ", e);
            }
        }
    }
}
