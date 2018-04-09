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
        private List<string> AppChargeUserAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        private List<string> AppMessageAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        private List<string> VerificactionAllowedServiceCode = new List<string>() { "Soltan", "ShahreKalameh", "DonyayeAsatir" };
        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppChargeUser([FromBody]MessageObject message)
        {
            dynamic result = new ExpandoObject();
            message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
            if (message.MobileNumber == "Invalid Mobile Number")
            {
                result.status = "Invalid Mobile Number";
            }
            else
            {
                if (!AppMessageAllowedServiceCode.Contains(message.ServiceCode))
                {
                    result.status = "This ServiceCode does not have permission";
                }
                else
                {
                    if (message.ShortCode == null || message.ShortCode == "")
                    {
                        result.status = "Invalid ShortCode";
                    }
                    else
                    {
                        message.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(message.ShortCode);
                        message.Content = SharedLibrary.MessageHandler.NormalizeContent(message.Content);
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                        message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                        message.IsReceivedFromIntegratedPanel = false;
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(message.ServiceCode);
                        if (service == null)
                        {
                            result.status = "Invalid ServiceCode";
                        }
                        else
                        {
                            message.ServiceId = service.Id;
                            if (message.Price == null)
                            {
                                result.status = "Invalid Price";
                            }
                            else
                            {
                                if (message.ServiceCode == "Soltan")
                                {
                                    var singlecharge = SoltanLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                    if (singlecharge == null)
                                        result.status = "Error in Charging";
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
                                            result.status = "Success";
                                        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                            result.status = "Insufficient Balance";
                                    }
                                }
                                else if (message.ServiceCode == "DonyayeAsatir")
                                {
                                    var singlecharge = DonyayeAsatirLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                    if (singlecharge == null)
                                        result.status = "Error in Charging";
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
                                            result.status = "Success";
                                        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                            result.status = "Insufficient Balance";
                                    }
                                }
                                else if (message.ServiceCode == "ShahreKalameh")
                                {
                                    var singlecharge = ShahreKalamehLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                    if (singlecharge == null)
                                        result.status = "Error in Charging";
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
                                            result.status = "Success";
                                        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                            result = "Insufficient Balance";
                                        else
                                            result.status = "Unknown error";
                                    }
                                }
                                else
                                    result = "General Error in AppChargeUser";
                            }
                        }
                    }
                }
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
                result.status = "Invalid Mobile Number";
            else if (!AppMessageAllowedServiceCode.Contains(messageObj.ServiceCode))
                result.status = "This ServiceCode does not have permission";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromApp" : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result.status = "Success";
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
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result.status = "Invalid Mobile Number";
            else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                result.status = "This ServiceCode does not have permission";
            else
            {
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                if (service == null)
                {
                    result.status = "Invalid ServiceCode";
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
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromAppVerification" : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result.MobileNumber = messageObj.MobileNumber;
                        result.status = "Subscribed";
                        result.ActivationCode = verficationId;
                    }
                    else
                    {
                        messageObj.Content = "SendServiceHelp";
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromAppVerification" : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result.MobileNumber = messageObj.MobileNumber;
                        result.status = "NotSubscribed";
                        result.ActivationCode = null;
                    }
                }
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
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result.status = "Invalid Mobile Number";
            else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                result.status = "This ServiceCode does not have permission";
            else
            {
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                if (service == null)
                {
                    result.status = "Invalid ServiceCode";
                }
                else
                {
                    messageObj.ServiceId = service.Id;
                    messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                    var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                    var daysLeft = 0;
                    if (messageObj.ServiceCode == "Soltan")
                    {
                        using (var entity = new SoltanLibrary.Models.SoltanEntities())
                        {
                            var now = DateTime.Now;
                            var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) <= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days; 
                        }
                    }
                    else if (messageObj.ServiceCode == "DonyayeAsatir")
                    {
                        using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                        {
                            var now = DateTime.Now;
                            var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) <= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                        }
                    }
                    else if (messageObj.ServiceCode == "ShahreKalameh")
                    {
                        using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                        {
                            var now = DateTime.Now;
                            var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) <= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                            daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                        }
                    }
                    if (daysLeft > 0)
                    {
                        result.MobileNumber = messageObj.MobileNumber;
                        result.status = "Active";
                        result.DaysLeft = daysLeft;
                    }
                    else
                    {
                        result.MobileNumber = messageObj.MobileNumber;
                        result.status = "NotActive";
                        result.DaysLeft = null;
                    }
                }
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
