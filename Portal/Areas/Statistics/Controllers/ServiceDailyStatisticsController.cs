using System;
using System.Linq;
using System.Web.Mvc;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Statistics.Controllers
{
    public class ServiceDailyStatisticsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Statistics/ServiceDailyStatistics
        private SharedLibrary.Models.PortalEntities db = new SharedLibrary.Models.PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "گزارش روز سرویس ها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult UserLog_Read([DataSourceRequest]DataSourceRequest request, string mobileNumber)
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    logs.Info(mobileNumber);
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    entity.Database.CommandTimeout = 2400;
                    if (mobileNumber == null || mobileNumber == "")
                        return Json("", JsonRequestBehavior.AllowGet);
                    var query = entity.GetUserLog(mobileNumber).ToList();
                    DataSourceResult result = query.ToDataSourceResult(request, messagesSendedToUserLog => new
                    {
                        Type = messagesSendedToUserLog.Type,
                        ServiceName = messagesSendedToUserLog.ServiceName,
                        MobileNumber = messagesSendedToUserLog.MobileNumber,
                        ShortCode = messagesSendedToUserLog.ShortCode,
                        PersianDate = messagesSendedToUserLog.PersianDate,
                        Time = messagesSendedToUserLog.Time,
                        Content = messagesSendedToUserLog.Content
                    });
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in UserLog_Read:", e);
            }
            return Json("", JsonRequestBehavior.AllowGet);
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