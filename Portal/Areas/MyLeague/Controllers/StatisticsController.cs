using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using MyLeagueLibrary.Models;
using SharedLibrary;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.MyLeague.Controllers
{
    public class StatisticsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MyLeague/Statistics
        private MyLeagueEntities db = new MyLeagueEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "لیگ من";
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
                NumberOfEventbasePrepaidMessagesFailed = dailyStatistics.NumberOfEventbasePrepaidMessagesFailed,
                NumberOfOnDemandMessagesDelivered = dailyStatistics.NumberOfOnDemandMessagesDelivered,
                NumberOfOnDemandPostpaidMessagesDelivered = dailyStatistics.NumberOfOnDemandPostpaidMessagesDelivered,
                NumberOfOnDemandPrepaidMessagesDelivered = dailyStatistics.NumberOfOnDemandPrepaidMessagesDelivered,
                NumberOfAutochargeMessagesDelivered = dailyStatistics.NumberOfAutochargeMessagesDelivered,
                NumberOfAutochargePostpaidMessagesDelivered = dailyStatistics.NumberOfAutochargePostpaidMessagesDelivered,
                NumberOfAutochargePrepaidMessagesDelivered = dailyStatistics.NumberOfAutochargePrepaidMessagesDelivered,
                NumberOfEventbaseMessagesDelivered = dailyStatistics.NumberOfEventbaseMessagesDelivered,
                NumberOfEventbasePostpaidMessagesDelivered = dailyStatistics.NumberOfEventbasePostpaidMessagesDelivered,
                NumberOfEventbasePrepaidMessagesDelivered = dailyStatistics.NumberOfEventbasePrepaidMessagesDelivered,
                NumberOfPostpaidMessagesDelivered = dailyStatistics.NumberOfPostpaidMessagesDelivered,
                NumberOfPrepaidMessagesDelivered = dailyStatistics.NumberOfPrepaidMessagesDelivered,
                NumberOfMessagesDelivered = dailyStatistics.NumberOfMessagesDelivered,
                NumberOfOnDemandMessagesDeliveryFailed = dailyStatistics.NumberOfOnDemandMessagesDeliveryFailed,
                NumberOfOnDemandPostpaidMessagesDeliveryFailed = dailyStatistics.NumberOfOnDemandPostpaidMessagesDeliveryFailed,
                NumberOfOnDemandPrepaidMessagesDeliveryFailed = dailyStatistics.NumberOfOnDemandPrepaidMessagesDeliveryFailed,
                NumberOfAutochargeMessagesDeliveryFailed = dailyStatistics.NumberOfAutochargeMessagesDeliveryFailed,
                NumberOfAutochargePostpaidMessagesDeliveryFailed = dailyStatistics.NumberOfAutochargePostpaidMessagesDeliveryFailed,
                NumberOfAutochargePrepaidMessagesDeliveryFailed = dailyStatistics.NumberOfAutochargePrepaidMessagesDeliveryFailed,
                NumberOfEventbaseMessagesDeliveryFailed = dailyStatistics.NumberOfEventbaseMessagesDeliveryFailed,
                NumberOfEventbasePostpaidMessagesDeliveryFailed = dailyStatistics.NumberOfEventbasePostpaidMessagesDeliveryFailed,
                NumberOfEventbasePrepaidMessagesDeliveryFailed = dailyStatistics.NumberOfEventbasePrepaidMessagesDeliveryFailed,
                NumberOfPostpaidMessagesDeliveryFailed = dailyStatistics.NumberOfPostpaidMessagesDeliveryFailed,
                NumberOfPrepaidMessagesDeliveryFailed = dailyStatistics.NumberOfPrepaidMessagesDeliveryFailed,
                NumberOfMessagesDeliveryFailed = dailyStatistics.NumberOfMessagesDeliveryFailed,
                NumberOfOnDemandMessagesDeliveryNotReceived = dailyStatistics.NumberOfOnDemandMessagesDeliveryNotReceived,
                NumberOfOnDemandPostpaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfOnDemandPostpaidMessagesDeliveryNotReceived,
                NumberOfOnDemandPrepaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfOnDemandPrepaidMessagesDeliveryNotReceived,
                NumberOfAutochargeMessagesDeliveryNotReceived = dailyStatistics.NumberOfAutochargeMessagesDeliveryNotReceived,
                NumberOfAutochargePostpaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfAutochargePostpaidMessagesDeliveryNotReceived,
                NumberOfAutochargePrepaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfAutochargePrepaidMessagesDeliveryNotReceived,
                NumberOfEventbaseMessagesDeliveryNotReceived = dailyStatistics.NumberOfEventbaseMessagesDeliveryNotReceived,
                NumberOfEventbasePostpaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfEventbasePostpaidMessagesDeliveryNotReceived,
                NumberOfEventbasePrepaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfEventbasePrepaidMessagesDeliveryNotReceived,
                NumberOfPostpaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfPostpaidMessagesDeliveryNotReceived,
                NumberOfPrepaidMessagesDeliveryNotReceived = dailyStatistics.NumberOfPrepaidMessagesDeliveryNotReceived,
                NumberOfMessagesDeliveryNotReceived = dailyStatistics.NumberOfMessagesDeliveryNotReceived,
                NumberOfOnDemandMessagesThatUserHasNoCharge = dailyStatistics.NumberOfOnDemandMessagesThatUserHasNoCharge,
                NumberOfOnDemandPostpaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfOnDemandPostpaidMessagesThatUserHasNoCharge,
                NumberOfOnDemandPrepaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfOnDemandPrepaidMessagesThatUserHasNoCharge,
                NumberOfAutochargeMessagesThatUserHasNoCharge = dailyStatistics.NumberOfAutochargeMessagesThatUserHasNoCharge,
                NumberOfAutochargePostpaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfAutochargePostpaidMessagesThatUserHasNoCharge,
                NumberOfAutochargePrepaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfAutochargePrepaidMessagesThatUserHasNoCharge,
                NumberOfEventbaseMessagesThatUserHasNoCharge = dailyStatistics.NumberOfEventbaseMessagesThatUserHasNoCharge,
                NumberOfEventbasePostpaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfEventbasePostpaidMessagesThatUserHasNoCharge,
                NumberOfEventbasePrepaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfEventbasePrepaidMessagesThatUserHasNoCharge,
                NumberOfPostpaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfPostpaidMessagesThatUserHasNoCharge,
                NumberOfPrepaidMessagesThatUserHasNoCharge = dailyStatistics.NumberOfPrepaidMessagesThatUserHasNoCharge,
                NumberOfMessagesThatUserHasNoCharge = dailyStatistics.NumberOfMessagesThatUserHasNoCharge,
                NumberOfFreeOnDemandMessagesSent = dailyStatistics.NumberOfFreeOnDemandMessagesSent,
                NumberOfFreeEventbaseMessagesSent = dailyStatistics.NumberOfFreeEventbaseMessagesSent,
                NumberOfFreeAutochargeMessagesSent = dailyStatistics.NumberOfFreeAutochargeMessagesSent,
                NumberOfFreeMessagesSent = dailyStatistics.NumberOfFreeMessagesSent,
                NumberOfFreeOnDemandMessagesDelivered = dailyStatistics.NumberOfFreeOnDemandMessagesDelivered,
                NumberOfFreeEventbaseMessagesDelivered = dailyStatistics.NumberOfFreeEventbaseMessagesDelivered,
                NumberOfFreeAutochargeMessagesDelivered = dailyStatistics.NumberOfFreeAutochargeMessagesDelivered,
                NumberOfFreeMessagesDelivered = dailyStatistics.NumberOfFreeMessagesDelivered,
                NumberOfPaidOnDemandMessagesSent = dailyStatistics.NumberOfPaidOnDemandMessagesSent,
                NumberOfPaidEventbaseMessagesSent = dailyStatistics.NumberOfPaidEventbaseMessagesSent,
                NumberOfPaidAutochargeMessagesSent = dailyStatistics.NumberOfPaidAutochargeMessagesSent,
                NumberOfPaidMessagesSent = dailyStatistics.NumberOfPaidMessagesSent,
                NumberOfPaidOnDemandMessagesFailed = dailyStatistics.NumberOfPaidOnDemandMessagesFailed,
                NumberOfPaidEventbaseMessagesFailed = dailyStatistics.NumberOfPaidEventbaseMessagesFailed,
                NumberOfPaidAutochargeMessagesFailed = dailyStatistics.NumberOfPaidAutochargeMessagesFailed,
                NumberOfPaidMessagesFailed = dailyStatistics.NumberOfPaidMessagesFailed,
                SumOfPaidOnDemandMessages = dailyStatistics.SumOfPaidOnDemandMessages,
                SumOfpaidEventbaseMessages = dailyStatistics.SumOfpaidEventbaseMessages,
                SumOfPaidAutochargeMessages = dailyStatistics.SumOfPaidAutochargeMessages,
                SumOfPaidMessages = dailyStatistics.SumOfPaidMessages,
                TotalPrepaidSubscribers = dailyStatistics.TotalPrepaidSubscribers,
                TotalPostpaidSubscribers = dailyStatistics.TotalPostpaidSubscribers,
                TotalUnSubscribers = dailyStatistics.TotalUnSubscribers,
                SumOfPaidOnDemandMessagesForPostPaidSubscribers = dailyStatistics.SumOfPaidOnDemandMessagesForPostPaidSubscribers,
                SumOfPaidOnDemandMessagesForPrePaidSubscribers = dailyStatistics.SumOfPaidOnDemandMessagesForPrePaidSubscribers,
                SumOfPaidEventbaseMessagesForPostPaidSubscribers = dailyStatistics.SumOfPaidEventbaseMessagesForPostPaidSubscribers,
                SumOfPaidEventbaseMessagesForPrePaidSubscribers = dailyStatistics.SumOfPaidEventbaseMessagesForPrePaidSubscribers,
                SumOfPaidAutochargeMessagesForPostPaidSubscribers = dailyStatistics.SumOfPaidAutochargeMessagesForPostPaidSubscribers,
                SumOfPaidAutochargeMessagesForPrePaidSubscribers = dailyStatistics.SumOfPaidAutochargeMessagesForPrePaidSubscribers,
                SumOfPaidMessagesForPostPaidSubscribers = dailyStatistics.SumOfPaidMessagesForPostPaidSubscribers,
                SumOfPaidMessagesForPrePaidSubscribers = dailyStatistics.SumOfPaidMessagesForPrePaidSubscribers,
                NumberOfSinglechargeSuccessfulPrepaidFullCharge = dailyStatistics.NumberOfSinglechargeSuccessfulPrepaidFullCharge,
                NumberOfSinglechargeSuccessfulPostpaidFullCharge = dailyStatistics.NumberOfSinglechargeSuccessfulPostpaidFullCharge,
                NumberOfSinglechargeSuccessfulFullCharge = dailyStatistics.NumberOfSinglechargeSuccessfulFullCharge,
                NumberOfSinglechargeSuccessfulPrepaidIncompleteCharge = dailyStatistics.NumberOfSinglechargeSuccessfulPrepaidIncompleteCharge,
                NumberOfSinglechargeSuccessfulPostpaidIncompleteCharge = dailyStatistics.NumberOfSinglechargeSuccessfulPostpaidIncompleteCharge,
                NumberOfSinglechargeSuccessfulIncompleteCharge = dailyStatistics.NumberOfSinglechargeSuccessfulIncompleteCharge,
                SumOfSinglechargeSuccessfulPrepaidCharge = dailyStatistics.SumOfSinglechargeSuccessfulPrepaidCharge,
                SumOfSinglechargeSuccessfulPostpaidCharge = dailyStatistics.SumOfSinglechargeSuccessfulPostpaidCharge,
                SumOfSinglechargeSuccessfulCharge = dailyStatistics.SumOfSinglechargeSuccessfulCharge,
                SumOfSinglechargeSuccessfulPrepaidFullCharge = dailyStatistics.SumOfSinglechargeSuccessfulPrepaidFullCharge,
                SumOfSinglechargeSuccessfulPostpaidFullCharge = dailyStatistics.SumOfSinglechargeSuccessfulPostpaidFullCharge,
                SumOfSinglechargeSuccessfulFullCharge = dailyStatistics.SumOfSinglechargeSuccessfulFullCharge,
                SumOfSinglechargeSuccessfulPrepaidIncompleteCharge = dailyStatistics.SumOfSinglechargeSuccessfulPrepaidIncompleteCharge,
                SumOfSinglechargeSuccessfulPostpaidIncompleteCharge = dailyStatistics.SumOfSinglechargeSuccessfulPostpaidIncompleteCharge,
                SumOfSinglechargeSuccessfulIncompleteCharge = dailyStatistics.SumOfSinglechargeSuccessfulIncompleteCharge,
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
                var serviceId = portalEntity.Services.FirstOrDefault(o => o.ServiceCode == "MyLeague").Id;
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