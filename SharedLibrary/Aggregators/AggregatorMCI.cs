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
    public class AggregatorMCI : Aggregator
    {
        public AggregatorMCI()
            : this(false)
        {

        }


        public AggregatorMCI(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.MCI, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.dehnadReceivePortalOnTohid, SharedLibrary.HelpfulFunctions.enumServersActions.dehnadMCIDelivery);
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Where(o => o.AggregatorName == "MciDirect").FirstOrDefault();
                this.prp_userName = agg.AggregatorUsername;
                this.prp_password = agg.AggregatorPassword;
            }
        }

        protected override HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            //Uri uri = new Uri(url, UriKind.Absolute);

            //HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            //webRequest.Headers.Add("serviceKey", this.prp_service.AggregatorServiceId);
            //webRequest.Timeout = 60 * 1000;

            ////webRequest.Headers.Add("SOAPAction", action);
            //webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            //webRequest.Accept = "text/xml";
            //webRequest.Method = "POST";

            HttpWebRequest webRequest = base.fnc_createWebRequestHeader(service, url);
            webRequest.Headers.Add("serviceKey", service.AggregatorServiceId);
            return webRequest;
        }

        internal override string fnc_sendMessage_createBodyString(
            SharedLibrary.Models.vw_servicesServicesInfo service, SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey, bool useBulk)
        {
            string shortCode = service.ShortCode;
            //DateTime dateTimeCorrelator = request.prp_dateTimeCorrelator;
            //string mobileNumber = request.prp_mobileNumber;
            string aggregatorServiceId = service.AggregatorServiceId;

            string correlator = SharedLibrary.MessageSender.fnc_getCorrelator(shortCode, dateTimeCorrelator.Ticks, true);


            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode.Replace("-", "");
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');
            string xmlString = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v4_0/local"">
                           <soapenv:Header/>
                           <soapenv:Body>
                              <loc:sendSms>
                        <loc:addresses>{0}</loc:addresses>
                                 <loc:senderName>{1}</loc:senderName>", mobileNumber, shortCode);
            if (price.HasValue && price.Value != 0)
            {
                xmlString += string.Format(@"
                            <loc:charging>
                                <description></description>
                                <currency>RLS</currency>
                                <amount>{0}</amount>
                                <code>{1}</code>
                             </loc:charging>", price + "0", imiChargeKey);
            }
            xmlString += string.Format(@"
                        <loc:message> {0} </loc:message>
                            <loc:receiptRequest>
                                         <endpoint>{2}</endpoint>
                                    <interfaceName>SMS</interfaceName>
                                    <correlator>{1}</correlator>
                                    </loc:receiptRequest>
                                  </loc:sendSms>
                                </soapenv:Body>
                              </soapenv:Envelope>", messageContent, correlator, this.prp_url_delivery);
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
                manager.AddNamespace("ns2", "http://www.csapi.org/schema/parlayx/sms/send/v4_0/local");
                XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:sendSmsResponse/ns2:result", manager);
                if (successNode != null)
                {
                    isSucceeded = true;
                    resultDescription = successNode.InnerText.Trim();
                }
                else
                {
                    XmlNode faultCode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/soapenv:Fault/faultcode", manager);
                    XmlNode faultString = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/soapenv:Fault/faultstring", manager);
                    XmlNode faultDetail = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/soapenv:Fault/detail", manager);
                    if (faultCode != null)
                        resultDescription += "faultCode=" + faultCode.InnerText;
                    if (faultString != null && !string.IsNullOrEmpty(faultString.InnerText))
                        resultDescription += "faultString=" + faultString.InnerText;
                    else
                    {
                        resultDescription += "faultString=" + this.fnc_getAggregatorErrorDescription(faultCode.InnerText);
                    }
                    if (faultDetail != null)
                        resultDescription += "faultDetail=" + faultDetail.InnerText + this.fnc_getAggregatorErrorDescription(faultDetail.InnerText); ;
                    SharedVariables.logs.Info("AggregatorMCI " + service.ServiceCode + " fnc_sendMessage_parseResult Message was not sended with error: " + resultDescription);

                }
            }
            else
            {
                isSucceeded = false;
                SharedVariables.logs.Error("AggregatorMCI " + service.ServiceCode + " fnc_sendMessage_parseResult returns emptyString ");
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
                    SharedVariables.logs.Error("AggregatorMCI " + parameter.prp_service.ServiceCode + " sb_finishRequest: Exception in SendingMessage: ", ex);
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
                        SharedVariables.logs.Error("AggregatorMCI " + parameter.prp_service.ServiceCode + " sb_finishRequest: Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    SharedVariables.logs.Error("AggregatorMCI " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage2: ", ex);
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
                string parsedResult;
                parsedResult = this.fnc_sendMessage_parseResult(parameter.prp_service, result, out isSucceeded);
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
