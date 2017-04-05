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
    [Authorize(Roles = "Admin")]
    public class SentMessagesController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: SepidRood/SentMessages
        private SepidRoodEntities db = new SepidRoodEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "سپید رود";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult SentMessages_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.vw_SentMessages.Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.Success).ToDataSourceResult(request, sentMessages => new
            {
                SentDate = sentMessages.SentDate,
                PersianSentDate = sentMessages.PersianSentDate,
                MobileNumber = sentMessages.MobileNumber,
                Content = sentMessages.Content,
                MessagePoint = sentMessages.MessagePoint,
                MessageType = sentMessages.MessageType,
                ImiChargeCode = sentMessages.ImiChargeCode,
                DeliveryStatus = sentMessages.DeliveryStatus,
                DeliveryDescription = sentMessages.DeliveryDescription
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