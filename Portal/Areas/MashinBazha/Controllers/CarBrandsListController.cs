using System;
using System.Linq;
using System.Web.Mvc;
using MashinBazhaLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.MashinBazha.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CarBrandsListController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MashinBazha/CarBrandsList
        private MashinBazhaEntities db = new MashinBazhaEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "ماشین بازها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CarBrandsList_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.CarBrandsLists.ToDataSourceResult(request, CarBrandsList => new
            {
                Id = CarBrandsList.Id,
                Number = CarBrandsList.Number,
                CarBrand = CarBrandsList.CarBrand
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CarBrandsList_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] CarBrandsList CarBrandsList)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new CarBrandsList
                    {
                        Id = CarBrandsList.Id,
                        Number = CarBrandsList.Number,
                        CarBrand = CarBrandsList.CarBrand
                    };

                    db.CarBrandsLists.Add(entity);
                    db.SaveChanges();
                    CarBrandsList.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CarBrandsListController :" + e);
            }

            return Json(new[] { CarBrandsList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CarBrandsList_Update([DataSourceRequest]DataSourceRequest request, CarBrandsList CarBrandsList)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new CarBrandsList
                    {
                        Id = CarBrandsList.Id,
                        Number = CarBrandsList.Number,
                        CarBrand = CarBrandsList.CarBrand
                    };

                    db.CarBrandsLists.Attach(entity);
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
            return Json(new[] { CarBrandsList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CarBrandsList_Destroy([DataSourceRequest]DataSourceRequest request, CarBrandsList CarBrandsList)
        {
            var entity = new CarBrandsList
            {
                Id = CarBrandsList.Id,
            };

            db.CarBrandsLists.Attach(entity);
            db.CarBrandsLists.Remove(entity);
            db.SaveChanges();

            return Json(new[] { CarBrandsList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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