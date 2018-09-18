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
        static SharedLibrary.ThrottleMTN v_throttle;
        static SharedLibrary.ThrottleDedicated v_throttleDedicated;

        static bool v_tpsDedicatedChanged;
        public int ProcessInstallment(int installmentCycleNumber, int tpsOperator, int tps, DateTime lastExecutionTime, bool forciblyExecute)
        {
            var income = 0;

            try
            {
                v_throttle = new ThrottleMTN(@"E:\Windows Services\MTNThrottleTPS");
                v_throttleDedicated = new ThrottleDedicated(@"E:\Windows Services\MTNThrottleTpsOccupied");

                string aggregatorName = Properties.Settings.Default.AggregatorName;
                var serviceCode = Properties.Settings.Default.ServiceCode;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
                var installmentCount = SharedLibrary.ServiceHandler.GetActiveSubscribersAndChargesCount(Service.v_dbName, serviceCode, Service.maxServiceTries, installmentCycleNumber
                    , Service.maxChargeLimit, DateTime.Now, false, "", lastExecutionTime, forciblyExecute);
                if (installmentCount == 0) return 0;
                //List<string> installmentList;
                using (var entity = new PorShetabEntities())
                {


                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 120;
                    List<ImiChargeCode> chargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();

                    logs.Info("start of installmentCycleNumber " + installmentCycleNumber);
                    ////installmentList = ((IEnumerable)SharedLibrary.InstallmentHandler.GetInstallmentList(entity)).OfType<SinglechargeInstallment>().ToList();

                    ////List<string> installmentListNotOrdered = new List<string>();// SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersFromServiceCode(serviceCode);
                    ////using (var portal = new PortalEntities())
                    ////{
                    ////    installmentListNotOrdered = portal.Subscribers.Where(o => o.ServiceId != 10039 && o.ServiceId != 10028 && o.ServiceId != 10025 && o.ServiceId != 10036 && o.DeactivationDate == null).GroupBy(o => o.MobileNumber).Select(o => new { MobileNumber = o.Key }).OrderBy(o => o.MobileNumber).Skip(40000).Take(30000).Select(o => o.MobileNumber).ToList();
                    ////}
                    //////var installmentExceededRetries = entity.Singlecharges.GroupBy(o => o.MobileNumber).Where(o => o.Count() > maxServiceTries).Select(o => o.Key).ToList();
                    //////installmentListNotOrdered.RemoveAll(o => installmentExceededRetries.Contains(o));

                    //List<string> installmentListNotOrdered = SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersFromServiceCode(serviceCode);

                    //var installmentExceededRetries = entity.Singlecharges.GroupBy(o => o.MobileNumber).Where(o => o.Count() > maxServiceTries).Select(o => o.Key).ToList();
                    //installmentListNotOrdered.RemoveAll(o => installmentExceededRetries.Contains(o));

                    //Dictionary<string, int> orderedSubscribers = getSubscribersDueToTotalPriceYesterday(entity);

                    //installmentList = (from a in installmentListNotOrdered
                    //                   join b in orderedSubscribers on a equals b.Key into ab
                    //                   from b in ab.DefaultIfEmpty()
                    //                   orderby b.Value descending
                    //                   select new { mobileNumber = a }).Select(t => t.mobileNumber).ToList();

                    //logs.Info("installmentList all users count:" + installmentList.Count);
                    installmentCount = SharedLibrary.ServiceHandler.GetServiceActiveMobileNumbersCountFromServiceCode(serviceCode);
                    var today = DateTime.Now;
                    int chargeCompletedCount;
                    var delayDateBetweenCharges = today.AddDays(0);
                    if (delayDateBetweenCharges.Date != today.Date)
                    {
                        chargeCompletedCount = entity.vw_Singlecharge.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) >= DbFunctions.TruncateTime(delayDateBetweenCharges) && DbFunctions.TruncateTime(o.DateCreated) <= DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) >= Service.maxChargeLimit).Count();
                    }
                    else
                    {
                        chargeCompletedCount = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) >= Service.maxChargeLimit).Count();
                    }
                    var waitingListCount = entity.SinglechargeWaitings.AsNoTracking().Count();
                    logs.Info("installmentList all users count:" + installmentCount);
                    logs.Info("installmentList compeleted charge users count:" + chargeCompletedCount);
                    logs.Info("installmentList users in waiting list count:" + waitingListCount);
                    //installmentList.RemoveAll(o => chargeCompleted.Contains(o));
                    //installmentList.RemoveAll(o => waitingList.Contains(o));

                    //Dictionary<string, int> singleCharge = getSubscribersChargesPriceToday(entity);

                    //Dictionary<string, int> subscribersAndCharges = (from mobile in installmentList
                    //                                                 join charges in singleCharge on mobile equals charges.Key
                    //                                                 into temp
                    //                                                 from charges in temp.DefaultIfEmpty()
                    //                                                 select new { mobile, chargePrice = charges.Value }).ToDictionary(o => o.mobile, o => o.chargePrice);

                    //var yesterday = DateTime.Now.AddDays(-1);
                    ////var sucessfulyChargedYesterday = entity.SinglechargeArchives.AsNoTracking().Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(yesterday) && o.IsSucceeded == true && o.Price > 0).GroupBy(o => o.MobileNumber).Select(o => o.Key).ToList();
                    ////var randomList = installmentList.OrderBy(o => Guid.NewGuid()).ToList();
                    //int installmentListCount = installmentList.Count;
                    //logs.Info("installmentList final list count:" + installmentListCount);
                    //var installmentListTakeSize = Properties.Settings.Default.DefaultSingleChargeTakeSize;

                    List<SharedLibrary.ServiceHandler.SubscribersAndCharges> subscribersAndChargesList =
                       SharedLibrary.ServiceHandler.GetActiveSubscribersAndCharges(Service.v_dbName, serviceCode, Service.maxServiceTries, installmentCycleNumber
                       , Service.maxChargeLimit, true, null, DateTime.Now, false, "", lastExecutionTime, forciblyExecute);

                    logs.Info("installmentList final list count:" + subscribersAndChargesList.Count);

                    v_throttleDedicated.occupyTps(Service.v_dbName, tps, tpsOperator);
                    v_throttleDedicated.ev_tpsOccupiedChanged += delegate (object sender, EventArgs e) { v_tpsDedicatedChanged = true; };

                    income += InstallmentJob(Service.maxChargeLimit, installmentCycleNumber, serviceCode, chargeCodes
                        , subscribersAndChargesList, serviceAdditionalInfo, tpsOperator, tps);

                    subscribersAndChargesList =
                       SharedLibrary.ServiceHandler.GetActiveSubscribersAndCharges(Service.v_dbName, serviceCode, Service.maxServiceTries, installmentCycleNumber
                       , Service.maxChargeLimit, true, null, DateTime.Now, true, "SVC0001: Service Error;POL0904: SP API level request rate control not pass, sla id is 1002."
                       , lastExecutionTime, forciblyExecute);

                    logs.Info("wipe Count:" + subscribersAndChargesList.Count);

                    income += InstallmentJob(Service.maxChargeLimit, installmentCycleNumber, serviceCode, chargeCodes
                        , subscribersAndChargesList, serviceAdditionalInfo, tpsOperator, tps);

                    v_throttleDedicated.releaseTps(Service.v_dbName, tps, tpsOperator);

                    logs.Info("end of installmentCycleNumber " + installmentCycleNumber);

                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ProcessInstallment:", e);
            }
            return income;
        }

        public static int InstallmentJob(int maxChargeLimit, int installmentCycleNumber
            , string serviceCode, dynamic chargeCodes, List<SharedLibrary.ServiceHandler.SubscribersAndCharges> installmentList
            , Dictionary<string, string> serviceAdditionalInfo, int tpsOperator, int tps)
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

                int rowCount = installmentList.Count;

                List<Task> tasksNew = new List<Task>();
                //Task task;

                //System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection();
                //try
                //{
                //    cnn = DambelLibrary.publicVariables.GetConnection();
                //    cnn.Open();
                //}
                //catch (Exception e)
                //{
                //    logs.Error("Exception in Opening Connection: ", e);
                //    return 0;
                //}
                int loopNo = 0;
                int taskCount = 0;


                //List<string> mobiles = installmentArr.Keys.ToList();
                //List<int> chargedPrices = installmentArr.Values.ToList();
                rowCount = installmentList.Count;
                while (position < rowCount)
                {
                    if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                        break;
                    string mobileNumber = installmentList[position].mobileNumber;
                    int chargedPriceToday = (installmentList[position].pricePaidToday.HasValue ? installmentList[position].pricePaidToday.Value : 0);

                    int currentTps = tps;
                    if (v_tpsDedicatedChanged) { v_tpsDedicatedChanged = false; currentTps = v_throttleDedicated.getMaxTps(tpsOperator, tps); logs.Info(";tpsChanged;tpsChanged;porshetab;" + mobileNumber + ";" + taskCount + ";" + tps + ";" + tpsOperator + ";" + currentTps + ";"); }

                    v_throttleDedicated.throttleRequests("porshetab", mobileNumber, Guid.NewGuid().ToString(), currentTps);

                    lock (obj) { taskCount = taskCount + 1; }
                    loopNo++;
                    int threadNo = taskCount;
                    int lpNo = loopNo;
                    Task<int> task = new Task<int>(() =>
                    {
                        return ProcessMtnInstallment(maxChargeLimit, mobileNumber, chargedPriceToday
                        , serviceAdditionalInfo, chargeCodes, installmentCycleNumber, lpNo, threadNo, isCampaignActive, DateTime.Now);
                    });
                    task.ContinueWith(o => { lock (obj) { taskCount = taskCount - 1; income += o.Result; } });
                    logs.Warn(";porshetab;InstallmentJob;" + mobileNumber);
                    task.Start();
                    while (task.Status == TaskStatus.WaitingForActivation || task.Status == TaskStatus.WaitingForChildrenToComplete || task.Status == TaskStatus.WaitingToRun
                         || task.Status == TaskStatus.Created)
                    {

                    }

                    position++;


                }

                while (taskCount > 0)
                {
                }

                //while (position < rowCount)
                //{
                //    if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                //        break;
                //    String mobileNumber = installmentDic[position];

                //    throttleDedicated.throttleRequests("dambel", mobileNumber, Guid.NewGuid().ToString(), 1050, tps);

                //    taskCount++;
                //    loopNo++;
                //    int threadNo = taskCount;
                //    int lpNo = loopNo;
                //    task = new Task<int>(() =>
                //    {
                //        return ProcessMtnInstallment(cnn, maxChargeLimit, mobileNumber
                //        , serviceAdditionalInfo, chargeCodes, installmentCycleNumber, installmentInnerCycleNumber, lpNo, threadNo, isCampaignActive, DateTime.Now).Result;
                //    });
                //    ((Task<int>)task).ContinueWith(o => { lock (obj) { taskCount--; income += o.Result; } });

                //    while (task.Status == TaskStatus.WaitingForActivation || task.Status == TaskStatus.WaitingForChildrenToComplete || task.Status == TaskStatus.WaitingToRun
                //         || task.Status == TaskStatus.Created)
                //    {

                //    }
                //}

                //while (taskCount > 0)
                //{
                //}
                //cnn.Close();

            }
            catch (Exception e)
            {
                logs.Error("Exception in SinglechargeInstallment InstallmentJob: ", e);
            }
            logs.Info("installmentCycleNumber:" + installmentCycleNumber + " ended");
            logs.Info("InstallmentJob ended!");
            return income;
        }


        private static int ProcessMtnInstallment(int maxChargeLimit, string mobileNumber, int priceUserChargedToday, Dictionary<string, string> serviceAdditionalInfo
            , dynamic chargeCodes, int installmentCycleNumber, int loopNo, int taskId, int isCampaignActive
            , DateTime timeLoop)
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

                    ////singlecharge = reserverdSingleCharge;
                    //newcomment System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("select isnull(sum(price),0) from singleCharge where isSucceeded = 1 and mobileNumber = '" + mobileNumber + "' and convert(date,dateCreated) ='" + DateTime.Now.ToString("yyyy-MM-dd") + "'");
                    //newcomment cmd.Connection = cnn;
                    //newcomment int priceUserChargedToday = int.Parse(cmd.ExecuteScalar().ToString());

                    DateTime timeAfterWhere = DateTime.Now;
                    ////int priceUserChargedToday = entity.Singlecharges.Where(o => o.MobileNumber == mobileNumber && o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == today.Date).ToList().Sum(o => o.Price);
                    //newcomment bool isSubscriberActive = SharedLibrary.HandleSubscription.IsSubscriberActive(mobileNumber, serviceAdditionalInfo["serviceId"]);
                    //newcomment if (priceUserChargedToday >= maxChargeLimit || isSubscriberActive == false)
                    //newcomment {
                    //newcomment     return 0;
                    //newcomment }
                    var message = new SharedLibrary.Models.MessageObject();
                    message.MobileNumber = mobileNumber;
                    message.ShortCode = serviceAdditionalInfo["shortCode"];
                    message = ChooseMtnSinglechargePrice(message, chargeCodes, priceUserChargedToday, maxChargeLimit);
                    if (installmentCycleNumber == 1 && message.Price != maxChargeLimit)
                        return 0;
                    else if (installmentCycleNumber > 2)
                        message.Price = 250;
                    if (priceUserChargedToday + message.Price > maxChargeLimit)
                        return 0;
                    logs.Warn(";porshetab;processMTNInstallment1;" + mobileNumber);
                    var response = ChargeMtnSubscriber(timeStartProcessMtnInstallment, timeAfterEntity, timeAfterWhere
                        , entity, message, false, false, serviceAdditionalInfo["aggregatorServiceId"], installmentCycleNumber, loopNo, taskId, timeLoop);
                    logs.Warn(";porshetab;processMTNInstallment2;" + mobileNumber);
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
                                    logs.Warn(";porshetab;processMTNInstallment3;" + mobileNumber);
                                    SharedLibrary.UsefulWebApis.DanoopReferralWithWebRequest("http://79.175.164.52/porshetab/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                    logs.Warn(";porshetab;processMTNInstallment4;" + mobileNumber);
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

        public static Singlecharge ChargeMtnSubscriber(
            DateTime timeStartProcessMtnInstallment, DateTime timeAfterEntity, DateTime timeAfterWhere,
            PorShetabEntities entity, MessageObject message, bool isRefund, bool isInAppPurchase
            , string aggregatorServiceId, int installmentCycleNumber, int loopNo, int threadNumber
            , DateTime timeLoop, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeAfterXML = null;
            Nullable<DateTime> timeBeforeHTTPClient = null;
            Nullable<DateTime> timeBeforeSendMTNClient = null;
            Nullable<DateTime> timeAfterSendMTNClient = null;
            Nullable<DateTime> timeBeforeReadStringClient = null;
            Nullable<DateTime> timeAfterReadStringClient = null;
            string guidStr = Guid.NewGuid().ToString();

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
, timeStamp, mobile, rialedPrice, referenceCode, charge, aggregatorServiceId, spId);
            try
            {
                singlecharge.ReferenceId = referenceCode;

                timeBeforeHTTPClient = DateTime.Now;

                DateTime timeLimitToSend = DateTime.Now.AddSeconds(-1);

                while (DateTime.Now > timeLimitToSend)
                {

                    timeLimitToSend = v_throttle.throttleRequests("porshetab", mobile, guidStr);

                    payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
                    , timeStamp, mobile, rialedPrice, referenceCode, charge, aggregatorServiceId, spId);
                    //v_client.Timeout = TimeSpan.FromSeconds(60);

                    //request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    if (DateTime.Now > timeLimitToSend)
                        logs.Warn(";porshetab;TimePassed;" + DateTime.Now.ToString("HH:mm:ss,fff") + ";" + timeLimitToSend.ToString("HH:mm:ss,fff"));
                }
                logs.Warn(";porshetab;ChargeMtnSubscriber2;" + mobile);
                timeBeforeSendMTNClient = DateTime.Now;
                //logs.Warn(";****Porshetab;" + mobile + ";" + guidStr + ";" + DateTime.Now.ToString("HH:mm:ss.fff"));
                bool internalServerError;
                WebExceptionStatus status;
                string httpResult;
                httpResult = SharedLibrary.UsefulWebApis.sendPostWithWebRequest(url, payload, out internalServerError, out status);
                logs.Warn(";porshetab;ChargeMtnSubscriber3;" + mobile);
                timeAfterSendMTNClient = DateTime.Now;

                if (status == WebExceptionStatus.Success || internalServerError)
                {
                    logs.Warn(";porshetab;ChargeMtnSubscriber4;" + mobile);
                    timeBeforeReadStringClient = DateTime.Now;
                    //string httpResult = response.Content.ReadAsStringAsync().Result;
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
                    singlecharge.Description = status.ToString();
                }
                timeAfterXML = DateTime.Now;
                //using (var response = await v_client.SendAsync(request))
                //{
                //    timeAfterSendMTNClient = DateTime.Now;


                //    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                //    {

                //        timeBeforeReadStringClient = DateTime.Now;
                //        string httpResult = response.Content.ReadAsStringAsync().Result;
                //        timeAfterReadStringClient = DateTime.Now;

                //        XmlDocument xml = new XmlDocument();
                //        xml.LoadXml(httpResult);
                //        XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                //        manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                //        manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                //        manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local");
                //        XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns1:chargeAmountResponse", manager);
                //        if (successNode != null)
                //        {
                //            singlecharge.IsSucceeded = true;
                //        }
                //        else
                //        {
                //            singlecharge.IsSucceeded = false;

                //            manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                //            XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                //            foreach (XmlNode fault in faultNode)
                //            {
                //                XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                //                XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                //                singlecharge.Description = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        singlecharge.IsSucceeded = false;
                //        singlecharge.Description = response.StatusCode.ToString();
                //    }
                //    timeAfterXML = DateTime.Now;
                //}

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
                timingTable.guid = guidStr;
                timingTable.timeAfterReadStringClient = timeAfterReadStringClient;
                timingTable.timeAfterSendMTNClient = timeAfterSendMTNClient;
                timingTable.timeAfterXML = timeAfterXML;
                timingTable.timeBeforeHTTPClient = timeBeforeHTTPClient;
                timingTable.timeBeforeReadStringClient = timeBeforeReadStringClient;
                timingTable.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                timingTable.timeCreate = timeLoop;
                timingTable.timeFinish = singlecharge.DateCreated;
                timingTable.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                timingTable.timeStartProcessMtnInstallment = timeStartProcessMtnInstallment;
                timingTable.timeAfterWhere = timeAfterWhere;
                timingTable.timeAfterEntity = timeAfterEntity;
                entity.SingleChargeTimings.Add(timingTable);
                entity.SaveChanges();
                logs.Warn(";porshetab;ChargeMtnSubscriber5;" + mobile);
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChargeMtnSubscriber on saving values to db: " + e);
            }
            return singlecharge;
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
