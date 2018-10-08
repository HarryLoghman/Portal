using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvvalPod500Library.Models;
using AvvalPod500Library;
using System.Data.Entity;
using System.Threading;
using System.Collections;
using System.Net.Http;

namespace DehnadAvvalPod500Service
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int maxChargeLimit = 500;
        public int ProcessInstallment(int installmentCycleNumber)
        {
            var income = 0;
            try
            {
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<string> installmentList;

                using (var entity = new AvvalPod500Entities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 240;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    for (int installmentInnerCycleNumber = 1; installmentInnerCycleNumber <= 1; installmentInnerCycleNumber++)
                    {
                        logs.Info("start of installmentInnerCycleNumber " + installmentInnerCycleNumber);
                        //installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();

                        installmentList = SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersFromServiceCode(serviceCode);
                        var today = DateTime.Now;
                        List<string> chargeCompleted;
                        var delayDateBetweenCharges = today.AddDays(0);
                        if (delayDateBetweenCharges.Date != today.Date)
                        {
                            chargeCompleted = entity.vw_Singlecharge.AsNoTracking()
                                .Where(o => DbFunctions.TruncateTime(o.DateCreated) >= DbFunctions.TruncateTime(delayDateBetweenCharges) && DbFunctions.TruncateTime(o.DateCreated) <= DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                                .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) >= maxChargeLimit).Select(o => o.Key).ToList();
                        }
                        else
                        {
                            chargeCompleted = entity.Singlecharges.AsNoTracking()
                                .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                                .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) >= maxChargeLimit).Select(o => o.Key).ToList();
                        }
                        var waitingList = entity.SinglechargeWaitings.AsNoTracking().Select(o => o.MobileNumber).ToList();
                        installmentList.RemoveAll(o => chargeCompleted.Contains(o));
                        installmentList.RemoveAll(o => waitingList.Contains(o));
                        int installmentListCount = installmentList.Count;
                        var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                        income += TelepromoMapfaInstallmentJob(maxChargeLimit, installmentCycleNumber, installmentInnerCycleNumber, serviceCode, chargeCodes, installmentList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo);
                        logs.Info("end of installmentInnerCycleNumber " + installmentInnerCycleNumber);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessInstallment:", e);
            }
            return income;
        }

        public static int TelepromoMapfaInstallmentJob(int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber, string serviceCode, dynamic chargeCodes, List<string> installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo)
        {
            var income = 0;
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return income;
                }
                int isCampaignActive = 0;
                using (var entity = new AvvalPod500Entities())
                {
                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        isCampaignActive = Convert.ToInt32(campaign.Value);
                }
                logs.Info("installmentList count:" + installmentList.Count);

                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(installmentListCount, installmentListTakeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                var TaskList = new List<Task<int>>();
                for (int i = 0; i < take.Length; i++)
                {
                    var chunkedInstallmentList = installmentList.Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(TelepromoMapfaProcessInstallmentChunk(maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, installmentInnerCycleNumber, isCampaignActive));
                }
                Task.WaitAll(TaskList.ToArray());
                income = TaskList.Select(o => o.Result).Sum();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment TelepromoMapfaInstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }

        private static async Task<int> TelepromoMapfaProcessInstallmentChunk(int maxChargeLimit, List<string> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, int installmentInnerCycleNumber, int isCampaignActive)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            int income = 0;
            await Task.Delay(10); // for making it async
            try
            {
                using (var entity = new AvvalPod500Entities())
                {
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        int priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment && o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today)).Select(o => o.Price).ToList().Sum(o => o);
                        bool isSubscriberActive = SharedLibrary.HandleSubscription.IsSubscriberActive(installment, serviceAdditionalInfo["serviceId"]);
                        if (priceUserChargedToday >= maxChargeLimit || isSubscriberActive == false)
                        {
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment;
                        message.ShortCode = serviceAdditionalInfo["shortCode"];
                        message = SharedLibrary.InstallmentHandler.ChooseMapfaSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                        if (message.Price == 0)
                            continue;

                        var response = TelepromoMapfaSinglecharge(message, serviceAdditionalInfo).Result;
                        if (response.IsSucceeded == true)
                        {
                            income += message.Price.GetValueOrDefault();
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
        public static async Task<dynamic> TelepromoMapfaSinglecharge(SharedLibrary.Models.MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            var startTime = DateTime.Now;
            using (var entity = new AvvalPod500Entities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var singlecharge = new Singlecharge();
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var url = SharedLibrary.MessageSender.telepromoIp + "/samsson-gateway/chargingpardis/";
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = SharedLibrary.ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price.Value).PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    var description = string.Format("deliverychannel=WAP|discoverychannel=WAP|origin={0}|contentid=1", "98" + paridsShortCodes.FirstOrDefault().ShortCode);
                    var json = string.Format(@"{{
                                ""username"": ""{0}"",
                                ""password"": ""{1}"",
                                ""serviceid"": ""{2}"",
                                ""msisdn"": ""{3}"",
                                ""description"": ""{4}"",
                    }}", username, password, aggregatorServiceId, mobileNumber, description);
                    using (var client = new HttpClient())
                    {
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var result = await client.PostAsync(url, content);
                        var responseString = await result.Content.ReadAsStringAsync();
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                        if (jsonResponse.data.ToString().Length > 5)
                        {
                            singlecharge.IsSucceeded = true;
                        }
                        else
                        {
                            singlecharge.IsSucceeded = false;
                        }
                        singlecharge.ReferenceId = jsonResponse.data.ToString();
                        singlecharge.Description = jsonResponse.status_code.ToString() + "-" + jsonResponse.status_txt.ToString() + "-" + jsonResponse.data.ToString();
                    }   
                }
                catch (Exception e)
                {
                    logs.Error("Exception in TelepromoMapfaSinglecharge: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {

                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;
                    var endTime = DateTime.Now;
                    var duration = endTime - startTime;
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in TelepromoMapfaSinglecharge on saving values to db: " + e);
                }
                return singlecharge;
            }
        }
    }
}
