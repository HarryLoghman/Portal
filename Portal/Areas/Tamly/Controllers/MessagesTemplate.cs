using System;
using System.Linq;
using System.Web.Mvc;
using SharedLibrary.Models.ServiceModel;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.Tamly.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MessagesTemplateController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Tamly/messagesTemplate
        private SharedLibrary.Models.ServiceModel.SharedServiceEntities db = new SharedLibrary.Models.ServiceModel.SharedServiceEntities("Tamly");

        public ActionResult Index()
        {
            ViewBag.ServiceName = "تاملی";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MessagesTemplate_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.MessagesTemplates.ToDataSourceResult(request, messagesTemplate => new
            {
                Id = messagesTemplate.Id,
                Title = messagesTemplate.Title,
                PersianTitle = messagesTemplate.PersianTitle,
                Content = messagesTemplate.Content
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult MessagesTemplate_Update([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Title,PersianTitle")] MessagesTemplate messagesTemplate)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new MessagesTemplate
                    {
                        Id = messagesTemplate.Id,
                        Title = "not allowed for editing",
                        PersianTitle = "not allowed for editing",
                        Content = messagesTemplate.Content
                    };

                    db.MessagesTemplates.Attach(entity);
                    db.Entry(entity).State = EntityState.Modified;
                    db.Entry(entity).Property(x => x.Title).IsModified = false;
                    db.Entry(entity).Property(x => x.PersianTitle).IsModified = false;
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
            return Json(new[] { messagesTemplate }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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