﻿using Newtonsoft.Json;
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
    public class AggregatorTelepromoMapfa : Aggregator
    {
        List<SharedLibrary.Models.ParidsShortCode> v_pardisShortCodes;
        public AggregatorTelepromoMapfa()
            : this(false)
        {

        }

        public AggregatorTelepromoMapfa(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.TelepromoMapfa, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = "";

            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Where(o => o.AggregatorName == "TelepromoMapfa").FirstOrDefault();
                this.prp_userName = agg.AggregatorUsername;
                this.prp_password = agg.AggregatorPassword;
                var servicesShortCodes = portal.vw_servicesServicesInfo.Where(o => o.aggregatorName == "TelepromoMapfa").Select(o => o.ShortCode).ToList();
                v_pardisShortCodes = portal.ParidsShortCodes.Where(o => servicesShortCodes.Contains(o.ShortCode)).ToList();
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
            SharedVariables.logs.Info(url);
            return webRequest;
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service, SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey, bool useBulk)
        {
            string shortCode = service.ShortCode;
            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode.Replace("-", "");
            if (!mobileNumber.StartsWith("98"))
                mobileNumber = "98" + mobileNumber.TrimStart('0');
            string correlator = SharedLibrary.MessageSender.fnc_getCorrelator(service.ShortCode, dateTimeCorrelator.Ticks, true);



            var isFree = true;
            var amount = "0";
            if (price.HasValue && price.Value > 0)
            {
                amount = (price * 10).ToString();
                isFree = false;
            }
            var pardisServiceId = v_pardisShortCodes.Where(o => o.ShortCode == service.ShortCode && o.Price == 0 && o.ServiceId == service.Id).Select(o => o.PardisServiceId).FirstOrDefault();
            //var aggregatorServiceId = service.AggregatorServiceId;
            var description = "";
            Dictionary<string, string> dic = new Dictionary<string, string>()
                            {
                                { "username" , this.prp_userName }
                                ,{ "password" , this.prp_password}
                                ,{"serviceid" , pardisServiceId }
                                ,{"shortcode" , shortCode }
                                ,{ "msisdn" , mobileNumber }
                                ,{"servicename" , service.Name }
                                ,{"currency" , "RLS" }
                                ,{"chargecode" ,"" }
                                ,{"correlator" , correlator }
                                ,{"is_free" , isFree.ToString()}
                                ,{"description",description}
                                ,{ "amount",amount}
                                ,{ "message",messageContent}
                            };
            string json = JsonConvert.SerializeObject(dic);
            SharedVariables.logs.Info(json);
            return json;
        }

        internal override string fnc_sendMessage_parseResult(SharedLibrary.Models.vw_servicesServicesInfo service, string result
            , out bool isSucceeded)
        {
            string resultDescription = "";
            isSucceeded = false;
            var resultArr = new Dictionary<string, string>();
            dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);
            if (jsonResponse.status_txt == "OK" && jsonResponse.status_code == "0")
            {
                isSucceeded = true;
                resultDescription = jsonResponse.data;
            }
            else
            {
                isSucceeded = false;
                resultDescription = result + this.fnc_getAggregatorErrorDescription(jsonResponse.data.ToString());
            }

            return resultDescription;
        }

        internal override void sb_finishRequest(WebRequestParameter parameter, Exception ex, bool changeTaskCount)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                parameter.prp_isSucceeded = false;
                if (ex is WebException)
                {
                    SharedVariables.logs.Error("AggregatorTelepromoMapfa " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage: ", ex);
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
                        SharedVariables.logs.Error("AggregatorTelepromoMapfa " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;
                    ((WebRequestParameterMessage)parameter).prp_referenceId = null;
                    SharedVariables.logs.Error("AggregatorTelepromoMapfa " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage2: ", ex);
                }

            }
            this.sb_saveResponseToDB(parameter, changeTaskCount);
        }
        internal override void sb_finishRequest(WebRequestParameter parameter, bool httpOK, string result, bool changeTaskCount)
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
                    parameterMessage.prp_referenceId = parsedResult;
                    parameterMessage.prp_result = "Success";
                }
                else
                {
                    parameterMessage.prp_isSucceeded = isSucceeded;
                    parameterMessage.prp_referenceId = null;
                    parameterMessage.prp_result = parsedResult;
                }

            }
            this.sb_saveResponseToDB(parameter, changeTaskCount);
        }
    }
}