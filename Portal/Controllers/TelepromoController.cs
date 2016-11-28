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

namespace Portal.Controllers
{
    public class TelepromoController : ApiController
    {
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Services(string status, string msisdn, string serviceId = null)
        {
            List<dynamic> history = new List<dynamic>();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = "0" + msisdn.Remove(0, 2);
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
                        subscriberHistory.subscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(sub.ActivationDate.GetValueOrDefault());
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
                        subscriberHistory.unsubscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(sub.DeactivationDate.GetValueOrDefault());
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
                            subscriberHistory.unsubscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(sub.DeactivationDate.GetValueOrDefault());
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
                        subscriberHistory.subscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(sub.ActivationDate.GetValueOrDefault());
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
        public HttpResponseMessage UnSubscribe(string msisdn, string serviceId)
        {
            dynamic responseJson = new ExpandoObject();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = "0" + msisdn.Remove(0, 2);
                var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
                var subscriber = entity.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceInfo.ServiceId).FirstOrDefault();
                if (subscriber == null)
                    responseJson.status = 4;
                else
                {
                    if (subscriber.DeactivationDate != null)
                        responseJson.status = 1001;
                    else
                    {
                        var recievedMessage = new MessageObject();
                        recievedMessage.Content = serviceId;
                        recievedMessage.MobileNumber = mobileNumber;
                        recievedMessage.ShortCode = serviceInfo.ShortCode;
                        recievedMessage.IsReceivedFromIntegratedPanel = true;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(recievedMessage);
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
                var mobileNumber = "0" + msisdn.Remove(0, 2);
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
                    newSubscriber.SubscriberUniqueId = SharedLibrary.HandleSubscription.CreateUniqueId();
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
        public HttpResponseMessage History(string msisdn, long fromDate, long toDate, string serviceId = null)
        {
            dynamic responseJson = new ExpandoObject();
            List<dynamic> history = new List<dynamic>();
            using (var entity = new PortalEntities())
            {
                var mobileNumber = "0" + msisdn.Remove(0, 2);
                IQueryable<SubscribersHistory> subscriberHistoryQuery;
                if (serviceId == null || serviceId == "")
                    subscriberHistoryQuery = entity.SubscribersHistories.Where(o => o.MobileNumber == mobileNumber && o.AggregatorId == 3);
                else
                    subscriberHistoryQuery = entity.SubscribersHistories.Where(o => o.MobileNumber == mobileNumber && o.AggregatorId == 3 && o.AggregatorServiceId == serviceId);
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
                            subscriberHistoryObject.subscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(subscriber.DateTime.GetValueOrDefault());
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
                            subscriberHistoryObject.unsubscriptionDate = SharedLibrary.Date.DateTimeToUnixTimestamp(subscriber.DateTime.GetValueOrDefault());
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
                var mobileNumber = "0" + msisdn.Remove(0, 2);
                IQueryable<vw_ReceivedMessages> recievedMessages;
                if (serviceId != null)
                {
                    var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromAggregatorServiceId(serviceId);
                    recievedMessages = entity.vw_ReceivedMessages.Where(o => o.MobileNumber == mobileNumber && o.IsReceivedFromIntegratedPanel == false && o.ShortCode == serviceInfo.ShortCode);
                }
                else
                {
                    var serviceInfo = entity.ServiceInfoes.Where(o => o.AggregatorId == 3).Select(o => o.ShortCode).ToList();
                    recievedMessages = entity.vw_ReceivedMessages.Where(o => o.MobileNumber == mobileNumber && o.IsReceivedFromIntegratedPanel == false && serviceInfo.Contains(o.ShortCode));
                }
                var recievedMessagesList = recievedMessages.ToList();
                foreach (var recievedMessage in recievedMessagesList)
                {
                    dynamic receiveEvent = new ExpandoObject();
                    receiveEvent.type = 0;
                    receiveEvent.shortCode = "98" + recievedMessage.ShortCode;
                    receiveEvent.service = null;
                    receiveEvent.date = SharedLibrary.Date.DateTimeToUnixTimestamp(recievedMessage.ReceivedTime);
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
    }
}
