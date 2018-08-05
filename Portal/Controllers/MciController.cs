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
using System.Xml;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class MciController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Mo()
        {
            string recievedPayload = await Request.Content.ReadAsStringAsync();
            var messageObj = new SharedLibrary.Models.MessageObject();
            XmlDocument xml = new XmlDocument();
            logs.Info(recievedPayload);
            xml.LoadXml(recievedPayload);
            XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
            manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            manager.AddNamespace("loc", "http://www.csapi.org/schema/parlayx/sms/send/v4_0/local");

            XmlNode mobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:addresses", manager);
            XmlNode shortcodeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:senderName", manager);
            XmlNode contentNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:message", manager);
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Trim());
            messageObj.ShortCode = shortcodeNode.InnerText.Substring(2).Trim();
            messageObj.Content = contentNode.InnerText;
            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);

            var result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage BatchMo()
        {

            XmlDocument xml = new XmlDocument();
            //xml.LoadXml(message);
            XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
            manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            manager.AddNamespace("loc", "http://www.csapi.org/schema/parlayx/sms/send/v4_0/local");

            XmlNodeList messagesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/loc:sendSmsCollection", manager);

            var ip = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            foreach (XmlNode item in messagesList)
            {
                XmlNode mobileNumberNode = item.SelectSingleNode("/loc:SendSms/loc:addresses", manager); ;
                XmlNode shortcodeNode = item.SelectSingleNode("/loc:SendSms/loc:senderName", manager); ;
                XmlNode contentNode = item.SelectSingleNode("/loc:SendSms/loc:message", manager); ;
                var messageObj = new SharedLibrary.Models.MessageObject();
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Trim());
                messageObj.ShortCode = shortcodeNode.InnerText.Substring(2).Trim();
                messageObj.Content = contentNode.InnerText;
                messageObj.ReceivedFrom = ip;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
            }

            var result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string message)
        {
            //XmlDocument xml = new XmlDocument();
            //xml.LoadXml(message);
            //XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
            //manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            //manager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            //manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            //manager.AddNamespace("ns", "http://www.csapi.org/schema/parlayx/sms/notification/v2_1/local");
            //manager.AddNamespace("n", "");

            //XmlNode correlatorNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/notifySmsDeliveryReceipt:ns/correlator", manager);
            //XmlNode mobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/notifySmsDeliveryReceipt:ns/deliveryStatus/address:n", manager);
            //XmlNode statusNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/notifySmsDeliveryReceipt:ns/deliveryStatus/deliveryStatus:n", manager);
            //var correlator = correlatorNode.InnerText.Trim();
            //var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Substring(4).Trim());
            //var status = statusNode.InnerText;

            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage BatchDelivery(string message)
        {
            //XmlDocument xml = new XmlDocument();
            //xml.LoadXml(message);
            //XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
            //manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            //manager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            //manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            //manager.AddNamespace("ns", "http://www.csapi.org/schema/parlayx/sms/notification/v2_1/local");
            //manager.AddNamespace("n", "");

            //XmlNodeList deliveryList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/notifySmsDeliveryReceipt:ns/notifySmsDeliveryReceiptCollection", manager);

            //foreach (XmlNode item in deliveryList)
            //{
            //    XmlNode correlatorNode = xml.SelectSingleNode("/NotifySmsDeliveryReceipt:ns/correlator", manager);
            //    XmlNode mobileNumberNode = xml.SelectSingleNode("/NotifySmsDeliveryReceipt/deliveryStatus/address:n", manager);
            //    XmlNode statusNode = xml.SelectSingleNode("/NotifySmsDeliveryReceipt/deliveryStatus/deliveryStatus:n", manager);
            //    var correlator = correlatorNode.InnerText.Trim();
            //    var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Substring(4).Trim());
            //    var status = statusNode.InnerText;
            //}

            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Notify()
        {
            string result = "";
            string recievedPayload = await Request.Content.ReadAsStringAsync();
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(recievedPayload);
                XmlNodeList nodes = xml.SelectNodes("/notifications/notification");
                foreach (XmlNode node in nodes)
                {
                    var msisdn = node.SelectSingleNode("msisdn").InnerText;
                    var sid = node.SelectSingleNode("sid").InnerText;
                    var shortcode = node.SelectSingleNode("shortcode").InnerText;
                    var keyword = node.SelectSingleNode("keyword").InnerText;
                    var event_type = node.SelectSingleNode("event-type").InnerText;
                    if (event_type == "1.1" || event_type == "1.2")
                    {
                        var messageObj = new SharedLibrary.Models.MessageObject();
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                        if (shortcode == null || shortcode == "null")
                        {
                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetShortCodeFromOperatorServiceId(sid);
                        }
                        else
                            messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(shortcode);
                        messageObj.Content = keyword;
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : "";
                        if (event_type == "1.1")
                            messageObj.ReceivedFrom += "-IMI-Notify-Register";
                        else
                            messageObj.ReceivedFrom += "-IMI-Notify-Unsubscription";
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                    }
                }
                result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            }
            catch (Exception e)
            {
                logs.Error("Exception in stopSmsNotificationRequest: " + e);
                result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, "Error");
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
