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
    public class MapfaController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                    ,{ "content", (string.IsNullOrEmpty(messageObj.Text)? "" : messageObj.Text.Replace(":","-"))}
                    ,{ "mobile", messageObj.From}}
                    , null, "Portal:MapfaController:Message");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    messageObj.MobileNumber = messageObj.From;
                    messageObj.ShortCode = messageObj.To;
                    messageObj.Content = messageObj.Text;
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    string recievedPayload = Request.Content.ReadAsStringAsync().Result;
                    logs.Info("MapfaController PardisMessagePayload:" + recievedPayload);
                    logs.Info("MapfaController pardisMessage:MobileNumber:" + messageObj.From + ",ShortCode:" + messageObj.To + ",Content:" + messageObj.Text
                        + ",ReceivedFrom:" + messageObj.ReceivedFrom);

                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else if (messageObj.MobileNumber == "09105246145" || messageObj.MobileNumber == "09174565469")
                        result = "-1";
                    else
                    {
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;

                        string headers = String.Empty;

                        foreach (var key in HttpContext.Current.Request.Headers.AllKeys)
                            headers += key + "=" + HttpContext.Current.Request.Headers[key] + Environment.NewLine;

                        logs.Info("MapfaController:Message:Headers MobileNumber:" + messageObj.From + ", ShortCode: " + messageObj.To + ", Content: " + messageObj.Text
                        + ",ReceivedFrom:" + messageObj.ReceivedFrom + Environment.NewLine + headers);
                        if (HttpContext.Current.Request.Headers["action"] != null)
                        {

                            if (HttpContext.Current.Request.Headers["action"] == "subscribe")
                                messageObj.ReceivedFrom += "-FromIMI-Register";
                            else if (HttpContext.Current.Request.Headers["action"] == "unsubscribe")
                                messageObj.ReceivedFrom += "-FromIMI-Unsubscribe";

                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromOperatorServiceId(HttpContext.Current.Request.Headers["serviceId"]).ShortCode;

                            if (HttpContext.Current.Request.Headers["action"] == "unsubscribe")
                                messageObj.Content = HttpContext.Current.Request.Headers["actor"];
                            else
                                messageObj.Content = messageObj.Text;
                        }
                        else
                        {
                            logs.Info("MapfaController:message:action is null");
                        }

                        logs.Info(messageObj.ShortCode);
                        messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);

                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result = "1";
                    }
                }
            }
            catch (Exception e)
            {
                //result = e.Message;
                //logs.Error("Portal:MapfaController:Message:"+e.Message);
                logs.Error("Portal:MapfaController:Message", e);
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
        public HttpResponseMessage Delivery(string requestId, string receiver, string status)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                    new Dictionary<string, string>() { { "requestId", requestId}
                    ,{ "mobile", receiver}
                    ,{ "status", status}}
                    , null, "Portal:MapfaController:Delivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(requestId))
                    {
                        result = "requestId is not specified";
                        resultOk = false;
                        goto endSection;
                    }
                    if (string.IsNullOrEmpty(receiver))
                    {
                        result = "receiver is not specified";
                        resultOk = false;
                        goto endSection;
                    }
                    string shortCode;
                    SharedLibrary.MessageSender.sb_processCorrelator(requestId, ref receiver, out shortCode);
                    var MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(receiver);

                    long aggregatorId;
                    using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                    {
                        var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.ShortCode == shortCode);
                        if (entryService == null || !entryService.AggregatorId.HasValue)
                        {
                            result = "Unknown ServiceCode or Aggregator";
                            resultOk = false;
                            goto endSection;
                        }
                        aggregatorId = entryService.AggregatorId.Value;
                    }
                    if (MobileNumber == "Invalid Mobile Number")
                    {
                        result = "-1";
                        resultOk = false;
                        goto endSection;
                    }
                    else
                    {
                        result = "";
                    }


                    var delivery = new SharedLibrary.Models.Delivery();
                    delivery.AggregatorId = aggregatorId;
                    delivery.Correlator = requestId;
                    if (status.ToLower() == "DeliveredToTerminal".ToLower())
                        delivery.Delivered = true;
                    else delivery.Delivered = false;

                    delivery.DeliveryTime = DateTime.Now;
                    delivery.Description = status;
                    delivery.IsProcessed = false;
                    delivery.MobileNumber = MobileNumber;
                    delivery.ReferenceId = null;
                    delivery.ShortCode = shortCode;
                    delivery.Status = status;

                    using (var portal = new SharedLibrary.Models.PortalEntities())
                    {
                        portal.Deliveries.Add(delivery);
                        portal.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MapfaController:Delivery", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}