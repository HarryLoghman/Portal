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

namespace Portal.Controllers
{
    public class AppController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> AppChargeUserAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        private List<string> AppMessageAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        private List<string> VerificactionAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        private List<string> TimeBasedServices = new List<string>() { "ShahreKalameh" };
        private List<string> PriceBasedServices = new List<string>() { "Soltan", "DonyayeAsatir" };
        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage ChargeUser([FromBody]MessageObject message)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = message.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("ChargeUser" + message.ServiceCode + message.MobileNumber);
                if (message.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
                    message.Content = message.Price == null ? " " : message.Price.ToString();
                    if (message.MobileNumber == "Invalid Mobile Number")
                    {
                        result.Status = "Invalid Mobile Number";
                    }
                    else
                    {
                        if (!AppMessageAllowedServiceCode.Contains(message.ServiceCode))
                        {
                            result.Status = "This ServiceCode does not have permission";
                        }
                        else
                        {
                            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(message.ServiceCode);
                            if (service == null)
                            {
                                result.Status = "Invalid ServiceCode";
                            }
                            else
                            {
                                message.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                                message = SharedLibrary.MessageHandler.ValidateMessage(message);
                                message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                                message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                                message.IsReceivedFromIntegratedPanel = false;
                                message.ServiceId = service.Id;

                                if (message.Price == null)
                                {
                                    result.Status = "Invalid Price";
                                }
                                else
                                {
                                    message.Content = message.Price.ToString();
                                    if (message.ServiceCode == "Soltan")
                                    {
                                        var singlecharge = SoltanLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                        if (singlecharge == null)
                                            result.Status = "Error in Charging";
                                        else
                                        {
                                            using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                            {
                                                entity.Singlecharges.Attach(singlecharge);
                                                singlecharge.IsCalledFromInAppPurchase = true;
                                                entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                            if (singlecharge.IsSucceeded == true)
                                                result.Status = "Success";
                                            else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                                result.Status = "Insufficient Balance";
                                        }
                                    }
                                    else if (message.ServiceCode == "DonyayeAsatir")
                                    {
                                        var singlecharge = DonyayeAsatirLibrary.ContentManager.HandleSinglechargeContent(message, service, null, DonyayeAsatirLibrary.ServiceHandler.GetServiceMessagesTemplate());
                                        if (singlecharge == null)
                                            result.Status = "Error in Charging";
                                        else
                                        {
                                            using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                            {
                                                entity.Singlecharges.Attach(singlecharge);
                                                singlecharge.IsCalledFromInAppPurchase = true;
                                                entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                            if (singlecharge.IsSucceeded == true)
                                                result.Status = "Success";
                                            else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                                result.Status = "Insufficient Balance";
                                        }
                                    }
                                    else if (message.ServiceCode == "ShahreKalameh")
                                    {
                                        var singlecharge = ShahreKalamehLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                        if (singlecharge == null)
                                            result.Status = "Error in Charging";
                                        else
                                        {
                                            using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                            {
                                                entity.Singlecharges.Attach(singlecharge);
                                                singlecharge.IsCalledFromInAppPurchase = true;
                                                entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                                entity.SaveChanges();
                                            }
                                            if (singlecharge.IsSucceeded == true)
                                                result.Status = "Success";
                                            else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                                result = "Insufficient Balance";
                                            else
                                                result.Status = "Unknown error";
                                        }
                                    }
                                    else
                                        result.Status = "General Error in AppChargeUser";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AppChargeUser:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("AppMessage" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
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
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (!AppMessageAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
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
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
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
                            messageObj.ServiceId = service.Id;
                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                            if (subscriber != null && subscriber.DeactivationDate == null)
                            {
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
                                messageObj.Content = "SendServiceHelp";
                                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-Verification" : null;
                                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                result.Status = "NotSubscribed";
                                result.ActivationCode = null;
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
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
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
                            messageObj.ServiceId = service.Id;
                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                            var daysLeft = 0;
                            var pricePayed = -1;
                            if (messageObj.ServiceCode == "Soltan")
                            {
                                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        daysLeft = 0;
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
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
                                using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        daysLeft = 0;
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
    }
}
