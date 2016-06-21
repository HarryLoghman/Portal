using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Portal.Models;

namespace Portal.Services.RockPaperScissor
{
    public class RpsServiceController : Controller
    {
        private PortalEntities db = new PortalEntities();

        public ActionResult Index()
        {
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Services_Read([DataSourceRequest]DataSourceRequest request)
        {
            var services = RpsServices.GetRpsServiceInfo(db);
            DataSourceResult result = services.ToDataSourceResult(request, serviceInfo => new
            {
                Id = serviceInfo.Id,
                Name = serviceInfo.Name,
                ServiceCode = serviceInfo.ServiceCode,
                DateCreated = serviceInfo.DateCreated,
                OnKeywords = serviceInfo.OnKeywords,
                ServiceIsActive = serviceInfo.ServiceIsActive,
                WelcomeMessage = serviceInfo.WelcomeMessage,
                LeaveMessage = serviceInfo.LeaveMessage,
                InvalidContentWhenSubscribed = serviceInfo.InvalidContentWhenSubscribed,
                InvalidContentWhenNotSubscribed = serviceInfo.InvalidContentWhenNotSubscribed,
                ServiceHelp = serviceInfo.ServiceHelp,

            });
            return Json("");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Services_Create([DataSourceRequest]DataSourceRequest request, Service service)
        {
            if (ModelState.IsValid)
            {
                var entity = new Service
                {
                    Name = service.Name,
                    ServiceCode = service.ServiceCode,
                    DateCreated = service.DateCreated,
                    OnKeywords = service.OnKeywords,
                    ServiceIsActive = service.ServiceIsActive,
                    WelcomeMessage = service.WelcomeMessage,
                    LeaveMessage = service.LeaveMessage,
                    InvalidContentWhenSubscribed = service.InvalidContentWhenSubscribed,
                    InvalidContentWhenNotSubscribed = service.InvalidContentWhenNotSubscribed,
                    ServiceHelp = service.ServiceHelp,
                };

                db.Services.Add(entity);
                db.SaveChanges();
                service.Id = entity.Id;
            }

            return Json(new[] { service }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Services_Update([DataSourceRequest]DataSourceRequest request, Service service)
        {
            if (ModelState.IsValid)
            {
                var entity = new Service
                {
                    Id = service.Id,
                    Name = service.Name,
                    ServiceCode = service.ServiceCode,
                    DateCreated = service.DateCreated,
                    OnKeywords = service.OnKeywords,
                    ServiceIsActive = service.ServiceIsActive,
                    WelcomeMessage = service.WelcomeMessage,
                    LeaveMessage = service.LeaveMessage,
                    InvalidContentWhenSubscribed = service.InvalidContentWhenSubscribed,
                    InvalidContentWhenNotSubscribed = service.InvalidContentWhenNotSubscribed,
                    ServiceHelp = service.ServiceHelp,
                };

                db.Services.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                db.SaveChanges();
            }

            return Json(new[] { service }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Services_Destroy([DataSourceRequest]DataSourceRequest request, Service service)
        {
            if (ModelState.IsValid)
            {
                var entity = new Service
                {
                    Id = service.Id,
                    Name = service.Name,
                    ServiceCode = service.ServiceCode,
                    DateCreated = service.DateCreated,
                    OnKeywords = service.OnKeywords,
                    ServiceIsActive = service.ServiceIsActive,
                    WelcomeMessage = service.WelcomeMessage,
                    LeaveMessage = service.LeaveMessage,
                    InvalidContentWhenSubscribed = service.InvalidContentWhenSubscribed,
                    InvalidContentWhenNotSubscribed = service.InvalidContentWhenNotSubscribed,
                    ServiceHelp = service.ServiceHelp,
                };

                db.Services.Attach(entity);
                db.Services.Remove(entity);
                db.SaveChanges();
            }

            return Json(new[] { service }.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
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
