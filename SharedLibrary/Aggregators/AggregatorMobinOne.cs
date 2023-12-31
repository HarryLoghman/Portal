﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace SharedLibrary.Aggregators
{
    public class AggregatorMobinOne : Aggregator
    {
        public AggregatorMobinOne()
            : this(false)
        {

        }

        public AggregatorMobinOne(bool addErrorDescription)
            : base(addErrorDescription)
        {
            this.prp_url_sendMessage = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.mobinOne, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            this.prp_url_delivery = "";
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Where(o => o.AggregatorName == "MobinOne").FirstOrDefault();
                this.prp_userName = agg.AggregatorUsername;
                this.prp_password = agg.AggregatorPassword;
            }
        }

        protected override HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            Uri uri = new Uri(url, UriKind.Absolute);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Headers.Add("cache-control", "no-cache");
            webRequest.Headers.Add("SOAPAction", "\"urn: tpswsdl#sendSms\"");
            webRequest.Timeout = 60 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service
            , SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
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

            string userName = this.prp_userName;
            string password = this.prp_password;
            string serviceKey = service.AggregatorServiceId;
            string type = "mt";
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!type == bulk does not work on testing the service!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (messageType == SharedLibrary.MessageHandler.MessageType.EventBase
                 && useBulk)
                type = "bulk";
            string chargeKey;
            if (!price.HasValue)
                price = 0;
            if (price == 0)
                chargeKey = "";
            else
                chargeKey = imiChargeKey;

            string xmlString = $"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                "<s:Body s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                "<q1:sendSms xmlns:q1=\"urn:tpswsdl\"><msg href=\"#id1\"/></q1:sendSms>" +
                "<q2:ArrayReq id=\"id1\" xsi:type=\"q2:ArrayReq\" xmlns:q2=\"urn:tpswsdl\">" +
                $"   <username xsi:type=\"xsd:string\">{userName}</username>" +
                $"   <password xsi:type=\"xsd:string\">{password}</password>" +
                $"   <shortcode xsi:type=\"xsd:string\">{shortCode}</shortcode>" +
                $"   <servicekey xsi:type=\"xsd:string\">{serviceKey}</servicekey>" +
                "   <number href=\"#id2\"/>" +
                "   <message href=\"#id3\"/>" +
                $"   <type xsi:type=\"xsd:string\">{type}</type>" +
                "   <requestId href=\"#id4\"/>" +
                "</q2:ArrayReq>" +
                "<q3:Array id=\"id2\" q3:arrayType=\"xsd:string[1]\" xmlns:q3=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"   <Item>{mobileNumber}</Item>" +
                "</q3:Array>" +
                "<q4:Array id=\"id3\" q4:arrayType=\"xsd:string[1]\" xmlns:q4=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"   <Item>{messageContent}</Item>" +
                "</q4:Array>" +
                "<q5:Array id=\"id4\" q5:arrayType=\"xsd:string[1]\" xmlns:q5=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"   <Item>{price}</Item>" +
                "</q5:Array>" +
                "<q6:Array id=\"id5\" q6:arrayType=\"xsd:string[1]\" xmlns:q6=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"   <Item>{imiChargeKey}</Item>" +
                "</q6:Array>" +
                 "<q7:Array id=\"id6\" q7:arrayType=\"xsd:string[1]\" xmlns:q7=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"   <Item>{correlator}</Item>" +
                "</q7:Array>" +
                "</s:Body>" +
                "</s:Envelope>";
            SharedVariables.logs.Info(xmlString);
            return xmlString;
        }

        internal override string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service
            , SharedLibrary.MessageHandler.MessageType messageType, string[] mobileNumberArr, string[] messageContentArr, DateTime[] dateTimeCorrelatorArr
            , int?[] priceArr, string[] imiChargeKeyArr, bool useBulk)
        {
            string shortCode = service.ShortCode;
            if (!shortCode.StartsWith("98"))
                shortCode = "98" + shortCode.Replace("-", "");
            //DateTime dateTimeCorrelator = request.prp_dateTimeCorrelator;
            //string mobileNumber = request.prp_mobileNumber;
            string aggregatorServiceId = service.AggregatorServiceId;
            int i;
            string userName = this.prp_userName;
            string password = this.prp_password;
            string serviceKey = service.AggregatorServiceId;
            string type = "mt";
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!type == bulk does not work on testing the service!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (messageType == SharedLibrary.MessageHandler.MessageType.EventBase
                 && useBulk)
                type = "bulk";
            string xmlString = $"<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                    "<s:Body s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                    "<q1:sendSms xmlns:q1=\"urn:tpswsdl\"><msg href=\"#id1\"/></q1:sendSms>" +
                    "<q2:ArrayReq id=\"id1\" xsi:type=\"q2:ArrayReq\" xmlns:q2=\"urn:tpswsdl\">" +
                    $"   <username xsi:type=\"xsd:string\">{userName}</username>" +
                    $"   <password xsi:type=\"xsd:string\">{password}</password>" +
                    $"   <shortcode xsi:type=\"xsd:string\">{shortCode}</shortcode>" +
                    $"   <servicekey xsi:type=\"xsd:string\">{serviceKey}</servicekey>" +
                    "   <number href=\"#id2\"/>" +
                    "   <message href=\"#id3\"/>" +
                    $"   <type xsi:type=\"xsd:string\">{type}</type>" +
                    "   <requestId href=\"#id4\"/>" +
                    "</q2:ArrayReq>" +
                    "<q3:Array id=\"id2\" q3:arrayType=\"xsd:string[1]\" xmlns:q3=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            for (i = 0; i <= mobileNumberArr.Length - 1; i++)
            {
                if (!mobileNumberArr[i].StartsWith("98"))
                    mobileNumberArr[i] = "98" + mobileNumberArr[i].TrimStart('0');
                xmlString = xmlString
                    + "   <Item>" + mobileNumberArr[i] + "</Item>";
            }
            xmlString = xmlString
                   + "</q3:Array>";
            xmlString = xmlString + "<q4:Array id=\"id3\" q4:arrayType=\"xsd:string[1]\" xmlns:q4=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            for (i = 0; i <= messageContentArr.Length - 1; i++)
            {
                xmlString = xmlString
            + "   <Item>" + messageContentArr[i] + "</Item>";
            }

            xmlString = xmlString + "</q4:Array>";
            xmlString = xmlString + "<q5:Array id=\"id4\" q5:arrayType=\"xsd:string[1]\" xmlns:q5=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            xmlString += "   <Item></Item>";
            xmlString = xmlString + "</q5:Array>";
            xmlString = xmlString + "<q6:Array id=\"id5\" q6:arrayType=\"xsd:string[1]\" xmlns:q6=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            for (i = 0; i <= imiChargeKeyArr.Length - 1; i++)
            {
                xmlString = xmlString
                    + "<Item>" + imiChargeKeyArr[i] + "</Item>";
            }

            xmlString = xmlString + "</q6:Array>";
            xmlString = xmlString + "<q7:Array id=\"id6\" q7:arrayType=\"xsd:string[1]\" xmlns:q7=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            for (i = 0; i <= dateTimeCorrelatorArr.Length - 1; i++)
            {
                string correlator = SharedLibrary.MessageSender.fnc_getCorrelator(shortCode, dateTimeCorrelatorArr[i].Ticks, true);
                xmlString = xmlString
                    + "<Item>" + correlator + "</Item>";
            }
            xmlString = xmlString +
            "</q7:Array>" +
            "</s:Body>" +
            "</s:Envelope>";


            SharedVariables.logs.Info(xmlString);
            return xmlString;
        }

        internal override string fnc_sendMessage_parseResult(SharedLibrary.Models.vw_servicesServicesInfo service
            , string result, out bool isSucceeded)
        {
            string resultDescription = "";
            isSucceeded = false;
            if (!string.IsNullOrEmpty(result))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(result);
                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                manager.AddNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/");
                manager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns1", "urn:tpswsdl");
                XmlNode returnNode = xml.SelectSingleNode("/SOAP-ENV:Envelope/SOAP-ENV:Body/ns1:sendSmsResponse/return", manager);
                if (returnNode != null)
                {
                    //isSucceeded = true;
                    var returnText = returnNode.InnerText;
                    if (!string.IsNullOrEmpty(returnText))
                    {
                        string[] returnPartsArr = returnText.Split('-');
                        if (returnPartsArr.Length != 4)
                        {
                            if (returnText.StartsWith("-"))
                            {
                                resultDescription = returnText + this.fnc_getAggregatorErrorDescription(returnText);
                                isSucceeded = false;
                                SharedVariables.logs.Error("AggregatorMobinOne " + service.ServiceCode + " fnc_sendMessage_parseResult contains " + returnPartsArr.Length + " item(s). The result is :" + returnText);
                            }
                            else
                            {
                                resultDescription = returnText;
                                isSucceeded = true;
                            }
                        }
                        else
                        {
                            //status-description-responseCode-TxCode
                            if (returnPartsArr[0].ToLower() == "success" && returnPartsArr[1].ToLower() == "accepted" && returnPartsArr[2] == "0")
                            {
                                isSucceeded = true;
                                resultDescription = returnPartsArr[3];//referenceid
                            }
                            else
                            {
                                isSucceeded = false;
                                if (returnPartsArr[0].ToLower() == "failed")
                                {
                                    if (string.IsNullOrEmpty(returnPartsArr[2]))
                                        resultDescription = returnPartsArr[1];
                                    else
                                    {
                                        if (string.IsNullOrEmpty(returnPartsArr[1]))
                                            resultDescription = returnPartsArr[2] + this.fnc_getAggregatorErrorDescription(returnPartsArr[2]);
                                        else resultDescription = returnPartsArr[2] + "(" + returnPartsArr[1] + ")";
                                        resultDescription = resultDescription + "-" + returnPartsArr[3];
                                    }
                                }
                                else
                                {
                                    SharedVariables.logs.Error("AggregatorMobinOne " + service.ServiceCode + " fnc_sendMessage_parseResult unknown status " + returnText);
                                    resultDescription = returnText;
                                }
                            }
                        }
                    }
                    else
                    {
                        resultDescription = result;
                        isSucceeded = false;
                        SharedVariables.logs.Error("AggregatorMobinOne " + service.ServiceCode + " fnc_sendMessage_parseResult return text is null " + result);
                    }

                }
                else
                {
                    resultDescription = result;
                    isSucceeded = false;
                    SharedVariables.logs.Error("AggregatorMobinOne " + service.ServiceCode + " fnc_sendMessage_parseResult cannot find return node in result " + result);
                }
            }
            else
            {
                isSucceeded = false;
                SharedVariables.logs.Error("AggregatorMobinOne " + service.ServiceCode + " fnc_sendMessage_parseResult returns emptyString ");
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
                    SharedVariables.logs.Error("AggregatorMobinOne " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage: ", ex);
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
                        SharedVariables.logs.Error("AggregatorMobinOne " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;
                    ((WebRequestParameterMessage)parameter).prp_referenceId = null;
                    SharedVariables.logs.Error("AggregatorMobinOne " + parameter.prp_service.ServiceCode + " : Exception in SendingMessage2: ", ex);
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
