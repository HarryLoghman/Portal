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
    public class HubController : ApiController
    {
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message(string text, string from, string to, string smsId, string userId)
        {

            var messageObj = new MessageObject();
            messageObj.MobileNumber = from;
            messageObj.ShortCode = to;
            messageObj.Content = text;
            messageObj.MessageId = smsId;

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
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
