using System;
using System.Linq;
using System.Web.Mvc;
using MashinBazhaLibrary.Models;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;
using System.Globalization;

namespace Portal.Areas.MashinBazha.Controllers
{
    public class AutochargeTimeTableController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MashinBazha/AutochargeTimeTable
        private MashinBazhaEntities db = new MashinBazhaEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "ماشین بازها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeTimeTable_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.AutochargeTimeTables.ToDataSourceResult(request, autochargeTimeTable => new
            {
                Id = autochargeTimeTable.Id,
                Tag = autochargeTimeTable.Tag,
                SendTime = autochargeTimeTable.SendTime
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeTimeTable_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] AutochargeTimeTable autochargeTimeTable)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new AutochargeTimeTable
                    {
                        Tag = autochargeTimeTable.Tag,
                        SendTime = autochargeTimeTable.SendTime
                    };

                    db.AutochargeTimeTables.Add(entity);
                    db.SaveChanges();
                    autochargeTimeTable.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AutochargeTimeTableController :" + e);
            }

            return Json(new[] { autochargeTimeTable }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeTimeTable_Update([DataSourceRequest]DataSourceRequest request, AutochargeTimeTable autochargeTimeTable)
        {
            ModelState.Remove("SendTime");
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new AutochargeTimeTable
                    {
                        Id = autochargeTimeTable.Id,
                        Tag = autochargeTimeTable.Tag,
                        SendTime = autochargeTimeTable.SendTime
                    };

                    db.AutochargeTimeTables.Attach(entity);
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
            return Json(new[] { autochargeTimeTable }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeTimeTable_Destroy([DataSourceRequest]DataSourceRequest request, AutochargeTimeTable autochargeTimeTable)
        {
            var entity = new AutochargeTimeTable
            {
                Id = autochargeTimeTable.Id,
            };

            db.AutochargeTimeTables.Attach(entity);
            db.AutochargeTimeTables.Remove(entity);
            db.SaveChanges();

            return Json(new[] { autochargeTimeTable }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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