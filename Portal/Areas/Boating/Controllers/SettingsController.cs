using System;
using System.Linq;
using System.Web.Mvc;
using BoatingLibrary.Models;
using SharedLibrary;
using SharedLibrary.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Boating.Controllers
{
    public class SettingsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Boating/Settings
        private BoatingEntities db = new BoatingEntities();
        private PortalEntities portalEntity = new PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "قایقرانی";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Service_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = portalEntity.Services.Where( o => o.ServiceCode == "Boating").ToDataSourceResult(request, service => new
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