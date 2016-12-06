using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using BoatingLibrary.Models;
using SharedLibrary;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.Boating.Controllers
{
    public class StatisticsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Boating/Statistics
        private BoatingEntities db = new BoatingEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "قایقرانی";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Statistics_GridRead([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.DailyStatistics.ToDataSourceResult(request, dailyStatistics => new
            {
                Id = dailyStatistics.Id,
                Date = dailyStatistics.Date,
                PersianDate = dailyStatistics.PersianDate,
                NumberOfSubscriptions = dailyStatistics.NumberOfSubscriptions,
                NumberOfUnsubscriptions = dailyStatistics.NumberOfUnsubscriptions,
                NumberOfPostpaidSubscriptions = dailyStatistics.NumberOfPostpaidSubscriptions,
                NumberOfPrepaidSubscriptions = dailyStatistics.NumberOfPrepaidSubscriptions,
                NumberOfMessagesSent = dailyStatistics.NumberOfMessagesSent,
                NumberOfPostpaidMessagesSent = dailyStatistics.NumberOfPostpaidMessagesSent,
                NumberOfPrepaidMessagesSent = dailyStatistics.NumberOfPrepaidMessagesSent,
                TotalSubscribers = dailyStatistics.TotalSubscribers,
                NumberOfOnDemandMessagesSent = dailyStatistics.NumberOfOnDemandMessagesSent,
                NumberOfOnDemandPostpaidMessagesSent = dailyStatistics.NumberOfOnDemandPostpaidMessagesSent,
                NumberOfOnDemandPrepaidMessagesSent = dailyStatistics.NumberOfOnDemandPrepaidMessagesSent,
                NumberOfAutochargeMessagesSent = dailyStatistics.NumberOfAutochargeMessagesSent,
                NumberOfAutochargePostpaidMessagesSent = dailyStatistics.NumberOfAutochargePostpaidMessagesSent,
                NumberOfAutochargePrepaidMessagesSent = dailyStatistics.NumberOfAutochargePrepaidMessagesSent,
                NumberOfEventbaseMessagesSent = dailyStatistics.NumberOfEventbaseMessagesSent,
                NumberOfEventbasePostpaidMessagesSent = dailyStatistics.NumberOfEventbasePostpaidMessagesSent,
                NumberOfEventbasePrepaidMessagesSent = dailyStatistics.NumberOfEventbasePrepaidMessagesSent,
                NumberOfPostpaidUnsubscriptions = dailyStatistics.NumberOfPostpaidUnsubscriptions,
                NumberOfPrepaidUnsubscriptions = dailyStatistics.NumberOfPrepaidUnsubscriptions,
                NumberOfMessagesFailed = dailyStatistics.NumberOfMessagesFailed,
                NumberOfPostpaidMessagesFailed = dailyStatistics.NumberOfPostpaidMessagesFailed,
                NumberOfPrepaidMessagesFailed = dailyStatistics.NumberOfPrepaidMessagesFailed,
                NumberOfOnDemandMessagesFailed = dailyStatistics.NumberOfOnDemandMessagesFailed,
                NumberOfOnDemandPostpaidMessagesFailed = dailyStatistics.NumberOfOnDemandPostpaidMessagesFailed,
                NumberOfOnDemandPrepaidMessagesFailed = dailyStatistics.NumberOfOnDemandPrepaidMessagesFailed,
                NumberOfAutochargeMessagesFailed = dailyStatistics.NumberOfAutochargeMessagesFailed,
                NumberOfAutochargePostpaidMessagesFailed = dailyStatistics.NumberOfAutochargePostpaidMessagesFailed,
                NumberOfAutochargePrepaidMessagesFailed = dailyStatistics.NumberOfAutochargePrepaidMessagesFailed,
                NumberOfEventbaseMessagesFailed = dailyStatistics.NumberOfEventbaseMessagesFailed,
                NumberOfEventbasePostpaidMessagesFailed = dailyStatistics.NumberOfEventbasePostpaidMessagesFailed,
                NumberOfEventbasePrepaidMessagesFailed = dailyStatistics.NumberOfEventbasePrepaidMessagesFailed
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Statistics_Read([DataSourceRequest]DataSourceRequest request)
        {
            var result = db.DailyStatistics.OrderBy(o => o.Date);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Subscribers_Read([DataSourceRequest]DataSourceRequest request)
        {
            using (var portalEntity = new PortalEntities())
            {
                var serviceId = portalEntity.Services.FirstOrDefault(o => o.ServiceCode == "Boating").Id;
                var activeSubscribers = portalEntity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate == null).Count();
                var deactiveSubscribers = portalEntity.Subscribers.Where(o => o.ServiceId == serviceId && o.DeactivationDate != null).Count();
                var totalSubscribers = activeSubscribers + deactiveSubscribers;
                var result = new { TotalSubscribers = totalSubscribers, ActiveSubscribers = activeSubscribers, DeactiveSubscribers = deactiveSubscribers} ;
                return Json(result, JsonRequestBehavior.AllowGet);
            }
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