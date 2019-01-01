using System;
using System.Linq;
using System.Web.Mvc;
using SepidRoodLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;
using System.Collections.Generic;

namespace Portal.Areas.Tools.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SendBulkMessageController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ActionResult Index()
        {
            ViewBag.ServiceName = "ارسال بالک";
            return View();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult GetServices()
        {
            using (var entity = new SharedLibrary.Models.PortalEntities())
            {
                //var services = entity.ServiceInfoes
                //    .Join(entity.Services, sI => sI.ServiceId, s => s.Id, (s, sI) => new { s, sI }).Select(sc => new { text = sc.sI.Name + " (" + sc.s.ShortCode + ")", value = sc.sI.ServiceCode })
                //    .Where(o => o.value == "Tamly" || o.value == "Soltan" || o.value == "Fitshow" || o.value == "JabehAbzar" || o.value == "ShenoYad")
                //    .ToList();
                var services = entity.Services.Where(o => o.ServiceCode == "Tamly" || o.ServiceCode == "Soltan" || o.ServiceCode == "Fitshow" || o.ServiceCode == "JabehAbzar" || o.ServiceCode == "ShenoYad" || o.ServiceCode == "Nebula" || o.ServiceCode == "Phantom" || o.ServiceCode == "Tamly500" || o.ServiceCode == "Aseman").Select(o => new { text = o.Name, value = o.ServiceCode }).OrderBy(o => o.text).ToList();

                return Json(services, JsonRequestBehavior.AllowGet);
            }
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult SendBulkMessageToQueue([DataSourceRequest]DataSourceRequest request)
        {
            var mobileNumbers = Request["mobileNumbers"]; 
            var content = Request["content"];
            var serviceCode = Request["serviceCode"]; 
            using(var entity = new SharedLibrary.Models.PortalEntities())
            {
                var newBulk = new SharedLibrary.Models.BulkList();
                newBulk.IsDone = false;
                newBulk.ServiceCode = serviceCode;
                newBulk.Message = content;
                newBulk.MobileNumbersList = mobileNumbers;
                newBulk.DateAdded = DateTime.Now;
                newBulk.PersianDateAdded = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                entity.BulkLists.Add(newBulk);
                entity.SaveChanges();
            }
            return Content("Ok");
        }
    }
}