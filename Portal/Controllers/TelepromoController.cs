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
using System.ComponentModel;

namespace Portal.Controllers
{
    public class TelepromoController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message(string da, string oa, string txt)
        {
            if (da == "989168623674" || da == "989195411097")
            {
                var blackListResponse = new HttpResponseMessage(HttpStatusCode.OK);
                blackListResponse.Content = new StringContent("", System.Text.Encoding.UTF8, "text/plain");
                return blackListResponse;
            }
            var messageObj = new MessageObject();
            messageObj.MobileNumber = da;
            messageObj.ShortCode = oa;
            messageObj.Content = txt;

            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            string result = "";
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                if (messageObj.ShortCode == "307235" || messageObj.ShortCode == "307251" || messageObj.ShortCode == "3072316" || messageObj.ShortCode == "3072326")
                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-" : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage MessagePost([FromBody] dynamic input)
        {
            logs.Info(input);
            dynamic responseJson = new ExpandoObject();
            string msisdn = input.msisdn;
            string shortCode = input.shortcode;
            string message = input.message;
            string partnerName = input.partnername;
            //string transId = input.trans_id;//no map field in db
            //string dateTimeStr = input.datetime;//set while saving to db
            if (msisdn == "989168623674" || msisdn == "989195411097")
            {
                var blackListResponse = new HttpResponseMessage(HttpStatusCode.OK);
                blackListResponse.Content = new StringContent("", System.Text.Encoding.UTF8, "text/plain");
                return blackListResponse;
            }
            var messageObj = new MessageObject();
            
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
            messageObj.ShortCode = shortCode;
            messageObj.IsReceivedFromIntegratedPanel = false;
            messageObj.Content = message;
            
            string result = "";
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                if (messageObj.ShortCode == "307235" || messageObj.ShortCode == "307251" || messageObj.ShortCode == "3072316" || messageObj.ShortCode == "3072326"
                    || messageObj.ShortCode== "3072428")
                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-" : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        // /Telepromo/Delivery?refId=44353535&deliveryStatus=0
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string refId, string deliveryStatus)
        {
            //var delivery = new DeliveryObject();
            //delivery.ReferenceId = refId;
            //delivery.Status = deliveryStatus;
            //delivery.AggregatorId = 5;
            //SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Services(string status, string msisdn, string serviceId = null)
        {
            List<dynamic> history = new List<dynamic>();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                IQueryable<SubscribersHistory> subscriberServices;
                if (serviceId == null || serviceId == "")
                    subscriberServices = entity.SubscribersHistories.Where(o => o.AggregatorId == 5 && o.MobileNumber == mobileNumber);
                else
                    subscriberServices = entity.SubscribersHistories.Where(o => o.AggregatorId == 5 && o.MobileNumber == mobileNumber && o.AggregatorServiceId == serviceId);
                var serviceIds = subscriberServices.GroupBy(o => o.ServiceId).ToList().Select(o => o.First()).ToList();
                foreach (var service in serviceIds)
                {
                    dynamic subscriberHistory = new ExpandoObject();
                    subscriberHistory.service = service.AggregatorServiceId;

                    IQueryable<Subscriber> subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == service.ServiceId);
                    if (status == "1")
                    {
                        var sub = subscriber.FirstOrDefault(o => o.DeactivationDate == null);
                        if (sub == null)
                            continue;
                        subscriberHistory.subscriptionShortCode = "98" + service.ShortCode;
                        subscriberHistory.subscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(sub.ActivationDate.GetValueOrDefault());
                        subscriberHistory.subscriptionKeyword = sub.OnKeyword;
                        subscriberHistory.enabled = 0;
                        if (sub.OnMethod == "keyword")
                            subscriberHistory.subscriptionMethod = 1;
                        else if (sub.OnMethod == "Integrated Panel")
                            subscriberHistory.subscriptionMethod = 2;
                        else
                            subscriberHistory.subscriptionMethod = 3;
                    }
                    else if (status == "2")
                    {
                        var sub = subscriber.FirstOrDefault(o => o.DeactivationDate != null);
                        if (sub == null)
                            continue;
                        subscriberHistory.unsubscriptionShortCode = "98" + service.ShortCode;
                        subscriberHistory.unsubscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(sub.DeactivationDate.GetValueOrDefault());
                        subscriberHistory.unsubscriptionKeyword = sub.OnKeyword;
                        subscriberHistory.enabled = 1;
                        if (sub.OffMethod == "keyword")
                            subscriberHistory.unsubscriptionMethod = 1;
                        else if (sub.OffMethod == "Integrated Panel")
                            subscriberHistory.unsubscriptionMethod = 2;
                        else
                            subscriberHistory.unsubscriptionMethod = 3;
                    }
                    else
                    {
                        var sub = subscriber.FirstOrDefault();
                        if (sub == null)
                            continue;
                        if (sub.DeactivationDate != null)
                        {
                            subscriberHistory.unsubscriptionShortCode = "98" + service.ShortCode;
                            subscriberHistory.unsubscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(sub.DeactivationDate.GetValueOrDefault());
                            subscriberHistory.unsubscriptionKeyword = sub.OnKeyword;
                            subscriberHistory.enabled = 1;
                            if (sub.OffMethod == "keyword")
                                subscriberHistory.unsubscriptionMethod = 1;
                            else if (sub.OffMethod == "Integrated Panel")
                                subscriberHistory.unsubscriptionMethod = 2;
                            else
                                subscriberHistory.unsubscriptionMethod = 3;
                        }
                        subscriberHistory.subscriptionShortCode = "98" + service.ShortCode;
                        subscriberHistory.subscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(sub.ActivationDate.GetValueOrDefault());
                        subscriberHistory.subscriptionKeyword = sub.OnKeyword;
                        subscriberHistory.enabled = 0;
                        if (sub.OnMethod == "keyword")
                            subscriberHistory.subscriptionMethod = 1;
                        else if (sub.OnMethod == "Integrated Panel")
                            subscriberHistory.subscriptionMethod = 2;
                        else
                            subscriberHistory.subscriptionMethod = 3;
                    }

                    history.Add(subscriberHistory);
                }
            }
            dynamic responseJson = new ExpandoObject();
            responseJson.status = 0;
            responseJson.result = history;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Notify(string type, string msisdn, string serviceId, string channel, string keyword, string eventId = null)
        {
            dynamic responseJson = new ExpandoObject();

            if (type == "RENEWAL" || type == "RENEW")
            {
                //var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                //if (service.ServiceCode == "JabehAbzar")
                //{
                //    using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                //    {
                //        var singlecharge = new JabehAbzarLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Tamly")
                //{
                //    using (var entity = new TamlyLibrary.Models.TamlyEntities())
                //    {
                //        var singlecharge = new TamlyLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Soltan")
                //{
                //    using (var entity = new SoltanLibrary.Models.SoltanEntities())
                //    {
                //        var singlecharge = new SoltanLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "DonyayeAsatir")
                //{
                //    using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                //    {
                //        var singlecharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "ShenoYad")
                //{
                //    using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                //    {
                //        var singlecharge = new ShenoYadLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "FitShow")
                //{
                //    using (var entity = new FitShowLibrary.Models.FitShowEntities())
                //    {
                //        var singlecharge = new FitShowLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Takavar")
                //{
                //    using (var entity = new TakavarLibrary.Models.TakavarEntities())
                //    {
                //        var singlecharge = new TakavarLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "AvvalPod")
                //{
                //    using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                //    {
                //        var singlecharge = new AvvalPodLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "AvvalYad")
                //{
                //    using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                //    {
                //        var singlecharge = new AvvalYadLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
            }
            else
            {
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
                var message = new SharedLibrary.Models.MessageObject();
                message.MobileNumber = mobileNumber;
                message.ShortCode = serviceInfo.ShortCode;
                message.IsReceivedFromIntegratedPanel = false;
                message.Content = keyword;
                message.ServiceId = serviceInfo.ServiceId;
                if (type == "SUBSCRIBE")
                {
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356"
                        || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI-Register" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Register" : null;
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
                else if (type == "UNSUBSCRIBE")
                {
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356" || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI-Unsubscribe" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Unsubscribe" : null;
                    if (message.Content == null || message.Content.ToLower() == "null")
                    {
                        message.IsReceivedFromIntegratedPanel = true;
                        message.Content = "Off";
                    }
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
                else
                {
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356" || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI" : null;
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
            }

            responseJson.status = 0;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage NotifyPost([FromBody]dynamic input)
        {
            logs.Info(input);
            dynamic responseJson = new ExpandoObject();
            string msisdn = input.msisdn;
            string serviceId = input.serviceid;
            //string transId = input.trans_id;//no map field in db
            //string transStatus = input.trans_status;//no map field in db
            //string channel = input.channel;//no map field in db
            string shortCode = input.shortcode;
            string keyword = input.keyword;
            //string dateTimeStr = input.datetime;//set while saving to db
            //string chargeCode = input.chargecode;//no map field in db
            //string basePricePoint = input.base_price_point;//no map field in db
            //string baseBilledPoint = input.billed_price_point;//no map field in db
            //string eventType = input.event_type;//no map field in db
            string status = input.status;
            //string validity = input.validity; //no map field in db
            //string nextRenewalDate = input.next_renewal_date; //no map field in db


            if (status != "0" && status != "5")
            {
                //var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                //if (service.ServiceCode == "JabehAbzar")
                //{
                //    using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                //    {
                //        var singlecharge = new JabehAbzarLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Tamly")
                //{
                //    using (var entity = new TamlyLibrary.Models.TamlyEntities())
                //    {
                //        var singlecharge = new TamlyLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Soltan")
                //{
                //    using (var entity = new SoltanLibrary.Models.SoltanEntities())
                //    {
                //        var singlecharge = new SoltanLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "DonyayeAsatir")
                //{
                //    using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                //    {
                //        var singlecharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "ShenoYad")
                //{
                //    using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                //    {
                //        var singlecharge = new ShenoYadLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "FitShow")
                //{
                //    using (var entity = new FitShowLibrary.Models.FitShowEntities())
                //    {
                //        var singlecharge = new FitShowLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "Takavar")
                //{
                //    using (var entity = new TakavarLibrary.Models.TakavarEntities())
                //    {
                //        var singlecharge = new TakavarLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "AvvalPod")
                //{
                //    using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                //    {
                //        var singlecharge = new AvvalPodLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
                //else if (service.ServiceCode == "AvvalYad")
                //{
                //    using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                //    {
                //        var singlecharge = new AvvalYadLibrary.Models.Singlecharge();
                //        singlecharge.MobileNumber = message.MobileNumber;
                //        singlecharge.DateCreated = DateTime.Now;
                //        singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                //        singlecharge.Price = 400;
                //        singlecharge.IsSucceeded = true;
                //        singlecharge.IsApplicationInformed = false;
                //        singlecharge.IsCalledFromInAppPurchase = false;
                //        var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                //        if (installment != null)
                //            singlecharge.InstallmentId = installment.Id;
                //        entity.Singlecharges.Add(singlecharge);
                //        entity.SaveChanges();
                //    }
                //}
            }
            else
            {//status ==0 or status ==5
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromOperatorServiceId(serviceId);

                var message = new SharedLibrary.Models.MessageObject();
                message.MobileNumber = mobileNumber;
                message.ShortCode = serviceInfo.ShortCode;
                message.IsReceivedFromIntegratedPanel = false;
                message.Content = keyword;
                message.ServiceId = serviceInfo.ServiceId;

                if (status == "0" || status == "SUBSCRIBE")
                {//sub
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356"
                        || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c"
                        || serviceInfo.AggregatorServiceId == "6c9eb6912781471d88b2b3d367c54f89")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI-Register" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Register" : null;
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
                else if (status == "5" || status == "UNSUBSCRIBE")
                {//unsub
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356"
                        || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c"
                        || serviceInfo.AggregatorServiceId == "6c9eb6912781471d88b2b3d367c54f89")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI-Unsubscribe" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Unsubscribe" : null;
                    if (message.Content == null || message.Content.ToLower() == "null")
                    {
                        message.IsReceivedFromIntegratedPanel = true;
                        message.Content = "Off";
                    }
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
                else
                {
                    if (serviceInfo.AggregatorServiceId == "99ae330a73b14ef085594ee348aaa06b" || serviceInfo.AggregatorServiceId == "441faa36103e44b2b2d69de90d195356" 
                        || serviceInfo.AggregatorServiceId == "1eeed64ecd6c4148bf11574e1a472cd1" || serviceInfo.AggregatorServiceId == "a9a395e997ba46168bf11cefef08018c"
                        || serviceInfo.AggregatorServiceId == "6c9eb6912781471d88b2b3d367c54f89")
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-FromIMI" : null;
                    else
                        message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI" : null;
                    SharedLibrary.MessageHandler.SaveReceivedMessage(message);
                }
            }

            responseJson.status = 0;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage UnSubscribe(string msisdn, string serviceId)
        {
            dynamic responseJson = new ExpandoObject();
            Subscriber subscriber;
            var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
            using (var entity = new PortalEntities())
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceInfo.ServiceId).FirstOrDefault();
            }
            if (subscriber == null)
                responseJson.status = 4;
            else
            {
                if (subscriber.DeactivationDate != null)
                    responseJson.status = 1001;
                else
                {
                    var message = new SharedLibrary.Models.MessageObject();
                    message.MobileNumber = mobileNumber;
                    message.ShortCode = serviceInfo.ShortCode;
                    message.IsReceivedFromIntegratedPanel = true;
                    message.Content = "off";
                    message.ServiceId = serviceInfo.ServiceId;
                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                    message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                    message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                    if (service.ServiceCode == "Soltan")
                        SoltanLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "JabehAbzar")
                        JabehAbzarLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "Tamly")
                        TamlyLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "DonyayeAsatir")
                        DonyayeAsatirLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "ShenoYad")
                        ShenoYadLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "FitShow")
                        FitShowLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "Takavar")
                        TakavarLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "AvvalPod")
                        AvvalPodLibrary.HandleMo.ReceivedMessage(message, service);
                    else if (service.ServiceCode == "AvvalYad")
                        AvvalYadLibrary.HandleMo.ReceivedMessage(message, service);
                    //var recievedMessage = new MessageObject();
                    //recievedMessage.Content = serviceId;
                    //recievedMessage.MobileNumber = mobileNumber;
                    //recievedMessage.ShortCode = serviceInfo.ShortCode;
                    //recievedMessage.IsReceivedFromIntegratedPanel = true;
                    //SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);

                    //subscriber.DeactivationDate = DateTime.Now;
                    //subscriber.PersianDeactivationDate = SharedLibrary.Date.GetPersianDateTime();
                    //subscriber.OffMethod = "Integrated Panel";
                    //subscriber.OffKeyword = "Integrated Panel";
                    //entity.Entry(subscriber).State = System.Data.Entity.EntityState.Modified;
                    //entity.SaveChanges();
                    //var message = new MessageObject();
                    //message.MobileNumber = mobileNumber;
                    //message.ShortCode = serviceInfo.ShortCode;
                    //message.IsReceivedFromIntegratedPanel = true;
                    //message.Content = "Integrated Panel";
                    //var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                    //SharedLibrary.HandleSubscription.AddToSubscriberHistory(message, service, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated, SharedLibrary.HandleSubscription.WhoChangedSubscriberState.IntegratedPanel, null, serviceInfo);
                    responseJson.status = 0;
                }
            }
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Subscribe(string msisdn, string serviceId)
        {
            dynamic responseJson = new ExpandoObject();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
                var subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceInfo.ServiceId).FirstOrDefault();
                if (subscriber != null)
                {
                    if (subscriber.DeactivationDate == null)
                        responseJson.status = 1000;
                    else
                    {
                        subscriber.OffMethod = null;
                        subscriber.OffKeyword = null;
                        subscriber.DeactivationDate = null;
                        subscriber.PersianDeactivationDate = null;
                        subscriber.OnKeyword = "Integrated Panel";
                        subscriber.OnMethod = "Integrated Panel";
                        entity.Entry(subscriber).State = System.Data.Entity.EntityState.Modified;
                        entity.SaveChanges();
                        var message = new MessageObject();
                        message.MobileNumber = mobileNumber;
                        message.ShortCode = serviceInfo.ShortCode;
                        message.IsReceivedFromIntegratedPanel = true;
                        message.Content = "Integrated Panel";
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                        SharedLibrary.HandleSubscription.AddToSubscriberHistory(message, service, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated, SharedLibrary.HandleSubscription.WhoChangedSubscriberState.IntegratedPanel, null, serviceInfo);
                        responseJson.status = 0;
                    }
                }
                else
                {
                    var newSubscriber = new Subscriber();
                    newSubscriber.MobileNumber = mobileNumber;
                    newSubscriber.OnKeyword = "Integrated Panel";
                    newSubscriber.OnMethod = "Integrated Panel";
                    newSubscriber.ServiceId = serviceInfo.ServiceId;
                    newSubscriber.ActivationDate = DateTime.Now;
                    newSubscriber.PersianActivationDate = SharedLibrary.Date.GetPersianDate();
                    newSubscriber.MobileOperator = 1;
                    newSubscriber.OperatorPlan = 2;
                    newSubscriber.SubscriberUniqueId = "";// SharedLibrary.HandleSubscription.CreateUniqueId();
                    entity.Subscribers.Add(newSubscriber);
                    entity.SaveChanges();
                    var service = SharedLibrary.ServiceHandler.GetServiceFromServiceId(serviceInfo.ServiceId);
                    SharedLibrary.HandleSubscription.AddSubscriberToSubscriberPointsTable(newSubscriber, service);
                    var message = new MessageObject();
                    message.MobileNumber = mobileNumber;
                    message.ShortCode = serviceInfo.ShortCode;
                    message.IsReceivedFromIntegratedPanel = true;
                    message.Content = "Integrated Panel";
                    SharedLibrary.HandleSubscription.AddToSubscriberHistory(message, service, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated, SharedLibrary.HandleSubscription.WhoChangedSubscriberState.IntegratedPanel, null, serviceInfo);
                    responseJson.status = 0;
                }
            }
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage History(string msisdn, long fromDate = 0, long toDate = 0, string serviceId = null)
        {
            dynamic responseJson = new ExpandoObject();
            List<dynamic> history = new List<dynamic>();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                DateTime? from = null;
                DateTime? to = null;
                if (fromDate != 0)
                    from = SharedLibrary.Date.UnixTimeStampToDateTime(fromDate);
                if (toDate != 0)
                    to = SharedLibrary.Date.UnixTimeStampToDateTime(toDate);

                IQueryable<SubscribersHistory> subscriberHistoryQuery;
                if (serviceId == null || serviceId == "")
                    subscriberHistoryQuery = entity.SubscribersHistories.Where(o => o.MobileNumber == mobileNumber && o.AggregatorId == 5);
                else
                    subscriberHistoryQuery = entity.SubscribersHistories.Where(o => o.MobileNumber == mobileNumber && o.AggregatorId == 5 && o.AggregatorServiceId == serviceId);

                if (from != null && to != null)
                    subscriberHistoryQuery = subscriberHistoryQuery.Where(o => o.DateTime >= from && o.DateTime <= to);
                else if (from != null && to == null)
                    subscriberHistoryQuery = subscriberHistoryQuery.Where(o => o.DateTime >= from);
                else if (from == null && to != null)
                    subscriberHistoryQuery = subscriberHistoryQuery.Where(o => o.DateTime <= to);

                var subscriberHistory = subscriberHistoryQuery.ToList();
                if (subscriberHistory == null)
                    responseJson.status = 4;
                else
                {
                    foreach (var subscriber in subscriberHistory)
                    {
                        dynamic subscriberHistoryObject = new ExpandoObject();
                        subscriberHistoryObject.service = subscriber.AggregatorServiceId;
                        if (subscriber.SubscriptionKeyword != null && subscriber.SubscriptionKeyword != "")
                        {
                            subscriberHistoryObject.subscriptionShortCode = "98" + subscriber.ShortCode;
                            subscriberHistoryObject.subscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(subscriber.DateTime.GetValueOrDefault());
                            subscriberHistoryObject.subscriptionKeyword = subscriber.SubscriptionKeyword;
                            subscriberHistoryObject.enabled = 0;
                            if (subscriber.WhoChangedSubscriberStatus == (int)SharedLibrary.HandleSubscription.WhoChangedSubscriberState.User)
                                subscriberHistoryObject.subscriptionMethod = 1;
                            else if (subscriber.WhoChangedSubscriberStatus == (int)SharedLibrary.HandleSubscription.WhoChangedSubscriberState.IntegratedPanel)
                                subscriberHistoryObject.subscriptionMethod = 2;
                            else
                                subscriberHistoryObject.subscriptionMethod = 3;
                        }
                        else if (subscriber.UnsubscriptionKeyword != null && subscriber.UnsubscriptionKeyword != "")
                        {
                            subscriberHistoryObject.unsubscriptionShortCode = "98" + subscriber.ShortCode;
                            subscriberHistoryObject.unsubscriptionDate = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(subscriber.DateTime.GetValueOrDefault());
                            subscriberHistoryObject.unsubscriptionKeyword = subscriber.UnsubscriptionKeyword;
                            subscriberHistoryObject.enabled = 1;
                            if (subscriber.WhoChangedSubscriberStatus == (int)SharedLibrary.HandleSubscription.WhoChangedSubscriberState.User)
                                subscriberHistoryObject.unsubscriptionMethod = 1;
                            else if (subscriber.WhoChangedSubscriberStatus == (int)SharedLibrary.HandleSubscription.WhoChangedSubscriberState.IntegratedPanel)
                                subscriberHistoryObject.unsubscriptionMethod = 2;
                            else
                                subscriberHistoryObject.unsubscriptionMethod = 3;
                        }
                        history.Add(subscriberHistoryObject);
                    }
                    responseJson.status = 0;
                }
            }
            responseJson.result = history;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Events(string msisdn, string serviceId = null)
        {
            dynamic responseJson = new ExpandoObject();
            List<dynamic> eventsList = new List<dynamic>();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                IQueryable<vw_ReceivedMessages> recievedMessages;
                if (serviceId != null)
                {
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
                    recievedMessages = entity.vw_ReceivedMessages.Where(o => o.MobileNumber == mobileNumber && o.IsReceivedFromIntegratedPanel == false && o.ShortCode == serviceInfo.ShortCode);
                }
                else
                {
                    var serviceInfo = entity.ServiceInfoes.Where(o => o.AggregatorId == 5).Select(o => o.ShortCode).ToList();
                    recievedMessages = entity.vw_ReceivedMessages.Where(o => o.MobileNumber == mobileNumber && o.IsReceivedFromIntegratedPanel == false && serviceInfo.Contains(o.ShortCode));
                }
                var recievedMessagesList = recievedMessages.ToList();
                foreach (var recievedMessage in recievedMessagesList)
                {
                    dynamic receiveEvent = new ExpandoObject();
                    receiveEvent.type = 0;
                    receiveEvent.shortCode = "98" + recievedMessage.ShortCode;
                    receiveEvent.service = null;
                    receiveEvent.date = (long)SharedLibrary.Date.DateTimeToUnixTimestamp(recievedMessage.ReceivedTime);
                    receiveEvent.message = recievedMessage.Content;
                    receiveEvent.chargingCode = null;
                    eventsList.Add(receiveEvent);
                }
            }
            responseJson.status = 0;
            responseJson.result = eventsList;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage PardisMessage([FromBody]dynamic input)
        {
            var messageObj = new MessageObject();
            messageObj.MobileNumber = input.msisdn;
            messageObj.ShortCode = input.shortcode;
            messageObj.Content = input.message;
            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;

            if (input.action == "subscribe" || input.action == "unsubscribe")
            {
                if (input.action == "subscribe")
                    messageObj.ReceivedFrom += "-FromIMI-Register";
                else if (input.action == "unsubscribe")
                    messageObj.ReceivedFrom += "-FromIMI-Unsubscribe";
            }
                
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            string result = "";
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage PardisDelivery([FromBody]dynamic input)
        {
            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage PardisIntegratedPanel([FromBody]dynamic input)
        {
            var result = "";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage DeliveryPost([FromBody]dynamic input)
        {
            dynamic responseJson = new ExpandoObject();
            responseJson.status = 0;
            var json = JsonConvert.SerializeObject(responseJson);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

    }
}
