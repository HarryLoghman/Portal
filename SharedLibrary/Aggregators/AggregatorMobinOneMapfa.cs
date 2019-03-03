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
    public class AggregatorMobinOneMapfa : Aggregator
    {
        List<SharedLibrary.Models.ParidsShortCode> v_pardisShortCodes;
        public AggregatorMobinOneMapfa()
               : this(false)
        {
        }

        public AggregatorMobinOneMapfa(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.mobinOneMapfa, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = "";
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Where(o => o.AggregatorName == "MobinOneMapfa").FirstOrDefault();
                this.prp_userName = agg.AggregatorUsername;
                this.prp_password = agg.AggregatorPassword;

                var servicesShortCodes = portal.vw_servicesServicesInfo.Where(o => o.aggregatorName == "MobinOneMapfa").Select(o => o.ShortCode).ToList();
                v_pardisShortCodes = portal.ParidsShortCodes.Where(o => servicesShortCodes.Contains(o.ShortCode)).ToList();
            }
        }

        protected override HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            Uri uri = new Uri(url, UriKind.Absolute);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Headers.Add("cache-control", "no-cache");
            webRequest.Headers.Add("SOAPAction", "");
            webRequest.Timeout = 60 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service
            , SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey)
        {
            string shortCode = service.ShortCode;
            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode.Replace("-", "");
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');

            string userName = this.prp_userName;
            string password = this.prp_password;


            string domain;
            if (service.AggregatorId == 3)
            {
                domain = "pardis1";
            }
            else
            {
                domain = "alladmin";
            }

            var pardisServiceId = v_pardisShortCodes.Where(o => o.ShortCode == service.ShortCode && o.Price == 0).Select(o => o.PardisServiceId).FirstOrDefault();
            string xmlString = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "<s:Body xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
            "<ServiceSend xmlns=\"http://Srv/\">" +
            $"   <username>{userName}</username>" +
            $"   <password>{password}</password>" +
            $"   <domain>{domain}</domain>" +
            $"   <msgType>0</msgType>" +
            $"   <messages>{messageContent}</messages>" +
            $"   <destinations>{mobileNumber}</destinations>" +
            $"   <originators>{shortCode}</originators>" +
            "   <udhs/><mClass/>" +
            $"   <ServiceIds>{pardisServiceId}</ServiceIds>" +
            "</ServiceSend></s:Body></s:Envelope>";
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
                manager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns", "http://Srv/");
                XmlNode returnNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns:ServiceSendResponse", manager);
                if (returnNode != null)
                {
                    //isSucceeded = true;
                    var returnText = returnNode.InnerText;
                    if (!string.IsNullOrEmpty(returnText))
                    {
                        long lng;
                        if (long.TryParse(returnText, out lng))
                        {
                            if (lng < 100)
                            {
                                isSucceeded = false;
                                resultDescription = lng.ToString() + this.fnc_getAggregatorErrorDescription(lng.ToString());
                            }
                            else
                            {
                                isSucceeded = true;
                                resultDescription = lng.ToString();
                            }
                        }
                        else
                        {
                            SharedVariables.logs.Error("AggregatorMobinoneMapfa " + service.ServiceCode + " fnc_sendMessage_parseResult returns not long value " + lng.ToString());
                            isSucceeded = false;
                            resultDescription = returnText;
                        }
                    }
                    else
                    {
                        SharedVariables.logs.Error("AggregatorMobinoneMapfa " + service.ServiceCode + " fnc_sendMessage_parseResult returns node with empty string " + result);
                        isSucceeded = false;
                        resultDescription = result;
                    }
                }
                else
                {
                    SharedVariables.logs.Error("AggregatorMobinoneMapfa " + service.ServiceCode + " fnc_sendMessage_parseResult returns no node with ServiceSendResponse " + result);
                    isSucceeded = false;
                    resultDescription = result;
                }

            }
            else
            {
                isSucceeded = false;
                SharedVariables.logs.Error("AggregatorMobinoneMapfa " + service.ServiceCode + " fnc_sendMessage_parseResult returns emptyString ");
            }
            return resultDescription;
        }

        internal override void sb_finishRequest(WebRequestParameter parameter, Exception ex)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                parameter.prp_isSucceeded = false;
                string errorTitle = "SendingMessage";
                if (ex is WebException)
                {
                    SharedVariables.logs.Error("AggregatorMobinoneMapfa " + parameter.prp_service.ServiceCode + " : Exception in " + errorTitle + ": ", ex);
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
                                ((WebRequestParameterMessage)parameter).prp_referenceId = null;
                            }
                            webException.Response.Close();
                        }
                        //else eventbase.prp_resultDescription = ex.Message +"\r\n" + ex.StackTrace;
                    }
                    catch (Exception ex1)
                    {
                        SharedVariables.logs.Error("AggregatorMobinoneMapfa " + parameter.prp_service.ServiceCode + " : Exception in " + errorTitle + " inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;
                    ((WebRequestParameterMessage)parameter).prp_referenceId = null;
                    SharedVariables.logs.Error("AggregatorMobinoneMapfa " + parameter.prp_service.ServiceCode + " : Exception in " + errorTitle + "2: ", ex);
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
                    parameterMessage.prp_referenceId = null;
                    parameterMessage.prp_result = parsedResult;
                }

            }
            this.sb_saveResponseToDB(parameter);
        }
    }
}