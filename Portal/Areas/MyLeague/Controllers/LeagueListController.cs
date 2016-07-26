using System;
using System.Linq;
using System.Web.Mvc;
using Portal.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;

namespace Portal.Areas.MyLeague.Controllers
{
    public class LeagueListController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: MyLeague/LeagueList
        private MyLeagueEntities db = new MyLeagueEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "لیگ من";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult LeagueList_Read([DataSourceRequest]DataSourceRequest request)
        {
            DataSourceResult result = db.LeagueLists.ToDataSourceResult(request, leagueList => new
            {
                Id = leagueList.Id,
                Number = leagueList.Number,
                Name = leagueList.Name
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult LeagueList_Create([DataSourceRequest]DataSourceRequest request, [Bind(Exclude = "Id")] LeagueList leagueList)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var entity = new LeagueList
                    {
                        Id = leagueList.Id,
                        Number = leagueList.Number,
                        Name = leagueList.Name
                    };

                    db.LeagueLists.Add(entity);
                    db.SaveChanges();
                    leagueList.Id = entity.Id;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in LeagueListController :" + e);
            }

            return Json(new[] { leagueList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult LeagueList_Update([DataSourceRequest]DataSourceRequest request, LeagueList leagueList)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new LeagueList
                    {
                        Id = leagueList.Id,
                        Number = leagueList.Number,
                        Name = leagueList.Name
                    };

                    db.LeagueLists.Attach(entity);
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
            return Json(new[] { leagueList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult LeagueList_Destroy([DataSourceRequest]DataSourceRequest request, LeagueList leagueList)
        {
            var entity = new LeagueList
            {
                Id = leagueList.Id,
            };

            db.LeagueLists.Attach(entity);
            db.LeagueLists.Remove(entity);
            db.SaveChanges();

            return Json(new[] { leagueList }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
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