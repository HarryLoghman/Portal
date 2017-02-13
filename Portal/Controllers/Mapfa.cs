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
    public class Mapfa : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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