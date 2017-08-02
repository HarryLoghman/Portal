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

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpCharge([FromBody]MessageObject messageObj)
        {
            string result = "";
            var hash = SharedLibrary.Security.GetSha256Hash("OTPCharge" + messageObj.ServiceCode + messageObj.MobileNumber + messageObj.Price);
            if (messageObj.AccessKey != hash)
                result = "You do not have permission";
            else
            {
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                if (messageObj.MobileNumber == "Invalid Mobile Number")
                    result = "Invalid Mobile Number";
                else
                {
                    try
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result = "Invalid serviceCode";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                            if (service.ServiceCode == "Soltan")
                            {
                                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                {
                                    var imiChargeCode = new SoltanLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
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
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
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
                            else if (service.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                {
                                    var imiChargeCode = new DonyayeAsatirLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod")
                            {
                                using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                                {
                                    var imiChargeCode = new AvvalPodLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new AvvalPodLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "FitShow")
                            {
                                using (var entity = new FitShowLibrary.Models.FitShowEntities())
                                {
                                    var imiChargeCode = new FitShowLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new FitShowLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "JabehAbzar")
                            {
                                using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                                {
                                    var imiChargeCode = new JabehAbzarLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new JabehAbzarLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad")
                            {
                                using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                                {
                                    var imiChargeCode = new ShenoYadLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new ShenoYadLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Takavar")
                            {
                                using (var entity = new TakavarLibrary.Models.TakavarEntities())
                                {
                                    var imiChargeCode = new TakavarLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new TakavarLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly")
                            {
                                using (var entity = new TamlyLibrary.Models.TamlyEntities())
                                {
                                    var imiChargeCode = new TamlyLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new TamlyLibrary.Models.Singlecharge();
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
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpConfirm([FromBody]MessageObject messageObj)
        {
            string result = "";
            var hash = SharedLibrary.Security.GetSha256Hash("OTPConfirm" + messageObj.ServiceCode + messageObj.MobileNumber + messageObj.ConfirmCode);
            if (messageObj.AccessKey != hash)
                result = "You do not have permission";
            else
            {
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                if (messageObj.MobileNumber == "Invalid Mobile Number")
                    result = "Invalid Mobile Number";
                else
                {
                    try
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result = "Invalid serviceId";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
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
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
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
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod")
                            {
                                using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                                {
                                    var singleCharge = new AvvalPodLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                {
                                    var singleCharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "FitShow")
                            {
                                using (var entity = new FitShowLibrary.Models.FitShowEntities())
                                {
                                    var singleCharge = new FitShowLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "JabehAbzar")
                            {
                                using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                                {
                                    var singleCharge = new JabehAbzarLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad")
                            {
                                using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                                {
                                    var singleCharge = new ShenoYadLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Takavar")
                            {
                                using (var entity = new TakavarLibrary.Models.TakavarEntities())
                                {
                                    var singleCharge = new TakavarLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly")
                            {
                                using (var entity = new TamlyLibrary.Models.TamlyEntities())
                                {
                                    var singleCharge = new TamlyLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("MenchBaz", aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
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
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
