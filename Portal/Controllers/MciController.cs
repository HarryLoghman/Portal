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

namespace Portal.Controllers
{
    public class MciController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Mo(string message)
        {
            var messageObj = new SharedLibrary.Models.MessageObject();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(message);
            XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
            manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            manager.AddNamespace("loc", "http://www.csapi.org/schema/parlayx/sms/send/v4_0/local");

            XmlNode mobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:addresses", manager);
            XmlNode shortcodeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:senderName", manager);
            XmlNode contentNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:message", manager);
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Trim());
            messageObj.ShortCode = shortcodeNode.InnerText.Substring(2).Trim();
            messageObj.Content = contentNode.InnerText;
            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);

            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        public HttpResponseMessage BatchMo(string message)
        {

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(message);
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

            var result = "";
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
    }
}
