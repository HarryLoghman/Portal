using System;
using System.Linq;
using System.Web.Mvc;
using SharedLibrary.Models.ServiceModel;
using SharedLibrary;
using SharedLibrary.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Aseman.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReceiveController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Aseman/ReceievedMessages
        private PortalEntities db = new PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "آسمان";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Receive_Read([DataSourceRequest]DataSourceRequest request)
        {
            var service = db.Services.FirstOrDefault(o => o.ServiceCode == "Aseman");
            var shortCode = db.ServiceInfoes.Where(o => o.ServiceId == service.Id).Select(o => o.ShortCode).FirstOrDefault();
            DataSourceResult result = db.vw_ReceivedMessages.Where(o => o.ShortCode == shortCode).ToDataSourceResult(request, receivedMessages => new
            {
                Id = receivedMessages.Id,
                MobileNumber = receivedMessages.MobileNumber,
                PersianReceivedTime = receivedMessages.PersianReceivedTime,
                Content = receivedMessages.Content,
                IsReceivedFromIntegratedPanel = receivedMessages.IsReceivedFromIntegratedPanel,
                ReceivedFrom = receivedMessages.ReceivedFrom
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
    }
}