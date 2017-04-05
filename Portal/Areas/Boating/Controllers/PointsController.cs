using System;
using System.Linq;
using System.Web.Mvc;
using BoatingLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Boating.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PointsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Boating/Points
        private BoatingEntities db = new BoatingEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "قایقرانی";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Points_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.PointsTables.ToDataSourceResult(request, points => new
            {
                Id = points.Id,
                Title = points.Title,
                PersianTitle = points.PersianTitle,
                Point = points.Point,
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Points_Update([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Title,PersianTitle")] PointsTable points)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new PointsTable
                    {
                        Id = points.Id,
                        Title = "not allowed for editing",
                        PersianTitle = "not allowed for editing",
                        Point = points.Point
                    };

                    db.PointsTables.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    db.Entry(entity).Property(o => o.Title).IsModified = false;
                    db.Entry(entity).Property(o => o.PersianTitle).IsModified = false;
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
            return Json(new[] { points }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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