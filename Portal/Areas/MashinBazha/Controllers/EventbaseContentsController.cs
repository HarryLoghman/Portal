using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using MashinBazhaLibrary.Models;
using SharedLibrary;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Portal.Areas.MashinBazha.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EventbaseContentsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MashinBazha/EventbaseContents
        private MashinBazhaEntities db = new MashinBazhaEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "ماشین بازها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult EventbaseContents_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.EventbaseContents.ToDataSourceResult(request, eventbaseContent => new
            {
                Id = eventbaseContent.Id,
                Content = eventbaseContent.Content,
                Point = eventbaseContent.Point,
                Price = eventbaseContent.Price,
                SubscriberNotSendedMoInDays = eventbaseContent.SubscriberNotSendedMoInDays,
                DateCreated = eventbaseContent.DateCreated,
                PersianDateCreated = eventbaseContent.PersianDateCreated,
                IsAddingMessagesToSendQueue = eventbaseContent.IsAddingMessagesToSendQueue,
                IsAddedToSendQueueFinished = eventbaseContent.IsAddedToSendQueueFinished,
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult EventbaseContents_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] EventbaseContent eventbaseContent)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new EventbaseContent
                    {
                        Content = eventbaseContent.Content,
                        Point = eventbaseContent.Point,
                        Price = eventbaseContent.Price,
                        DateCreated = DateTime.Now,
                        SubscriberNotSendedMoInDays = eventbaseContent.SubscriberNotSendedMoInDays,
                        PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now),
                        IsAddingMessagesToSendQueue = false,
                        IsAddedToSendQueueFinished = false,
                    };

                    db.EventbaseContents.Add(entity);
                    db.SaveChanges();
                    eventbaseContent.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in EventbaseContentsController :" + e);
            }

            return Json(new[] { eventbaseContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult EventbaseContents_Update([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "DateCreated,PersianDateCreated")] EventbaseContent eventbaseContent)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new EventbaseContent
                    {
                        Id = eventbaseContent.Id,
                        Content = eventbaseContent.Content,
                        Point = eventbaseContent.Point,
                        Price = eventbaseContent.Price,
                        SubscriberNotSendedMoInDays = eventbaseContent.SubscriberNotSendedMoInDays,
                        PersianDateCreated = "This will not be saved!",
                        IsAddingMessagesToSendQueue = false,
                        IsAddedToSendQueueFinished = false,
                    };

                    db.EventbaseContents.Attach(entity);
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
            return Json(new[] { eventbaseContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult EventbaseContents_Destroy([DataSourceRequest]DataSourceRequest request, EventbaseContent eventbaseContent)
        {
            var entity = new EventbaseContent
            {
                Id = eventbaseContent.Id,
            };

            db.EventbaseContents.Attach(entity);
            db.EventbaseContents.Remove(entity);
            db.SaveChanges();

            return Json(new[] { eventbaseContent }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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
        public ActionResult SendEventbaseToQueue([DataSourceRequest]DataSourceRequest request)
        {
            var id = Convert.ToInt64(Request["id"]);
            var eventbaseContent = db.EventbaseContents.FirstOrDefault(o => o.Id == id);
            eventbaseContent.IsAddingMessagesToSendQueue = true;
            db.Entry(eventbaseContent).State = EntityState.Modified;
            db.SaveChanges();
            return Content("Ok");
        }
    }
}