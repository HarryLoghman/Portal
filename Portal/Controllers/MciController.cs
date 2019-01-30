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
            var result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:Mo");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    //result = tpsRatePassed;
                    result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, tpsRatePassed);
                    resultOk = false;
                }
                else
                {
                    string recievedPayload = await Request.Content.ReadAsStringAsync();
                    var messageObj = new SharedLibrary.Models.MessageObject();
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(recievedPayload);
                    XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                    manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                    manager.AddNamespace("loc", "http://www.csapi.org/schema/parlayx/sms/send/v4_0/local");
                    if (recievedPayload.Contains("sendSmsCollection"))
                    {
                        XmlNodeList messagesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:sendSmsCollection/loc:SendSms", manager);
                        foreach (XmlNode item in messagesList)
                        {
                            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item["loc:addresses"].InnerText.Trim());
                            messageObj.ShortCode = item["loc:senderName"].InnerText.Substring(2).Trim();
                            messageObj.Content = HttpUtility.UrlDecode(item["loc:message"].InnerText);
                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                            logs.Info("MCI Controller MO:" + messageObj.MobileNumber + "," + messageObj.ShortCode + "," + messageObj.Content + "," + messageObj.ReceivedFrom);
                        }
                    }
                    else
                    {
                        XmlNode mobileNumberNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:addresses", manager);
                        XmlNode shortcodeNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:senderName", manager);
                        XmlNode contentNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/loc:sendSms/loc:message", manager);
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Trim());
                        messageObj.ShortCode = shortcodeNode.InnerText.Substring(2).Trim();
                        messageObj.Content = HttpUtility.UrlDecode(contentNode.InnerText);
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        logs.Info("MCI Controller MO:" + messageObj.MobileNumber + "," + messageObj.ShortCode + "," + messageObj.Content + "," + messageObj.ReceivedFrom);
                    }
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in Mo: ", e);
                //result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, e.Message);
                logs.Error("Portal:MciController:MO", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
                result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, result);

            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage BatchMo()
        {
            var result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:BatchMo");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    //result = tpsRatePassed;
                    result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, tpsRatePassed);
                    resultOk = false;
                }
                else
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
                        messageObj.Content = HttpUtility.UrlDecode(contentNode.InnerText);
                        messageObj.ReceivedFrom = ip;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        logs.Info("MCI Controller BatchMo:" + messageObj.MobileNumber + "," + messageObj.ShortCode + "," + messageObj.Content + "," + messageObj.ReceivedFrom);
                    }
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in BatchMo: ", e);
                //result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, e.Message);
                logs.Error("Portal:MciController:BatchMo", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
                result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, result);
            }
            //var result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string message)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:Delivery1Param");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
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
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in Delivery: ", e);
                logs.Error("Portal:MciController:Delivery1Param", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Delivery()
        {
            var result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:Delivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    string message = Request.Content.ReadAsStringAsync().Result;
                    logs.Info("MCI Controller Delivery:" + message);
                    this.sb_deliveryProcess(message);
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in Delivery : ", e);
                //result = e.Message;
                logs.Error("Portal:MciController:Message", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }


        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage BatchDelivery(string message)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:BatchDelivery1Param");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
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
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in BatchDelivery: ", e);
                logs.Error("Portal:MciController:BatchDelivery1Param", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }


        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage BatchDelivery()
        {
            var result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:BatchDelivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    string message = Request.Content.ReadAsStringAsync().Result;
                    logs.Info("MCI Controller BatchDelivery:" + message);
                    this.sb_deliveryProcess(message);
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in BatchDelivery: ", e);
                //result = e.Message;
                logs.Error("Portal:MciController:BatchDelivery", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (result != "")
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        private void sb_deliveryProcess(string soapMessage)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(soapMessage);
                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                manager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns", "http://www.csapi.org/schema/parlayx/sms/notification/v2_1/local");

                XmlNodeList correlatorNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:notifySmsDeliveryReceiptCollection/ns:NotifySmsDeliveryReceipt/ns:correlator", manager);
                XmlNodeList mobileNumberNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:notifySmsDeliveryReceiptCollection/ns:NotifySmsDeliveryReceipt/ns:deliveryStatus/address", manager);
                XmlNodeList statusNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:notifySmsDeliveryReceiptCollection/ns:NotifySmsDeliveryReceipt/ns:deliveryStatus/deliveryStatus", manager);

                if (correlatorNodesList == null || correlatorNodesList.Count == 0)
                {
                    correlatorNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:correlator", manager);
                    mobileNumberNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:deliveryStatus/address", manager);
                    statusNodesList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns:notifySmsDeliveryReceipt/ns:deliveryStatus/deliveryStatus", manager);
                }
                XmlNode correlatorNode, mobileNumberNode, statusNode;
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {

                    for (int i = 0; i <= correlatorNodesList.Count - 1; i++)
                    {
                        correlatorNode = correlatorNodesList[i];
                        mobileNumberNode = mobileNumberNodesList[i];
                        statusNode = statusNodesList[i];

                        var correlator = correlatorNode.InnerText.Trim();
                        var mobilenumber = mobileNumberNode.InnerText.Trim();
                        var status = statusNode.InnerText.Trim();
                        string shortCode;

                        SharedLibrary.MessageSender.sb_processCorrelator(correlator, ref mobilenumber, out shortCode);

                        var delivery = new SharedLibrary.Models.Delivery();
                        delivery.AggregatorId = 12;
                        delivery.Correlator = correlator;
                        if (status == "DeliveredToTerminal")
                            delivery.Delivered = true;
                        else delivery.Delivered = false;

                        delivery.DeliveryTime = DateTime.Now;
                        delivery.Description = status;
                        delivery.IsProcessed = false;
                        delivery.MobileNumber = mobilenumber;
                        delivery.ReferenceId = null;
                        delivery.ShortCode = shortCode;
                        delivery.Status = status;
                        portal.Deliveries.Add(delivery);
                        if (i % 500 == 0) portal.SaveChanges();
                    }

                    portal.SaveChanges();
                }

            }
            catch (Exception e)
            {
                logs.Error("MCI Controller Exception in sb_deliveryProcess: ", e);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Notify()
        {
            string result = "";
            bool resultOk = true;
            string recievedPayload = await Request.Content.ReadAsStringAsync();
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:MciController:Notify");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, tpsRatePassed);
                    resultOk = false;
                }
                else
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
                        logs.Info("MCI Controller Notify:" + msisdn + "," + sid + "," + shortcode + "," + keyword + "," + event_type);
                    }
                    result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 200, "Success");
                }
            }
            catch (Exception e)
            {
                //logs.Error("MCI Controller Exception in Notify: " + e);
                //result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, "Error");
                logs.Error("Portal:MciController:Notify", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
                result = string.Format(@"<response>    <status>{0}</status>  <description>{1}</description > </response>", 400, result);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
