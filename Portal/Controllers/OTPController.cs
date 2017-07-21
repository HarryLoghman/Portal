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
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class OTPController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpCharge(string mobileNumber, long serviceId, int price)
        {
            string result = "";
            var messageObj = new MessageObject();
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "Invalid Mobile Number";
            else
            {
                try
                {
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceId);
                    if (service == null)
                        result = "Invalid serviceId";
                    else
                    {
                        messageObj.Price = price;
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                        if (service.ServiceCode == "Soltan")
                        {
                            using(var entity = new SoltanLibrary.Models.SoltanEntities())
                            {
                                var singleCharge = new SoltanLibrary.Models.Singlecharge();
                                string aggregatorName = "Telepromo";
                                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soltan", aggregatorName);
                                singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                result = singleCharge.Description;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Excepiton in OtpCharge method: ", e);
                }
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
