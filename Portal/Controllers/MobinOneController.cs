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

namespace Portal.Controllers
{
    public class MobinOneController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        //[HttpGet]
        //[AllowAnonymous]
        //public HttpResponseMessage Notification(string sid, string msisdn, string datetime, string shortcode, string keyword, string chargecode, string basepricepoint,string billedpricepoint, string eventtype, string validity, string nextrenewaldate, string status)
        //{
        //    var result = "1";
        //    ServiceInfo serviceInfo = null;
        //    Service service = null;
        //    bool IsNotified = true;
        //    var message = new MessageObject();

        //    if (pardisImiMciServiceIds.ContainsKey(integratedPanelObj.ServiceID))
        //    {
        //        service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(pardisImiMciServiceIds[integratedPanelObj.ServiceID]);
        //        serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
        //    }
        //    else
        //    {
        //        IsNotified = false;
        //        serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(integratedPanelObj.ServiceID);
        //        service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
        //    }
        //    if (integratedPanelObj.EventID == "1.2" /* && integratedPanelObj.NewStatus == 5*/)
        //    {
        //        if (serviceInfo == null)
        //            result = "-999";
        //        else
        //        {
        //            integratedPanelObj.Address = SharedLibrary.MessageHandler.ValidateNumber(integratedPanelObj.Address);
        //            if (integratedPanelObj.Address == "Invalid Mobile Number")
        //                result = "-1";
        //            else
        //            {
        //                var recievedMessage = new MessageObject();
        //                if (IsNotified == true)
        //                {
        //                    recievedMessage.Content = "off notify";
        //                    recievedMessage.MobileNumber = integratedPanelObj.Address;
        //                    recievedMessage.ShortCode = serviceInfo.ShortCode;
        //                    recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifyUnsubscription" : null;
        //                    recievedMessage.IsReceivedFromIntegratedPanel = false;
        //                }
        //                else
        //                {
        //                    recievedMessage.Content = integratedPanelObj.ServiceID;
        //                    recievedMessage.MobileNumber = integratedPanelObj.Address;
        //                    recievedMessage.ShortCode = serviceInfo.ShortCode;
        //                    recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
        //                    recievedMessage.IsReceivedFromIntegratedPanel = true;
        //                }
        //                SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
        //                result = "1";
        //            }
        //        }
        //    }
        //    else if (integratedPanelObj.EventID == "1.1")
        //    {
        //        if (serviceInfo == null)
        //            result = "-999";
        //        else
        //        {
        //            integratedPanelObj.Address = SharedLibrary.MessageHandler.ValidateNumber(integratedPanelObj.Address);

        //            if (integratedPanelObj.Address == "Invalid Mobile Number")
        //                result = "-1";
        //            else
        //            {
        //                var recievedMessage = new MessageObject();
        //                recievedMessage.Content = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(service.OnKeywords);
        //                recievedMessage.ShortCode = serviceInfo.ShortCode;
        //                recievedMessage.MobileNumber = integratedPanelObj.Address;
        //                recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifySubscription" : null;
        //                SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
        //                result = "1";
        //            }
        //        }
        //    }
        //    var response = new HttpResponseMessage(HttpStatusCode.OK);
        //    response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
        //    return response;
        //}
    }
}
