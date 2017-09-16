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
using System.Net.Http.Formatting;

namespace Portal.Controllers
{
    public class MobinOneController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Dictionary<string, string> mobinOneMciServiceIds = new Dictionary<string, string>()
            {
                { "5530", "Danestaneh" },
            };

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message(string sender, string scode, string text)
        {
            string result = "";
            var messageObj = new MessageObject();
            messageObj.MobileNumber = sender;
            messageObj.ShortCode = scode;
            messageObj.Content = text;

            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "1";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string requestId, string receiver, string status)
        {
            var delivery = new DeliveryObject();
            delivery.ReferenceId = requestId;
            delivery.Status = status;
            delivery.AggregatorId = 8;
            SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;

        }

        //public HttpResponseMessage Notification(string sid, string msisdn, string datetime, string shortcode, string keyword, string chargecode, string basepricepoint, string billedpricepoint, string eventtype, string validity, string nextrenewaldate, string status)
        public HttpResponseMessage Notification()
        {
            var result = "1";
            ServiceInfo serviceInfo = null;
            Service service = null;
            var message = new MessageObject();
            var queryString = this.Request.GetQueryNameValuePairs();
            var sid = queryString.FirstOrDefault(o => o.Key == "sid").Value;
            var msisdn = queryString.FirstOrDefault(o => o.Key == "msisdn").Value;
            var keyword = queryString.FirstOrDefault(o => o.Key == "keyword").Value;
            var eventType = queryString.FirstOrDefault(o => o.Key == "event-type").Value;

            if (mobinOneMciServiceIds.ContainsKey(sid))
            {
                service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(mobinOneMciServiceIds[sid]);
                serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
            }

            if (eventType == "1.2")
            {
                if (serviceInfo == null)
                    result = "-999";
                else
                {
                    message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                    if (message.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        var recievedMessage = new MessageObject();
                        recievedMessage.Content = keyword;
                        recievedMessage.MobileNumber = message.MobileNumber;
                        recievedMessage.ShortCode = serviceInfo.ShortCode;
                        recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifyUnsubscription" : null;
                        recievedMessage.IsReceivedFromIntegratedPanel = false;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                        result = "1";
                    }
                }
            }
            else if (eventType == "1.1")
            {
                if (serviceInfo == null)
                    result = "-999";
                else
                {
                    message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                    if (message.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        var recievedMessage = new MessageObject();
                        recievedMessage.Content = keyword;
                        recievedMessage.ShortCode = serviceInfo.ShortCode;
                        recievedMessage.MobileNumber = message.MobileNumber;
                        recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifySubscription" : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                        result = "1";
                    }
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
