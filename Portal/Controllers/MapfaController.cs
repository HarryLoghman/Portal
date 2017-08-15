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
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            string result = "";
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
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage IntegratedPanel(string PAKServiceId, string Mobile, string Type)
        {
            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(PAKServiceId);
            Mobile = SharedLibrary.MessageHandler.ValidateNumber(Mobile);

            var recievedMessage = new MessageObject();
            recievedMessage.Content = PAKServiceId;
            recievedMessage.MobileNumber = Mobile;
            recievedMessage.ShortCode = serviceInfo.ShortCode;
            recievedMessage.IsReceivedFromIntegratedPanel = true;
            SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
            var result = "0";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}