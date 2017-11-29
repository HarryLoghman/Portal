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
        private static int maxChargeLimit = 400;
        public void ProcessInstallment(int installmentCycleNumber)
        {
            try
            {
                int maxChargeLimit = 300;
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = "MusicYad";
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<SinglechargeInstallment> installmentList;
                Singlecharge singlecharge = new Singlecharge();
                using (var entity = new MusicYadEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();
                    int installmentListCount = installmentList.Count;
                    var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;

                    InstallmentJob(maxChargeLimit, installmentCycleNumber, serviceCode, chargeCodes, installmentList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo, singlecharge);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessInstallment:", e);
            }
        }

        public static void InstallmentJob(int maxChargeLimit, int installmentCycleNumber, string serviceCode, dynamic chargeCodes, List<SinglechargeInstallment> installmentList, int installmentListCount, int installmentListTakeSize, Dictionary<string, string> serviceAdditionalInfo, dynamic singlecharge)
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
                    var chunkedInstallmentList = installmentList.Skip(skip[i]).Take(take[i]).ToList();
                    TaskList.Add(ProcessMtnInstallmentChunk(maxChargeLimit, chunkedInstallmentList, serviceAdditionalInfo, chargeCodes, i, installmentCycleNumber, singlecharge));
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

        private static async Task ProcessMtnInstallmentChunk(int maxChargeLimit, List<SinglechargeInstallment> chunkedSingleChargeInstallment, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes, int taskId, int installmentCycleNumber, dynamic singlecharge)
        {
            logs.Info("InstallmentJob Chunk started: task: " + taskId);
            var today = DateTime.Now.Date;
            int batchSaveCounter = 0;
            //dynamic reserverdSingleCharge = singlecharge;
            await Task.Delay(10); // for making it async
            try
            {
                using (var entity = new MusicYadEntities())
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
                        //singlecharge = reserverdSingleCharge;
                        int priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == installment.MobileNumber && o.IsSucceeded == true && o.InstallmentId == installment.Id && DbFunctions.TruncateTime(o.DateCreated) == today.Date).ToList().Sum(o => o.Price);
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
                        var response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                        if (response.IsSucceeded == false && installmentCycleNumber == 1)
                            continue;
                        else if (response.IsSucceeded == false)
                        {
                            //singlecharge = reserverdSingleCharge;
                            if (message.Price == 300)
                            {
                                SharedLibrary.InstallmentHandler.SetMessagePrice(message, chargeCodes, 200);
                                response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false)
                                {
                                    //singlecharge = reserverdSingleCharge;
                                    SharedLibrary.InstallmentHandler.SetMessagePrice(message, chargeCodes, 100);
                                    response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                    if (response.IsSucceeded == false)
                                    {
                                        //singlecharge = reserverdSingleCharge;
                                        SharedLibrary.InstallmentHandler.SetMessagePrice(message, chargeCodes, 50);
                                        response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                    }
                                }
                            }
                            else if (message.Price == 200)
                            {
                                message.Price = 100;
                                response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                if (response.IsSucceeded == false)
                                {
                                    //singlecharge = reserverdSingleCharge;
                                    message.Price = 50;
                                    response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
                                }
                            }
                            else if (message.Price == 100)
                            {
                                //singlecharge = reserverdSingleCharge;
                                message.Price = 50;
                                response = ChargeMtnSubscriber(entity, message, false, false, serviceAdditionalInfo, installment.Id).Result;
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
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }

            logs.Info("InstallmentJob Chunk task " + taskId + " ended");
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
