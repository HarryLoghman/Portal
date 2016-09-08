using System;
using System.Linq;
using System.Web.Mvc;
using MyLeagueLibrary.Models;
using SharedLibrary;
using SharedLibrary.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.MyLeague.Controllers
{
    public class SettingsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MyLeague/Settings
        private MyLeagueEntities db = new MyLeagueEntities();
        private PortalEntities portalEntity = new PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "لیگ من";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Service_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = portalEntity.Services.Where( o => o.ServiceCode == "MyLeague").ToDataSourceResult(request, service => new
            {
                Id = service.Id,
                OnKeywords = service.OnKeywords
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Service_Update([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "DateCreated,PersianDateCreated")] Service service)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new Service
                    {
                        Id = service.Id,
                        OnKeywords = service.OnKeywords,
                        Name = "",
                        ServiceCode ="",
                        IsServiceActive = true,
                        DateCreated = DateTime.Now
                    };

                    portalEntity.Services.Attach(entity);
                    portalEntity.Entry(entity).State = EntityState.Modified;
                    portalEntity.Entry(entity).Property(x => x.DateCreated).IsModified = false;
                    portalEntity.Entry(entity).Property(x => x.IsServiceActive).IsModified = false;
                    portalEntity.Entry(entity).Property(x => x.Name).IsModified = false;
                    portalEntity.Entry(entity).Property(x => x.ServiceCode).IsModified = false;
                    portalEntity.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }

            }
            return Json(new[] { service }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }


        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RechargeKeywords_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.ServiceRechargeKeywords.ToDataSourceResult(request, rechargeKeywords => new
            {
                Id = rechargeKeywords.Id,
                Keyword = rechargeKeywords.Keyword,
                Price = rechargeKeywords.Price
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RechargeKeywords_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] ServiceRechargeKeyword serviceRechargeKeywords)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new ServiceRechargeKeyword
                    {
                        Keyword = serviceRechargeKeywords.Keyword,
                        Price = serviceRechargeKeywords.Price
                    };

                    db.ServiceRechargeKeywords.Add(entity);
                    db.SaveChanges();
                    serviceRechargeKeywords.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in SettingsController :" + e);
            }

            return Json(new[] { serviceRechargeKeywords }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RechargeKeywords_Update([DataSourceRequest]DataSourceRequest request, ServiceRechargeKeyword rechargeKeywords)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new ServiceRechargeKeyword
                    {
                        Id = rechargeKeywords.Id,
                        Keyword = rechargeKeywords.Keyword,
                        Price = rechargeKeywords.Price
                    };

                    db.ServiceRechargeKeywords.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }

            }
            return Json(new[] { rechargeKeywords }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult RechargeKeywords_Destroy([DataSourceRequest]DataSourceRequest request, ServiceRechargeKeyword serviceRechargeKeywords)
        {
            var entity = new ServiceRechargeKeyword
            {
                Id = serviceRechargeKeywords.Id,
            };

            db.ServiceRechargeKeywords.Attach(entity);
            db.ServiceRechargeKeywords.Remove(entity);
            db.SaveChanges();

            return Json(new[] { serviceRechargeKeywords }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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