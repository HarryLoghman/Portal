using System;
using System.Linq;
using System.Web.Mvc;
using SepidRoodLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.SepidRood.Controllers
{
    public class SendDirectMessageController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: SepidRood/SendDirectMessage
        private SepidRoodEntities db = new SepidRoodEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "سپید رود";
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
            var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("SepidRood");
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
            messageObject = SepidRoodLibrary.MessageHandler.SetImiChargeInfo(messageObject, price, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            SepidRoodLibrary.MessageHandler.InsertMessageToQueue(messageObject);
            return Content("Ok");
        }
    }
}