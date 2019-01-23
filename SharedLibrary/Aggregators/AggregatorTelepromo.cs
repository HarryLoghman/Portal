using Newtonsoft.Json;
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
    public class AggregatorTelepromo : Aggregator
    {
        public AggregatorTelepromo()
            : this(false)
        {

        }
        public AggregatorTelepromo(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.telepromoJson, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = "";
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Where(o => o.AggregatorName == "Telepromo").FirstOrDefault();
                this.prp_userName = agg.AggregatorUsername;
                this.prp_password = agg.AggregatorPassword;
            }
        }

        protected override HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            Uri uri = new Uri(url, UriKind.Absolute);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Headers.Add("cache-control", "no-cache");
            webRequest.Timeout = 60 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "application/json;charset=\"utf-8\"";
            webRequest.Accept = "application/json";
            webRequest.Method = "POST";
            return webRequest;
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service, SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey)
        {

            string shortCode = service.ShortCode;
            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode;
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');
            string correlator = SharedLibrary.MessageSender.fnc_getCorrelator(service.ShortCode, dateTimeCorrelator.Ticks, true);

            var description = "deliverychannel:WAP|discoverychannel:WAP|origin:" + shortCode + "|contentid:" + ((int)messageType).ToString();
            Dictionary<string, string> dic = new Dictionary<string, string>()
                            {
                                { "username" , this.prp_userName }
                                ,{ "password" , this.prp_password}
                                ,{"serviceid" , service.OperatorServiceId }
                                ,{"shortcode" , shortCode }
                                ,{ "msisdn" , mobileNumber }
                                ,{"description" , description }
                                ,{"chargecode" ,imiChargeKey }
                                ,{"amount" , "0" }
                                ,{"currency" , "RLS"}
                                ,{"message",  messageContent }
                                ,{"is_free","1" }
                                ,{"correlator" , correlator }
                                ,{ "servicename" , service.serviceName }
                            };
            string json = JsonConvert.SerializeObject(dic);
            return json;
        }

        internal override string fnc_sendMessage_parseResult(SharedLibrary.Models.vw_servicesServicesInfo service, string result, out bool isSucceeded)
        {
            string resultDescription = "";
            isSucceeded = false;
            var resultArr = new Dictionary<string, string>();
            dynamic results = JsonConvert.DeserializeObject<dynamic>(result);
            resultArr["status_code"] = results["status_code"];
            resultArr["status_txt"] = results["status_txt"];
            resultArr["result"] = results["data"]["result"];
            resultArr["success"] = results["data"]["success"];
            if (resultArr["status_code"] == "0" && resultArr["status_txt"].ToLower().Contains("ok"))
            {
                isSucceeded = true;
                resultDescription = resultArr["result"];

            }
            else
            {
                SharedVariables.logs.Info("AggregatorTelepromo " + service.ServiceCode + " fnc_sendMessage_parseResult Message was not sended with status of: " + resultArr["status"] + " - description: " + resultArr["message"]);
                isSucceeded = false;
                resultDescription = resultArr["message"] + this.fnc_getAggregatorErrorDescription(resultArr["message"]);
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
                    SharedVariables.logs.Error("AggregatorTelepromo " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage: ", ex);
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
                        SharedVariables.logs.Error("AggregatorTelepromo " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    SharedVariables.logs.Error("AggregatorTelepromo " + parameter.prp_service.ServiceCode + " sb_finishRequest : Exception in SendingMessage2: ", ex);
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
