using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Portal.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.Danestan.Controllers
{
    public class MonitoringController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Danestan/Monitoring
        private DanestanEntities db = new DanestanEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "دانستان";
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
            if(messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.AutochargeMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.InQueue).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.TryingToSend);
            else
                db.EventbaseMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.InQueue).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.TryingToSend);
            db.SaveChanges();
            return Content("Ok");
        }

        public ActionResult PauseSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            if (messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.AutochargeMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.Paused);
            else
                db.EventbaseMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.TryingToSend).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.Paused);
            db.SaveChanges();
            return Content("Ok");
        }

        public ActionResult ResumeSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            if (messageType == (int)Shared.MessageHandler.MessageType.AutoCharge)
                db.AutochargeMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.Paused).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.TryingToSend);
            else
                db.EventbaseMessagesBuffers.Where(o => o.Id == contentId && o.MessageType == messageType && o.ProcessStatus == (int)Shared.MessageHandler.ProcessStatus.Paused).ToList().ForEach(o => o.ProcessStatus = (int)Shared.MessageHandler.ProcessStatus.TryingToSend);
            db.SaveChanges();
            return Content("Ok");
        }
    }
}