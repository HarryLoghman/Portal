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
            messageObj.MobileNumber = messageObj.From;
            messageObj.ShortCode = messageObj.To;
            messageObj.Content = messageObj.Text;
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            string result = "";
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
                        messageObj.ReceivedFrom += "-FromImi-Register";
                    else if (HttpContext.Current.Request.Headers["action"] == "unsubscribe")
                        messageObj.ReceivedFrom += "-FromImi-Unsubscribe";
                    
                    messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromOperatorServiceId(HttpContext.Current.Request.Headers["serviceId"]).ShortCode;
                    messageObj.Content = HttpContext.Current.Request.Headers["actor"];
                }
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);

                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "1";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string id, string subscriber, string shortcode, string part, string Status)
        {
            string result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}