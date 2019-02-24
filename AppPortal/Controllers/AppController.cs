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
using System.Data.Entity;
using System.Threading.Tasks;


namespace Portal.Controllers
{
    public class AppController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> OtpAllowedServiceCodes = new List<string>() { /*"Soltan", */ "DonyayeAsatir", "MenchBaz", "Soraty", "DefendIran"
            , "AvvalYad", "BehAmooz500", "Darchin", "Halghe","Hoshang" };
        private List<string> AppMessageAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir", "Tamly"
            , "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin"
            , "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500", "Darchin"
            , "Dambel", "Aseman", "Medad", "PorShetab", "TajoTakht", "LahzeyeAkhar", "Hazaran", "JhoobinDambel", "JhoobinMedad", "JhoobinMusicYad", "JhoobinPin", "JhoobinPorShetab", "JhoobinTahChin"
            , "Halghe", "Achar","Hoshang" };
        private List<string> VerificactionAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir"
            , "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin"
            , "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500", "Darchin"
            , "Dambel", "Aseman", "Medad", "PorShetab", "TajoTakht", "LahzeyeAkhar", "Hazaran", "JhoobinDambel", "JhoobinMedad", "JhoobinMusicYad", "JhoobinPin", "JhoobinPorShetab", "JhoobinTahChin"
            , "Halghe", "Achar","Hoshang" };

        #region OTP Request
        //for old apps, old landings and localcall
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpCharge([FromBody]MessageObject messageObj)
        {
            logs.Info("AppController:OtpCharge,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);

            dynamic result = new ExpandoObject();
            bool resultOk = true;
            bool localCall = false;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                        , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}
                        , null
                        , "AppPortal:AppController:OtpCharge");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }

                string hash = "";
                SharedLibrary.MessageHandler.MessageSourceType messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.app;
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    //came from outside
                    hash = SharedLibrary.Encrypt.GetSha256Hash("OtpCharge" + messageObj.ServiceCode + messageObj.MobileNumber);

                }
                else
                {
                    //localcall
                    try
                    {
                        string extraParameter = SharedLibrary.Encrypt.DecryptString_RijndaelManaged(messageObj.ExtraParameter);
                        if (extraParameter == "localcall")
                        {
                            localCall = true;
                            hash = SharedLibrary.Encrypt.GetHMACSHA256Hash("OtpCharge" + messageObj.ServiceCode + messageObj.MobileNumber);
                            messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.sms;
                            //logs.Info(extraParameter);
                        }
                        else if (extraParameter == "landingcall")
                        {
                            hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                            messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.landing;
                        }
                        else
                        {
                            result = "Wrong Parameter";
                            //logs.Info("wrongparameter");
                            resultOk = false;
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                                , "OTPCharge: Wrong Parameter sent from " + HttpContext.Current.Request.UserHostAddress + " parameter=" + extraParameter);
                        }
                    }
                    catch (Exception e)
                    {
                        logs.Error("AppPortal:AppController:OtpCharge:WrongHash:", e);
                        resultOk = false;
                        result = "Wrong Hash";
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                                , "OTPCharge: Wrong Hash sent from " + HttpContext.Current.Request.UserHostAddress + " parameterEncrypted=" + messageObj.ExtraParameter);
                    }
                }
                if (!localCall && messageObj.ServiceCode == "Hoshang")
                {
                    result.Status = "Use New Method";
                    resultOk = false;
                }
                logs.Info(messageObj.AccessKey + "-" + hash);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = await this.fnc_processOTPRequest(messageObj, localCall, messageSourceType);
                    result.Status = status;
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:OtpCharge", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:OtpCharge,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        //for new apps and landings
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> RequestOTPForApps([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            bool localCall = false;

            logs.Info("AppPortal:AppController:RequestOTPForApps,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                            , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}
                            , null
                            , "AppPortal:AppController:RequestOTPForApps");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, true, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:RequestOTPForApps,DetectApp," + errorType + "," + errorDescription);
                    result = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;
                string hash = "";

                SharedLibrary.MessageHandler.MessageSourceType messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.app;
                hash = SharedLibrary.Encrypt.GetSha256Hash("RequestOTPForApps" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);

                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {

                    string status = await this.fnc_processOTPRequest(messageObj, localCall, messageSourceType);
                    result.Status = status;
                }

            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:RequestOTPForApps", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:RequestOTPForApps,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        private async Task<string> fnc_processOTPRequest(MessageObject messageObj, bool localCall
            , SharedLibrary.MessageHandler.MessageSourceType messageSourceType)
        {
            string status = "";
            if (messageObj.Price == null)
                messageObj.Price = 0;

            if (messageObj.ServiceCode == "NabardGah")
                messageObj.ServiceCode = "Soltan";
            else if (messageObj.ServiceCode == "ShenoYad")
                messageObj.ServiceCode = "ShenoYad500";
            else if (!OtpAllowedServiceCodes.Contains(messageObj.ServiceCode) && messageObj.Price.Value > 7) // Hub use price 5 and 6 for sub and unsub
                return "This ServiceCode does not have permission for OTP operation";

            messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
            if (messageObj.Number != null)
            {
                messageObj.Number = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Number);
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateLandLineNumber(messageObj.Number);
            }
            else
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                return "Invalid Mobile Number";
            else if (messageObj.MobileNumber == "Invalid Number")
                return "Invalid Number";

            if (!localCall)
            {
                if (messageObj.Price > 0 && !messageObj.InAppPurchase)
                {
                    return "InAppPurchase/Price Conflict";
                }
                else if (messageObj.Price == 0 && messageObj.InAppPurchase)
                {
                    return "InAppPurchase/Price Conflict";
                }
                else if (messageObj.Price < 0)
                {
                    return "Invalid Price";
                }
            }

            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
            if (service == null)
                return "Invalid serviceCode";

            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);

            using (var entity = new PortalEntities())
            {
                if (messageObj.Content != null || (string.IsNullOrEmpty(messageObj.Content) && !localCall))
                {
                    var mo = new ReceievedMessage()
                    {
                        MobileNumber = messageObj.MobileNumber,
                        ShortCode = serviceInfo.ShortCode,
                        ReceivedTime = DateTime.Now,
                        PersianReceivedTime = SharedLibrary.Date.GetPersianDateTime(DateTime.Now),
                        Content = (messageObj.Content == null) ? " " : messageObj.Content,
                        IsProcessed = true,
                        IsReceivedFromIntegratedPanel = false,
                        ReceivedFromSource = (int)messageSourceType,
                        ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-OtpCharge" : null
                    };
                    entity.ReceievedMessages.Add(mo);
                    entity.SaveChanges();


                }
                List<string> servicesCodes = entity.vw_servicesServicesInfo.Select(o => o.ServiceCode).ToList();
                var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(messageObj.MobileNumber, service.Id);
                //if (messageObj.Price.Value == 0 && subscriber != null && subscriber.DeactivationDate == null)
                //    result.Status = "User already subscribed";
                //else
                //{
                var minuetesBackward = DateTime.Now.AddMinutes(-5);
                //List<string> servicesNames = new List<string> { "Phantom", "TajoTakht", "Hazaran", "LahzeyeAkhar","Soltan"
                //,"MenchBaz","DonyayeAsatir","Aseman","AvvalPod","AvvalPod500","AvvalYad","BehAmooz500"
                //,"FitShow","JabehAbzar","ShenoYad","ShenoYad500","Takavar","Tamly","Tamly500","Halghe","Dezhban","ShahreKalameh"
                //,"Soraty","DefendIran","SepidRood","Nebula","Medio","Achar"};
                if (service.ServiceCode == "Darchin")
                {

                }
                else if (!servicesCodes.Any(o => o == service.ServiceCode))
                {
                    return "Invalid Service Code";
                }

                using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
                {
                    var imiChargeCode = new SharedLibrary.Models.ServiceModel.ImiChargeCode();
                    if (messageObj.Price.Value == 0)
                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entityService, messageObj, messageObj.Price.Value, 0
                            , SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Activated);
                    //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    else if (messageObj.Price.Value == -1)
                    {//only local call can deactive subscribers
                        if (localCall)
                        {
                            messageObj.Price = 0;
                            messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entityService, messageObj, messageObj.Price.Value, 0
                                , SharedLibrary.SubscriptionHandler.ServiceStatusForSubscriberState.Deactivated);
                            //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                        }
                        else
                        {
                            return "Not have permission";
                        }
                    }
                    else //price>0
                    {
                        if (!entityService.ImiChargeCodes.Any(o => o.Price == messageObj.Price))
                        {
                            return "Invalid Price";
                        }
                        else
                        {
                            messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entityService, messageObj, messageObj.Price.Value, 0, null);
                        }
                        //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                    }

                    if (messageObj.Price == null)
                        return "Invalid Price";


                    var otpMessage = "webservice";
                    if (messageObj.Content != null)
                        otpMessage = otpMessage + "-" + messageObj.Content;
                    var logId = SharedLibrary.MessageHandler.OtpLog(service, messageObj.MobileNumber, "request", otpMessage);
                    var isOtpExists = entityService.Singlecharges.Where(o => o.MobileNumber == messageObj.MobileNumber && o.Price == messageObj.Price && o.Description == "SUCCESS-Pending Confirmation" && o.DateCreated > minuetesBackward).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                    if (isOtpExists != null && messageObj.Price.Value == 0)
                    {
                        return "Otp request already exists for this subscriber";
                    }

                    var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
                    singleCharge.IsCalledFromInAppPurchase = messageObj.InAppPurchase;
                    //string aggregatorName = "MobinOneMapfa";
                    string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                    singleCharge = await SharedLibrary.MessageSender.OTPRequestGeneral(aggregatorName, entityService, singleCharge, messageObj, serviceAdditionalInfo);
                    status = singleCharge.Description;

                    //only for fitshow and soraty
                    if (status == "SUCCESS-Pending Confirmation" && (service.ServiceCode == "FitShow" || service.ServiceCode == "Soraty"))
                    {
                        /******************old Code for fitShow!!!!!!!!!!!!!! it uses soraty functions
                        if (result.Status == "SUCCESS-Pending Confirmation")
                        {

                            var messagesTemplate = SoratyLibrary.ServiceHandler.GetServiceMessagesTemplate();
                            int isCampaignActive = 0;
                            var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                            if (campaign != null)
                                isCampaignActive = Convert.ToInt32(campaign.Value);
                            var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(messageObj.MobileNumber, service.Id);
                            if (isInBlackList == true)
                                isCampaignActive = 0;
                            if (isCampaignActive == 1)
                            {
                                SharedLibrary.HandleSubscription.AddToTempReferral(messageObj.MobileNumber, service.Id, messageObj.Content);
                                messageObj.ShortCode = serviceInfo.ShortCode;
                                messageObj.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                                messageObj.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                                messageObj.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                                SoratyLibrary.MessageHandler.InsertMessageToQueue(messageObj);
                            }
                        }
                        ***************************/
                        /******************old Code for fitShow!!!!!!!!!!!!!! it uses soraty functions
                        var messagesTemplate = SoratyLibrary.ServiceHandler.GetServiceMessagesTemplate();
                        int isCampaignActive = 0;
                        var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                        if (campaign != null)
                            isCampaignActive = Convert.ToInt32(campaign.Value);
                        var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(messageObj.MobileNumber, service.Id);
                        if (isInBlackList == true)
                            isCampaignActive = 0;
                        if (isCampaignActive == 1)
                        {
                            SharedLibrary.HandleSubscription.AddToTempReferral(messageObj.MobileNumber, service.Id, messageObj.Content);
                            messageObj.ShortCode = serviceInfo.ShortCode;
                            messageObj.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                            messageObj.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                            messageObj.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                            SoratyLibrary.MessageHandler.InsertMessageToQueue(messageObj);
                        }
                        ********************************/
                        var messagesTemplate = entityService.MessagesTemplates.ToList();
                        int isCampaignActive = 0;
                        var campaign = entityService.Settings.FirstOrDefault(o => o.Name == "campaign");
                        if (campaign != null)
                            isCampaignActive = Convert.ToInt32(campaign.Value);
                        var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(messageObj.MobileNumber, service.Id);
                        if (isInBlackList == true)
                            isCampaignActive = 0;
                        if (isCampaignActive == 1)
                        {
                            SharedLibrary.SubscriptionHandler.AddToTempReferral(messageObj.MobileNumber, service.Id, messageObj.Content);
                            messageObj.ShortCode = serviceInfo.ShortCode;
                            messageObj.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                            messageObj.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                            messageObj.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                            SharedLibrary.MessageHandler.InsertMessageToQueue(entityService, messageObj);
                        }
                    }

                    SharedLibrary.MessageHandler.OtpLogUpdate(service, logId, status.ToString());

                }

            }




            return status;
        }

        #endregion

        #region otpConfirm
        /// <summary>
        /// for old apps,old landings and localCall
        /// </summary>
        /// <param name="messageObj"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpConfirm([FromBody]MessageObject messageObj)
        {
            logs.Info("AppController:OtpConfirm,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            bool localCall = false;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:OtpConfirm");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }

                string hash = "";
                SharedLibrary.MessageHandler.MessageSourceType messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.app;
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    //came from outside
                    hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                }
                else
                {
                    try
                    {

                        string extraParameter = SharedLibrary.Encrypt.DecryptString_RijndaelManaged(messageObj.ExtraParameter);
                        if (extraParameter == "localcall")
                        {
                            localCall = true;
                            hash = SharedLibrary.Encrypt.GetHMACSHA256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                            messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.sms;
                        }
                        else if (extraParameter == "landingcall")
                        {
                            hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                            messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.landing;
                        }
                        else
                        {
                            resultOk = false;
                            result = "Wrong Parameter";
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                                , "OTPConfirm: Wrong Parameter sent from " + HttpContext.Current.Request.UserHostAddress + " parameter=" + extraParameter);
                        }
                    }
                    catch (Exception e)
                    {
                        logs.Error("AppPortal:AppController:OtpConfirm:WrongHash:", e);
                        resultOk = false;
                        result = "Wrong Hash";
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                                , "OTPConfirm: Wrong Hash sent from " + HttpContext.Current.Request.UserHostAddress + " parameterEncrypted=" + messageObj.ExtraParameter);
                    }
                }

                if (!localCall && messageObj.ServiceCode == "Hoshang")
                {
                    result.Status = "Use New Method";
                    resultOk = false;
                }
                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = await this.fnc_processOTPConfirm(messageObj, localCall, messageSourceType);
                    result.Status = status;
                    if (status == "Exception has been occured!!! Contact Administrator")
                        resultOk = false;
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:OtpConfirm:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:OtpConfirm,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        /// <summary>
        /// new apps and new landings
        /// </summary>
        /// <param name="messageObj"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Confirm4DigitsOTPForApps([FromBody]MessageObject messageObj)
        {
            logs.Info("AppController:Confirm4DigitsOTPForApps,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            bool localCall = false;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:Confirm4DigitsOTPForApps");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, true, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:Confirm4DigitsOTPForApps,DetectApp," + errorType + "," + errorDescription);
                    result = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;

                string hash = "";
                hash = SharedLibrary.Encrypt.GetSha256Hash("Confirm4DigitsOTPForApps" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                SharedLibrary.MessageHandler.MessageSourceType messageSourceType = SharedLibrary.MessageHandler.MessageSourceType.app;

                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {

                    string status = await this.fnc_processOTPConfirm(messageObj, localCall, messageSourceType);
                    result.Status = status;
                    if (status == "Exception has been occured!!! Contact Administrator")
                    {
                        resultOk = false;
                    }


                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:Confirm4DigitsOTPForApps:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:Confirm4DigitsOTPForApps,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        private async Task<string> fnc_processOTPConfirm(MessageObject messageObj, bool localCall
            , SharedLibrary.MessageHandler.MessageSourceType messageSourceType)
        {

            string status = null;
            if (messageObj.ServiceCode == "NabardGah")
                messageObj.ServiceCode = "Soltan";
            else if (messageObj.ServiceCode == "ShenoYad")
                messageObj.ServiceCode = "ShenoYad500";

            messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
            if (messageObj.Number != null)
            {
                messageObj.Number = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Number);
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateLandLineNumber(messageObj.Number);
            }
            else
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);

            if (messageObj.MobileNumber == "Invalid Mobile Number")
                return "Invalid Mobile Number";
            else if (messageObj.MobileNumber == "Invalid Number")
                return "Invalid Number";
            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
            if (service == null)
                return "Invalid Service Code";

            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
            messageObj.ShortCode = serviceInfo.ShortCode;
            messageObj.ReceiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            messageObj.ReceivedFromSource = (int)messageSourceType;
            string errorType, errorDescription;
            ReceievedMessage messageEntity = null;
            if (messageObj.Content != null || (string.IsNullOrEmpty(messageObj.Content) && !localCall))
            {
                messageEntity = new ReceievedMessage()
                {
                    MobileNumber = messageObj.MobileNumber,
                    ShortCode = messageObj.ShortCode,
                    ReceivedTime = DateTime.Parse(messageObj.ReceiveTime),
                    PersianReceivedTime = SharedLibrary.Date.GetPersianDateTime(DateTime.Now),
                    Content = (string.IsNullOrEmpty(messageObj.Content)) ? (string.IsNullOrEmpty(messageObj.ConfirmCode) ? "" : messageObj.ConfirmCode) : messageObj.Content,
                    IsProcessed = true,
                    IsReceivedFromIntegratedPanel = false,
                    ReceivedFromSource = messageObj.ReceivedFromSource,
                    ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-OtpConfirm" : null
                };
            }
            using (var entity = new PortalEntities())
            {
                if (this.fnc_checkOTPConfirmSourceType(messageObj, localCall, out errorType, out errorDescription))
                {

                    //if (messageObj.Content != null)
                    //{



                    //}
                    List<string> servicesCodes = entity.vw_servicesServicesInfo.Select(o => o.ServiceCode).ToList();
                    //List<string> servicesNames = new List<string> { "Phantom", "TajoTakht", "Hazaran", "LahzeyeAkhar","Soltan"
                    //    ,"MenchBaz","DonyayeAsatir","Aseman","AvvalPod","AvvalPod500","AvvalYad","BehAmooz500"
                    //    ,"FitShow","JabehAbzar","ShenoYad","ShenoYad500","Takavar","Tamly","Tamly500","Halghe","Dezhban","ShahreKalameh"
                    //    ,"Soraty","DefendIran","SepidRood","Nebula","Medio","Achar"};

                    if (service.ServiceCode == "Darchin")
                    {
                        //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                        //{
                        //    var singleCharge = DarchinLibrary.ServiceHandler.GetOTPRequestId(entity, messageObj);
                        //    if (singleCharge == null)
                        //        result.Status = "No Otp Request Found";
                        //    else
                        //    {
                        //        messageObj.Price = singleCharge.Price;
                        //        messageObj.Token = singleCharge.ReferenceId;
                        //        var token = singleCharge.ReferenceId;
                        //        //string aggregatorName = "SamssonTci";
                        //        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                        //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                        //        singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, typeof(DarchinLibrary.Models.DarchinEntities), singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                        //        if (singleCharge.Description == "SUCCESS")
                        //        {
                        //            messageObj.Content = "Register";
                        //            DarchinLibrary.HandleMo.ReceivedMessage(messageObj, service);
                        //        }
                        //        result.Status = singleCharge.Description;
                        //        result.Token = token;
                        //    }
                        //}
                    }
                    else if (!servicesCodes.Any(o => o == service.ServiceCode))
                    {
                        return "Service is not defined";
                    }

                    using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
                    {
                        var logId = SharedLibrary.MessageHandler.OtpLog(service, messageObj.MobileNumber, "confirm", "webservice-" + messageObj.ConfirmCode);
                        var singleCharge = SharedLibrary.ServiceHandler.GetOTPRequestId(entityService, messageObj);
                        if (singleCharge == null)
                            return "No Otp Request Found";
                        //string aggregatorName = "MobinOneMapfa";
                        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                        singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, entityService, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                        status = singleCharge.Description;

                        SharedLibrary.MessageHandler.OtpLogUpdate(service, logId, status.ToString());
                    }

                    #region comment
                    //!!!!!!!!!!!!!!!!!!!!!oldcode
                    //if (service.ServiceCode == "AvvalPod")
                    // {
                    //     using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                    //     {
                    //         var singleCharge = AvvalPodLibrary.ServiceHandler.GetOTPRequestId(entity, messageObj);
                    //         if (singleCharge == null)
                    //             result.Status = "No Otp Request Found";
                    //         else
                    //         {
                    //             //string aggregatorName = "Telepromo";
                    //             string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                    //             var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                    //             singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                    //             result.Status = singleCharge.Description;
                    //         }
                    //     }
                    // }
                    //!!!!!!!!!!!!!!!!!!!!!oldcode
                    //if (service.ServiceCode == "AvvalYad")
                    // {
                    //     using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                    //     {
                    //         var singleCharge = AvvalYadLibrary.ServiceHandler.GetOTPRequestId(entity, messageObj);
                    //         if (singleCharge == null)
                    //             result.Status = "No Otp Request Found";
                    //         else
                    //         {
                    //             //string aggregatorName = "Telepromo";
                    //             string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                    //             var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                    //             singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                    //             result.Status = singleCharge.Description;
                    //         }
                    //     }
                    // }
                    //!!!!!!!!!!!!!!!!!!!!!oldcode
                    //if (service.ServiceCode == "SepidRood")
                    //   {
                    //       using (var entity = new SepidRoodLibrary.Models.SepidRoodEntities())
                    //       {
                    //           var singleCharge = new SepidRoodLibrary.Models.Singlecharge();
                    //           singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                    //           if (singleCharge == null)
                    //               result.Status = "No Otp Request Found";
                    //           else
                    //           {
                    //               //string aggregatorName = "PardisImi";
                    //               string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                    //               var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                    //               singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                    //               result.Status = singleCharge.Description;
                    //           }
                    //       }
                    //   }


                    //else
                    //    result.Status = "Service does not defined";
                    #endregion
                }

                else
                {
                    //this.fnc_checkOTPConfirmSourceType(messageObj, out errorType, out errorDescription)==false;

                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + ":" + errorDescription);
                    //resultOk = false;
                    status = "Exception has been occured!!! Contact Administrator";
                    if (messageEntity != null)
                        messageEntity.description = (errorType + "-" + errorDescription).Substring(0, Math.Min((errorType + "-" + errorDescription).Length, 200));

                }
                if (messageEntity != null)
                {
                    entity.ReceievedMessages.Add(messageEntity);
                    entity.SaveChanges();
                }
            }



            return status;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageObject">messageobject should have serviceCode,shortcode,mobilenumber,receivetime and ReceivedFromSource</param>
        /// <param name="errorType"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        private bool fnc_checkOTPConfirmSourceType(MessageObject messageObject, bool localCall
            , out string errorType, out string errorDescription)
        {
            errorType = "";
            errorDescription = "";
            //if (string.IsNullOrEmpty(messageObject.Content) && localCall)
            //{
            //    return true;
            //}
            try
            {
                //check if the otprequest is came from the same source(sms,app,landing) that otp confirm comes
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(messageObject.ServiceCode))
                    {

                        var setting = entityService.Settings.FirstOrDefault(o => o.Name == "checkotpconfirmsource");
                        if (setting == null) return true;
                        if (string.IsNullOrEmpty(setting.Value)) return true;
                        if (string.IsNullOrEmpty(messageObject.MobileNumber))
                        {
                            errorType = "MobileNumber is not specified";
                            return false;
                        }
                        if (string.IsNullOrEmpty(messageObject.ShortCode))
                        {
                            errorType = "ShortCode is not specified";
                            return false;
                        }
                        if (string.IsNullOrEmpty(messageObject.ReceiveTime))
                        {
                            errorType = "ReceivedTime is not specified";
                            return false;
                        }
                        DateTime receivedTime;
                        if (!DateTime.TryParse(messageObject.ReceiveTime, out receivedTime))
                        {
                            errorType = "ReceivedTime cannot be parsed to DateTime type";
                            errorDescription = messageObject.ReceiveTime;
                            return false;
                        }

                        var receivedOtpCharge = entityPortal.ReceievedMessages.Where(o => o.ShortCode == messageObject.ShortCode
                        && DbFunctions.TruncateTime(o.ReceivedTime) == DbFunctions.TruncateTime(receivedTime)
                        && o.MobileNumber == messageObject.MobileNumber && o.ReceivedFrom.Contains("OtpCharge")
                        && o.ReceivedTime < receivedTime).OrderByDescending(o => o.ReceivedTime).FirstOrDefault();

                        if (receivedOtpCharge != null)
                        {
                            if (setting.Value.ToLower() == "all")
                            {

                                if (receivedOtpCharge.ReceivedFromSource != messageObject.ReceivedFromSource)
                                {
                                    errorType = "Different Source Type has been detected";
                                    errorDescription = "MobileNumber" + messageObject.MobileNumber
                                        + " and ShortCode =" + messageObject.ShortCode + " ReceivedTime =" + receivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                        + " and OTPChargeContent=" + receivedOtpCharge.Content
                                        + " and OTPChargeTime=" + receivedOtpCharge.ReceivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {

                                if (setting.Value.Split(';').Any(o => o == receivedOtpCharge.Content))
                                {
                                    if (receivedOtpCharge.ReceivedFromSource != messageObject.ReceivedFromSource)
                                    {
                                        errorType = "Different Source Type has been detected";
                                        errorDescription = "MobileNumber" + messageObject.MobileNumber
                                            + " and ShortCode =" + messageObject.ShortCode + " ReceivedTime =" + receivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                            + " and OTPChargeContent=" + receivedOtpCharge.Content
                                            + " and OTPChargeTime=" + receivedOtpCharge.ReceivedTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        return false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    return true;
                                }
                            }

                        }
                        else
                        {
                            errorType = "There is no OTPCharge corresponding OTPConfirm";
                            errorDescription = "MobileNumber =" + messageObject.MobileNumber
                                + " and ShortCode =" + messageObject.ShortCode;
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:fnc_checkOTPConfirmSourceType:", e);
                errorType = "Exception has been occured";
                errorDescription = e.Message;
                return false;
            }
        }
        #endregion

        #region AppCharge
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ConsumeAppCharge([FromBody] MessageObject messageObj)
        {
            //logs.Info("AppController:ConsumeSubscriberCharge,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}
                                                            ,{ "price",messageObj.MobileNumber}}, null, "AppPortal:AppController:ConsumeAppCharge");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, true, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:ConsumeAppCharge,DetectApp," + errorType + "," + errorDescription);
                    result = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;

                string hash = "";
                hash = SharedLibrary.Encrypt.GetSha256Hash("ConsumeAppCharge" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);

                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = "";
                    string statusDetail;
                    SharedLibrary.SubscriptionHandler.consumeAppCharge(HttpContext.Current.Request.UserHostAddress
                        , messageObj.ServiceCode, messageObj.appName, messageObj.MobileNumber, messageObj.Price, out errorType, out errorDescription);
                    if (!string.IsNullOrEmpty(errorType))
                    {
                        status = errorType;
                        statusDetail = errorDescription;
                        result.Status = status;
                        result.StatusDetail = statusDetail;
                        resultOk = false;
                    }
                    else
                    {
                        status = "Successfully Consumed";
                        result.Status = status;
                        resultOk = true;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:ConsumeAppCharge:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:ConsumeAppCharge,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        public async Task<HttpResponseMessage> AppChargeStatus([FromBody] MessageObject messageObj)
        {
            //logs.Info("AppController:ConsumeSubscriberCharge,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}
                                                            ,{ "appname",messageObj.appName} }, null, "AppPortal:AppController:AppChargeStatus");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, true, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:AppChargeStatus,DetectApp," + errorType + "," + errorDescription);
                    result = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;

                string hash = "";
                hash = SharedLibrary.Encrypt.GetSha256Hash("AppChargeStatus" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);

                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = "";
                    string statusDetail;
                    int totalCharged, totalConsumed, remained;
                    remained = SharedLibrary.SubscriptionHandler.getRemainAppCharge(messageObj.ServiceCode, messageObj.appName, messageObj.MobileNumber
                        , out totalCharged, out totalConsumed, out errorType, out errorDescription);
                    if (!string.IsNullOrEmpty(errorType))
                    {
                        status = errorType;
                        statusDetail = errorDescription;
                        result.Status = status;
                        result.StatusDetail = statusDetail;
                        resultOk = false;
                    }
                    else
                    {
                        result.Status = "Successfully Fetched";
                        result.Remained = remained;
                        result.TotalCharged = totalCharged;
                        result.TotalConsumed = totalConsumed;
                        resultOk = true;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:AppChargeStatus:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:AppChargeStatus,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        public async Task<HttpResponseMessage> AppChargeDetail([FromBody] MessageObject messageObj)
        {
            //logs.Info("AppController:ConsumeSubscriberCharge,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}
                                                            ,{ "appname",messageObj.MobileNumber}}, null, "AppPortal:AppController:AppChargeDetail");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, true, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:AppChargeDetail,DetectApp," + errorType + "," + errorDescription);
                    result = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;

                string hash = "";
                hash = SharedLibrary.Encrypt.GetSha256Hash("AppChargeDetail" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);

                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = "";
                    string statusDetail;
                    Dictionary<DateTime, int> dicCharged, dicConsumed;
                    SharedLibrary.SubscriptionHandler.getAppChargeDetail(messageObj.ServiceCode, messageObj.appName, messageObj.MobileNumber
                        , out dicCharged, out dicConsumed, out errorType, out errorDescription);
                    if (!string.IsNullOrEmpty(errorType))
                    {
                        status = errorType;
                        statusDetail = errorDescription;
                        result.Status = status;
                        result.StatusDetail = statusDetail;
                        resultOk = false;
                    }
                    else
                    {
                        result.Status = "Successfully Fetched";
                        result.ChargedDetail = JsonConvert.SerializeObject(dicCharged);
                        result.ConsumedDetail = JsonConvert.SerializeObject(dicConsumed);

                        resultOk = true;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:AppChargeDetail:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:AppChargeDetail,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }
        #endregion

        /// <summary>
        /// this function is implemented for those apps who cannot encrypt users
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="extraParameter"></param>
        /// <returns></returns>
        [HttpPost]
        [System.Web.Mvc.RequireHttps]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetAppCipherText([FromBody] AppEncryption app)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            if (HttpContext.Current.Request.Url.Scheme != Uri.UriSchemeHttps)
            {
                result = "Require Https";
                resultOk = false;
                goto endSection;
            }

            try
            {
                string appName = app.appName;
                string extraParameter = app.ExtraParameter;
                string keyVector = app.keyVector;
                string IV = app.IV;

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                       , new Dictionary<string, string>() { { "appname", appName } }, null, "AppPortal:AppController:GetAppCipherText");


                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;

                    goto endSection;
                }

                if (string.IsNullOrEmpty(appName))
                {
                    result = "AppName is not specified";
                    resultOk = false;

                    goto endSection;
                }
                if (string.IsNullOrEmpty(extraParameter))
                {
                    result = "ExtraParameter is not specified";
                    resultOk = false;

                    goto endSection;
                }

                string errorType, errorDescription;


                using (var portal = new SharedLibrary.Models.PortalEntities())
                {

                    var entryApp = portal.Apps.Where(o => o.appName == appName).FirstOrDefault();
                    if (entryApp == null)
                    {
                        errorType = "Invalid AppName";
                        errorDescription = "AppName = " + appName;
                        result = errorType;
                        resultOk = false;
                        goto endSection;
                    }
                    if (!entryApp.state.HasValue || entryApp.state.Value == 0)
                    {
                        errorType = "App is disabled";
                        errorDescription = "AppName = " + appName;
                        result = errorType;
                        resultOk = false;
                        goto endSection;
                    }
                    if (entryApp.keyVector != keyVector)
                    {
                        errorType = "Wrong keyvector";
                        errorDescription = "AppName = " + appName;
                        result = errorType;
                        resultOk = false;
                        goto endSection;
                    }
                    if (entryApp.IV != IV)
                    {
                        errorType = "Wrong IV";
                        errorDescription = "AppName = " + appName;
                        result = errorType;
                        resultOk = false;
                        goto endSection;
                    }
                    //if (app.keySize / 8 / 2 != System.Text.Encoding.UTF8.GetByteCount(app.IV))
                    //{
                    //    errorType = "IV byte length is not equal with keysize/16";
                    //    errorDescription = "AppName = " + appName;
                    //    return "";
                    //}
                    //logs.Error("Ciphersdasdasdasd");
                    string cipherText = SharedLibrary.Encrypt.fnc_enryptAppParameter(entryApp.enryptAlghorithm, appName
                        , entryApp.keySize, entryApp.IV, entryApp.keyVector, extraParameter, out errorType, out errorDescription);
                    //logs.Error("Cipher1100");
                    if (!string.IsNullOrEmpty(errorType))
                    {
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, errorType + " " + errorDescription);

                        result = errorType;
                        resultOk = false;
                    }
                    else
                    {
                        result = cipherText;
                        resultOk = true;
                    }

                }


            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:GetAppCipherText:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
                //result = e.Message + e.StackTrace;
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

            return response;

        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> WebMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();

            bool resultOk = true;
            result.Status = "";
            try
            {

                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                                                            , new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:WebMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

                    var hash = SharedLibrary.Encrypt.GetSha256Hash("WebMessage" + messageObj.ServiceCode + messageObj.MobileNumber);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("WebMessage" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else if (messageObj.Content == null)
                        result.Status = "Content cannot be null";
                    else if (messageObj.ServiceCode == null)
                        result.Status = "ServiceCode cannot be null";
                    else
                    {
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid ServiceCode";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                            using (var entity = new PortalEntities())
                            {
                                var mo = new ReceievedMessage()
                                {
                                    MobileNumber = messageObj.MobileNumber,
                                    ShortCode = serviceInfo.ShortCode,
                                    ReceivedTime = DateTime.Now,
                                    PersianReceivedTime = SharedLibrary.Date.GetPersianDateTime(DateTime.Now),
                                    Content = (messageObj.Content == null) ? " " : messageObj.Content,
                                    IsProcessed = false,
                                    IsReceivedFromIntegratedPanel = false,
                                    ReceivedFromSource = 0,
                                    ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-WebMessage" : null
                                };
                                entity.ReceievedMessages.Add(mo);
                                entity.SaveChanges();
                            }
                            result.Status = "Success";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:WebMessage:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Register([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.Token = messageObj.Token;
            result.Status = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}
                    , null, "AppPortal:AppController:Register");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {


                    var hash = SharedLibrary.Encrypt.GetSha256Hash("Register" + messageObj.ServiceCode + messageObj.Number);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("Register" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid serviceCode";
                        else
                        {
                            if (service.ServiceCode == "Darchin")
                            {
                                //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                                //{
                                //    if (messageObj.Price.Value == 7000)
                                //    {
                                //        messageObj.Content = "ir.darchin.app;Darchin123";
                                //    }
                                //    if (messageObj.Price != 7000)
                                //        result.Status = "Invalid Price";
                                //    else
                                //    {
                                //        var singleCharge = new DarchinLibrary.Models.Singlecharge();
                                //        string aggregatorName = "SamssonTci";
                                //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                //        singleCharge = await SharedLibrary.MessageSender.SamssonTciOTPRequest(typeof(DarchinLibrary.Models.DarchinEntities), singleCharge, messageObj, serviceAdditionalInfo);
                                //        result.Status = singleCharge.Description;
                                //        result.Token = singleCharge.ReferenceId;
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:Register", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Deregister([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.Token = messageObj.Token;
            result.Status = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:Deregister");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var hash = SharedLibrary.Encrypt.GetSha256Hash("Deregister" + messageObj.ServiceCode + messageObj.Token);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("Deregister" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid serviceCode";
                        else
                        {
                            if (service.ServiceCode == "Darchin")
                            {
                                //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                                //{
                                //    if (messageObj.Price.Value == -7000)
                                //    {
                                //        messageObj.Content = "ir.darchin.app;Darchin123";
                                //    }
                                //    else
                                //        result.Status = "Invalid Price";
                                //    if (result.Status != "Invalid Price")
                                //    {
                                //        var singleCharge = new DarchinLibrary.Models.Singlecharge();
                                //        string aggregatorName = "SamssonTci";
                                //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);

                                //        singleCharge = await SharedLibrary.MessageSender.SamssonTciSinglecharge(typeof(DarchinLibrary.Models.DarchinEntities), typeof(DarchinLibrary.Models.Singlecharge), messageObj, serviceAdditionalInfo, true);
                                //        result.Status = singleCharge.IsSucceeded;
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:Deregister", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppMessage([FromBody]MessageObject messageObj)
        {
            logs.Info("1");
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:AppMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    result.Status = "error";
                    var hash = SharedLibrary.Encrypt.GetSha256Hash("AppMessage" + messageObj.ServiceCode + messageObj.MobileNumber + messageObj.Content.Substring(0, 3));
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("AppMessage" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        if (messageObj.ServiceCode == "NabardGah")
                            messageObj.ServiceCode = "Soltan";

                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                        messageObj.ShortCode = serviceInfo.ShortCode;
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                        if (messageObj.MobileNumber == "Invalid Mobile Number")
                            result.Status = "Invalid Mobile Number";
                        else if (!AppMessageAllowedServiceCode.Contains(messageObj.ServiceCode))
                            result.Status = "This ServiceCode does not have permission";
                        else
                        {
                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromApp" : null;
                            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                            result.Status = "Success";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:AppMessage", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage IsUserSubscribed([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;

            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:IsUserSubscribed");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                    //hash = "";
                }
                else
                {
                    if (messageObj.Number != null)
                    {
                        messageObj.MobileNumber = messageObj.Number;
                        result.Number = messageObj.MobileNumber;
                    }
                    else
                        result.MobileNumber = messageObj.MobileNumber;

                    var hash = SharedLibrary.Encrypt.GetSha256Hash("IsUserSubscribed" + messageObj.ServiceCode + messageObj.MobileNumber);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("IsUserSubscribed" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        string status = this.fnc_checkUserStatus(messageObj);
                        result.Status = status;
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:IsUserSubscribed:" + e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubscriberActivationState([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;

            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:SubscriberActivationState");

                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result.Status = tpsRatePassed;
                    resultOk = false;
                    goto endSection;
                }

                if (string.IsNullOrEmpty(messageObj.appName))
                {
                    result.Status = "AppName is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.cipherParameter))
                {
                    result.Status = "cipherParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }
                if (string.IsNullOrEmpty(messageObj.ExtraParameter))
                {
                    result.Status = "ExtraParameter is not specified";
                    resultOk = false;
                    goto endSection;
                }

                string errorType, errorDescription;
                if (!SharedLibrary.Encrypt.fnc_detectApp(messageObj.appName, messageObj.ServiceCode, messageObj.Content, messageObj.ExtraParameter
                    , messageObj.cipherParameter, false, out errorType, out errorDescription))
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                        , errorType + " " + errorDescription);
                    logs.Error("AppPortal:AppController:SubscriberActivationState,DetectApp," + errorType + "," + errorDescription);
                    result.Status = errorType;
                    resultOk = false;
                }
                if (!resultOk)
                    goto endSection;
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;

                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;
                string hash = "";
                hash = SharedLibrary.Encrypt.GetSha256Hash("SubscriberActivationState" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);

                //var hash = SharedLibrary.Encrypt.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else if (resultOk)
                {
                    string status = this.fnc_checkUserStatus(messageObj);
                    result.Status = status;
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:SubscriberActivationState:", e);
                //result = e.Message;
                resultOk = false;
                result = "Exception has been occured!!! Contact Administrator";
            }
            endSection: var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:SubscriberActivationState,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;

        }
        private string fnc_checkUserStatus([FromBody]MessageObject messageObj)
        {
            string status = null;



            messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
            if (messageObj.Number != null)
            {
                messageObj.Number = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Number);
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateLandLineNumber(messageObj.Number);
            }
            else
                messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);

            if (messageObj.MobileNumber == "Invalid Mobile Number")
                status = "Invalid Mobile Number";
            else if (messageObj.MobileNumber == "Invalid Number")
                status = "Invalid Number";
            else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                status = "This ServiceCode does not have permission";
            else
            {
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                if (service == null)
                {
                    status = "Invalid ServiceCode";
                }
                else
                {
                    var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(messageObj.MobileNumber, service.Id);
                    if (subscriber != null && subscriber.DeactivationDate == null)
                        status = "Subscribed";
                    else
                        status = "NotSubscribed";
                }
            }

            return status;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Verification([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                result.MobileNumber = messageObj.MobileNumber;
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:Verification");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var hash = SharedLibrary.Encrypt.GetSha256Hash("Verification" + messageObj.ServiceCode + messageObj.MobileNumber);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("Verification" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        if (messageObj.ServiceCode == "NabardGah")
                            messageObj.ServiceCode = "Soltan";
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                        if (messageObj.MobileNumber == "Invalid Mobile Number")
                            result.Status = "Invalid Mobile Number";
                        else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                            result.Status = "This ServiceCode does not have permission";
                        else
                        {
                            if (messageObj.ServiceCode == "Tamly")
                                messageObj.ServiceCode = "Tamly500";
                            else if (messageObj.ServiceCode == "AvvalPod")
                                messageObj.ServiceCode = "AvvalPod500";
                            else if (messageObj.ServiceCode == "AvvalYad")
                                messageObj.ServiceCode = "BehAmooz500";
                            else if (messageObj.ServiceCode == "ShenoYad")
                                messageObj.ServiceCode = "ShenoYad500";
                            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                            if (service == null)
                            {
                                result.Status = "Invalid ServiceCode";
                            }
                            else
                            {
                                messageObj.ServiceId = service.Id;
                                messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                                var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                                if (subscriber != null && subscriber.DeactivationDate == null)
                                {
                                    Random random = new Random();
                                    var verficationId = random.Next(1000, 9999).ToString();
                                    messageObj.Content = "SendVerification-" + verficationId;
                                    if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-AppVerification" : null;
                                    else
                                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-AppVerification" : null;
                                    SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                    result.Status = "Subscribed";
                                    result.ActivationCode = verficationId;
                                }
                                else
                                {
                                    if (service.ServiceCode == "DonyayeAsatir")
                                    {
                                        var sub = SharedLibrary.SubscriptionHandler.GetSubscriber(messageObj.MobileNumber, 10004);
                                        if (sub != null && sub.DeactivationDate == null)
                                        {
                                            messageObj.ServiceId = 10004;
                                            messageObj.ShortCode = "307229";
                                            Random random = new Random();
                                            var verficationId = random.Next(1000, 9999).ToString();
                                            messageObj.Content = "SendVerification-" + verficationId;
                                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-AppVerification" : null;
                                            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                            result.Status = "Subscribed";
                                            result.ActivationCode = verficationId;
                                        }
                                        else
                                        {
                                            messageObj.Content = "SendServiceSubscriptionHelp";
                                            if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-Verification" : null;
                                            else
                                                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-Verification" : null;
                                            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                            result.Status = "NotSubscribed";
                                            result.ActivationCode = null;
                                        }
                                    }
                                    else
                                    {
                                        messageObj.Content = "SendServiceSubscriptionHelp";
                                        if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-Verification" : null;
                                        else
                                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-Verification" : null;
                                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                        result.Status = "NotSubscribed";
                                        result.ActivationCode = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:Verification:" + e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubscriberStatus([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "AppPortal:AppController:SubscriberStatus");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var hash = SharedLibrary.Encrypt.GetSha256Hash("SubscriberStatus" + messageObj.ServiceCode + messageObj.MobileNumber);
                    var hashNew = SharedLibrary.Encrypt.GetSha256Hash("SubscriberStatus" + "-" + messageObj.ServiceCode + "-" + messageObj.MobileNumber);
                    if (messageObj.AccessKey != hash && messageObj.AccessKey != hashNew)
                    {
                        result.Status = "You do not have permission";
                    }
                    else
                    {
                        if (messageObj.ServiceCode == "NabardGah")
                            messageObj.ServiceCode = "Soltan";
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                        if (messageObj.MobileNumber == "Invalid Mobile Number")
                            result.Status = "Invalid Mobile Number";
                        else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                            result.Status = "This ServiceCode does not have permission";
                        else
                        {
                            if (messageObj.ServiceCode == "Tamly")
                                messageObj.ServiceCode = "Tamly500";
                            else if (messageObj.ServiceCode == "AvvalYad")
                                messageObj.ServiceCode = "BehAmooz500";
                            else if (messageObj.ServiceCode == "ShenoYad")
                                messageObj.ServiceCode = "ShenoYad500";
                            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                            if (service == null)
                            {
                                result.Status = "Invalid ServiceCode";
                            }
                            else
                            {
                                messageObj.ServiceId = service.Id;
                                messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                                var subscriber = SharedLibrary.SubscriptionHandler.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                                var daysLeft = 0;
                                var pricePayed = -1;
                                if (messageObj.ServiceCode == "Soltan")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soltan"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "DonyayeAsatir")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DonyayeAsatir"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var singlecharge = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                            if (singlecharge != null)
                                            {
                                                pricePayed = 0;
                                            }
                                            else
                                                pricePayed = -1;
                                        }
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "MenchBaz")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MenchBaz"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "Soraty")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soraty"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "DefendIran")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DefendIran"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "ShahreKalameh")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShahreKalameh"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Aseman")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Aseman"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Tamly")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Tamly500")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly500"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Dezhban")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Dezhban"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "TahChin")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("TahChin"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "MusicYad")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("MusicYad"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Dambel")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Dambel"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Medad")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medad"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode.ToLower() == "PorShetab")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("PorShetab"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "JabehAbzar")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("JabehAbzar"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "ShenoYad")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "ShenoYad500")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShenoYad500"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "FitShow")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("FitShow"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Takavar")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Takavar"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "AvvalPod")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("AvvalPod"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "AvvalPod500")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("AvvalPod500"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "AvvalYad")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("AvvalYad"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "BehAmooz500")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("BehAmooz500"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                            pricePayed = -1;
                                        else
                                        {
                                            var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                                singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                            pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                            singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                            if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                            {
                                                entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "Nebula")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Nebula"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Phantom")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Phantom"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Hazaran")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Hazaran"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Medio")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Medio"))
                                    {
                                        var now = DateTime.Now;
                                        using (var portalEntity = new SharedLibrary.Models.PortalEntities())
                                        {
                                            var singlechargeInstallment = portalEntity.Subscribers.Where(o => o.MobileNumber == messageObj.MobileNumber && o.ServiceId == 10030).FirstOrDefault();
                                            if (singlechargeInstallment == null)
                                            {
                                                daysLeft = 0;
                                            }
                                            else if (singlechargeInstallment.DeactivationDate != null)
                                                daysLeft = 0;
                                            else
                                                daysLeft = 30;
                                        }
                                    }
                                }
                                else if (messageObj.ServiceCode == "TajoTakht")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("TajoTakht"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "LahzeyeAkhar")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("LahzeyeAkhar"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                else if (messageObj.ServiceCode == "Achar")
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Achar"))
                                    {
                                        var now = DateTime.Now;
                                        var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlechargeInstallment == null)
                                        {
                                            var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                            if (installmentQueue != null)
                                                daysLeft = 30;
                                            else
                                                daysLeft = 0;
                                        }
                                        else
                                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                    }
                                }
                                if (daysLeft > 0 || pricePayed > -1)
                                {
                                    result.Status = "Active";
                                    if (daysLeft > 0)
                                        result.DaysLeft = daysLeft;
                                    else
                                        result.PricePayed = pricePayed;
                                }
                                else
                                {
                                    result.Status = "NotActive";
                                    result.DaysLeft = null;
                                    result.PricePayed = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //logs.Error("Exception in SubscriberStatus:" + e);
                ////result.Status = "General error occurred";
                //resultOk = false;
                //result = e.Message;
                logs.Error("AppPortal:AppController:SubscriberStatus", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetIrancellOtpUrl(string serviceCode, string callBackParam)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",serviceCode }}, null, "AppPortal:AppController:GetIrancellOtpUrl");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    if (serviceCode != null && (serviceCode == "TahChin" || serviceCode == "MusicYad" || serviceCode == "Dambel" || serviceCode == "Medad" || serviceCode == "PorShetab"))
                    {
                        string timestampParam = DateTime.Now.ToString("yyyyMMddhhmmss");
                        string requestIdParam = Guid.NewGuid().ToString();
                        var price = "3000";
                        var modeParam = "1"; //Web
                        var pageNo = 0;
                        var authKey = "393830313130303036333739";
                        var sign = "";
                        var cpId = "980110006379";

                        var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode).Value;
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                        if (serviceCode == "TahChin")
                            pageNo = 146;
                        else if (serviceCode == "MusicYad")
                            pageNo = 206;
                        else if (serviceCode == "Dambel")
                            pageNo = 0;
                        else if (serviceCode == "Medad")
                            pageNo = 0;
                        else if (serviceCode == "PorShetab")
                        {
                            pageNo = 299;
                            price = "5000";
                        }

                        sign = SharedLibrary.HelpfulFunctions.IrancellSignatureGenerator(authKey, cpId, serviceInfo.AggregatorServiceId, price, timestampParam, requestIdParam);
                        //var url = string.Format(@"http://92.42.51.91/CGGateway/Default.aspx?Timestamp={0}&RequestID={1}&pageno={2}&Callback={3}&Sign={4}&mode={5}"
                        //                        , timestampParam, requestIdParam, pageNo, callBackParam, sign, modeParam);
                        var url = string.Format(SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.MTNGet, SharedLibrary.HelpfulFunctions.enumServersActions.MTNOtpGet)
                                                , timestampParam, requestIdParam, pageNo, callBackParam, sign, modeParam);
                        result.Status = "Success";
                        result.uuid = requestIdParam;
                        result.Description = url;
                    }
                    else
                    {
                        result.Status = "Invalid Service Code";
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:GetIrancellOtpUrl", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetIrancellUnsubUrl(string serviceCode, string callBackParam)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",serviceCode }}, null, "AppPortal:AppController:GetIrancellUnsubUrl");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

                    if (serviceCode != null && (serviceCode == "TahChin" || serviceCode == "MusicYad" || serviceCode == "Dambel" || serviceCode == "Medad" || serviceCode == "PorShetab"))
                    {
                        string timestampParam = DateTime.Now.ToString("yyyyMMddhhmmss");
                        string requestIdParam = Guid.NewGuid().ToString();
                        var price = "3000";
                        var modeParam = "1"; //Web
                        var pageNo = 0;
                        var authKey = "393830313130303036333739";
                        var sign = "";
                        var cpId = "980110006379";

                        var serviceId = SharedLibrary.ServiceHandler.GetServiceId(serviceCode).Value;
                        var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                        if (serviceCode == "TahChin")
                            pageNo = 146;
                        else if (serviceCode == "MusicYad")
                            pageNo = 206;
                        else if (serviceCode == "Dambel")
                            pageNo = 0;
                        else if (serviceCode == "Medad")
                            pageNo = 0;
                        else if (serviceCode == "PorShetab")
                        {
                            pageNo = 299;
                            price = "5000";
                        }

                        sign = SharedLibrary.HelpfulFunctions.IrancellSignatureGenerator(authKey, cpId, serviceInfo.AggregatorServiceId, price, timestampParam, requestIdParam);
                        //var url = string.Format(@"http://92.42.51.91/CGGateway/UnSubscribe.aspx?Timestamp={0}&RequestID={1}&CpCode={2}&Callback={3}&Sign={4}&mode={5}"
                        //                        , timestampParam, requestIdParam, cpId, callBackParam, sign, modeParam);
                        var url = string.Format(SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.MTNGet, SharedLibrary.HelpfulFunctions.enumServersActions.MTNUnsubGet)
                                                , timestampParam, requestIdParam, cpId, callBackParam, sign, modeParam);
                        result.Status = "Success";
                        result.uuid = requestIdParam;
                        result.Description = url;
                    }
                    else
                    {
                        result.Status = "Invalid Service Code";
                    }
                }
            }
            //catch (Exception e)
            //{
            //    logs.Error("Exception in GetIrancellOtpUrl:" + e);
            //    result.Status = "Error";
            //    result.Description = "General error occurred";
            //}
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:GetIrancellUnsubUrl", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage DecryptIrancellMessage(string data)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "data",data}}, null, "AppPortal:AppController:DecryptIrancellMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

                    result.Status = "Error";
                    result.Description = "General error occurred";
                    result.uuid = "";
                    var authKey = "393830313130303036333739";
                    var message = SharedLibrary.HelpfulFunctions.IrancellEncryptedResponse(data, authKey);
                    var splitedMessage = message.Split('&');
                    foreach (var item in splitedMessage)
                    {
                        if (item.Contains("msisdn"))
                            result.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.Remove(0, 7));
                        else if (item.ToLower().Contains("requestid"))
                            result.uuid = item.Remove(0, 10);
                        else if (item.Contains("status"))
                        {
                            var status = item.Remove(0, 7);
                            if (status == "00000000")
                            {
                                result.Status = "Success";
                                result.Description = "Successful Subscription";
                            }
                            else if (status == "22007201" || status == "22007238")
                            {
                                result.Status = "Error";
                                result.Description = "Already Subscribed";
                            }
                            else if (status == "10001211")
                            {
                                result.Status = "Error";
                                result.Description = "ServiceID + IP not whitelisted or used only 3G services service ID";
                            }
                            else if (status == "22007306")
                            {
                                result.Status = "Error";
                                result.Description = "MSISDN Blacklist";
                            }
                            else if (status == "22007230")
                            {
                                result.Status = "Error";
                                result.Description = "cannot be subscribed to by a third party";
                            }
                            else if (status == "22007330")
                            {
                                result.Status = "Error";
                                result.Description = "The account balance is Insufficient.";
                            }
                            else if (status == "22007306")
                            {
                                result.Status = "Error";
                                result.Description = "The user is blacklisted and cannot Subscribe to the product.";
                            }
                            else if (status == "99999999")
                            {
                                result.Status = "Error";
                                result.Description = "Subscription attempt failed. Please try again.";
                            }
                            else if (status == "88888888")
                            {
                                result.Status = "Error";
                                result.Description = "Cancel Button Clicked";
                            }
                            else
                            {
                                result.Status = "Error";
                                result.Description = "Unknown status code: " + status;
                            }
                        }
                    }
                }
            }
            //catch (Exception e)
            //{
            //    logs.Error("Exception in DecryptIrancellMessage:" + e);
            //    result.Status = "Error";
            //    result.Description = "General error occurred";
            //}
            catch (Exception e)
            {
                logs.Error("AppPortal:AppController:DecryptIrancellMessage", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
