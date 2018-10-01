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
using System.Data.Entity;
using System.Threading.Tasks;
using SharedLibrary;
using System.Data.SqlClient;

namespace Portal.Controllers
{
    public class PingController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage TelepromoImiMT()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Tamly500", "Telepromo");
                var telepromoIp = SharedLibrary.MessageSender.telepromoIp;
                var url = telepromoIp + "/samsson-sdp/transfer/send?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                Random rnd = new Random();
                var message = new SharedLibrary.Models.MessageObject();
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                var to = "98" + message.MobileNumber.TrimStart('0');
                var messageContent = message.Content;

                var messageId = rnd.Next(1000000, 9999999).ToString();
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId);
                if (message.Price > 0)
                    urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    using (var response = client.GetAsync(new Uri(url)).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            result.status = "success";
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            result.status = "http status code: " + response.StatusCode;
                        }
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }

            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping TelepromoImiMT:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage TelepromoImiOtp()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Tamly500", "Telepromo");
                var telepromoIp = SharedLibrary.MessageSender.telepromoIp;
                var url = telepromoIp + "/samsson-sdp/pin/generate?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                Random rnd = new Random();
                var message = new SharedLibrary.Models.MessageObject();
                message.MobileNumber = "09900000000";
                var to = "98" + message.MobileNumber.TrimStart('0');
                var messageContent = "InAppPurchase";
                var contentId = rnd.Next(00001, 99999).ToString();
                var messageId = rnd.Next(1000000, 9999999).ToString();
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}&contentId={8}&chargingCode={9}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId, contentId, message.ImiChargeKey);
                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    using (var response = client.GetAsync(new Uri(url)).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            result.status = "success";
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            result.status = "http status code: " + response.StatusCode;
                        }
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }

            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping TelepromoImiOtp:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MciDirectImiMT()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soraty", "MciDirect");
                var mciIp = SharedLibrary.MessageSender.mciIp;
                var url = mciIp + "/parlayxsmsgw/services/SendSmsService";
                var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                var message = new SharedLibrary.Models.MessageObject();
                message.Price = 0;
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v4_0/local"">
                           <soapenv:Header/>
                           <soapenv:Body>
                              <loc:sendSms>
                        <loc:addresses>{0}</loc:addresses>
                                 <loc:senderName>{1}</loc:senderName>", mobileNumber, shortcode);
                if (message.Price != 0)
                {
                    payload += string.Format(@"
                            <loc:charging>
                                <description></description>
                                <currency>RLS</currency>
                                <amount>{0}</amount>
                                <code>{1}</code>
                             </loc:charging>", message.Price + "0", message.ImiChargeKey);
                }
                payload += string.Format(@"
                        <loc:message> {0} </loc:message>
                            <loc:receiptRequest>
                                         <endpoint>http://10.20.96.65:8090/api/Mci/Delivery</endpoint>
                                    <interfaceName>SMS</interfaceName>
                                    <correlator>{1}</correlator>
                                    </loc:receiptRequest>
                                  </loc:sendSms>
                                </soapenv:Body>
                              </soapenv:Envelope>", message.Content, serviceId);

                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    client.DefaultRequestHeaders.Add("serviceKey", aggregatorServiceId);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            result.status = "success";
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            result.status = "http status code: " + response.StatusCode;
                        }
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }

            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping MciDirectImiMT:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MciDirectImiOtp()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soraty", "MciDirect");
                var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                var mciIp = SharedLibrary.MessageSender.mciIp;
                var url = mciIp + "/apigw/charging/pushotp";
                var message = new SharedLibrary.Models.MessageObject();
                message.Price = 0;
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                using (var entity = new SoratyLibrary.Models.SoratyEntities())
                {
                    var imiChargeCode = new SoratyLibrary.Models.ImiChargeCode();
                    message = SoratyLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, message.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var rnd = new Random();
                var refrenceCode = rnd.Next(100000000, 999999999).ToString();
                var jsonPayload = string.Format(@"{{
                            ""accesInfo"": {{
                                ""servicekey"": ""{0}"",
                                ""msisdn"": ""{1}"",
                                ""serviceName"": ""{5}"",
                                ""referenceCode"": ""{2}"",
                                ""shortCode"": ""{3}"",
                                ""contentId"":""1""
                            }} ,
                        ""charge"": {{
                                ""code"": ""{4}"",
                                ""amount"":0,
                                ""description"": ""otp""
                            }}
                    }}", aggregatorServiceId, mobileNumber, refrenceCode, shortcode, message.ImiChargeKey, serviceAdditionalInfo["serviceName"]);
                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var resultHttp = await client.PostAsync(url, content);
                    var responseString = await resultHttp.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.statusInfo.statusCode.ToString() == "200")
                    {
                        result.status = "success";
                        string httpResult = jsonResponse.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        result.status = "http status code: " + jsonResponse.StatusCode;
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }

            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping MciDirectImiOtp:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MobinOneImiMT()
        {
            dynamic result = new ExpandoObject();
            var now = DateTime.Now;
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Nebula", "MobinOne");
                Random rnd = new Random();

                var smsList = new SharedLibrary.MobinOneServiceReference.ArrayReq();
                smsList.number = new string[1];
                smsList.message = new string[1];
                smsList.chargecode = new string[1];
                smsList.amount = new string[1];
                smsList.requestId = new string[1];
                smsList.type = "mt";
                smsList.username = serviceAdditionalInfo["username"];
                smsList.password = serviceAdditionalInfo["password"];
                smsList.shortcode = "98" + serviceAdditionalInfo["shortCode"];
                smsList.servicekey = serviceAdditionalInfo["aggregatorServiceId"];
                smsList.number[0] = "98" + "9900000000";
                smsList.message[0] = "ping";
                smsList.chargecode[0] = "";
                smsList.amount[0] = "";
                var messageId = rnd.Next(1000000, 9999999).ToString();
                smsList.requestId[0] = messageId.ToString();
                using (var mobineOneClient = new SharedLibrary.MobinOneServiceReference.tpsPortTypeClient())
                {
                    var smsResult = mobineOneClient.sendSms(smsList);
                    result.status = "success";
                }
            }
            catch (Exception e)
            {
                result.status = "success";
            }
            var after = DateTime.Now;
            var duration = (long)((after - now).TotalMilliseconds);
            result.time = duration;
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MobinOneImiOtp()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Nebula", "MobinOne");
                var shortCode = "98" + serviceAdditionalInfo["shortCode"];
                var message = new SharedLibrary.Models.MessageObject();
                message.Price = 0;
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                using (var entity = new NebulaLibrary.Models.NebulaEntities())
                {
                    var imiChargeCode = new NebulaLibrary.Models.ImiChargeCode();
                    message = NebulaLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, message.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                var mobile = "98" + message.MobileNumber.TrimStart('0');
                var stringedPrice = "";
                var rnd = new Random();
                var requestId = rnd.Next(1000000, 9999999).ToString();

                var now = DateTime.Now;
                using (var client = new SharedLibrary.MobinOneServiceReference.tpsPortTypeClient())
                {
                    var otpResult = client.inAppCharge(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], shortCode, serviceAdditionalInfo["aggregatorServiceId"], message.ImiChargeKey, mobile, stringedPrice, requestId);
                }
                var after = DateTime.Now;
                var duration = (long)((after - now).TotalMilliseconds);
                result.time = duration;
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping MobinOneImiOtp:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MobinOnePardisMT()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Phantom", "MobinOneMapfa");
                var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                var domain = "alladmin";

                string[] mobileNumbers = new string[1];
                string[] shortCodes = new string[1];
                string[] messageContents = new string[1];
                string[] aggregatorServiceIds = new string[1];
                string[] udhs = new string[1];
                string[] mclass = new string[1];
                mobileNumbers[0] = "98" + "09900000000".TrimStart('0');
                shortCodes[0] = "98" + paridsShortCodes.FirstOrDefault(o => o.Price == 0).ShortCode;
                messageContents[0] = "ping";
                aggregatorServiceIds[0] = paridsShortCodes.FirstOrDefault(o => o.Price == 0).PardisServiceId;
                udhs[0] = "";
                mclass[0] = "";
                mobileNumbers = mobileNumbers.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                shortCodes = shortCodes.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                messageContents = messageContents.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                aggregatorServiceIds = aggregatorServiceIds.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                using (var mobinonePardisClient = new SharedLibrary.MobinOneMapfaSendServiceReference.SendClient())
                {
                    var now = DateTime.Now;
                    var pardisResponse = mobinonePardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                    result.status = "success";
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in MobinOnePardisMT: " + e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MobinOnePardisOtp()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Phantom", "MobinOneMapfa");

                var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                var aggregatorServiceId = paridsShortCodes.OrderByDescending(o => o.Price).FirstOrDefault().PardisServiceId;
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                var channelType = (int)MessageHandler.MapfaChannels.SMS;
                var domain = "alladmin";
                var mobileNumber = "98" + "09900000000".TrimStart('0');
                using (var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient())
                {
                    var now = DateTime.Now;
                    var otpResult = client.sendVerificationCode(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping MobinOnePardisOtp:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> MobinOnePardisCharging()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Phantom", "MobinOneMapfa");

                var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                var paridsShortCodes = SharedLibrary.ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == 0).PardisServiceId;
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                var channelType = (int)SharedLibrary.MessageHandler.MapfaChannels.SMS;
                var domain = "alladmin";
                var mobileNumber = "98" + "09900000000".TrimStart('0');
                using (var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient())
                {
                    var chargeResult = client.singleCharge(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    var now = DateTime.Now;
                    var otpResult = client.sendVerificationCode(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result.time = duration;
                }
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping MobinOnePardisCharging:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> IrancellMT()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("TahChin", "MTN");
                var url = SharedLibrary.MessageSender.irancellIp + "/SendSmsService/services/SendSms";
                var username = serviceAdditionalInfo["username"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                using (var client = new HttpClient())
                {
                    var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
                    string payload = SharedLibrary.MessageHandler.CreateMtnSoapEnvelopeString(serviceAdditionalInfo["aggregatorServiceId"], timeStamp, "09350000000", serviceAdditionalInfo["shortCode"], "ping", serviceId);
                    var now = DateTime.Now;
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                            result.status = "success";
                        }
                        else
                            result.status = response.StatusCode.ToString();

                        var after = DateTime.Now;
                        var duration = (long)((after - now).TotalMilliseconds);
                        result.time = duration;
                    }
                }
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in IrancellMT: " + e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> IrancellCharging()
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.time = 0;
                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("TahChin", "MTN");

                var spId = "980110006379";
                var charge = "chargeAmount";
                var mobile = "98" + "09350000000".TrimStart('0');
                var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
                int rialedPrice = 0;
                var referenceCode = Guid.NewGuid().ToString();
                var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
                string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
    , timeStamp, mobile, rialedPrice, referenceCode, charge, serviceAdditionalInfo["aggregatorServiceId"], spId);

                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    client.Timeout = TimeSpan.FromSeconds(60);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                            result.status = "success";
                        }
                        else
                        {
                            result.status = response.StatusCode.ToString();
                        }
                        var after = DateTime.Now;
                        var duration = (long)((after - now).TotalMilliseconds);
                        result.time = duration;
                    }
                }
            }
            catch (Exception e)
            {
                result.status = "exception:" + e.Message;
                result.time = -1;
                logs.Error("Exception in Ping IrancellCharging:", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return httpResponse;
        }
    }
}
