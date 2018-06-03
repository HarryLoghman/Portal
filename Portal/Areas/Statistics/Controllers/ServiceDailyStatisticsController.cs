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
            ViewBag.ServiceName = "گزارش روزانه سرویس ها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RealTimeStatisticsFor3GServices_Read([DataSourceRequest]DataSourceRequest request)
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    DataSourceResult result = entity.RealtimeStatisticsFor3GServices.AsNoTracking().OrderByDescending(o => o.PersianDate).Take(1000).ToDataSourceResult(request, realtimeStatisticsFor3GServices => new
                    {
                        Id = realtimeStatisticsFor3GServices.Id,
                        ServiceName = realtimeStatisticsFor3GServices.ServiceName,
                        PersianDate = realtimeStatisticsFor3GServices.PersianDate,
                        TotalSubscriptions = realtimeStatisticsFor3GServices.TotalSubscriptions,
                        TotalUnsubscriptions = realtimeStatisticsFor3GServices.TotalUnsubscriptions,
                        TodayGenuineSubscriptions = realtimeStatisticsFor3GServices.TodayGenuineSubscriptions,
                        ActivationRateByMinute = realtimeStatisticsFor3GServices.ActivationRateByMinute,
                        ActivationRateByHour = realtimeStatisticsFor3GServices.ActivationRateByHour,
                        DeactivationRateByMinute = realtimeStatisticsFor3GServices.DeactivationRateByMinute,
                        DeactivationRateByHour = realtimeStatisticsFor3GServices.DeactivationRateByHour,
                        GeniuneActivationRateByMinute = realtimeStatisticsFor3GServices.GeniuneActivationRateByMinute,
                        GeniueActivationRateByHour = realtimeStatisticsFor3GServices.GeniueActivationRateByHour,
                        AllDeactivedSubscribers = realtimeStatisticsFor3GServices.AllDeactivedSubscribers,
                        ActiveSubscribersFromHistory = realtimeStatisticsFor3GServices.ActiveSubscribersFromHistory,
                        ActiveSubscribersFromHistoryUnique = realtimeStatisticsFor3GServices.ActiveSubscribersFromHistoryUnique,
                        DeactivedSubscribersFromHistory = realtimeStatisticsFor3GServices.DeactivedSubscribersFromHistory,
                        DeactivedSubscribersFromHistoryUnqiue = realtimeStatisticsFor3GServices.DeactivedSubscribersFromHistoryUnqiue,
                    });
                    var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
                    jsonResult.MaxJsonLength = Int32.MaxValue;
                    return jsonResult;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in RealTimeStatisticsFor3GServices_Read:", e);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RealTimeStatisticsFor2GServices_Read([DataSourceRequest]DataSourceRequest request)
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    DataSourceResult result = entity.RealtimeStatisticsFor2GServices.AsNoTracking().ToDataSourceResult(request, realtimeStatisticsFor2GServices => new
                    {
                        Id = realtimeStatisticsFor2GServices.Id,
                        ServiceName = realtimeStatisticsFor2GServices.ServiceName,
                        PersianDate = realtimeStatisticsFor2GServices.PersianDate,
                        PrepaidSubscriptions = realtimeStatisticsFor2GServices.PrepaidSubscriptions,
                        PostPaidSubscriptions = realtimeStatisticsFor2GServices.PostPaidSubscriptions,
                        PrepaidUnsubscriptions = realtimeStatisticsFor2GServices.PrepaidUnsubscriptions,
                        PostPaidUnsubscriptions = realtimeStatisticsFor2GServices.PostPaidUnsubscriptions,
                    });
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in RealTimeStatisticsFor2GServices_Read:", e);
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