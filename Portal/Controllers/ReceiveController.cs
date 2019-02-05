using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System;
using System.Collections.Generic;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Dictionary<string, string> pardisImiMciServiceIds = new Dictionary<string, string>()
            {
                { "5530", "Danestaneh" },
                { "6409", "Mobiligia" },
                { "6411", "MashinBazha" },
                { "5328", "MyLeague" },
                { "6523", "BimeKarbala" },
                { "6516", "Tirandazi" },
                { "6560", "Boating" },
                { "6489", "SepidRood" },
            };

        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message([FromUri]MessageObject messageObj)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode", messageObj.To}
                    ,{ "content", messageObj.Content}
                    ,{ "mobile", (!string.IsNullOrEmpty(messageObj.Address) ? messageObj.Address : messageObj.From)}}
                    , null, "Portal:ReceiveController:Message");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (messageObj.Address != null)
                    {
                        messageObj.MobileNumber = messageObj.Address;
                        messageObj.Content = messageObj.Message;
                    }
                    else if (messageObj.From != null)
                    {
                        messageObj.MobileNumber = messageObj.From;
                        messageObj.ShortCode = messageObj.To;
                    }
                    logs.Info("ReceiveController : " + messageObj.MobileNumber);
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
                    if (messageObj.Content == null || messageObj.Content == "")
                    {
                        if (HttpContext.Current.Request.Headers["action"] != null)
                        {
                            if (HttpContext.Current.Request.Headers["action"] == "subscribe")
                                messageObj.Content = "1-header";
                            else if (HttpContext.Current.Request.Headers["action"] == "unsubscribe")
                                messageObj.Content = "off";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:Message", e);
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
        public HttpResponseMessage WebMessage([FromUri]MessageObject messageObj, int subscribeUser)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode", messageObj.To}
                    ,{ "mobile", (string.IsNullOrEmpty(messageObj.Address) ? messageObj.From : messageObj.Address)}}
                    , null
                    , "Portal:ReceiveController:WebMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (subscribeUser != 1)
                        messageObj.Content = "SendServiceHelp";

                    if (messageObj.Address != null)
                    {
                        messageObj.MobileNumber = messageObj.Address;
                        messageObj.Content = messageObj.Message;
                    }
                    else if (messageObj.From != null)
                    {
                        messageObj.MobileNumber = messageObj.From;
                        messageObj.ShortCode = messageObj.To;
                    }
                    logs.Info("ReceiveController WebMessage : " + messageObj.MobileNumber);
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);

                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                        messageObj.ReceivedFromSource = 1;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result = "1";
                    }
                }
            }
            catch (Exception e)
            {
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
                resultOk = false;
                logs.Error("Portal:ReceiveController:WebMessage", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        // /Receive/TelepromoMessage?da=989125612694&oa=2050&txt=hi
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage TelepromoMessage(string da, string oa, string txt)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode", oa}
                        ,{ "mobile", da}}
                    , null, "Portal:ReceiveController:TelepromoMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (da == "989168623674" || da == "989195411097")
                    {
                        var blackListResponse = new HttpResponseMessage(HttpStatusCode.OK);
                        blackListResponse.Content = new StringContent("", System.Text.Encoding.UTF8, "text/plain");
                        return blackListResponse;
                    }
                    var messageObj = new MessageObject();
                    messageObj.MobileNumber = da;
                    messageObj.ShortCode = oa;
                    messageObj.Content = txt;

                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    logs.Info("ReceiveController TelepromoMessage : " + messageObj.MobileNumber);

                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result = "";
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:TelepromoMessage", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        // /Receive/ReceiveMessage?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpPost]
        [AllowAnonymous]
        public string ReceiveMessage([FromBody]MessageObject messageObj)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                     new Dictionary<string, string>() { { "shortcode", messageObj.To}
                    ,{ "mobile", (string.IsNullOrEmpty(messageObj.Address) ? messageObj.From : messageObj.Address)}}
                    , null, "Portal:ReceiveController:ReceiveMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (messageObj.Address != null)
                    {
                        messageObj.MobileNumber = messageObj.Address;
                        messageObj.Content = messageObj.Message;
                    }
                    else if (messageObj.From != null)
                    {
                        messageObj.MobileNumber = messageObj.From;
                        messageObj.ShortCode = messageObj.To;
                    }
                    logs.Info("ReceiveController  ReceiveMessage : " + messageObj.MobileNumber);
                    messageObj.ShortCode = "307229";
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        return "-1";
                    messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                    SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:ReceiveMessage", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            return result;
        }

        // /Receive/Delivery?PardisId=44353535&Status=DeliveredToNetwork&ErrorMessage=error
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery([FromUri]DeliveryObject delivery)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:ReceiveController:Delivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    delivery.AggregatorId = 2;
                    SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
                    result = "1";
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:Delivery", e);
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

        // /Receive/TelepromoDelivery?refId=44353535&deliveryStatus=0
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage TelepromoDelivery(string refId, string deliveryStatus)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "Portal:ReceiveController:TelepromoDelivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var delivery = new DeliveryObject();
                    delivery.ReferenceId = refId;
                    delivery.Status = deliveryStatus;
                    delivery.AggregatorId = 5;
                    SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:TelepromoDelivery", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            //var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;

        }

        // /Receive/PardisIntegratedPanel?Address=09125612694&ServiceID=1245&EventId=error
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage PardisIntegratedPanel([FromUri]IntegratedPanel integratedPanelObj)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "serviceid", integratedPanelObj.ServiceID}
                        ,{ "mobile", integratedPanelObj.Address}}
                    , null, "Portal:ReceiveController:PardisIntegratedPanel");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    result = "1";
                    ServiceInfo serviceInfo = null;
                    vw_servicesServicesInfo service = null;
                    bool IsNotified = true;
                    if (pardisImiMciServiceIds.ContainsKey(integratedPanelObj.ServiceID))
                    {
                        service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(pardisImiMciServiceIds[integratedPanelObj.ServiceID]);
                        serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                    }
                    else
                    {
                        IsNotified = false;
                        serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(integratedPanelObj.ServiceID);
                        service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                    }
                    if (integratedPanelObj.EventID == "1.2" /* && integratedPanelObj.NewStatus == 5*/)
                    {
                        if (serviceInfo == null)
                            result = "-999";
                        else
                        {
                            integratedPanelObj.Address = SharedLibrary.MessageHandler.ValidateNumber(integratedPanelObj.Address);
                            if (integratedPanelObj.Address == "Invalid Mobile Number")
                                result = "-1";
                            else
                            {
                                var recievedMessage = new MessageObject();
                                if (IsNotified == true)
                                {
                                    recievedMessage.Content = "off notify";
                                    recievedMessage.MobileNumber = integratedPanelObj.Address;
                                    recievedMessage.ShortCode = serviceInfo.ShortCode;
                                    recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifyUnsubscription" : null;
                                    recievedMessage.IsReceivedFromIntegratedPanel = false;
                                }
                                else
                                {
                                    recievedMessage.Content = integratedPanelObj.ServiceID;
                                    recievedMessage.MobileNumber = integratedPanelObj.Address;
                                    recievedMessage.ShortCode = serviceInfo.ShortCode;
                                    recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                                    recievedMessage.IsReceivedFromIntegratedPanel = true;
                                }
                                logs.Info("ReceiveController PardisIntegratedPanel1 : " + recievedMessage.MobileNumber);
                                SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                                result = "1";
                            }
                        }
                    }
                    else if (integratedPanelObj.EventID == "1.1")
                    {
                        if (serviceInfo == null)
                            result = "-999";
                        else
                        {
                            integratedPanelObj.Address = SharedLibrary.MessageHandler.ValidateNumber(integratedPanelObj.Address);

                            if (integratedPanelObj.Address == "Invalid Mobile Number")
                                result = "-1";
                            else
                            {
                                var recievedMessage = new MessageObject();
                                recievedMessage.Content = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(service.OnKeywords);
                                recievedMessage.ShortCode = serviceInfo.ShortCode;
                                recievedMessage.MobileNumber = integratedPanelObj.Address;
                                recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-NotifySubscription" : null;
                                SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                                result = "1";
                                logs.Info("ReceiveController PardisIntegratedPanel2 : " + recievedMessage.MobileNumber);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:ReceiveController:PardisIntegratedPanel", e);
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
    }
}
