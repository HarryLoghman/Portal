using SharedLibrary;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DehnadMTNChargeServices
{
    class ServiceCharge
    {

        #region properties
        public int prp_serviceId { get; set; }
        public string prp_serviceName { get; }
        public string prp_serviceCode { get; }
        public string prp_shortCode { get; }
        public string prp_databaseName { get; }
        /// <summary>
        /// initial tps value
        /// </summary>
        public int prp_tpsServiceInitial { get; }
        /// <summary>
        /// tps during assign
        /// </summary>
        public int prp_tpsServiceCurrent { get; set; }
        public virtual int prp_maxPrice { get; }
        public virtual int prp_maxTries { get; }
        public string prp_aggregatorServiceId { get; }
        public int prp_rowCount { get; set; }
        protected List<SharedLibrary.ServiceHandler.SubscribersAndCharges> v_subscribersAndCharges;
        protected string[] v_wipeDescription = new string[] { "SVC0001: Service Error", "POL0904: SP API level request rate control not pass, sla id is 1002." };
        public int prp_rowIndex { get; set; }
        public int prp_rowsProcessedInSecond { get; set; }
        public int prp_remainRowCount
        {
            get
            {
                return this.prp_rowCount - this.prp_rowIndex;
            }
        }
        public SharedLibrary.ServiceHandler.SubscribersAndCharges prp_subscriberCurrent
        {
            get
            {
                if (this.prp_rowIndex >= 0 && this.prp_rowIndex < this.v_subscribersAndCharges.Count)
                    return this.v_subscribersAndCharges[this.prp_rowIndex];
                else return null;
            }
        }
        internal int prp_cycleNumber { get; set; }

        #endregion

        public ServiceCharge(int serviceId, int tpsService, string aggregatorServiceId, int maxTries, int cycleNumber)
        {
            SharedLibrary.Models.Service service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceId);

            if (service == null)
            {
                throw new Exception("There is no service with serviceId=" + serviceId.ToString());
            }

            SharedLibrary.Models.ServiceInfo serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);

            if (serviceInfo == null)
            {
                throw new Exception("There is no serviceInfo with serviceId=" + serviceId.ToString());
            }

            if (string.IsNullOrWhiteSpace(serviceInfo.databaseName))
            {
                throw new Exception("The database name is not specified for serviceId=" + serviceId.ToString());
            }
            if (maxTries <= 0)
            {
                throw new Exception("Max tries should be a positive value");
            }
            this.prp_serviceId = serviceId;
            this.prp_serviceCode = service.ServiceCode;
            this.prp_serviceName = service.Name;
            this.prp_shortCode = serviceInfo.ShortCode;
            this.prp_databaseName = serviceInfo.databaseName;
            this.prp_maxPrice = (serviceInfo.chargePrice.HasValue ? serviceInfo.chargePrice.Value : 0);
            this.prp_maxTries = maxTries;

            this.prp_tpsServiceInitial = tpsService;
            this.prp_tpsServiceCurrent = tpsService;

            this.prp_aggregatorServiceId = aggregatorServiceId;
            this.prp_cycleNumber = cycleNumber;
            this.prp_rowCount = 0;
            this.prp_rowIndex = 0;
            this.prp_rowsProcessedInSecond = 0;

        }

        #region subs and functions
        public virtual bool fnc_canStartCharging(int cycleNumber, out string reason)
        {
            return this.fnc_canStartCharging(cycleNumber, DateTime.Now, out reason);
        }
        public virtual bool fnc_canStartCharging(int cycleNumber, DateTime date, out string reason)
        {
            reason = "";
            bool canStart = false;
            if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")))
            {
                reason = "23:45:00 to 00:01:00";
                return canStart;
            }
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = publicVariables.GetConnectionPortal();
                cmd.Connection.Open();
                cmd.CommandText = "Select top 1 value from " + this.prp_databaseName + ".dbo.settings where Name='IsInMaintenanceTime'";
                object isInMaintenace = cmd.ExecuteScalar();
                bool bl;
                if (isInMaintenace == null || isInMaintenace == DBNull.Value || isInMaintenace.ToString() == "" || (bool.TryParse(isInMaintenace.ToString(), out bl) && !bl))
                {
                    cmd.CommandText = "Select top 1 value from " + this.prp_databaseName + ".dbo.settings where Name='LastSingleCharge'";
                    object lastSingleCharge = cmd.ExecuteScalar();
                    if (lastSingleCharge == null || lastSingleCharge == DBNull.Value || lastSingleCharge.ToString() == "" || (!lastSingleCharge.ToString().StartsWith(date.ToString("yyyy-MM-dd")) || !lastSingleCharge.ToString().EndsWith(";" + cycleNumber.ToString())))
                    {
                        canStart = true;
                    }
                    else
                    {
                        reason = "Cycle " + cycleNumber.ToString() + " for date " + date.ToString("yyyy-MM-dd") + " has been completed recently";
                        canStart = false;
                    }
                }
                else
                {
                    reason = "Service " + this.prp_serviceId.ToString() + " is in maintenance mode";
                    canStart = false;
                }
            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_serviceCode + ": can start charging", e);
            }
            return canStart;
        }

        public virtual void sb_fill()
        {
            try
            {
                this.v_subscribersAndCharges = null;
                this.prp_rowCount = 0;
                this.prp_rowIndex = 0;

                this.v_subscribersAndCharges = SharedLibrary.ServiceHandler.getActiveSubscribersForCharge(this.prp_databaseName, this.prp_serviceId, DateTime.Now, this.prp_maxPrice, this.prp_maxTries, this.prp_cycleNumber
                , true, false, null, null, null);
                this.prp_rowCount = this.v_subscribersAndCharges.Count;

            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_serviceCode + " : fill", e);
            }
        }

        public virtual void sb_fillWipe()
        {
            try
            {
                this.v_subscribersAndCharges = null;
                this.prp_rowCount = 0;
                this.prp_rowIndex = 0;

                this.v_subscribersAndCharges = SharedLibrary.ServiceHandler.getActiveSubscribersForCharge(this.prp_databaseName, this.prp_serviceId, DateTime.Now, this.prp_maxPrice, this.prp_maxTries, this.prp_cycleNumber
                    , true, true, string.Join(";", this.v_wipeDescription), null, null);
                this.prp_rowCount = this.v_subscribersAndCharges.Count;

            }
            catch (Exception e)
            {
                Program.logs.Error(this.prp_serviceCode + " : fill wipe", e);
            }

        }

        public virtual void sb_chargeMtnSubscriber(SqlConnection cnn,
       SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber
        , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, out bool isSucceed, long installmentId = 0)
        {
            isSucceed = false;
            var message = new SharedLibrary.Models.MessageObject();
            Nullable<int> pricePaidToday = subscriber.pricePaidToday;
            //message.MobileNumber = "100" + subscriber.mobileNumber;
            message.MobileNumber = subscriber.mobileNumber;
            message.ShortCode = this.prp_shortCode;
            message = ChooseMtnSinglechargePrice(message, pricePaidToday.Value);

            if (installmentCycleNumber < 2 && message.Price != this.prp_maxPrice)
                return;
            else if (installmentCycleNumber > 2)
                message.Price = this.prp_maxPrice / 2;

            if (pricePaidToday + message.Price > this.prp_maxPrice)
                return;
            object obj = new object();
            lock (obj)
            {
                chargeServices.v_taskCount++;
            }
            this.sb_chargeMtnSubscriberWithOutThread(cnn, subscriber, message, installmentCycleNumber, loopNo, threadNumber, timeLoop
                , out isSucceed, installmentId);
        }

        public virtual void sb_chargeMtnSubscriberDepricated(SqlConnection cnn,
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
, timeStamp, mobile, rialedPrice, referenceCode, charge, this.prp_aggregatorServiceId, spId);
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
                Program.logs.Info(this.prp_serviceCode + " : " + payload);
                Program.logs.Error(this.prp_serviceCode + " : Exception in ChargeMtnSubscriber: " + e);
            }
            try
            {
                this.insertSingleCharge(cnn, message.MobileNumber, referenceCode, dateCreated, message.Price.GetValueOrDefault(), isSucceed
                   , resultDescription, false, installmentId, false, (duration.HasValue ? (int)duration.Value.TotalMilliseconds : (int?)null), installmentCycleNumber, threadNumber);

                this.insertSingleChargeTiming(cnn, installmentCycleNumber, loopNo, threadNumber, message.MobileNumber, guidStr
                    , timeLoop, null, null, null, timeStartChargeMtnSubscriber, timeBeforeHTTPClient, timeBeforeSendMTNClient
                    , timeAfterSendMTNClient, timeBeforeReadStringClient, timeAfterReadStringClient, timeAfterXML, dateCreated);
            }
            catch (Exception e)
            {
                Program.logs.Info(this.prp_serviceCode + ":" + payload);
                Program.logs.Error(this.prp_serviceCode + " : Exception in Save to DB: " + e);
            }
        }

        public virtual void sb_chargeMtnSubscriberWithOutThread(SqlConnection cnn,
        SharedLibrary.ServiceHandler.SubscribersAndCharges subscriber, MessageObject message
         , int installmentCycleNumber, int loopNo, int threadNumber, DateTime timeLoop, out bool isSucceed, long installmentId = 0)
        {
            DateTime timeStartChargeMtnSubscriber = DateTime.Now;
            Nullable<DateTime> timeBeforeSendMTNClient = null;

            string guidStr = Guid.NewGuid().ToString();
            isSucceed = false;
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
, timeStamp, mobile, rialedPrice, referenceCode, charge, this.prp_aggregatorServiceId, spId);
            #endregion

            DateTime dateCreated = DateTime.Now;


            singleChargeRequest singleChargeReq = new singleChargeRequest();
            try
            {
                timeBeforeSendMTNClient = DateTime.Now;

                singleChargeReq.dateCreated = dateCreated;
                singleChargeReq.guidStr = guidStr;
                singleChargeReq.httpResult = "";
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

                sendPostAsync(cnn, singleChargeReq);


            }
            catch (Exception e)
            {
                Program.logs.Info(this.prp_serviceCode + " : " + payload);
                Program.logs.Error(this.prp_serviceCode + " : Exception in ChargeMtnSubscriber: ", e);
                singleChargeReq.httpResult = e.Message + "\r\n" + e.StackTrace;
                this.saveResponseToDB(cnn, singleChargeReq);
            }

        }

        public void sendPostAsync(SqlConnection cnn, singleChargeRequest singleChargeReq)
        {
            Uri uri = new Uri(singleChargeReq.url, UriKind.Absolute);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = 60 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";

            webRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { cnn, webRequest, singleChargeReq });

        }

        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            SqlConnection cnn = (SqlConnection)objs[0];
            HttpWebRequest webRequest = (HttpWebRequest)objs[1];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[2];
            try
            {
                singleChargeReq.timeStartProcessMtnInstallment = DateTime.Now;

                using (Stream stream = webRequest.EndGetRequestStream(parameters))
                {
                    Byte[] bts = UnicodeEncoding.UTF8.GetBytes(singleChargeReq.payload);
                    stream.Write(bts, 0, bts.Count());
                }

                webRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), new object[] { cnn, webRequest, singleChargeReq });
                singleChargeReq.timeAfterEntity = DateTime.Now;

            }
            catch (System.Net.WebException ex)
            {
                singleChargeReq.webStatus = ex.Status;
                if (ex.Response != null)
                {
                    singleChargeReq.internalServerError = true;
                    using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        singleChargeReq.httpResult = rd.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                else singleChargeReq.httpResult = "";
                Program.logs.Error(this.prp_serviceCode + " : Exception in GetRequestStreamCallBack: ", ex);
                this.saveResponseToDB(cnn, singleChargeReq);
            }
            catch (Exception ex1)
            {
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.httpResult = ex1.Message + "\r\n" + ex1.StackTrace;
                singleChargeReq.internalServerError = true;

                Program.logs.Error(this.prp_serviceCode + " : Exception in GetRequestStreamCallBack: ", ex1);
                this.saveResponseToDB(cnn, singleChargeReq);

            }
        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            SqlConnection cnn = (SqlConnection)objs[0];
            HttpWebRequest webRequest = (HttpWebRequest)objs[1];
            singleChargeRequest singleChargeReq = (singleChargeRequest)objs[2];

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
                singleChargeReq.httpResult = result;
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
                singleChargeReq.isSucceeded = true;
                //singleChargeReq.url = url;
                this.saveResponseToDB(cnn, singleChargeReq);
                this.afterSend(singleChargeReq);
            }
            catch (System.Net.WebException ex)
            {
                singleChargeReq.webStatus = ex.Status;
                if (ex.Response != null)
                {
                    singleChargeReq.internalServerError = true;
                    using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        singleChargeReq.httpResult = rd.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                else singleChargeReq.httpResult = "";
                Program.logs.Error(this.prp_serviceCode + " : Exception in GetResponseCallback123: ", ex);
                this.saveResponseToDB(cnn, singleChargeReq);
            }
            catch (Exception ex1)
            {
                singleChargeReq.webStatus = WebExceptionStatus.UnknownError;
                singleChargeReq.httpResult = ex1.Message + "\r\n" + ex1.StackTrace;
                singleChargeReq.internalServerError = true;

                Program.logs.Error(this.prp_serviceCode + " : Exception in GetResponseCallback: ", ex1);
                this.saveResponseToDB(cnn, singleChargeReq);
            }


        }

        internal void saveResponseToDB(SqlConnection cnn, singleChargeRequest singleChargeReq)
        {
            //DateTime timeResponse = DateTime.Now;
            //Nullable<DateTime> timeAfterXML = null;
            //Nullable<DateTime> timeBeforeHTTPClient = null;
            //Nullable<DateTime> timeBeforeSendMTNClient = null;
            //Nullable<DateTime> timeAfterSendMTNClient = null;
            //Nullable<DateTime> timeBeforeReadStringClient = null;
            //Nullable<DateTime> timeAfterReadStringClient = null;
            object obj = new object();
            lock (obj)
            {
                chargeServices.v_taskCount--;
            }
            singleChargeReq.timeAfterSendMTNClient = DateTime.Now;
            bool isSucceeded = false;
            string resultDescription = "";
            Nullable<TimeSpan> duration = null;

            try
            {
                //timeAfterSendMTNClient = DateTime.Now;
                if (singleChargeReq.webStatus == WebExceptionStatus.Success || singleChargeReq.internalServerError)
                {

                    singleChargeReq.timeBeforeReadStringClient = DateTime.Now;
                    singleChargeReq.timeAfterReadStringClient = DateTime.Now;
                    if (!string.IsNullOrEmpty(singleChargeReq.httpResult))
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(singleChargeReq.httpResult);
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
                }
                else
                {
                    isSucceeded = false;
                    resultDescription = singleChargeReq.webStatus.ToString();
                }
                singleChargeReq.timeAfterXML = DateTime.Now;

                //dateCreated = DateTime.Now;
                var endTime = DateTime.Now;
                duration = endTime - singleChargeReq.timeBeforeSendMTNClient;


            }
            catch (Exception e)
            {
                Program.logs.Info(this.prp_serviceCode + " :  " + singleChargeReq.payload);
                Program.logs.Error(this.prp_serviceCode + " : Exception in ChargeMtnSubscriber: ", e);
            }
            try
            {
                this.insertSingleCharge(cnn, singleChargeReq.mobileNumber, singleChargeReq.referenceCode, singleChargeReq.dateCreated, singleChargeReq.Price.GetValueOrDefault()
                    , isSucceeded
                   , resultDescription, false, singleChargeReq.installmentCycleNumber, false, (duration.HasValue ? (int)duration.Value.TotalMilliseconds : (int?)null), singleChargeReq.installmentCycleNumber, singleChargeReq.threadNumber);

                this.insertSingleChargeTiming(cnn, singleChargeReq.installmentCycleNumber, singleChargeReq.loopNo, singleChargeReq.threadNumber, singleChargeReq.mobileNumber, singleChargeReq.guidStr
                    , singleChargeReq.timeLoop, singleChargeReq.timeStartProcessMtnInstallment, singleChargeReq.timeAfterEntity, singleChargeReq.timeAfterWhere
                    , singleChargeReq.timeStartChargeMtnSubscriber, singleChargeReq.timeBeforeHTTPClient
                    , singleChargeReq.timeBeforeSendMTNClient, singleChargeReq.timeAfterSendMTNClient, singleChargeReq.timeBeforeReadStringClient
                    , singleChargeReq.timeAfterReadStringClient, singleChargeReq.timeAfterXML, singleChargeReq.dateCreated);
            }
            catch (Exception e)
            {
                Program.logs.Info(this.prp_serviceCode + " : " + singleChargeReq.payload);
                Program.logs.Error(this.prp_serviceCode + " : Exception in Save to DB: ", e);
            }
        }

        protected void insertSingleCharge(SqlConnection cnn, string MobileNumber, string ReferenceId, DateTime DateCreated, int Price, bool IsSucceeded
            , string Description, bool IsApplicationInformed, long InstallmentId, bool IsCalledFromInAppPurchase, Nullable<int> ProcessTimeInMilliSecond, int CycleNumber,
                         int ThreadNumber)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            cmd.CommandText = "insert into " + this.prp_databaseName + ".dbo.singleCharge "
                         + "(MobileNumber, ReferenceId, DateCreated, PersianDateCreated, Price, IsSucceeded, Description, IsApplicationInformed, InstallmentId"
                         + ", IsCalledFromInAppPurchase, ProcessTimeInMilliSecond, CycleNumber, ThreadNumber) "
                         + " values"
                         + "('" + MobileNumber + "'" + ","
                         + (string.IsNullOrEmpty(ReferenceId) ? "Null" : "'" + ReferenceId + "'") + ","
                         + "'" + DateCreated.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" + ","
                         + "'" + SharedLibrary.Date.GetPersianDate(DateCreated) + " " + DateCreated.ToString("HH:mm:ss.fff") + "'" + ","
                         + Price + ","
                         + (IsSucceeded ? "1" : "0") + ","
                         + (string.IsNullOrEmpty(Description) ? "Null" : "'" + Description + "'") + ","
                         + (IsApplicationInformed ? "1" : "0") + ","
                         + InstallmentId.ToString() + ","
                         + (IsCalledFromInAppPurchase ? "1" : "0") + ","
                         + (ProcessTimeInMilliSecond.HasValue ? ProcessTimeInMilliSecond.Value.ToString() : "null") + ","
                         + CycleNumber.ToString() + ","
                         + ThreadNumber.ToString() + ")";

            cmd.ExecuteNonQuery();

        }

        protected void insertSingleChargeTiming(SqlConnection cnn, int cycleNumber, int loopNo, int threadNumber, string mobileNumber, string guid, Nullable<DateTime> timeCreate
            , Nullable<DateTime> timeStartProcessMtnInstallment, Nullable<DateTime> timeAfterEntity, Nullable<DateTime> timeAfterWhere, Nullable<DateTime> timeStartChargeMtnSubscriber
            , Nullable<DateTime> timeBeforeHTTPClient, Nullable<DateTime> timeBeforeSendMTNClient, Nullable<DateTime> timeAfterSendMTNClient, Nullable<DateTime> timeBeforeReadStringClient
            , Nullable<DateTime> timeAfterReadStringClient, Nullable<DateTime> timeAfterXML, Nullable<DateTime> timeFinish)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            cmd.CommandText = "insert into " + this.prp_databaseName + ".dbo.singleChargeTiming "
                         + "(cycleNumber, loopNo, threadNumber, mobileNumber, guid, timeCreate, timeStartProcessMtnInstallment, timeAfterEntity, timeAfterWhere "
                         + ", timeStartChargeMtnSubscriber, timeBeforeHTTPClient, timeBeforeSendMTNClient, timeAfterSendMTNClient, timeBeforeReadStringClient"
                         + ", timeAfterReadStringClient, timeAfterXML, timeFinish) "
                         + " values"
                         + "("
                         + cycleNumber.ToString() + ","
                         + loopNo.ToString() + ","
                         + threadNumber.ToString() + ","
                         + "'" + mobileNumber + "'" + ","
                         + "'" + guid + "'" + ","
                         + (timeCreate.HasValue ? "'" + timeCreate.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeStartProcessMtnInstallment.HasValue ? "'" + timeStartProcessMtnInstallment.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeAfterEntity.HasValue ? "'" + timeAfterEntity.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeAfterWhere.HasValue ? "'" + timeAfterWhere.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeStartChargeMtnSubscriber.HasValue ? "'" + timeStartChargeMtnSubscriber.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeBeforeHTTPClient.HasValue ? "'" + timeBeforeHTTPClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeBeforeSendMTNClient.HasValue ? "'" + timeBeforeSendMTNClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeAfterSendMTNClient.HasValue ? "'" + timeAfterSendMTNClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeBeforeReadStringClient.HasValue ? "'" + timeBeforeReadStringClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeAfterReadStringClient.HasValue ? "'" + timeAfterReadStringClient.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeAfterXML.HasValue ? "'" + timeAfterXML.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ","
                         + (timeFinish.HasValue ? "'" + timeFinish.Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ")";

            cmd.ExecuteNonQuery();
        }

        protected virtual void afterSend(singleChargeRequest chargeRequest)
        {

        }
        public SharedLibrary.Models.MessageObject ChooseMtnSinglechargePrice(SharedLibrary.Models.MessageObject message, Nullable<int> pricePaidToday)
        {
            if (!pricePaidToday.HasValue || pricePaidToday == 0)
            {
                message.Price = this.prp_maxPrice;
            }
            else if (pricePaidToday <= this.prp_maxPrice / 2)
            {
                message.Price = this.prp_maxPrice / 2;
            }
            else
                message.Price = 0;
            return message;
        }

        public virtual void sb_finishCharge(int duration)
        {
            string yesterday = (DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");
            string str = "";

            str = "exec sp_finishCharging "
                + " '" + this.prp_databaseName + "'"
                + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                + "," + this.prp_cycleNumber.ToString()
                + "," + duration.ToString();
            using (PortalEntities portal = new PortalEntities())
            {
                portal.Database.CommandTimeout = 180;
                portal.Database.ExecuteSqlCommand(str);
            }

        }
        #endregion
    }
}
