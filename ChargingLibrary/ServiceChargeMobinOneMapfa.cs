using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Models.ServiceModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChargingLibrary
{
    public class ServiceChargeMobinOneMapfa : ServiceCharge
    {
        string v_url;
        int v_isCampaignActive;
        string v_pardisShortCode;
        public Aggregator prp_aggregator { get; set; }
        public override string[] prp_wipeDescription
        {
            get
            {
                return new string[] { "1001", "1006", "1007","1012","1013", "1015", "1016", "1018", "1027", "12","-2"
                , "-30000", "3002", "3004", "-4", "4998","4999","5030","-999" , "Exception"
                , "The request channel timed out while waiting for a reply after [%]"/*starts with 'The request channel timed out while waiting for a reply after' */
                , "The underlying connection was closed: A connection that was expected to be kept alive was closed by the server[%]"/*starts with 'The underlying connection was closed: A connection that was expected to be kept alive was closed by the server'*/
                };
            }
        }
        public ServiceChargeMobinOneMapfa(long serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice, string notifIcon
             , TimeSpan illegalStartTime, TimeSpan illegalEndTime)
            : base(serviceId, tpsService, maxTries, cycleNumber, cyclePrice, notifIcon, illegalStartTime, illegalEndTime)
        {
            v_isCampaignActive = 0;
            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                using (var entity = new SharedServiceEntities(this.prp_service.databaseName))
                {
                    this.prp_aggregator = entityPortal.Aggregators.Where(o => o.Id == this.prp_service.AggregatorId).FirstOrDefault();
                    if (this.prp_aggregator == null)
                        throw new Exception("There is no aggregator with id =" + this.prp_service.AggregatorId + " in " + this.prp_service.ServiceCode);


                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        v_isCampaignActive = Convert.ToInt32(campaign.Value);
                }

                var entryPardisShortCode = entityPortal.ParidsShortCodes.FirstOrDefault(o => o.ServiceId == serviceId && o.Price == cyclePrice);
                if (entryPardisShortCode == null)
                {
                    throw new Exception("Pardis shortCode is not defined for serviceId=" + serviceId + " and price =" + cyclePrice);
                }
                this.v_pardisShortCode = entryPardisShortCode.PardisServiceId;
            }
            this.v_url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.mobinOneMapfa, HelpfulFunctions.enumServersActions.charge);

        }

        public override void sb_charge(ServiceHandler.SubscribersAndCharges subscriber
            , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {
            this.sb_chargeSubscriber(subscriber, installmentCycleNumber, loopNo, threadNumber, timeLoop, installmentId);
        }
        public virtual void sb_chargeSubscriber(
      SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber
      , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {

            var message = this.ChooseSinglechargePrice(subscriber);
            if (message == null) return;
            if (!ServiceCharge.fnc_isChargingLegalTime(this.prp_illegalStartTime, this.prp_illegalEndTime))
            {
                //System.Threading.Interlocked.Decrement(ref ChargingController.v_taskCount);
                return;
            }
            System.Threading.Interlocked.Increment(ref ChargingController.v_taskCount);
            //object obj = new object();
            //lock (obj) { int t = chargeServices.v_taskCount; chargeServices.v_taskCount = t + 1; }
            this.sb_chargeSubscriberWithOutThread(message, installmentCycleNumber, loopNo, threadNumber, timeLoop
                , installmentId);
        }

        public virtual void sb_chargeSubscriberWithOutThread(
         MessageObject message, int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {
            if (!ServiceCharge.fnc_isChargingLegalTime(this.prp_illegalStartTime, this.prp_illegalEndTime))
            {
                System.Threading.Interlocked.Decrement(ref ChargingController.v_taskCount);
                return;
            }
            singleChargeRequest singleChargeReq = new singleChargeRequest();
            try
            {
                DateTime timeStartChargeMtnSubscriber = DateTime.Now;
                Nullable<DateTime> timeBeforeSendMTNClient = null;

                string guidStr = Guid.NewGuid().ToString();

                //PorShetabLibrary.Models.Singlecharge singlecharge;


                #region prepare Request
                //var startTime = DateTime.Now;
                //string referenceCode;
                var startTime = DateTime.Now;
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var aggregatorId = this.prp_service.AggregatorId;
                var channelType = (int)SharedLibrary.MessageHandler.MapfaChannels.SMS;
                var domain = "";
                if (aggregatorId.ToString() == "3")
                    domain = "pardis1";
                else
                    domain = "alladmin";

                //var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
                //var url = SharedLibrary.HelpfulFunctions.fnc_getServerURL(HelpfulFunctions.enumServers.MTN, HelpfulFunctions.enumServersActions.charge);
                string payload = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">"
                                + "<s:Body>"
                                + "<singleCharge xmlns=\"http://services.mapfa.net\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">"
                                + "<username xmlns=\"\">" + this.prp_aggregator.AggregatorUsername + "</username>"
                                + "<password xmlns=\"\">" + this.prp_aggregator.AggregatorPassword + "</password>"
                                + "<domain xmlns=\"\">" + domain + "</domain>"
                                + "<channel xmlns=\"\">" + channelType + "</channel>"
                                + "<mobilenum xmlns=\"\">" + mobileNumber + "</mobilenum>"
                                + "<serviceId xmlns=\"\">" + this.v_pardisShortCode + "</serviceId></singleCharge>"
                                + "</s:Body></s:Envelope>";
                #endregion
                Program.logs.Info(this.v_url + " " + payload);

                DateTime dateCreated = DateTime.Now;



                timeBeforeSendMTNClient = DateTime.Now;

                singleChargeReq.dateCreated = dateCreated;
                singleChargeReq.guidStr = guidStr;
                singleChargeReq.resultDescription = "";
                singleChargeReq.isSucceeded = false;
                singleChargeReq.installmentCycleNumber = installmentCycleNumber;
                singleChargeReq.installmentId = installmentId;
                singleChargeReq.internalServerError = true;
                singleChargeReq.loopNo = loopNo;
                singleChargeReq.mobileNumber = message.MobileNumber;
                singleChargeReq.payload = payload;
                singleChargeReq.Price = message.Price;
                singleChargeReq.referenceCode = guidStr;
                singleChargeReq.threadNumber = threadNumber;
                singleChargeReq.timeAfterEntity = null;
                singleChargeReq.timeAfterReadStringClient = null;
                singleChargeReq.timeAfterSendRequest = null;
                singleChargeReq.timeAfterXML = null;
                singleChargeReq.timeBeforeHTTPClient = null;
                singleChargeReq.timeBeforeReadStringClient = null;
                singleChargeReq.timeBeforeSendRequest = timeBeforeSendMTNClient;
                singleChargeReq.timeLoop = timeLoop;
                singleChargeReq.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                singleChargeReq.timeStartProcessMtnInstallment = null;
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.url = this.v_url;

                sendPostAsync(singleChargeReq);


            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in ChargeMobinOneMapfa: ", e);
                singleChargeReq.resultDescription = e.Message + "\r\n" + e.StackTrace;
                singleChargeReq.isSucceeded = false;
                this.saveResponseToDB(singleChargeReq);
            }

        }

        public void sendPostAsync(singleChargeRequest singleChargeReq)
        {

            try
            {
                Uri uri = new Uri(singleChargeReq.url, UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";
                webRequest.Proxy = null;
                webRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequest, singleChargeReq });
            }
            catch (Exception ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in Charging SendPostAsync: ", ex);
                singleChargeReq.resultDescription = ex.Message + "\r\n" + ex.StackTrace;
                singleChargeReq.isSucceeded = false;
                this.saveResponseToDB(singleChargeReq);
            }
        }

        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[1];
            try
            {
                singleChargeReq.timeStartProcessMtnInstallment = DateTime.Now;

                using (Stream stream = webRequest.EndGetRequestStream(parameters))
                {
                    Byte[] bts = UnicodeEncoding.UTF8.GetBytes(singleChargeReq.payload);
                    stream.Write(bts, 0, bts.Count());
                }

                webRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), new object[] { webRequest, singleChargeReq });
                singleChargeReq.timeAfterEntity = DateTime.Now;

            }
            catch (System.Net.WebException ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack1: ", ex);

                singleChargeReq.webStatus = ex.Status;
                singleChargeReq.internalServerError = true;
                singleChargeReq.resultDescription = "";
                singleChargeReq.isSucceeded = false;
                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            singleChargeReq.resultDescription = rd.ReadToEnd();
                        }
                        ex.Response.Close();
                    }
                    else singleChargeReq.resultDescription = "";
                }
                catch (Exception e1)
                {
                    Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack inner try: ", e1);
                }

                this.saveResponseToDB(singleChargeReq);
            }
            catch (Exception ex1)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack2: ", ex1);
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.resultDescription = ex1.Message + "\r\n" + ex1.StackTrace;
                singleChargeReq.isSucceeded = false;
                singleChargeReq.internalServerError = true;
                this.saveResponseToDB(singleChargeReq);

            }

        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            bool isSucceeded = false;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[1];

            singleChargeReq.timeAfterWhere = DateTime.Now;
            try
            {

                HttpWebResponse response = (HttpWebResponse)webRequest.EndGetResponse(parameters);
                string result = "";
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
                response.Close();

                //singleChargeReq.dateCreated = dateCreated;
                //singleChargeReq.guidStr = guidStr;

                singleChargeReq.resultDescription = this.parse_XMLResult(result, out isSucceeded);
                //singleChargeReq.installmentCycleNumber = installmentCycleNumber;
                //singleChargeReq.installmentId = installmentId;
                singleChargeReq.internalServerError = false;
                //singleChargeReq.loopNo = loopNo;
                //singleChargeReq.mobileNumber = message.MobileNumber;
                //singleChargeReq.payload = payload;
                //singleChargeReq.Price = message.Price;
                //singleChargeReq.referenceCode = referenceCode;
                //singleChargeReq.threadNumber = threadNumber;
                //singleChargeReq.timeAfterReadStringClient = null;
                //singleChargeReq.timeAfterSendMTNClient = null;
                //singleChargeReq.timeAfterXML = null;
                //singleChargeReq.timeBeforeHTTPClient = null;
                //singleChargeReq.timeBeforeReadStringClient = null;
                //singleChargeReq.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                //singleChargeReq.timeLoop = timeLoop;
                //singleChargeReq.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                singleChargeReq.webStatus = WebExceptionStatus.Success;
                singleChargeReq.isSucceeded = isSucceeded;
                //singleChargeReq.url = url;
                //this.saveResponseToDB(singleChargeReq);
                //this.afterSend(singleChargeReq);
            }
            catch (System.Net.WebException ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1: ", ex);
                singleChargeReq.webStatus = ex.Status;
                singleChargeReq.internalServerError = true;
                singleChargeReq.isSucceeded = false;
                singleChargeReq.resultDescription = "";
                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            singleChargeReq.resultDescription = this.parse_XMLResult(rd.ReadToEnd(), out isSucceeded);
                        }
                        ex.Response.Close();
                    }
                    else singleChargeReq.resultDescription = "";
                }
                catch (Exception ex1)
                {
                    Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1 inner try: ", ex1);
                }

                //this.saveResponseToDB(singleChargeReq);
            }
            catch (Exception ex1)
            {
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.resultDescription = ex1.Message + "\r\n" + ex1.StackTrace;
                singleChargeReq.internalServerError = true;
                singleChargeReq.isSucceeded = false;
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback2: ", ex1);
                //this.saveResponseToDB(singleChargeReq);
            }
            this.saveResponseToDB(singleChargeReq);
            if (!singleChargeReq.internalServerError)
            {
                this.afterSend(singleChargeReq);
            }

        }


        protected string parse_XMLResult(string xmlResult, out bool isSucceeded)
        {
            isSucceeded = false;
            string resultDescription = "";
            try
            {

                if (!string.IsNullOrEmpty(xmlResult))
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(xmlResult);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("S", "http://schemas.xmlsoap.org/soap/envelope/");
                    //manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("ns2", "http://services.mapfa.net");
                    XmlNode successNode = xml.SelectSingleNode("/S:Envelope/S:Body/ns2:singleChargeResponse/return", manager);
                    if (successNode != null)
                    {
                        if (string.IsNullOrEmpty(successNode.InnerText))
                        {
                            resultDescription = "No Inner Text";
                            isSucceeded = false;
                        }
                        else
                        {
                            long returnValue;
                            if (long.TryParse(successNode.InnerText, out returnValue))
                            {
                                if (returnValue > 10000)
                                {
                                    isSucceeded = true;
                                }
                                else
                                {
                                    isSucceeded = false;
                                }
                                resultDescription = successNode.InnerText;
                            }
                            else
                            {
                                resultDescription = successNode.InnerText;
                                isSucceeded = false;
                            }
                        }
                    }
                    else
                    {
                        resultDescription = "No Return Value";
                        isSucceeded = false;
                    }
                }
            }
            catch (Exception ex)
            {
                resultDescription = xmlResult;
                Program.logs.Error("Error in MobinOneMapfa parse_XMLResult:", ex);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , (string.IsNullOrEmpty(this.prp_notifIcon) ? "" : this.prp_notifIcon) + "ChargingLibrary:ServiceChargeMobinOneMapfa:parse_XMLResult:" + ex.Message);
            }
            return resultDescription;

        }

        protected override void afterSend(singleChargeRequest chargeRequest)
        {
            if (v_isCampaignActive == 1 || v_isCampaignActive == 2)
            {
                try
                {
                    var serviceId = Convert.ToInt64(this.prp_service.Id);
                    var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(chargeRequest.mobileNumber, serviceId);
                    if (isInBlackList != true)
                    {
                        var sub = SharedLibrary.SubscriptionHandler.GetSubscriber(chargeRequest.mobileNumber, serviceId);
                        if (sub != null)
                        {
                            if (sub.SpecialUniqueId != null)
                            {
                                var sha = SharedLibrary.Encrypt.GetSha256Hash(sub.SpecialUniqueId + chargeRequest.mobileNumber);
                                var price = 0;
                                if (chargeRequest.isSucceeded == true)
                                    price = chargeRequest.Price.Value;
                                var result = SharedLibrary.UsefulWebApis.DanoopReferral(this.prp_service.referralUrl + (this.prp_service.referralUrl.EndsWith("/") ? "" : "/") + "platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, chargeRequest.mobileNumber, chargeRequest.Price, sha)).Result;
                                //if (serviceAdditionalInfo["serviceCode"] == "Phantom")
                                //    await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/phantom/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                                //else if (serviceAdditionalInfo["serviceCode"] == "Medio")
                                //    await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/medio/platformCharge.php", string.Format("code={0}&number={1}&amount={2}&kc={3}", sub.SpecialUniqueId, message.MobileNumber, price, sha));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.logs.Error("Exception in calling danoop charge service: " + e);
                }
            }
        }
    }
}
