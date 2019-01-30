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
                    ,new Dictionary<string, string>() { { "shortcode", messageObj.To}
                    ,{ "content", messageObj.Text}
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
        public HttpResponseMessage Delivery(string id, string subscriber, string shortcode, string part, string Status)
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, 
                    new Dictionary<string, string>() { { "shortcode", shortcode}
                    ,{ "mobile", subscriber}}
                    , null, "Portal:MapfaController:Delivery");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                }
            }
            catch (Exception e)
            {
                logs.Error("Portal:MapfaController:Delivery", e);
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