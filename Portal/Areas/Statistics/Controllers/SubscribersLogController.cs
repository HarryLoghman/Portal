using System;
using System.Linq;
using System.Web.Mvc;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Statistics.Controllers
{
    public class SubscribersLogController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Statistics/SubscribersLog
        private SharedLibrary.Models.PortalEntities db = new SharedLibrary.Models.PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "گزارش کاربران";
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

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult UserActiveServices_Read([DataSourceRequest]DataSourceRequest request, string mobileNumber)
        {
            //var mobileNumber = "";
            DataSourceResult result = db.Subscribers.Where(o => o.MobileNumber == mobileNumber && o.DeactivationDate == null).ToDataSourceResult(request, messagesSendedToUserLog => new
            {
                MobileNumber = messagesSendedToUserLog.MobileNumber,
                ServiceName = messagesSendedToUserLog.Service.Name,
                PersianActivationDate = messagesSendedToUserLog.PersianActivationDate,
                ShortCode = messagesSendedToUserLog.Service.ServiceInfoes.Where(o => o.ServiceId == messagesSendedToUserLog.ServiceId).FirstOrDefault().ShortCode,
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult UserSubscriptionLog_Read([DataSourceRequest]DataSourceRequest request, string mobileNumber)
        {
            //var mobileNumber = "";
            DataSourceResult result = db.Subscribers.Where(o => o.MobileNumber == mobileNumber).ToDataSourceResult(request, messagesSendedToUserLog => new
            {
                MobileNumber = messagesSendedToUserLog.MobileNumber,
                ServiceName = messagesSendedToUserLog.Service.Name,
                PersianActivationDate = messagesSendedToUserLog.PersianActivationDate,
                PersianDeactivationDate = messagesSendedToUserLog.PersianDeactivationDate,
                OnKeyword = messagesSendedToUserLog.OnKeyword,
                OffKeyword = messagesSendedToUserLog.OffKeyword,
                ShortCode = messagesSendedToUserLog.Service.ServiceInfoes.Where(o => o.ServiceId == messagesSendedToUserLog.ServiceId).FirstOrDefault().ShortCode,
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
        public ActionResult UnSubscribeUser([DataSourceRequest]DataSourceRequest request)
        {
            var mobileNumber = Request["MobileNumber"];
            var serviceName = Request["ServiceName"];
            var subscriberService = db.Subscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.Service.Name == serviceName);
            if (subscriberService == null)
                return Content("Not Subscribed!");
            var message = new SharedLibrary.Models.MessageObject();
            message.MobileNumber = subscriberService.MobileNumber;
            var serviceOnKeyword = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(subscriberService.Service.OnKeywords);
            message.Content = "Off " + serviceOnKeyword;
            message.ReceivedFrom = "Portal";
            message.IsReceivedFromIntegratedPanel = true;
            message.ShortCode = subscriberService.Service.ServiceInfoes.FirstOrDefault(o => o.ServiceId == subscriberService.ServiceId).ShortCode;
            SharedLibrary.MessageHandler.SaveReceivedMessage(message);
            return Content("Ok");
        }
    }
}