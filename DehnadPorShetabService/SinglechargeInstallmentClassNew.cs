using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PorShetabLibrary.Models;
using PorShetabLibrary;
using System.Data.Entity;
using System.Threading;
using System.Collections;
using System.Net.Http;
using System.Xml;
using System.Net;
using SharedLibrary;
using SharedLibrary.Models;

namespace DehnadPorShetabService
{
    public class SinglechargeInstallmentClassNew
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int maxChargeLimit = 500;
        static int v_requestSleepInMilliseconds;
        static Nullable<DateTime> v_requestFirstStartTime;
        static int v_requestCounterPerSecond;
        static int v_requestTPS;

        public int ProcessInstallment(int installmentCycleNumber)
        {
            var income = 0;

            v_requestSleepInMilliseconds = 0;
            v_requestFirstStartTime = null;
            v_requestCounterPerSecond = 0;
            v_requestTPS = 95;

            try
            {
                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                List<string> installmentList;
                using (var entity = new PorShetabEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 120;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    for (int installmentInnerCycleNumber = 1; installmentInnerCycleNumber <= 1; installmentInnerCycleNumber++)
                    {
                        logs.Info("start of installmentInnerCycleNumber " + installmentInnerCycleNumber);
                        //installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();

                        List<string> installmentListNotOrdered = SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersFromServiceCode(serviceCode);

                        Dictionary<string, int> orderedSubscribers = getSubscribersDueToTotalPriceYesterday(entity);

                        installmentList = (from a in installmentListNotOrdered
                                           join b in orderedSubscribers on a equals b.Key into ab
                                           from b in ab.DefaultIfEmpty()
                                           orderby b.Value descending
                                           select new { mobileNumber = a }).Select(t => t.mobileNumber).ToList();

                        logs.Info("installmentList all users count:" + installmentList.Count);
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
                        logs.Info("installmentList compeleted charge users count:" + chargeCompleted.Count);
                        logs.Info("installmentList users in waiting list count:" + waitingList.Count);
                        installmentList.RemoveAll(o => chargeCompleted.Contains(o));
                        installmentList.RemoveAll(o => waitingList.Contains(o));
                        var yesterday = DateTime.Now.AddDays(-1);
                        //var sucessfulyChargedYesterday = entity.SinglechargeArchives.AsNoTracking().Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(yesterday) && o.IsSucceeded == true && o.Price > 0).GroupBy(o => o.MobileNumber).Select(o => o.Key).ToList();
                        var randomList = installmentList.OrderBy(o => Guid.NewGuid()).ToList();
                        int installmentListCount = installmentList.Count;
                        logs.Info("installmentList final list count:" + installmentListCount);
                        var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;
                        income += InstallmentJob(maxChargeLimit, installmentCycleNumber, installmentInnerCycleNumber, serviceCode, chargeCodes, randomList, installmentListCount, installmentListTakeSize, serviceAdditionalInfo);
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

        private static Dictionary<string, int> getSubscribersDueToTotalPriceYesterday(PorShetabEntities entity)
        {
            DateTime today = DateTime.Now.AddDays(-1);
            Dictionary<string, int> chargeOnes = (from sca in entity.SinglechargeArchives where System.Data.Entity.DbFunctions.TruncateTime(sca.DateCreated) == today.Date && (sca.IsSucceeded == true) group sca by sca.MobileNumber into scat let totalPrice = scat.Sum(t => t.Price) select new { mobileNumber = (string)scat.Key, totalPrice }).ToDictionary(t => t.mobileNumber, t => t.totalPrice);

            return chargeOnes;
        }

        public static int InstallmentJob(int maxChargeLimit, int installmentCycleNumber, int installmentInnerCycleNumber
            , string serviceCode, dynamic chargeCodes, List<string> installmentList, int installmentListCount, int installmentListTakeSize
            , Dictionary<string, string> serviceAdditionalInfo)
        {
            var income = 0;
            object obj = new object();
            try
            {
                if (installmentList.Count == 0)
                {
                    logs.Info("InstallmentJob is empty!");
                    return income;
                }
                logs.Info("installmentList count:" + installmentList.Count);

                int isCampaignActive = 0;
                using (var entity = new PorShetabEntities())
                {
                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        isCampaignActive = Convert.ToInt32(campaign.Value);
                }
                int position = 0;
                int maxTaskCount = 120;
                int tps = 95;
                int rowCount = installmentList.Count;
                int i;
                string mobileNumber;
                List<Task> tasksNew = new List<Task>();
                Task task;

                DateTime startTime;
                startTime = DateTime.Now;
                TimeSpan waitTime;

                //int threadNo;
                //threadNo = 0;
                //bool checkTimeDifferences;
                System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection();
                try
                {
                    cnn = PorShetabLibrary.publicVariables.GetConnectionPorshetab();
                    cnn.Open();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in Opening Connection: ", e);
                    return 0;
                }
                int loopNo = 0;
                while (position < rowCount)
                {
                    while (v_requestSleepInMilliseconds > 0)
                    {
                        //wait till the sleep time is zero
                    }
                    i = 0;

                    while (i <= tps - 1 && //DateTime.Now.Second == startTime.Second
                        (DateTime.Now - startTime).TotalMilliseconds < 1000
                        && position < rowCount)
                    {
                        if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                            break;
                        int threadNo = -1;
                        mobileNumber = installmentList[position];

                        task = getTask(tasksNew, cnn, maxTaskCount
                            , maxChargeLimit, mobileNumber, serviceAdditionalInfo, chargeCodes, installmentCycleNumber
                            , installmentInnerCycleNumber, loopNo, out threadNo, isCampaignActive);
                        if (task == null)
                        {

                        }
                        else
                        {
                            ((Task<int>)tasksNew[threadNo]).ContinueWith(o => { lock (obj) { income += o.Result; } });
                            tasksNew[threadNo].Start();
                            position++;
                            i++;
                        }

                    }
                    //while (DateTime.Now.Second == startTime.Second)
                    //{

                    //}
                    waitTime = DateTime.Now - startTime;
                    if (waitTime.TotalMilliseconds < 1000)
                        Thread.Sleep(1100 - (int)waitTime.TotalMilliseconds);
                    startTime = DateTime.Now;
                    loopNo++;
                }

                while (true)
                {
                    for (i = 0; i <= tasksNew.Count - 1; i++)
                    {
                        if (!tasksNew[i].IsCompleted)
                            break;
                    }
                    if (i == tasksNew.Count)
                    {
                        cnn.Close();
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }


        private static Task<int> getTask(List<Task> tasks, System.Data.SqlClient.SqlConnection cnn, int maxCounterCount
            , int maxChargeLimit, string mobileNumber, Dictionary<string, string> serviceAdditionalInfo, dynamic chargeCodes
            , int installmentCycleNumber, int installmentInnerCycleNumber, int loopNo, out int threadNo, int isCampaignActive)
        {
            threadNo = -1;

            int j, k;
            for (j = 0; j <= tasks.Count - 1; j++)
            {
                if (tasks[j].IsCompleted)
                {
                    threadNo = j;
                    break;
                }
            }

            if (threadNo == -1)
            {
                //new task needed
                if (tasks.Count <= maxCounterCount)
                {
                    //not meet the maximum
                    //add new task
                    threadNo = tasks.Count;
                    k = threadNo;
                    tasks.Add(new Task<int>(() =>
                    {
                        return ProcessMtnInstallment(cnn, maxChargeLimit, mobileNumber, serviceAdditionalInfo
, chargeCodes, installmentCycleNumber, installmentInnerCycleNumber, loopNo, k, isCampaignActive);
                    }));
                }
                else
                {
                    //meet the maximum
                    return null;
                }
            }
            else
            {
                k = threadNo;
                tasks[threadNo] = new Task<int>(() =>
                {
                    return ProcessMtnInstallment(cnn, maxChargeLimit, mobileNumber, serviceAdditionalInfo
, chargeCodes, installmentCycleNumber, installmentInnerCycleNumber, loopNo, k, isCampaignActive);
                });
                //tasks[threadNo] = new Task<int>(ProcessMtnInstallment, new object[]{beat, threadNo, delay, this.v_position + 1 });
            }
            return (Task<int>)tasks[threadNo];
        }

        private static async Task<int> ProcessMtnInstallment(System.Data.SqlClient.SqlConnection cnn, int maxChargeLimit, string mobileNumber, Dictionary<string, string> serviceAdditionalInfo
            , dynamic chargeCodes, int installmentCycleNumber, int installmentInnerCycleNumber, int loopNo, int taskId, int isCampaignActive)
        {
            //logs.Info("InstallmentJob Chunk started: task: " + taskId + " - installmentList count:" + chunkedSingleChargeInstallment.Count);
            DateTime timeStartProcessMtnInstallment = DateTime.Now;
            var today = DateTime.Now.Date;
            int income = 0;

            try
            {
                using (var entity = new PorShetabEntities())
                {
                    DateTime timeAfterEntity = DateTime.Now;
                    entity.Configuration.AutoDetectChangesEnabled = false;

                    if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                        return 0;

                    //singlecharge = reserverdSingleCharge;
                    System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("select isnull(sum(price),0) from singleCharge where isSucceeded = 1 and mobileNumber = '" + mobileNumber + "' and convert(date,dateCreated) ='" + DateTime.Now.ToString("yyyy-MM-dd") + "'");
                    cmd.Connection = cnn;
                    int priceUserChargedToday = int.Parse(cmd.ExecuteScalar().ToString());

                    DateTime timeAfterWhere = DateTime.Now;
                    //int priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == mobileNumber && o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == today.Date).ToList().Sum(o => o.Price);
                    bool isSubscriberActive = SharedLibrary.HandleSubscription.IsSubscriberActive(mobileNumber, serviceAdditionalInfo["serviceId"]);
                    if (priceUserChargedToday >= maxChargeLimit || isSubscriberActive == false)
                    {
                        return 0;
                    }
                    var message = new SharedLibrary.Models.MessageObject();
                    message.MobileNumber = mobileNumber;
                    message.ShortCode = serviceAdditionalInfo["shortCode"];
                    message = ChooseMtnSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                    if (installmentCycleNumber == 1 && installmentInnerCycleNumber == 1 && message.Price != maxChargeLimit)
                        return 0;
                    else if (installmentCycleNumber > 2)
                        message.Price = 250;
                    if (priceUserChargedToday + message.Price > maxChargeLimit)
                        return 0;
                    var response = ChargeMtnSubscriber(timeStartProcessMtnInstallment, timeAfterEntity, timeAfterWhere
                        , entity, message, false, false, serviceAdditionalInfo, installmentCycleNumber, loopNo, taskId).Result;

                    if (response.IsSucceeded == true)
                    {
                        income += message.Price.GetValueOrDefault();
                    }
                    if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend)
                    {
                        try
                        {
                            var serviceId = Convert.ToInt64(serviceAdditionalInfo["serviceId"]);

                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, serviceId);
                            if (sub != null)
                            {
                                if (sub.SpecialUniqueId != null)
                                {
                                    var sha = SharedLibrary.Security.GetSha256Hash(sub.SpecialUniqueId + message.MobileNumber);
                                    var price = 0;
                                    if (response.IsSucceeded == true)
                                        price = message.Price.Value;
                                    await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            logs.Error("Exception in calling danoop charge service: " + e);
                        }
                    }

                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in InstallmentJob Chunk task " + taskId + ":", e);
            }

            //logs.Info("InstallmentJob Chunk task " + taskId + " ended");
            return income;
        }

        public static async Task<Singlecharge> ChargeMtnSubscriber(
            DateTime timeStartProcessMtnInstallment, DateTime timeAfterEntity, DateTime timeAfterWhere,
            PorShetabEntities entity, MessageObject message, bool isRefund, bool isInAppPurchase
            , Dictionary<string, string> serviceAdditionalInfo, int installmentCycleNumber, int loopNo, int threadNumber, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeAfterXML = null;
            Nullable<DateTime> timeBeforeHTTPClient = null;
            Nullable<DateTime> timeBeforeSendMTNClient = null;
            Nullable<DateTime> timeAfterSendMTNClient = null;
            Nullable<DateTime> timeBeforeReadStringClient = null;
            Nullable<DateTime> timeAfterReadStringClient = null;


            var startTime = DateTime.Now;
            string charge = "";
            var spId = "980110006379";
            var singlecharge = new Singlecharge();
            singlecharge.MobileNumber = message.MobileNumber;
            singlecharge.ThreadNumber = threadNumber;
            singlecharge.CycleNumber = installmentCycleNumber;
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
, timeStamp, mobile, rialedPrice, referenceCode, charge, "98012000021271", spId);
            try
            {
                singlecharge.ReferenceId = referenceCode;

                throttleRequests(DateTime.Now);

                timeBeforeHTTPClient = DateTime.Now;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");

                    timeBeforeSendMTNClient = DateTime.Now;


                    using (var response = await client.SendAsync(request))
                    {
                        timeAfterSendMTNClient = DateTime.Now;


                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                        {

                            timeBeforeReadStringClient = DateTime.Now;
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                            timeAfterReadStringClient = DateTime.Now;

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
                        timeAfterXML = DateTime.Now;
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

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                singlecharge.ProcessTimeInMilliSecond = (int)duration.TotalMilliseconds;
                entity.Singlecharges.Add(singlecharge);

                SingleChargeTiming timingTable = new SingleChargeTiming();
                timingTable.cycleNumber = installmentCycleNumber;
                timingTable.loopNo = loopNo;
                timingTable.threadNumber = threadNumber;
                timingTable.mobileNumber = message.MobileNumber;
                timingTable.timeAfterReadStringClient = timeAfterReadStringClient;
                timingTable.timeAfterSendMTNClient = timeAfterSendMTNClient;
                timingTable.timeAfterXML = timeAfterXML;
                timingTable.timeBeforeHTTPClient = timeBeforeHTTPClient;
                timingTable.timeBeforeReadStringClient = timeBeforeReadStringClient;
                timingTable.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                timingTable.timeFinish = singlecharge.DateCreated;
                timingTable.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                timingTable.timeStartProcessMtnInstallment = timeStartProcessMtnInstallment;
                timingTable.timeAfterWhere = timeAfterWhere;
                timingTable.timeAfterEntity = timeAfterEntity;
                entity.SingleChargeTimings.Add(timingTable);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeMtnSubscriber on saving values to db: " + e);
            }
            return singlecharge;
        }

        private static void throttleRequests(DateTime requestTime)
        {
            if (v_requestSleepInMilliseconds > 0)
            {

                Thread.Sleep(v_requestSleepInMilliseconds + 10);//add ten to provide time for the locking thread to set the v_sleepMilliseconds sooner than current thread

            }
            object obj = new object();
            if (!v_requestFirstStartTime.HasValue)
            {
                lock (obj)
                {
                    v_requestCounterPerSecond = 1;
                    v_requestFirstStartTime = requestTime;
                    v_requestSleepInMilliseconds = 0;
                }
                return;
            }
            TimeSpan waitTime = requestTime - v_requestFirstStartTime.Value;

            if (waitTime.TotalMilliseconds <= 1000)
            {
                if (v_requestCounterPerSecond <= v_requestTPS)
                {
                    lock (obj)
                    { v_requestCounterPerSecond++; }
                }
                else
                {
                    lock (obj)
                    {
                        v_requestSleepInMilliseconds = (int)(1000 - waitTime.TotalMilliseconds);
                    }
                    Thread.Sleep(v_requestSleepInMilliseconds);
                    lock (obj)
                    {
                        v_requestSleepInMilliseconds = 0;
                        v_requestCounterPerSecond = 1;
                        v_requestFirstStartTime = DateTime.Now;
                    }

                }
            }
            else
            {
                lock (obj)
                {
                    v_requestCounterPerSecond = 1;
                    v_requestFirstStartTime = requestTime;
                }
            }

        }

        public static SharedLibrary.Models.MessageObject ChooseMtnSinglechargePrice(SharedLibrary.Models.MessageObject message, dynamic chargeCodes, int priceUserChargedToday, int maxChargeLimit)
        {
            if (priceUserChargedToday == 0)
            {
                message.Price = maxChargeLimit;
            }
            else if (priceUserChargedToday <= 250)
            {
                message.Price = 250;
            }
            else
                message.Price = 0;
            return message;
        }
    }
}
