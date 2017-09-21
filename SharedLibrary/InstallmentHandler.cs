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
            return ((IEnumerable)entity.SinglechargeInstallments).Cast<dynamic>().Where(o => o.IsFullyPaid == false && o.IsExceededDailyChargeLimit == false && o.IsUserCanceledTheInstallment == false).ToList();
        }

        public static void InstallmentJob(dynamic entity, int maxChargeLimit, int installmentCycleNumber, string serviceCode, dynamic chargeCodes, dynamic installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo, dynamic singlecharge)
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
                    TaskList.Add(ProcessMtnInstallmentChunk(entity, maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, singlecharge));
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

        private static async Task ProcessMtnInstallmentChunk(dynamic entity, int maxChargeLimit, dynamic chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, dynamic singlecharge)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            dynamic reserverdSingleCharge = singlecharge;
            await Task.Delay(10); // for making it async
            try
            {
                foreach (var installment in chunkedSingleChargeInstallment)
                {
                    if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 5)
                        break;
                    if (batchSaveCounter >= 500)
                    {
                        entity.SaveChanges();
                        batchSaveCounter = 0;
                    }
                    singlecharge = reserverdSingleCharge;
                    int priceUserChargedToday = ((IEnumerable)entity.Singlecharges).Cast<dynamic>().Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.InstallmentId == installment.Id && o.DateCreated.Date.Equals(today.Date)).ToList().Sum(o => o.Price);
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
                    message = ChooseMtnSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                    var response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                    if (response.IsSucceeded == false && installmentCycleNumber == 1)
                        continue;
                    else if (response.IsSucceeded == false)
                    {
                        singlecharge = reserverdSingleCharge;
                        if (message.Price == 300)
                        {
                            SetMessagePrice(message, chargeCodes, 200);
                            response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                            if (response.IsSucceeded == false)
                            {
                                singlecharge = reserverdSingleCharge;
                                SetMessagePrice(message, chargeCodes, 100);
                                response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false)
                                {
                                    singlecharge = reserverdSingleCharge;
                                    SetMessagePrice(message, chargeCodes, 50);
                                    response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                }
                            }
                        }
                        else if (message.Price == 200)
                        {
                            message.Price = 100;
                            response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                            if (response.IsSucceeded == false)
                            {
                                singlecharge = reserverdSingleCharge;
                                message.Price = 50;
                                response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                            }
                        }
                        else if (message.Price == 100)
                        {
                            singlecharge = reserverdSingleCharge;
                            message.Price = 50;
                            response = SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singlecharge, message, false, false, serviceAdditionalInfo, installment.Id).Result;
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
            catch (Exception e)
            {
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }

            logs.Info("InstallmentJob Chunk task " + taskId + " ended");
        }

        public static SharedLibrary.Models.MessageObject ChooseMtnSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else if (priceUserChargedToday <= 100)
            {
                message.Price = 200;
            }
            else if (priceUserChargedToday <= 200)
            {
                message.Price = 100;
            }
            else if (priceUserChargedToday <= 250)
            {
                message.Price = 50;
            }
            return message;
        }

        public static SharedLibrary.Models.MessageObject SetMessagePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int price)
        {
            var chargecode = ((IEnumerable)chargeCodes).Cast<dynamic>().FirstOrDefault(o => o.Price == price);
            message.Price = chargecode.Price;
            message.ImiChargeKey = chargecode.ChargeKey;
            return message;
        }
    }
}
