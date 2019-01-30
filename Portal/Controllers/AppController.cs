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
        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppChargeUser([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "shortcode",messageObj.ShortCode }
                                                            ,{ "content",messageObj.Content}
                                                            ,{ "mobile",messageObj.MobileNumber}}, null, "Portal:AppController:AppChargeUser");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                    {
                        result.status = "Invalid Mobile Number";
                    }
                    else
                    {
                        if (!AppMessageAllowedServiceCode.Contains(messageObj.ServiceCode))
                        {
                            result.status = "This ServiceCode does not have permission";
                        }
                        else
                        {
                            if (messageObj.ShortCode == null || messageObj.ShortCode == "")
                            {
                                result.status = "Invalid ShortCode";
                            }
                            else
                            {
                                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                                messageObj.Content = SharedLibrary.MessageHandler.NormalizeContent(messageObj.Content);
                                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                                messageObj.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                                messageObj.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                                messageObj.IsReceivedFromIntegratedPanel = false;
                                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                                if (service == null)
                                {
                                    result.status = "Invalid ServiceCode";
                                }
                                else
                                {
                                    messageObj.ServiceId = service.Id;
                                    if (messageObj.Price == null)
                                    {
                                        result.status = "Invalid Price";
                                    }
                                    else
                                    {
                                        List<string> services = new List<string> { "Soltan", "DonyayeAsatir", "ShahreKalameh" };
                                        if (services.Any(o => o == service.ServiceCode))
                                        {
                                            var singlecharge = SharedShortCodeServiceLibrary.HandleMo.ReceivedMessageForSingleCharge(messageObj, service);
                                            if (singlecharge == null)
                                                result.status = "Error in Charging";
                                            else
                                            {
                                                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
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
                                        //if (message.ServiceCode == "Soltan")
                                        //{
                                        //    var singlecharge = SoltanLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                        //    if (singlecharge == null)
                                        //        result.status = "Error in Charging";
                                        //    else
                                        //    {
                                        //        using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                        //        {
                                        //            entity.Singlecharges.Attach(singlecharge);
                                        //            singlecharge.IsCalledFromInAppPurchase = true;
                                        //            entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                        //            entity.SaveChanges();
                                        //        }
                                        //        if (singlecharge.IsSucceeded == true)
                                        //            result.status = "Success";
                                        //        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                        //            result.status = "Insufficient Balance";
                                        //    }
                                        //}
                                        //else if (message.ServiceCode == "DonyayeAsatir")
                                        //{
                                        //    var singlecharge = DonyayeAsatirLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                        //    if (singlecharge == null)
                                        //        result.status = "Error in Charging";
                                        //    else
                                        //    {
                                        //        using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                        //        {
                                        //            entity.Singlecharges.Attach(singlecharge);
                                        //            singlecharge.IsCalledFromInAppPurchase = true;
                                        //            entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                        //            entity.SaveChanges();
                                        //        }
                                        //        if (singlecharge.IsSucceeded == true)
                                        //            result.status = "Success";
                                        //        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                        //            result.status = "Insufficient Balance";
                                        //    }
                                        //}
                                        //else if (message.ServiceCode == "ShahreKalameh")
                                        //{
                                        //    var singlecharge = ShahreKalamehLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                                        //    if (singlecharge == null)
                                        //        result.status = "Error in Charging";
                                        //    else
                                        //    {
                                        //        using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                        //        {
                                        //            entity.Singlecharges.Attach(singlecharge);
                                        //            singlecharge.IsCalledFromInAppPurchase = true;
                                        //            entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                                        //            entity.SaveChanges();
                                        //        }
                                        //        if (singlecharge.IsSucceeded == true)
                                        //            result.status = "Success";
                                        //        else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient Balance"))
                                        //            result = "Insufficient Balance";
                                        //        else
                                        //            result.status = "Unknown error";
                                        //    }
                                        //}
                                        else
                                            result = "General Error in AppChargeUser";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //logs.Error(e);
                //result = e.Message;
                logs.Error("Portal:AppController:AppChargeUser", e);
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
        public HttpResponseMessage AppMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                     , new Dictionary<string, string>() { { "shortcode",messageObj.To }
                                                            ,{ "content",messageObj.Message}
                                                            ,{ "mobile",(string.IsNullOrEmpty(messageObj.Address) ? messageObj.From : messageObj.Address)}}
                    , null, "Portal:AppController:AppMessage");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
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
                }
            }
            catch (Exception e)
            {
                //logs.Error(e);
                //result = e.Message;
                logs.Error("Portal:AppController:AppMessage", e);
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
        public HttpResponseMessage Verification([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "mobile",messageObj.MobileNumber}}
                    , null, "Portal:AppController:Verification");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

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
                }
            }
            catch (Exception e)
            {
                //result = e.Message;
                //logs.Error(e);
                logs.Error("Portal:AppController:Verification", e);
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
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() { { "servicecode",messageObj.ServiceCode }
                                                            ,{ "mobile",messageObj.MobileNumber}}
                    , null, "Portal:AppController:SubscriberStatus");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
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
                                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Soltan"))
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) <= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("DonyayeAsatir"))
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) <= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "ShahreKalameh")
                            {
                                using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("ShahreKalameh"))
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
                }
            }
            catch (Exception e)
            {
                //logs.Error(e);
                //result = e.Message;
                logs.Error("Portal:AppController:SubscriberStatus", e);
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
