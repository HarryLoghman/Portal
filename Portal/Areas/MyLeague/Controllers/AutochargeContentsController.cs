using System;
using System.Linq;
using System.Web.Mvc;
using Portal.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.MyLeague.Controllers
{
    public class AutochargeContentsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MyLeague/AutochargeContents
        private MyLeagueEntities db = new MyLeagueEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "لیگ من";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeContents_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.AutochargeContents.ToDataSourceResult(request, autochargeContent => new
            {
                Id = autochargeContent.Id,
                Content = autochargeContent.Content,
                SendDate = autochargeContent.SendDate,
                PersianSendDate = autochargeContent.PersianSendDate,
                Point = autochargeContent.Point,
                Price = autochargeContent.Price,
                DateCreated = autochargeContent.DateCreated,
                PersianDateCreated = autochargeContent.PersianDateCreated,
                IsAddedToSendQueue = autochargeContent.IsAddedToSendQueue,
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeContents_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] AutochargeContent autochargeContent)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new AutochargeContent
                    {
                        Content = autochargeContent.Content,
                        SendDate = autochargeContent.SendDate,
                        PersianSendDate = Shared.Date.GetPersianDateTime(autochargeContent.SendDate),
                        Point = autochargeContent.Point,
                        Price = autochargeContent.Price,
                        DateCreated = DateTime.Now,
                        PersianDateCreated = Shared.Date.GetPersianDateTime(DateTime.Now),
                        IsAddedToSendQueue = autochargeContent.IsAddedToSendQueue,
                    };

                    db.AutochargeContents.Add(entity);
                    db.SaveChanges();
                    autochargeContent.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AutochargeContentsController :" + e);
            }

            return Json(new[] { autochargeContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeContents_Update([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "DateCreated,PersianDateCreated")] AutochargeContent autochargeContent)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new AutochargeContent
                    {
                        Id = autochargeContent.Id,
                        Content = autochargeContent.Content,
                        SendDate = autochargeContent.SendDate,
                        PersianSendDate = Shared.Date.GetPersianDateTime(autochargeContent.SendDate),
                        Point = autochargeContent.Point,
                        Price = autochargeContent.Price,
                        PersianDateCreated = "This will not be saved!",
                        IsAddedToSendQueue = autochargeContent.IsAddedToSendQueue,
                    };

                    db.AutochargeContents.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    db.Entry(entity).Property(x => x.DateCreated).IsModified = false;
                    db.Entry(entity).Property(x => x.PersianDateCreated).IsModified = false;
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
            return Json(new[] { autochargeContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult AutochargeContents_Destroy([DataSourceRequest]DataSourceRequest request, AutochargeContent autochargeContent)
        {
            var entity = new AutochargeContent
            {
                Id = autochargeContent.Id,
            };

            db.AutochargeContents.Attach(entity);
            db.AutochargeContents.Remove(entity);
            db.SaveChanges();

            return Json(new[] { autochargeContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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