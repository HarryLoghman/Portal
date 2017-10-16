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
            var notify = Request.Content.ReadAsStringAsync().Result;
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
            
            message.MobileNumber = mobileNumberNode.InnerText.Trim();
            if (mobileNumberTypeNode.InnerText.Trim() == "0")
                message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
            if (subscriptionTypeNode.InnerText.Trim() == "1" || subscriptionTypeNode.InnerText.Trim() == "6")
                message.Content = "Subscription";
            else if(subscriptionTypeNode.InnerText.Trim() == "2")
                message.Content = "Unsubscription";

            message.MobileOperator = 2;
            message.ReceivedFrom = "92.42.55.180-Notify-" + subscriptionTypeNode.InnerText.Trim();

            SharedLibrary.MessageHandler.SaveReceivedMessage(message);

            string result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/data/sync/v1_0/local"">     <soapenv:Header/>     <soapenv:Body>        <loc:syncOrderRelationResponse>           <loc:result>0</loc:result>           <loc:resultDescription>OK</loc:resultDescription>                 </loc:syncOrderRelationResponse>     </soapenv:Body>  </soapenv:Envelope> ";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Delivery()
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


            var delivery = new DeliveryObject();
            delivery.ReferenceId = transactionId.InnerText.Trim();
            delivery.Status = delvieryStatusNode.InnerText.Trim();
            delivery.AggregatorId = 7;
            SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);

            string result = @"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification/v2_2/local"">    
            <soapenv:Header/>    
<soapenv:Body>       
<loc:notifySmsDeliveryReceiptResponse/>    
</soapenv:Body> 
</soapenv:Envelope>";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
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
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification_manager/v2_3/local"">
<soapenv:Header>        <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">
<spId>{4}</spId>  
<serviceId>{0}</serviceId>           <timeStamp>{1}</timeStamp>        </RequestSOAPHeader>     
</soapenv:Header>     
<soapenv:Body>        <loc:startSmsNotification>           <loc:reference>              <endpoint>{5}</endpoint>              <interfaceName>notifySmsReception</interfaceName>              <correlator>{2}</correlator>           </loc:reference>           <loc:smsServiceActivationNumber>{3}</loc:smsServiceActivationNumber>           <loc:criteria>demand</loc:criteria>        </loc:startSmsNotification>     </soapenv:Body>  </soapenv:Envelope>  "
, serviceId, timeStamp, correlatorId, shortCode, spId, endpointUrl);
            try
            {
                string url = "http://92.42.55.180:8310/SmsNotificationManagerService/services/SmsNotificationManager/startSmsNotificationRequest";
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
            catch (Exception e)
            {
                logs.Error("Exception in startSmsNotificationRequest: " + e);
                result = "Exception in startSmsNotificationRequest:" + e.Message;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> stopSmsNotificationRequest(string serviceId, string correlatorId)
        {
            string result = "";
            var spId = "980110006379";
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/notification_manager/v2_3/local"">     <soapenv:Header>        <v2:RequestSOAPHeader>           <v2:spId>{3}</v2:spId>     <v2:serviceId>{0}</v2:serviceId>           <v2:timeStamp>{1}</v2:timeStamp>        </v2:RequestSOAPHeader>     </soapenv:Header>     <soapenv:Body>        <loc:stopSmsNotification>          <loc:correlator>{2}</loc:correlator>        </loc:stopSmsNotification>     </soapenv:Body>   </soapenv:Envelope>  "
, serviceId, timeStamp, correlatorId, spId);
            try
            {
                string url = "http://92.42.55.180:8310/SmsNotificationManagerService/services/SmsNotificationManager/stopSmsNotificationRequest";
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
            catch (Exception e)
            {
                logs.Error("Exception in stopSmsNotificationRequest: " + e);
                result = "Exception in stopSmsNotificationRequest:" + e.Message;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> getReceivedSms(string serviceId, string shortCode)
        {
            string result = "";
            var spId = "980110006379";
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/receive/v2_2/local"">     <soapenv:Header>        <v2:RequestSOAPHeader>           <v2:spId>{3}</v2:spId>           <v2:serviceId>{0}</v2:serviceId>           <v2:timeStamp>{1}</v2:timeStamp>        </v2:RequestSOAPHeader>     </soapenv:Header>     <soapenv:Body>        <loc:getReceivedSms>            <loc:registrationIdentifier>{2}</loc:registrationIdentifier>        </loc:getReceivedSms>     </soapenv:Body>  </soapenv:Envelope> "
, serviceId, timeStamp, shortCode, spId);
            try
            {
                string url = "http://92.42.55.180:8310/ReceiveSmsService/services/ReceiveSms/getReceivedSmsRequest";
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
            catch (Exception e)
            {
                logs.Error("Exception in getReceivedSms: " + e);
                result = "Exception in getReceivedSms:" + e.Message;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChargeTest(string mobileNumber, int price, bool isRefund = false, bool isInAppPurchase = false)
        {
            string result = "";
            try
            {
                using (var entity = new IrancellTestLibrary.Models.IrancellTestEntities())
                {
                    //var singleCharge = new IrancellTestLibrary.Models.Singlecharge();
                    //MessageObject message = new MessageObject();
                    //message.MobileNumber = mobileNumber;
                    //message.Price = price;
                    //singleCharge = await SharedLibrary.MessageSender.ChargeMtnSubscriber(entity, singleCharge, message, isRefund, isInAppPurchase, );
                    
                    //result = "isSuccessed: " + singleCharge.IsSucceeded + Environment.NewLine;
                    //result += "Description: " + singleCharge.Description + Environment.NewLine;
                    //result += "ReferenceId: " + singleCharge.ReferenceId + Environment.NewLine; 
                }
            }
            catch (Exception e)
            {
                result = "Exception in chargeTest:" + e.Message;
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/html");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SyncOrderRelationRequest()
        {
            var syncData = Request.Content.ReadAsStringAsync().Result;
            logs.Info(syncData);
            string result = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/data/sync/v1_0/local"">     <soapenv:Header/>     <soapenv:Body>        <loc:syncOrderRelationResponse>           <loc:result>0</loc:result>           <loc:resultDescription>OK</loc:resultDescription>                 </loc:syncOrderRelationResponse>     </soapenv:Body>  </soapenv:Envelope>";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
