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
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                        if (service.ServiceCode == "Soltan")
                        {
                            using (var entity = new SoltanLibrary.Models.SoltanEntities())
                            {
                                var imiChargeCode = new SoltanLibrary.Models.ImiChargeCode();
                                messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, price, 0, null);
                                if (messageObj.Price == null)
                                    result = "Invalid Price";
                                else
                                {
                                    var singleCharge = new SoltanLibrary.Models.Singlecharge();
                                    string aggregatorName = "Telepromo";
                                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soltan", aggregatorName);
                                    singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                    result = singleCharge.Description;
                                }
                            }
                        }
                        else if (service.ServiceCode == "MenchBaz")
                        {
                            using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                            {
                                var imiChargeCode = new MenchBazLibrary.Models.ImiChargeCode();
                                messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, price, 0, null);
                                if (messageObj.Price == null)
                                    result = "Invalid Price";
                                else
                                {
                                    var singleCharge = new MenchBazLibrary.Models.Singlecharge();
                                    string aggregatorName = "Telepromo";
                                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                    singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                    result = singleCharge.Description;
                                }
                            }
                        }
                        else
                            result = "Service does not defined";
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Excepiton in OtpCharge method: ", e);
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpConfirm(string mobileNumber, long serviceId, string confirmCode)
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
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                        if (service.ServiceCode == "Soltan")
                        {
                            using (var entity = new SoltanLibrary.Models.SoltanEntities())
                            {
                                var singleCharge = new SoltanLibrary.Models.Singlecharge();
                                singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                if (singleCharge == null)
                                    result = "No Otp Request Found";
                                else
                                {
                                    string aggregatorName = "Telepromo";
                                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Soltan", aggregatorName);
                                    singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, confirmCode);
                                    result = singleCharge.Description;
                                }
                            }
                        }
                        else if (service.ServiceCode == "MenchBaz")
                        {
                            using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                            {
                                var singleCharge = new MenchBazLibrary.Models.Singlecharge();
                                singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                if (singleCharge == null)
                                    result = "No Otp Request Found";
                                else
                                {
                                    string aggregatorName = "Telepromo";
                                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                    singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, confirmCode);
                                    result = singleCharge.Description;
                                }
                            }
                        }
                        else
                            result = "Service does not defined";
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Excepiton in OtpConfirm method: ", e);
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
