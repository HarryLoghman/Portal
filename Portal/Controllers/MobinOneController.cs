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
using System.Net.Http.Formatting;

namespace Portal.Controllers
{
    public class MobinOneController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Dictionary<string, string> mobinOneMciServiceIds = new Dictionary<string, string>()
            {
                { "8071", "Nebula" },
            };

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message(string sender, string scode, string text)
        {
            string result = "";
            var messageObj = new MessageObject();
            messageObj.MobileNumber = sender;
            messageObj.ShortCode = scode;
            messageObj.Content = text;
            logs.Info("MobinOne Controller Notification : " + sender);
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "1";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Delivery(string requestId, string receiver, string status)
        {

            var result = "";
            try
            {
                logs.Info("MobinOne Controller Delivery:" + "requestId=" + requestId + ",receiver=" + receiver + ",status=" + status);
                string shortCode;
                SharedLibrary.MessageSender.sb_processCorrelator(requestId, ref receiver, out shortCode);
                var MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(receiver);
                if (MobileNumber == "Invalid Mobile Number")
                {
                    result = MobileNumber;
                }

                var delivery = new SharedLibrary.Models.Delivery();
                delivery.AggregatorId = 8;
                delivery.Correlator = requestId;
                if (status == "DeliveredToTerminal")
                    delivery.Delivered = true;
                else delivery.Delivered = false;

                delivery.DeliveryTime = DateTime.Now;
                delivery.Description = status;
                delivery.IsProcessed = false;
                delivery.MobileNumber = MobileNumber;
                delivery.ReferenceId = null;
                delivery.ShortCode = shortCode;
                delivery.Status = status;

                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    portal.Deliveries.Add(delivery);
                }
                //delivery.Delivered
            }
            catch (Exception e)
            {
                logs.Error("MobinOne Controller Exception in Delivery:", e);
            }

            //var delivery = new DeliveryObject();
            //delivery.ReferenceId = requestId;
            //delivery.Status = status;
            //delivery.AggregatorId = 8;
            //SharedLibrary.MessageHandler.SaveDeliveryStatus(delivery);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;

        }

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Notification()
        {
            var result = "1";
            var message = new MessageObject();
            var queryString = this.Request.GetQueryNameValuePairs();
            var sid = queryString.FirstOrDefault(o => o.Key == "sid").Value;
            var msisdn = queryString.FirstOrDefault(o => o.Key == "msisdn").Value;
            var keyword = queryString.FirstOrDefault(o => o.Key == "keyword").Value;
            var eventType = queryString.FirstOrDefault(o => o.Key == "event-type").Value;
            var status = queryString.FirstOrDefault(o => o.Key == "status").Value;
            logs.Info("MobinOne Controller Notification : " + msisdn);
            //var shortcode = queryString.FirstOrDefault(o => o.Key == "shortcode").Value;

            message.ShortCode = SharedLibrary.ServiceHandler.GetShortCodeFromOperatorServiceId(sid);
            if (message.ShortCode != null)
            {
                if (eventType == "1.2")
                {
                    message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                    if (message.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        var recievedMessage = new MessageObject();
                        recievedMessage.Content = keyword;
                        recievedMessage.MobileNumber = message.MobileNumber;
                        recievedMessage.ShortCode = message.ShortCode;
                        recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Unsubscribe" : null;
                        recievedMessage.IsReceivedFromIntegratedPanel = false;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                        result = "1";
                    }
                }
                else if (eventType == "1.1")
                {
                    message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                    if (message.MobileNumber == "Invalid Mobile Number")
                        result = "-1";
                    else
                    {
                        var recievedMessage = new MessageObject();
                        recievedMessage.Content = keyword;
                        recievedMessage.MobileNumber = message.MobileNumber;
                        recievedMessage.ShortCode = message.ShortCode;
                        recievedMessage.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromIMI-Register" : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
                        result = "1";
                    }
                }
                else if (eventType == "1.5")
                {
                    if (message.ShortCode == "307382")
                    {
                        using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Nebula"))
                        {
                            var singlecharge = new SharedLibrary.Models.ServiceModel.Singlecharge();
                            singlecharge.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(msisdn);
                            singlecharge.DateCreated = DateTime.Now;
                            singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            singlecharge.Price = 300;
                            if (status == "0")
                                singlecharge.IsSucceeded = true;
                            else
                                singlecharge.IsSucceeded = false;
                            singlecharge.IsApplicationInformed = false;
                            singlecharge.IsCalledFromInAppPurchase = false;
                            var installment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == message.MobileNumber && o.IsUserCanceledTheInstallment == false).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                            if (installment != null)
                                singlecharge.InstallmentId = installment.Id;
                            entity.Singlecharges.Add(singlecharge);
                            entity.SaveChanges();
                        }
                    }
                }
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
