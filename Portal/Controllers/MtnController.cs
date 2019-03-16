using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace Portal.Controllers
{
    public class MtnController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubUnsubNotify()
        {
            string result = "";
            bool resultOk = true;
            string notify = "";
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MtnController:SubUnsubNotify");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    notify = Request.Content.ReadAsStringAsync().Result;
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(notify);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("ns1", "http://www.huawei.com.cn/schema/common/v2_1");
                    manager.AddNamespace("ns2", "http://www.csapi.org/schema/parlayx/data/sync/v1_0/local");
                    XmlNode mobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:syncOrderRelation/ns2:userID/ID", manager);
                    XmlNode mobileNumberTypeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:syncOrderRelation/ns2:userID/type", manager);
                    XmlNode serviceIdNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:syncOrderRelation/ns2:serviceID", manager);
                    XmlNode subscriptionTypeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:syncOrderRelation/ns2:updateType", manager);
                    XmlNodeList extensionInfoList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns2:syncOrderRelation/ns2:extensionInfo/item", manager);
                    var message = new SharedLibrary.Models.MessageObject();
                    foreach (XmlNode item in extensionInfoList)
                    {
                        XmlNode key = item.SelectSingleNode("key");
                        if (key.InnerText.Trim() == "shortCode" || key.InnerText.Trim() == "accessCode")
                        {
                            XmlNode value = item.SelectSingleNode("value");
                            message.ShortCode = value.InnerText.Trim();
                            break;
                        }
                    }
                    if (message.ShortCode == null || message.ShortCode == "")
                    {
                        message.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceIdNode.InnerText.Trim()).ShortCode;
                    }
                    message.MobileNumber = mobileNumberNode.InnerText.Trim();
                    if (mobileNumberTypeNode.InnerText.Trim() == "0")
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
                    if (subscriptionTypeNode.InnerText.Trim() == "1" || subscriptionTypeNode.InnerText.Trim() == "6")
                        message.Content = "Subscription";
                    else if (subscriptionTypeNode.InnerText.Trim() == "2")
                        message.Content = "Unsubscription";

                    message.MobileOperator = 2;
                    message.ReceivedFrom = "92.42.55.180-Notify-" + subscriptionTypeNode.InnerText.Trim();

                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);

                    result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/data/sync/v1_0/local"">     <soapenv:Header/>     <soapenv:Body>        <loc:syncOrderRelationResponse>           <loc:result>0</loc:result>           <loc:resultDescription>OK</loc:resultDescription>                 </loc:syncOrderRelationResponse>     </soapenv:Body>  </soapenv:Envelope> ";
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MtnController:SubUnsubNotify:notify:" + notify, e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Delivery()
        {
            string result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local"">    
            <soapenv:Header/>    
<soapenv:Body>       
<loc:notifySmsDeliveryReceiptResponse/>    
</soapenv:Body> 
</soapenv:Envelope>";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MtnController:Delivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var deliveryData = Request.Content.ReadAsStringAsync().Result;
                    logs.Info("MTN Controller Delivery:" + deliveryData);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(deliveryData);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("ns1", "http://www.huawei.com.cn/schema/common/v2_1");
                    manager.AddNamespace("ns2", "http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local");

                    XmlNode transactionId = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Header/ns1:NotifySOAPHeader/ns1:traceUniqueID", manager);
                    XmlNode deliveryStatusNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/deliveryStatus", manager);
                    XmlNode deliveryMobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/address", manager);
                    XmlNode deliveryErrorCodeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/ErrorCode", manager);
                    XmlNode deliveryErrorSourceNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/ErrorSource", manager);
                    XmlNode deliveryCorrelatorNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:correlator", manager);

                    var correlator = "null";
                    var shortCode = "";
                    var mobileNumber = "null";
                    var errorCode = "null";
                    var errorSource = "null";
                    if (deliveryCorrelatorNode != null)
                    {
                        correlator = deliveryCorrelatorNode.InnerText.Trim().ToString();
                        //shortcode = (!string.IsNullOrEmpty(correlator) && correlator.Contains("s") ? correlator.Split('s')[0] : null);

                    }
                    if (deliveryMobileNumberNode != null)
                        mobileNumber = deliveryMobileNumberNode.InnerText.Trim().ToString();
                    if (deliveryErrorCodeNode != null)
                        errorCode = deliveryErrorCodeNode.InnerText.Trim().ToString();
                    if (deliveryErrorSourceNode != null)
                        errorSource = deliveryErrorSourceNode.InnerText.Trim().ToString();
                    var delivery = new Delivery();

                    SharedLibrary.MessageSender.sb_processCorrelator(correlator, ref mobileNumber, out shortCode);
                    //delivery.ReferenceId = transactionId.InnerText.Trim();
                    delivery.AggregatorId = 7;
                    delivery.Correlator = correlator;
                    if (deliveryStatusNode.InnerText.Trim() == "DeliveredToTerminal")
                        delivery.Delivered = true;
                    else delivery.Delivered = false;

                    delivery.DeliveryTime = DateTime.Now;
                    delivery.Description = "ShortCode=" + correlator + ";MobileNumber=" + mobileNumber + ";ErrorCode=" + errorCode + ";ErrorSource=" + errorSource;
                    delivery.IsProcessed = false;
                    delivery.MobileNumber = mobileNumber;
                    delivery.ReferenceId = transactionId.InnerText.Trim();
                    delivery.ShortCode = shortCode;
                    delivery.Status = deliveryStatusNode.InnerText.Trim();

                    //delivery.ErrorMessage = "ServiceId=" + correlator + ";MobileNumber=" + mobileNumber + ";ErrorCode=" + errorCode + ";ErrorSource=" + errorSource;
                    //delivery.AggregatorId = 7;
                    //SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
                    using (var portal = new SharedLibrary.Models.PortalEntities())
                    {
                        portal.Deliveries.Add(delivery);
                        portal.SaveChanges();

                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MtnController:Delivery", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }


            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage DeliveryOld()
        {
            bool resultOk = true;
            string result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local"">    
            <soapenv:Header/>    
<soapenv:Body>       
<loc:notifySmsDeliveryReceiptResponse/>    
</soapenv:Body> 
</soapenv:Envelope>";
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MtnController:DeliveryOld");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var deliveryData = Request.Content.ReadAsStringAsync().Result;
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(deliveryData);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("ns1", "http://www.huawei.com.cn/schema/common/v2_1");
                    manager.AddNamespace("ns2", "http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local");

                    XmlNode transactionId = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Header/ns1:NotifySOAPHeader/ns1:traceUniqueID", manager);
                    XmlNode delvieryStatusNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/deliveryStatus", manager);
                    XmlNode delvieryMobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/address", manager);
                    XmlNode delvieryErrorCodeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/ErrorCode", manager);
                    XmlNode delvieryErrorSourceNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:deliveryStatus/ErrorSource", manager);
                    XmlNode delvieryServiceIdNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns2:notifySmsDeliveryReceipt/ns2:correlator", manager);

                    var serviceId = "null";
                    var mobileNumber = "null";
                    var errorCode = "null";
                    var errorSource = "null";
                    if (delvieryServiceIdNode != null)
                        serviceId = delvieryServiceIdNode.InnerText.Trim().ToString();
                    if (delvieryMobileNumberNode != null)
                        mobileNumber = delvieryMobileNumberNode.InnerText.Trim().ToString();
                    if (delvieryErrorCodeNode != null)
                        errorCode = delvieryErrorCodeNode.InnerText.Trim().ToString();
                    if (delvieryErrorSourceNode != null)
                        errorSource = delvieryErrorSourceNode.InnerText.Trim().ToString();
                    var delivery = new DeliveryObject();
                    delivery.ReferenceId = transactionId.InnerText.Trim();
                    delivery.Status = delvieryStatusNode.InnerText.Trim();
                    delivery.ErrorMessage = "ServiceId=" + serviceId + ";MobileNumber=" + mobileNumber + ";ErrorCode=" + errorCode + ";ErrorSource=" + errorSource;
                    delivery.AggregatorId = 7;
                    SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MtnController:DeliveryOld:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> startSmsNotificationRequest(string serviceId, string correlatorId, string shortCode)
        {
            string result = "";
            var spId = "980110006379";
            var endpointUrl = "http://79.175.164.51:200/api/mtn/SubUnsubNotify";
            var timeStamp = SharedLibrary.Aggregators.AggregatorMTN.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification_manager/v2_3/local"">
<soapenv:Header>        <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">
<spId>{4}</spId>  
<serviceId>{0}</serviceId>           <timeStamp>{1}</timeStamp>        </RequestSOAPHeader>     
</soapenv:Header>     
<soapenv:Body>        <loc:startSmsNotification>           <loc:reference>              <endpoint>{5}</endpoint>              <interfaceName>notifySmsReception</interfaceName>              <correlator>{2}</correlator>           </loc:reference>           <loc:smsServiceActivationNumber>{3}</loc:smsServiceActivationNumber>           <loc:criteria>demand</loc:criteria>        </loc:startSmsNotification>     </soapenv:Body>  </soapenv:Envelope>  "
, serviceId, timeStamp, correlatorId, shortCode, spId, endpointUrl);
            bool resultOk = true;
            try
            {

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode", shortCode}}
                    , null, "Portal:MtnController:startSmsNotificationRequest");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    //string url = "http://92.42.55.180:8310/SmsNotificationManagerService/services/SmsNotificationManager/startSmsNotificationRequest";
                    string url = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.MTN, SharedLibrary.HelpfulFunctions.enumServersActions.MTNSmsNotification);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var client = new HttpClient())
                    {
                        using (var httpClientResponse = await client.SendAsync(request))
                        {
                            result = "status= " + httpClientResponse.StatusCode.ToString() + Environment.NewLine;
                            if (httpClientResponse.IsSuccessStatusCode || httpClientResponse.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                string httpResult = httpClientResponse.Content.ReadAsStringAsync().Result;
                                result = "description= " + httpResult;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MTNController:startSmsNotificationRequest:" + e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> stopSmsNotificationRequest(string serviceId, string correlatorId)
        {
            string result = "";
            var spId = "980110006379";
            var timeStamp = SharedLibrary.Aggregators.AggregatorMTN.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification_manager/v2_3/local"">     <soapenv:Header>        <v2:RequestSOAPHeader>           <v2:spId>{3}</v2:spId>     <v2:serviceId>{0}</v2:serviceId>           <v2:timeStamp>{1}</v2:timeStamp>        </v2:RequestSOAPHeader>     </soapenv:Header>     <soapenv:Body>        <loc:stopSmsNotification>          <loc:correlator>{2}</loc:correlator>        </loc:stopSmsNotification>     </soapenv:Body>   </soapenv:Envelope>  "
, serviceId, timeStamp, correlatorId, spId);
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "serviceid", serviceId}}
                    , null, "Portal:MtnController:stopSmsNotificationRequest");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    //string url = "http://92.42.55.180:8310/SmsNotificationManagerService/services/SmsNotificationManager/stopSmsNotificationRequest";
                    string url = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.MTN, SharedLibrary.HelpfulFunctions.enumServersActions.MTNSmsStopNotification);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var client = new HttpClient())
                    {
                        using (var httpClientResponse = await client.SendAsync(request))
                        {
                            result = "status= " + httpClientResponse.StatusCode.ToString() + Environment.NewLine;
                            if (httpClientResponse.IsSuccessStatusCode || httpClientResponse.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                string httpResult = httpClientResponse.Content.ReadAsStringAsync().Result;
                                result = "description= " + httpResult;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MTNController:stopSmsNotificationRequest:" + e);
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
                resultOk = false;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> getReceivedSms(string serviceId, string shortCode)
        {
            string result = "";
            var spId = "980110006379";
            var timeStamp = SharedLibrary.Aggregators.AggregatorMTN.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/receive/v2_2/local"">     <soapenv:Header>        <v2:RequestSOAPHeader>           <v2:spId>{3}</v2:spId>           <v2:serviceId>{0}</v2:serviceId>           <v2:timeStamp>{1}</v2:timeStamp>        </v2:RequestSOAPHeader>     </soapenv:Header>     <soapenv:Body>        <loc:getReceivedSms>            <loc:registrationIdentifier>{2}</loc:registrationIdentifier>        </loc:getReceivedSms>     </soapenv:Body>  </soapenv:Envelope> "
, serviceId, timeStamp, shortCode, spId);
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode", shortCode} }
                    , null
                    , "Portal:MtnController:getReceivedSms");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    //string url = "http://92.42.55.180:8310/ReceiveSmsService/services/ReceiveSms/getReceivedSmsRequest";
                    string url = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.MTN, SharedLibrary.HelpfulFunctions.enumServersActions.MTNGetReceivedSms);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var client = new HttpClient())
                    {
                        using (var httpClientResponse = await client.SendAsync(request))
                        {
                            result = "status= " + httpClientResponse.StatusCode.ToString() + Environment.NewLine;
                            if (httpClientResponse.IsSuccessStatusCode || httpClientResponse.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                string httpResult = httpClientResponse.Content.ReadAsStringAsync().Result;
                                result = "description= " + httpResult;
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml(httpResult);
                                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/sms/receive/v2_2/local");
                                XmlNodeList messages = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:getReceivedSmsResponse/ns1:result", manager);
                                foreach (XmlNode message in messages)
                                {
                                    var newMessage = new MessageObject();
                                    XmlNode contentNode = message.SelectSingleNode("message");
                                    XmlNode mobileNumberNode = message.SelectSingleNode("senderAddress");
                                    XmlNode shortCodeNode = message.SelectSingleNode("smsServiceActivationNumber");
                                    newMessage.Content = HttpUtility.UrlDecode(contentNode.InnerText, System.Text.UnicodeEncoding.Default);
                                    newMessage.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Replace("tel:", "").Trim());
                                    newMessage.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(shortCodeNode.InnerText.Replace("tel:", "").Trim());
                                    newMessage.ReceivedFrom = "92.42.55.180";
                                    SharedLibrary.MessageHandler.SaveReceivedMessage(newMessage);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MTNController:getReceivedSms: " + e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChargeTest(string mobileNumber, int price, bool isRefund = false, bool isInAppPurchase = false)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "mobile", mobileNumber } }
                    , null, "Portal:MtnController:ChargeTest");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    //using (var entity = new IrancellTestLibrary.Models.IrancellTestEntities())
                    //{
                    //    //var singleCharge = new IrancellTestLibrary.Models.Singlecharge();
                    //    //MessageObject message = new MessageObject();
                    //    //message.MobileNumber = mobileNumber;
                    //    //message.Price = price;
                    //    //singleCharge = await SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singleCharge, message, isRefund, isInAppPurchase, );

                    //    //result = "isSuccessed: " + singleCharge.IsSucceeded + Environment.NewLine;
                    //    //result += "Description: " + singleCharge.Description + Environment.NewLine;
                    //    //result += "ReferenceId: " + singleCharge.ReferenceId + Environment.NewLine; 
                    //}
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MtnController:ChargeTest", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SyncOrderRelationRequest()
        {
            bool resultOk = true;

            string result = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/data/sync/v1_0/local"">     <soapenv:Header/>     <soapenv:Body>        <loc:syncOrderRelationResponse>           <loc:result>0</loc:result>           <loc:resultDescription>OK</loc:resultDescription>                 </loc:syncOrderRelationResponse>     </soapenv:Body>  </soapenv:Envelope>";
            try
            {
                var syncData = Request.Content.ReadAsStringAsync().Result;
                logs.Info("MTN Controller " + syncData);
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null
                    , null
                    , "Portal:MtnController:SyncOrderRelationRequest");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MtnController:SyncOrderRelationRequest", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
