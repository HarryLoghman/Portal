﻿using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SharedLibrary.Models.ServiceModel;
using SharedLibrary;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.Achar.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonitoringController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Achar/Monitoring
        private SharedLibrary.Models.ServiceModel.SharedServiceEntities db = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Achar");

        public ActionResult Index()
        {
            ViewBag.ServiceName = "آچار";
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
            var monitoringId = Convert.ToInt64(Request["MonitoringId"]);
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
            {
                var tag = Convert.ToInt32(Request["Tag"]);
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, monitoringId);
            }
            else
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, monitoringId);

            return Content("Ok");
        }

        public ActionResult PauseSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var monitoringId = Convert.ToInt64(Request["MonitoringId"]);
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
            {
                var tag = Convert.ToInt32(Request["Tag"]);
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, (int)SharedLibrary.MessageHandler.ProcessStatus.Paused, monitoringId);
            }
            else
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, (int)SharedLibrary.MessageHandler.ProcessStatus.Paused, monitoringId);

            return Content("Ok");
        }

        public ActionResult ResumeSendingMessages([DataSourceRequest]DataSourceRequest request)
        {
            var monitoringId = Convert.ToInt64(Request["MonitoringId"]);
            var contentId = Convert.ToInt64(Request["ContentId"]);
            var messageType = Convert.ToInt32(Request["MessageType"]);
            var persianDateCreated = Request["PersianDateCreated"];
            if (messageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
            {
                var tag = Convert.ToInt32(Request["Tag"]);
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.AutoCharge, null, tag, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.Paused, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, monitoringId);
            }
            else
                db.ChangeMessageStatus((int)SharedLibrary.MessageHandler.MessageType.EventBase, contentId, null, persianDateCreated, (int)SharedLibrary.MessageHandler.ProcessStatus.Paused, (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, monitoringId);

            return Content("Ok");
        }
    }
}