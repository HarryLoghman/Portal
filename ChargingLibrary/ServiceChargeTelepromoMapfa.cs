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
    public class ServiceChargeTelepromoMapfa : ServiceCharge
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
        public ServiceChargeTelepromoMapfa(long serviceId, int tpsService, int maxTries, int cycleNumber, int cyclePrice, string notifIcon
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
            this.v_url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.TelepromoMapfa, HelpfulFunctions.enumServersActions.charge);

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
            Program.logs.Info("ChargeSubscriber" + subscriber.mobileNumber);
            System.Threading.Interlocked.Increment(ref ChargingController.v_taskCount);
            //object obj = new object();
            //lock (obj) { int t = chargeServices.v_taskCount; chargeServices.v_taskCount = t + 1; }
            this.sb_chargeSubscriberWithOutThread(message, installmentCycleNumber, loopNo, threadNumber, timeLoop
                , installmentId);
            Program.logs.Info("ChargeSubscriberEnd" + subscriber.mobileNumber);
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
                var shortCode = "98" + this.prp_service.ShortCode;
                var description = string.Format("deliverychannel=WAP|discoverychannel=WAP|origin={0}|contentid=1", shortCode);
                //var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
                //var url = SharedLibrary.HelpfulFunctions.fnc_getServerURL(HelpfulFunctions.enumServers.MTN, HelpfulFunctions.enumServersActions.charge);
                var json = string.Format(@"{{
                                ""username"": ""{0}"",
                                ""password"": ""{1}"",
                                ""serviceid"": ""{2}"",
                                ""msisdn"": ""{3}"",
                                ""description"": ""{4}"",
                                ""shortcode"": ""{5}""
                    }}", this.prp_aggregator.AggregatorUsername, this.prp_aggregator.AggregatorPassword
                        , this.v_pardisShortCode, mobileNumber, description, shortCode);

                #endregion
                Program.logs.Info(this.v_url + " " + json);

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
                singleChargeReq.payload = json;
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

                Program.logs.Info("ChargeSubscriberWithoutThread" + message.MobileNumber);
                sendPostAsync(singleChargeReq);


            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in ChargeTelepromoMapfa: ", e);
                singleChargeReq.resultDescription = e.Message + "\r\n" + e.StackTrace;
                singleChargeReq.isSucceeded = false;
                this.saveResponseToDB(singleChargeReq);
            }
            Program.logs.Info("ChargeSubscriberWithoutThreadEnd" + message.MobileNumber);
        }

        public void sendPostAsync(singleChargeRequest singleChargeReq)
        {

            try
            {
                Program.logs.Info("sendPostAsync" + singleChargeReq.mobileNumber);

                Uri uri = new Uri(singleChargeReq.url, UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "application/json;charset=\"utf-8\"";
                webRequest.Accept = "application/json";
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
            Program.logs.Info("sendPostAsyncEnd" + singleChargeReq.mobileNumber);
        }

        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[1];
            Program.logs.Info("GetRequestStreamCallBack" + singleChargeReq.mobileNumber);
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
            Program.logs.Info("GetRequestStreamCallBackEnd" + singleChargeReq.mobileNumber);
        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            bool isSucceeded = false;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[1];
            Program.logs.Info("GetResponseCallback" + singleChargeReq.mobileNumber);
            string referenceId;
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

                singleChargeReq.resultDescription = this.parse_JsonResult(result, out referenceId, out isSucceeded);
                //singleChargeReq.installmentCycleNumber = installmentCycleNumber;
                //singleChargeReq.installmentId = installmentId;
                singleChargeReq.internalServerError = false;
                //singleChargeReq.loopNo = loopNo;
                //singleChargeReq.mobileNumber = message.MobileNumber;
                //singleChargeReq.payload = payload;
                //singleChargeReq.Price = message.Price;
                singleChargeReq.referenceCode = referenceId;
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
                            string result = "";

                            result = rd.ReadToEnd();
                            singleChargeReq.resultDescription = this.parse_JsonResult(rd.ReadToEnd(), out referenceId, out isSucceeded);
                            singleChargeReq.referenceCode = referenceId;


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
            Program.logs.Info("GetResponseCallbackBeforeSave" + singleChargeReq.mobileNumber);
            this.saveResponseToDB(singleChargeReq);
           
            Program.logs.Info("GetResponseCallbackAfterSave" + singleChargeReq.mobileNumber);
        }


        protected string parse_JsonResult(string jsonResult, out string referenceId, out bool isSucceeded)
        {
            isSucceeded = false;
            string resultDescription = "";
            referenceId = "";
            
            try
            {
                dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResult);
                if (jsonResponse.data.ToString().Length > 5)
                {
                    isSucceeded = true;
                }
                else
                {
                    isSucceeded = false;
                }
                referenceId = jsonResponse.data.ToString();
                resultDescription = jsonResponse.status_code.ToString() + "-" + jsonResponse.status_txt.ToString() + "-" + jsonResponse.data.ToString();
            }
            catch (Exception ex)
            {
                resultDescription = jsonResult;
                Program.logs.Error("Error in TelepromoMapfa parse_JsonResult:", ex);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , (string.IsNullOrEmpty(this.prp_notifIcon) ? "" : this.prp_notifIcon) + "ChargingLibrary:ServiceChargeTelepromoMapfa:parse_JsonResult:" + ex.Message);

            }

            return resultDescription;

        }

     
    }
}
