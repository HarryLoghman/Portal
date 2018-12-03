using SharedLibrary;
using SharedLibrary.Models;
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
    public class ServiceChargeMTN : ServiceCharge
    {

        public override string[] prp_wipeDescription { get { return new string[] { "SVC0001: Service Error", "POL0904: SP API level request rate control not pass, sla id is 1002.", "POL0910: Minimum Amount per Transaction" }; ; } }
        public ServiceChargeMTN(int serviceId, int tpsService, string aggregatorServiceId, int maxTries, int cycleNumber, int cyclePrice)
            : base(serviceId, tpsService, aggregatorServiceId, maxTries, cycleNumber, cyclePrice)
        {

        }

        public override void sb_charge(ServiceHandler.SubscribersAndCharges subscriber
            , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {
            this.sb_chargeMtnSubscriber(subscriber, installmentCycleNumber, loopNo, threadNumber, timeLoop, installmentId);
        }
        public virtual void sb_chargeMtnSubscriber(
      SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber
      , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {

            var message = this.ChooseSinglechargePrice(subscriber);
            if (message == null) return;
            System.Threading.Interlocked.Increment(ref ChargingController.v_taskCount);
            //object obj = new object();
            //lock (obj) { int t = chargeServices.v_taskCount; chargeServices.v_taskCount = t + 1; }
            this.sb_chargeMtnSubscriberWithOutThread(subscriber, message, installmentCycleNumber, loopNo, threadNumber, timeLoop
                , installmentId);
        }

        public virtual void sb_chargeMtnSubscriberWithOutThread(
        SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber, MessageObject message
         , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeBeforeSendMTNClient = null;

            string guidStr = Guid.NewGuid().ToString();

            //PorShetabLibrary.Models.Singlecharge singlecharge;


            if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
            {
                System.Threading.Interlocked.Decrement(ref ChargingController.v_taskCount);
                return;
            }

            #region prepare Request
            var startTime = DateTime.Now;
            string charge = "chargeAmount";
            var spId = "980110006379";

            var mobile = "98" + message.MobileNumber.TrimStart('0');
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            int rialedPrice = message.Price.Value * 10;
            var referenceCode = Guid.NewGuid().ToString();

            var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
, timeStamp, mobile, rialedPrice, referenceCode, charge, this.prp_service.AggregatorServiceId, spId);
            #endregion

            DateTime dateCreated = DateTime.Now;


            singleChargeRequest singleChargeReq = new singleChargeRequest();
            try
            {
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
                singleChargeReq.referenceCode = referenceCode;
                singleChargeReq.threadNumber = threadNumber;
                singleChargeReq.timeAfterEntity = null;
                singleChargeReq.timeAfterReadStringClient = null;
                singleChargeReq.timeAfterSendMTNClient = null;
                singleChargeReq.timeAfterXML = null;
                singleChargeReq.timeBeforeHTTPClient = null;
                singleChargeReq.timeBeforeReadStringClient = null;
                singleChargeReq.timeBeforeSendMTNClient = timeBeforeSendMTNClient;
                singleChargeReq.timeLoop = timeLoop;
                singleChargeReq.timeStartChargeMtnSubscriber = timeStartChargeMtnSubscriber;
                singleChargeReq.timeStartProcessMtnInstallment = null;
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.url = url;

                sendPostAsync(singleChargeReq);


            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in ChargeMtnSubscriber: ", e);
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

                webRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequest, singleChargeReq });
            }
            catch (Exception ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in SendPostAsync: ", ex);
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
                
                singleChargeReq.resultDescription = this.parseMTN_XMLResult(result, out isSucceeded);
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
                            singleChargeReq.resultDescription = this.parseMTN_XMLResult(rd.ReadToEnd(), out isSucceeded);
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


        protected string parseMTN_XMLResult(string xmlResult, out bool isSucceeded)
        {
            isSucceeded = false;
            string resultDescription = "";
            if (!string.IsNullOrEmpty(xmlResult))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlResult);
                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local");
                XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns1:chargeAmountResponse", manager);
                if (successNode != null)
                {
                    isSucceeded = true;
                }
                else
                {
                    isSucceeded = false;

                    manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                    XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                    foreach (XmlNode fault in faultNode)
                    {
                        XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                        XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                        resultDescription = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                    }
                }
            }
            return resultDescription;

        }


        public virtual void sb_chargeMtnSubscriberDepricated(
       chargeInfo subscriber, MessageObject message
        , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, out bool isSucceed, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeAfterXML = null;
            Nullable<DateTime> timeBeforeHTTPClient = null;
            Nullable<DateTime> timeBeforeSendMTNClient = null;
            Nullable<DateTime> timeAfterSendMTNClient = null;
            Nullable<DateTime> timeBeforeReadStringClient = null;
            Nullable<DateTime> timeAfterReadStringClient = null;

            bool internalServerError;
            WebExceptionStatus status;
            string httpResult;
            string guidStr = Guid.NewGuid().ToString();
            isSucceed = false;
            string resultDescription = "";
            //PorShetabLibrary.Models.Singlecharge singlecharge;


            if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00"))
                return;

            #region prepare Request
            var startTime = DateTime.Now;
            string charge = "chargeAmount";
            var spId = "980110006379";

            var mobile = "98" + message.MobileNumber.TrimStart('0');
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            int rialedPrice = message.Price.Value * 10;
            var referenceCode = Guid.NewGuid().ToString();

            var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
, timeStamp, mobile, rialedPrice, referenceCode, charge, this.prp_service.AggregatorServiceId, spId);
            #endregion

            DateTime dateCreated = DateTime.Now;

            Nullable<TimeSpan> duration = null;

            try
            {
                timeBeforeSendMTNClient = DateTime.Now;

                httpResult = SharedLibrary.UsefulWebApis.sendPostWithWebRequest(url, payload, out internalServerError, out status);

                timeAfterSendMTNClient = DateTime.Now;

                if (status == WebExceptionStatus.Success || internalServerError)
                {

                    timeBeforeReadStringClient = DateTime.Now;
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
                        isSucceed = true;
                    }
                    else
                    {
                        isSucceed = false;

                        manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                        XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                        foreach (XmlNode fault in faultNode)
                        {
                            XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                            XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                            resultDescription = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                        }
                    }
                }
                else
                {
                    isSucceed = false;
                    resultDescription = status.ToString();
                }
                timeAfterXML = DateTime.Now;

                //dateCreated = DateTime.Now;
                var endTime = DateTime.Now;
                duration = endTime - startTime;


            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in ChargeMtnSubscriber: " + e);
            }
            try
            {
                this.insertSingleCharge(message.MobileNumber, referenceCode, dateCreated, message.Price.GetValueOrDefault(), isSucceed
                   , resultDescription, false, installmentId, false, (duration.HasValue ? (int)duration.Value.TotalMilliseconds : (int?)null), installmentCycleNumber, threadNumber);

                this.insertSingleChargeTiming(installmentCycleNumber, loopNo, threadNumber, message.MobileNumber, guidStr
                    , timeLoop, null, null, null, timeStartChargeMtnSubscriber, timeBeforeHTTPClient, timeBeforeSendMTNClient
                    , timeAfterSendMTNClient, timeBeforeReadStringClient, timeAfterReadStringClient, timeAfterXML, dateCreated);
            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + ":" + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in Save to DB: " + e);
            }
        }


    }
}
