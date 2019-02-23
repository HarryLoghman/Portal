using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models.ServiceModel;

using System.Data.Entity;
using System.Threading;
using System.Collections;
using SharedLibrary.Models;

namespace DehnadMedioService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int maxChargeLimit = 400;
        public static int MapfaInstallmentJob(int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber, string serviceCode, dynamic chargeCodes, List<string> installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo)
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
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
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
                    TaskList.Add(MapfaProcessInstallmentChunk(maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, installmentInnerCycleNumber, isCampaignActive));
                }
                Task.WaitAll(TaskList.ToArray());
                income = TaskList.Select(o => o.Result).Sum();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment MapfaInstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }

        private static async Task<int> MapfaProcessInstallmentChunk(int maxChargeLimit, List<string> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, int installmentInnerCycleNumber, int isCampaignActive)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            int income = 0;
            await Task.Delay(10); // for making it async
            try
            {
                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
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
                        bool isSubscriberActive = SharedLibrary.SubscriptionHandler.IsSubscriberActive(installment, serviceAdditionalInfo["serviceId"]);
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

                        var response = MapfaStaticPriceSinglecharge(message, serviceAdditionalInfo).Result;
                        if (response.IsSucceeded == true)
                        {
                            income += message.Price.GetValueOrDefault();
                        }
                        if (isCampaignActive == 1 || isCampaignActive == 2)
                        {
                            try
                            {
                                var serviceId = Convert.ToInt64(serviceAdditionalInfo["serviceId"]);
                                var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, serviceId);
                                if (isInBlackList != true)
                                {
                                    var sub = SharedLibrary.SubscriptionHandler.GetSubscriber(message.MobileNumber, Convert.ToInt64(serviceAdditionalInfo["serviceId"]));
                                    if (sub != null)
                                    {
                                        if (sub.SpecialUniqueId != null)
                                        {
                                            var sha = SharedLibrary.Encrypt.GetSha256Hash(sub.SpecialUniqueId + message.MobileNumber);
                                            var price = 0;
                                            if (response.IsSucceeded == true)
                                                price = message.Price.Value;
                                            if (serviceAdditionalInfo["serviceCode"] == "Phantom")
                                                await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/phantom/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                            else if (serviceAdditionalInfo["serviceCode"] == "Medio")
                                                await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/medio/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logs.Error("Exception in calling danoop charge service: " + e);
                            }
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
        public static async Task<dynamic> MapfaStaticPriceSinglecharge(MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            var startTime = DateTime.Now;
            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var singlecharge = new Singlecharge();
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = SharedLibrary.ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price.Value).PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var channelType = (int)SharedLibrary.MessageHandler.MapfaChannels.SMS;
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    using (var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient())
                    {
                        var result = client.singleCharge(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                        if (result > 10000)
                            singlecharge.IsSucceeded = true;
                        else
                            singlecharge.IsSucceeded = false;

                        singlecharge.Description = result.ToString();
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaStaticPriceSinglecharge: " + e);
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
                    singlecharge.ProcessTimeInMilliSecond = (int)duration.TotalMilliseconds;
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaStaticPriceSinglecharge on saving values to db: " + e);
                }
                return singlecharge;
            }
        }
    }
}
