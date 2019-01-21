﻿using System.Web.Http;
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

        private List<string> OtpAllowedServiceCodes = new List<string>() { /*"Soltan", */ "DonyayeAsatir", "MenchBaz", "Soraty", "DefendIran", "AvvalYad", "BehAmooz500", "Darchin", "Halghe" };
        private List<string> AppMessageAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500", "Darchin", "Dambel", "Aseman", "Medad", "PorShetab", "TajoTakht", "LahzeyeAkhar", "Hazaran", "JhoobinDambel", "JhoobinMedad", "JhoobinMusicYad", "JhoobinPin", "JhoobinPorShetab", "JhoobinTahChin", "Halghe", "Achar" };
        private List<string> VerificactionAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500", "Darchin", "Dambel", "Aseman", "Medad", "PorShetab", "TajoTakht", "LahzeyeAkhar", "Hazaran", "JhoobinDambel", "JhoobinMedad", "JhoobinMusicYad", "JhoobinPin", "JhoobinPorShetab", "JhoobinTahChin", "Halghe", "Achar" };
        private List<string> TimeBasedServices = new List<string>() { "ShahreKalameh", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "AvvalPod", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "ShenoYad500", "Tamly500", "AvvalPod500", "Darchin", "Dambel", "Medad", "PorShetab", "TajoTakht", "LahzeyeAkhar", "Hazaran", "JhoobinDambel", "JhoobinMedad", "JhoobinMusicYad", "JhoobinPin", "JhoobinPorShetab", "JhoobinTahChin", "Halghe", "Achar" };
        private List<string> PriceBasedServices = new List<string>() { /*"Soltan",*/ "DonyayeAsatir", "MenchBaz", "Soraty", "DefendIran", "AvvalYad", "BehAmooz500", "Darchin" };

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpCharge([FromBody]MessageObject messageObj)
        {
            logs.Info("AppController:OtpCharge,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;
                var hash = SharedLibrary.Security.GetSha256Hash("OtpCharge" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.Price == null)
                        messageObj.Price = 0;
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
                    else if (messageObj.ServiceCode == "ShenoYad")
                        messageObj.ServiceCode = "ShenoYad500";
                    else if (!OtpAllowedServiceCodes.Contains(messageObj.ServiceCode) && messageObj.Price.Value > 7) // Hub use price 5 and 6 for sub and unsub
                        result.Status = "This ServiceCode does not have permission for OTP operation";
                    else
                    {
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
                        if (messageObj.Number != null)
                        {
                            messageObj.Number = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Number);
                            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateLandLineNumber(messageObj.Number);
                        }
                        else
                            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                        if (messageObj.MobileNumber == "Invalid Mobile Number")
                            result.Status = "Invalid Mobile Number";
                        else if (messageObj.MobileNumber == "Invalid Number")
                            result.Status = "Invalid Number";
                        else
                        {
                            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);

                            if (service == null)
                                result.Status = "Invalid serviceCode";
                            else
                            {
                                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                                if (messageObj.Content != null)
                                {
                                    using (var entity = new PortalEntities())
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
                                            IsReceivedFromWeb = false,
                                            ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-OtpCharge" : null
                                        };
                                        entity.ReceievedMessages.Add(mo);
                                        entity.SaveChanges();
                                    }
                                }
                                var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, service.Id);
                                //if (messageObj.Price.Value == 0 && subscriber != null && subscriber.DeactivationDate == null)
                                //    result.Status = "User already subscribed";
                                //else
                                //{
                                var minuetesBackward = DateTime.Now.AddMinutes(-5);
                                List<string> servicesNames = new List<string> { "Phantom", "TajoTakht", "Hazaran", "LahzeyeAkhar","Soltan"
                                ,"MenchBaz","DonyayeAsatir","Aseman","AvvalPod","AvvalPod500","AvvalYad","BehAmooz500"
                                ,"FitShow","JabehAbzar","ShenoYad","ShenoYad500","Takavar","Tamly","Tamly500","Halghe","Dezhban","ShahreKalameh"
                                ,"Soraty","DefendIran","SepidRood","Nebula","Medio","Achar"};
                                if (service.ServiceCode == "Darchin")
                                {
                                    //using (var entity = new DarchinLibrary.Models.DarchinEntities())
                                    //{
                                    //    if (messageObj.Price.Value < 0)
                                    //    {
                                    //        messageObj.Content = "Unsubscribe";
                                    //    }
                                    //    else if (messageObj.Price != 7000)
                                    //        messageObj.Price = null;
                                    //    if (messageObj.Price == null)
                                    //        result.Status = "Invalid Price";
                                    //    else
                                    //    {
                                    //        var singleCharge = new DarchinLibrary.Models.Singlecharge();
                                    //        //string aggregatorName = "SamssonTci";
                                    //        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                                    //        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                    //        if (messageObj.Price >= 0)
                                    //            singleCharge = await SharedLibrary.MessageSender.OTPRequestGeneral(aggregatorName, typeof(DarchinLibrary.Models.DarchinEntities), singleCharge, messageObj, serviceAdditionalInfo);
                                    //        else
                                    //        {
                                    //            var activeTokens = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    //            if (activeTokens != null)
                                    //            {
                                    //                messageObj.Token = activeTokens.UserToken;
                                    //                singleCharge = await SharedLibrary.MessageSender.SamssonTciSinglecharge(typeof(DarchinLibrary.Models.DarchinEntities), typeof(DarchinLibrary.Models.Singlecharge), messageObj, serviceAdditionalInfo, true);
                                    //            }
                                    //            else
                                    //                singleCharge.Description = "User Does Not Exists";
                                    //            DarchinLibrary.HandleMo.ReceivedMessage(messageObj, service);
                                    //        }
                                    //        result.Status = singleCharge.Description;
                                    //        result.Token = singleCharge.ReferenceId;
                                    //    }
                                    //}
                                }
                                else if (!servicesNames.Any(o => o == service.ServiceCode))
                                {
                                    result.Status = "Service does not defined";
                                }
                                else
                                {
                                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
                                    {
                                        var imiChargeCode = new SharedLibrary.Models.ServiceModel.ImiChargeCode();
                                        if (messageObj.Price.Value == 0)
                                            messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, messageObj, messageObj.Price.Value, 0
                                                , SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                        //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                        else if (messageObj.Price.Value == -1)
                                        {
                                            messageObj.Price = 0;
                                            messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, messageObj, messageObj.Price.Value, 0
                                                , SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                            //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                        }
                                        else
                                        {
                                            messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, messageObj, messageObj.Price.Value, 0, null);
                                            //messageObj = PhantomLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                        }
                                        if (messageObj.Price == null)
                                            result.Status = "Invalid Price";
                                        else
                                        {
                                            var otpMessage = "webservice";
                                            if (messageObj.Content != null)
                                                otpMessage = otpMessage + "-" + messageObj.Content;
                                            var logId = SharedLibrary.MessageHandler.OtpLog(service, messageObj.MobileNumber, "request", otpMessage);
                                            var isOtpExists = entity.Singlecharges.Where(o => o.MobileNumber == messageObj.MobileNumber && o.Price == 0 && o.Description == "SUCCESS-Pending Confirmation" && o.DateCreated > minuetesBackward).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                            if (isOtpExists != null && messageObj.Price.Value == 0)
                                            {
                                                result.Status = "Otp request already exists for this subscriber";
                                            }
                                            else
                                            {
                                                var singleCharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
                                                //string aggregatorName = "MobinOneMapfa";
                                                string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                                                var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                                singleCharge = await SharedLibrary.MessageSender.OTPRequestGeneral(aggregatorName, entity, singleCharge, messageObj, serviceAdditionalInfo);
                                                result.Status = singleCharge.Description;

                                                //only for fitshow and soraty
                                                if (result.Status == "SUCCESS-Pending Confirmation" && (service.ServiceCode == "FitShow" || service.ServiceCode == "Soraty"))
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
                                                    var messagesTemplate = entity.MessagesTemplates.ToList();
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
                                                        SharedLibrary.MessageHandler.InsertMessageToQueue(entity, messageObj);
                                                    }
                                                }
                                            }
                                            SharedLibrary.MessageHandler.OtpLogUpdate(service, logId, result.Status.ToString());
                                        }
                                    }
                                }




                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in OtpCharge method: ", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:OtpCharge,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpConfirm([FromBody]MessageObject messageObj)
        {
            logs.Info("AppController:OtpConfirm,Start," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            dynamic result = new ExpandoObject();
            try
            {
                if (messageObj.Number != null)
                {
                    messageObj.MobileNumber = messageObj.Number;
                    result.Number = messageObj.MobileNumber;
                }
                else
                    result.MobileNumber = messageObj.MobileNumber;
                var hash = SharedLibrary.Security.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
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
                        result.Status = "Invalid Mobile Number";
                    else if (messageObj.MobileNumber == "Invalid Number")
                        result.Status = "Invalid Number";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid Service Code";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                            if (messageObj.Content != null)
                            {
                                using (var entity = new PortalEntities())
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
                                        IsReceivedFromWeb = false,
                                        ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-OtpConfirm" : null
                                    };
                                    entity.ReceievedMessages.Add(mo);
                                    entity.SaveChanges();
                                }
                            }
                            List<string> servicesNames = new List<string> { "Phantom", "TajoTakht", "Hazaran", "LahzeyeAkhar","Soltan"
                                ,"MenchBaz","DonyayeAsatir","Aseman","AvvalPod","AvvalPod500","AvvalYad","BehAmooz500"
                                ,"FitShow","JabehAbzar","ShenoYad","ShenoYad500","Takavar","Tamly","Tamly500","Halghe","Dezhban","ShahreKalameh"
                                ,"Soraty","DefendIran","SepidRood","Nebula","Medio","Achar"};
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
                            else if (!servicesNames.Any(o => o == service.ServiceCode))
                            {
                                result.Status = "Service does not defined";
                            }
                            else
                            {
                                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
                                {
                                    var logId = SharedLibrary.MessageHandler.OtpLog(service, messageObj.MobileNumber, "confirm", "webservice-" + messageObj.ConfirmCode);
                                    var singleCharge = SharedLibrary.ServiceHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        //string aggregatorName = "MobinOneMapfa";
                                        string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(service.ServiceCode);
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.OTPConfirmGeneral(aggregatorName, entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                    SharedLibrary.MessageHandler.OtpLogUpdate(service, logId, result.Status.ToString());
                                }

                            }
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
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in OtpConfirm method: ", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            logs.Info("AppController:OtpConfirm,End," + messageObj.ServiceCode + "," + messageObj.MobileNumber);
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> WebMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.Status = "";
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("WebMessage" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
                                IsReceivedFromWeb = false,
                                ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-WebMessage" : null
                            };
                            entity.ReceievedMessages.Add(mo);
                            entity.SaveChanges();
                        }
                        result.Status = "Success";
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in WebMessage: ", e);
                result.Status = "Failed";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
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
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("Register" + messageObj.ServiceCode + messageObj.Number);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
            catch (Exception e)
            {
                logs.Error("Exception in Deregister:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
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
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("Deregister" + messageObj.ServiceCode + messageObj.Token);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
            catch (Exception e)
            {
                logs.Error("Exception in Deregister:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppMessage([FromBody]MessageObject messageObj)
        {
            logs.Info("1");
            dynamic result = new ExpandoObject();
            try
            {
                result.Status = "error";
                var hash = SharedLibrary.Security.GetSha256Hash("AppMessage" + messageObj.ServiceCode + messageObj.MobileNumber + messageObj.Content.Substring(0, 3));
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
            catch (Exception e)
            {
                logs.Error("Exception in AppMessage:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage IsUserSubscribed([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            if (messageObj.Number != null)
            {
                messageObj.MobileNumber = messageObj.Number;
                result.Number = messageObj.MobileNumber;
            }
            else
                result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("IsUserSubscribed" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.NormalizeContent(messageObj.MobileNumber);
                    if (messageObj.Number != null)
                    {
                        messageObj.Number = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Number);
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateLandLineNumber(messageObj.Number);
                    }
                    else
                        messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);

                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (messageObj.MobileNumber == "Invalid Number")
                        result.Status = "Invalid Number";
                    else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                        {
                            result.Status = "Invalid ServiceCode";
                        }
                        else
                        {
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, service.Id);
                            if (subscriber != null && subscriber.DeactivationDate == null)
                                result.Status = "Subscribed";
                            else
                                result.Status = "NotSubscribed";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsUserSubscribed:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Verification([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("Verification" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
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
                                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, 10004);
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
            catch (Exception e)
            {
                logs.Error("Exception in Verification:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubscriberStatus([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("SubscriberStatus" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
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
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
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
                                using (var entity = new TahChinLibrary.Models.TahChinEntities())
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
                                using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
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
                                using (var entity = new DambelLibrary.Models.DambelEntities())
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
                                using (var entity = new MedadLibrary.Models.MedadEntities())
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
                                using (var entity = new PorShetabLibrary.Models.PorShetabEntities())
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
            catch (Exception e)
            {
                logs.Error("Exception in SubscriberStatus:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetIrancellOtpUrl(string serviceCode, string callBackParam)
        {
            dynamic result = new ExpandoObject();
            try
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
            catch (Exception e)
            {
                logs.Error("Exception in GetIrancellOtpUrl:" + e);
                result.Status = "Error";
                result.Description = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetIrancellUnsubUrl(string serviceCode, string callBackParam)
        {
            dynamic result = new ExpandoObject();
            try
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
            catch (Exception e)
            {
                logs.Error("Exception in GetIrancellOtpUrl:" + e);
                result.Status = "Error";
                result.Description = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage DecryptIrancellMessage(string data)
        {
            dynamic result = new ExpandoObject();
            try
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
            catch (Exception e)
            {
                logs.Error("Exception in DecryptIrancellMessage:" + e);
                result.Status = "Error";
                result.Description = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
