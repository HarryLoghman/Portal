﻿using System;
using System.Linq;
using System.Web.Mvc;
using Tabriz2018Library.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Tabriz2018.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SendDirectMessageController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Tabriz2018/SendDirectMessage
        private Tabriz2018Entities db = new Tabriz2018Entities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "تبریز 2018";
            return View();
        }
        

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Send([DataSourceRequest]DataSourceRequest request)
        {
            var mobileNumber = Request["mobile"];
            var content = Request["message"];
            var price = Convert.ToInt32(Request["price"]);
            var point = Convert.ToInt32(Request["point"]);
            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Tabriz2018");
            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
            var messageObject = new SharedLibrary.Models.MessageObject();
            messageObject.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
            messageObject.MobileNumber = mobileNumber;
            messageObject.Content = content;
            messageObject.Price = price;
            messageObject.Point = point;
            messageObject.ServiceId = service.Id;
            messageObject.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
            messageObject.AggregatorId = serviceInfo.AggregatorId;
            messageObject.SubscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(messageObject.MobileNumber, messageObject.ServiceId);
            if ((messageObject.SubscriberId == null || messageObject.SubscriberId == 0) && messageObject.Point != 0)
            {
                return Content("Cant add point to unsubscribed");
            }
            messageObject = Tabriz2018Library.MessageHandler.SetImiChargeInfo(messageObject, price, 0, null);
            Tabriz2018Library.MessageHandler.InsertMessageToQueue(messageObject);
            return Content("Ok");
        }
    }
}