using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicYadLibrary.Models;
using MusicYadLibrary;
using System.Data.Entity;
using System.Threading;
using System.Collections;
using System.Net.Http;
using System.Xml;
using System.Net;
using SharedLibrary;
using SharedLibrary.Models;

namespace DehnadMusicYadService
{
    public class SinglechargeInstallmentClass
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int maxChargeLimit = 300;
        public int ProcessInstallment(int installmentCycleNumber)
        {
            var income = 0;
            try
            {
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<string> installmentList;

                using (var entity = new MusicYadEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 120;
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
                        income += InstallmentJob(maxChargeLimit, installmentCycleNumber, installmentInnerCycleNumber, serviceCode, chargeCodes, installmentList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo);
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

        public static int InstallmentJob(int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber, string serviceCode, dynamic chargeCodes, List<string> installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo)
        {
            var income = 0;
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return income;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                //var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbers(installmentListCount, installmentListTakeSize);
                var threadsNo = SharedLibrary.MessageHandler.CalculateServiceSendMessageThreadNumbersByTps(installmentListCount, installmentListTakeSize);
                var take = threadsNo["take"];
                var skip = threadsNo["skip"];

                var TaskList = new List<Task<int>>();
                for (int i = 0; i < take.Length; i++)
                {
                    var chunkedInstallmentList = installmentList.Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(ProcessMtnInstallmentChunk(maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, installmentInnerCycleNumber));
                }
                Task.WaitAll(TaskList.ToArray());
                income = TaskList.Select(o => o.Result).ToList().Sum();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }

        public static async Task<int> ProcessMtnInstallmentChunk(int maxChargeLimit, List<string> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, int installmentInnerCycleNumber)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId + " - installmentList count:" + chunkedSingleChargeInstallment.Count);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            int income = 0;
            var previousStart = DateTime.Now.TimeOfDay;
            await Task.Delay(10); // for making it async
            try
            {
                using (var entity = new MusicYadEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var installment in chunkedSingleChargeInstallment)
                    {
                        if ((DateTime.Now.Hour == 23 && DateTime.Now.Minute > 57) || (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 01))
                            break;
                        if (batchSaveCounter >= 500)
                        {
                            entity.SaveChanges();
                            batchSaveCounter = 0;
                        }
                        //singlecharge = reserverdSingleCharge;
                        int priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment && o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == today.Date).ToList().Sum(o => o.Price);
                        bool isSubscriberActive = SharedLibrary.HandleSubscription.IsSubscriberActive(installment, serviceAdditionalInfo["serviceId"]);
                        if (priceUserChargedToday >= maxChargeLimit || isSubscriberActive == false)
                        {
                            continue;
                        }
                        var message = new SharedLibrary.Models.MessageObject();
                        message.MobileNumber = installment;
                        message.ShortCode = serviceAdditionalInfo["shortCode"];
                        message = SharedLibrary.InstallmentHandler.ChooseMtnSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                        if (installmentInnerCycleNumber == 1 && message.Price != 300)
                            continue;
                        else if (installmentInnerCycleNumber == 2 && message.Price >= 100)
                            message.Price = 100;
                        var start = DateTime.Now.TimeOfDay;
                        var diff = start - previousStart;
                        if (diff.Milliseconds < 1000)
                            Thread.Sleep(1000 - diff.Milliseconds);
                        previousStart = start;
                        var response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo).Result;
                        //if (response.IsSucceeded == false && message.Price == 300)
                        //{
                        //    message.Price = 100;
                        //    response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                        //}
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

        public static async Task<Singlecharge> ChargeMtnSubscriber(MusicYadEntities entity, MessageObject message, bool isRefund, bool isInAppPurchase, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            string charge = "";
            var spId = "980110006379";
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            if (isRefund == true)
                charge = "refundAmount";
            else
                charge = "chargeAmount";
            var mobile = "98" + message.MobileNumber.TrimStart('0');
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            int rialedPrice = message.Price.Value * 10;
            var referenceCode = Guid.NewGuid().ToString();
            var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
, timeStamp, mobile, rialedPrice, referenceCode, charge, serviceAdditionalInfo["aggregatorServiceId"], spId);
            try
            {
                using (var client = new HttpClient())
                {
                    //client.Timeout = TimeSpan.FromSeconds(15);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                            XmlDocument xml = new XmlDocument();
                            xml.LoadXml(httpResult);
                            XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                            manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                            manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                            manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local");
                            XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns1:chargeAmountResponse", manager);
                            if (successNode != null)
                            {
                                singlecharge.IsSucceeded = true;
                            }
                            else
                            {
                                singlecharge.IsSucceeded = false;
                                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                                XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                                foreach (XmlNode fault in faultNode)
                                {
                                    XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                                    XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                                    singlecharge.Description = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                                }
                            }
                        }
                        else
                        {
                            singlecharge.IsSucceeded = false;
                            singlecharge.Description = response.StatusCode.ToString();
                        }
                        singlecharge.ReferenceId = referenceCode;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Info(payload);
                logs.Error("Exception in ChargeMtnSubscriber: " + e);
            }
            try
            {
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "IsSucceeded") != true)
                    singlecharge.IsSucceeded = false;
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                if (isRefund == true)
                    singlecharge.Price = message.Price.GetValueOrDefault() * -1;
                else
                    singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                if (installmentId != 0)
                    singlecharge.InstallmentId = installmentId;

                singlecharge.IsCalledFromInAppPurchase = isInAppPurchase;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeMtnSubscriber on saving values to db: " + e);
            }
            return singlecharge;
        }
    }
}
