using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Portal.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.MyLeague.Controllers
{
    public class MonitoringController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MyLeague/Monitoring
        private MyLeagueEntities db = new MyLeagueEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "لیگ من";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Monitoring_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.MessagesMonitorings.ToDataSourceResult(request, messageMonitoring => new
            {
                Id = messageMonitoring.Id,
                ContentId = messageMonitoring.ContentId,
                MessageType = messageMonitoring.MessageType,
                PersianDateCreated = messageMonitoring.PersianDateCreated,
                TotalMessages = messageMonitoring.TotalMessages,
                TotalSuccessfulySended = messageMonitoring.TotalSuccessfulySended,
                TotalFailed = messageMonitoring.TotalFailed,
                TotalWithoutCharge = messageMonitoring.TotalWithoutCharge,
                Status = messageMonitoring.Status,
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        [HttpPost]
        public ActionResult Pdf_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult StartSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var tag = Convert.ToInt32(Request["Tag"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.InQueue, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend);
            else
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.InQueue, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend);

            return Content("Ok");
        }

        public ActionResult PauseSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var tag = Convert.ToInt32(Request["Tag"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend, (int)Portal.Shared.MessageHandler.ProcessStatus.Paused);
            else
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend, (int)Portal.Shared.MessageHandler.ProcessStatus.Paused);

            return Content("Ok");
        }

        public ActionResult ResumeSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var tag = Convert.ToInt32(Request["Tag"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.Paused, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend);
            else
                db.ChangeMessageStatus((int)Portal.Shared.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)Portal.Shared.MessageHandler.ProcessStatus.Paused, (int)Portal.Shared.MessageHandler.ProcessStatus.TryingToSend);

            return Content("Ok");
        }
    }
}