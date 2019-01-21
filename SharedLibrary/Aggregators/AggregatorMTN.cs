using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SharedLibrary.Aggregators
{
    public class AggregatorMTN : Aggregator
    {
        protected string v_spId = "980110006379";
        public AggregatorMTN()
            : this(false)
        {

        }
        public AggregatorMTN(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.MTN, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.dehnadReceivePortal, SharedLibrary.HelpfulFunctions.enumServersActions.dehnadMTNDelivery);
        }

        public static string MTNTimestamp(DateTime? date = null)
        {
            if (date == null)
                date = DateTime.Now;
            return date.Value.ToString("yyyyMMddHHmmss");
        }

        protected override HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            //Uri uri = new Uri(url, UriKind.Absolute);
            //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            //webRequest.Timeout = 60 * 1000;

            ////webRequest.Headers.Add("SOAPAction", action);
            //webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            //webRequest.Accept = "text/xml";
            //webRequest.Method = "POST";

            //return webRequest;
            return base.fnc_createWebRequestHeader(service, url);
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service, SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey)
        {
            string shortCode = service.ShortCode;
            //DateTime dateTimeCorrelator = request.prp_dateTimeCorrelator;
            //string mobileNumber = request.prp_mobileNumber;
            string aggregatorServiceId = service.AggregatorServiceId;
            //string messageContent =request.prp_messageContent;

            string correlator = SharedLibrary.MessageSender.fnc_getCorrelator(service.ShortCode, dateTimeCorrelator.Ticks, true);
            var timeStamp = MTNTimestamp(DateTime.Now);

            //var deliveryUrl = "http://79.175.164.51:200/api/Mtn/Delivery";
            string xmlString = string.Format(@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v2_2/local"">
    <soapenv:Header>
        <v2:RequestSOAPHeader>
            <v2:spId>{6}</v2:spId>
            <v2:serviceId>{0}</v2:serviceId>
            <v2:timeStamp>{1}</v2:timeStamp>
        </v2:RequestSOAPHeader>
    </soapenv:Header>
    <soapenv:Body>
        <loc:sendSms>
            <loc:addresses>{2}</loc:addresses>
            <loc:senderName>{3}</loc:senderName>
            <loc:message>{4}</loc:message>
            <loc:receiptRequest>
                <endpoint>{7}</endpoint>
                <interfaceName>SmsNotification</interfaceName>
                <correlator>{5}</correlator>
            </loc:receiptRequest>
        </loc:sendSms>
    </soapenv:Body>
</soapenv:Envelope>"
, aggregatorServiceId, timeStamp, mobileNumber, shortCode, messageContent, correlator, this.v_spId, this.prp_url_delivery);
            return xmlString;
        }

        internal override string fnc_sendMessage_parseResult(SharedLibrary.Models.vw_servicesServicesInfo service, string result, out bool isSucceeded)
        {
            string resultDescription = "";
            isSucceeded = false;
            if (!string.IsNullOrEmpty(result))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(result);
                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/sms/send/v2_2/local");
                XmlNodeList successNodeList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:sendSmsResponse", manager);
                if (successNodeList.Count > 0)
                {
                    isSucceeded = true;
                    foreach (XmlNode success in successNodeList)
                    {
                        XmlNode successResultNode = success.SelectSingleNode("ns1:result", manager);
                        resultDescription = successResultNode.InnerText.Trim();
                    }
                }
                else
                {
                    //isSucceeded = false;

                    manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                    XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                    foreach (XmlNode fault in faultNode)
                    {
                        XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                        XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                        if (faultStringNode == null || string.IsNullOrEmpty(faultStringNode.InnerText))
                            resultDescription = faultCodeNode.InnerText.Trim() + ": " + this.fnc_getAggregatorErrorDescription(faultCodeNode.InnerText);
                        else resultDescription = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                    }
                }
            }
            else
            {
                isSucceeded = false;
                SharedVariables.logs.Error("AggregatorMTN " + service.ServiceCode + " fnc_sendMessage_parseResult returns emptyString ");
            }
            return resultDescription;
        }

        internal override void sb_finishRequest(WebRequestParameter parameter, Exception ex)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                parameter.prp_isSucceeded = false;
                if (ex is WebException)
                {
                    SharedVariables.logs.Error("AggregatorMTN " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage: ", ex);
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    WebException webException = (WebException)ex;
                    try
                    {
                        if (webException.Response != null)
                        {
                            using (StreamReader rd = new StreamReader(webException.Response.GetResponseStream()))
                            {
                                bool isSucceeded;
                                parameter.prp_result = this.fnc_sendMessage_parseResult(parameter.prp_service, rd.ReadToEnd(), out isSucceeded);
                            }
                            webException.Response.Close();
                        }
                        //else eventbase.prp_resultDescription = ex.Message +"\r\n" + ex.StackTrace;
                    }
                    catch (Exception ex1)
                    {
                        SharedVariables.logs.Error("AggregatorMTN " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    SharedVariables.logs.Error("AggregatorMTN " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage2: ", ex);
                }

            }
            this.sb_saveResponseToDB(parameter);
        }
        internal override void sb_finishRequest(WebRequestParameter parameter, bool httpOK, string result)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                WebRequestParameterMessage parameterMessage = (WebRequestParameterMessage)parameter;
                bool isSucceeded;

                string parsedResult = this.fnc_sendMessage_parseResult(parameter.prp_service, result, out isSucceeded);
                if (isSucceeded)
                {
                    parameterMessage.prp_isSucceeded = isSucceeded;
                    parameterMessage.prp_referenceId = parameter.prp_result;
                    parameterMessage.prp_result = "Success";
                }
                else
                {
                    parameterMessage.prp_isSucceeded = isSucceeded;
                    parameterMessage.prp_result = parsedResult;
                }

            }
            this.sb_saveResponseToDB(parameter);
        }
    }
}
