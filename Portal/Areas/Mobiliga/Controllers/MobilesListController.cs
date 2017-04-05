using System;
using System.Linq;
using System.Web.Mvc;
using MobiligaLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Mobiliga.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MobilesListController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Mobiliga/MobilesList
        private MobiligaEntities db = new MobiligaEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "موبایلیگا";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MobilesList_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.MobilesLists.ToDataSourceResult(request, MobilesList => new
            {
                Id = MobilesList.Id,
                Number = MobilesList.Number,
                MobileName = MobilesList.MobileName
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MobilesList_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] MobilesList MobilesList)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new MobilesList
                    {
                        Id = MobilesList.Id,
                        Number = MobilesList.Number,
                        MobileName = MobilesList.MobileName
                    };

                    db.MobilesLists.Add(entity);
                    db.SaveChanges();
                    MobilesList.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in MobilesListController :" + e);
            }

            return Json(new[] { MobilesList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MobilesList_Update([DataSourceRequest]DataSourceRequest request, MobilesList MobilesList)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new MobilesList
                    {
                        Id = MobilesList.Id,
                        Number = MobilesList.Number,
                        MobileName = MobilesList.MobileName
                    };

                    db.MobilesLists.Attach(entity);
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
            return Json(new[] { MobilesList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MobilesList_Destroy([DataSourceRequest]DataSourceRequest request, MobilesList MobilesList)
        {
            var entity = new MobilesList
            {
                Id = MobilesList.Id,
            };

            db.MobilesLists.Attach(entity);
            db.MobilesLists.Remove(entity);
            db.SaveChanges();

            return Json(new[] { MobilesList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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