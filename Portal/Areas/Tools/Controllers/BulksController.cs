using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.SignalR;
using RealTimeProgressBar;
using SharedLibrary.Models;

namespace Portal.Areas.Tools.Controllers
{
    [System.Web.Mvc.Authorize(Roles = "Admin")]
    public class BulksController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private PortalEntities db = new PortalEntities();
        // GET: Tools/Bulks
        public ActionResult Index()
        {
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Bulks_Read([DataSourceRequest]DataSourceRequest request)
        {

            var bulks = (from b in db.Bulks
                         join sers in db.Services on b.ServiceId equals sers.Id
                         select new
                         {
                             Id = b.Id,
                             bulkName = b.bulkName,
                             serviceName = sers.Name,
                             ServiceId = b.ServiceId,
                             tps = b.tps,
                             startTime = b.startTime,
                             endTime = b.endTime,
                             message = b.message,
                             TotalMessages = b.TotalMessages,
                             TotalSuccessfullySent = b.TotalSuccessfullySent,
                             TotalFailed = b.TotalFailed,
                             TotalRetry = b.TotalRetry,
                             TotalRetryUnique = b.TotalRetryUnique,
                             TotalDelivery = b.TotalDelivery,
                             status = b.status

                         }).OrderBy(o => o.startTime).ThenBy(o => o.bulkName);
            DataSourceResult result = bulks.ToDataSourceResult(request, b => new
            {
                Id = b.Id,
                bulkName = b.bulkName,
                serviceName = b.serviceName,
                ServiceId = b.ServiceId,
                tps = b.tps,
                startTime = b.startTime.ToString("yyyy/MM/dd HH:mm:ss"),
                endTime = b.endTime.ToString("yyyy/MM/dd HH:mm:ss"),
                message = b.message,
                TotalMessages = b.TotalMessages,
                TotalSuccessfullySent = b.TotalSuccessfullySent,
                TotalFailed = b.TotalFailed,
                TotalDelivery = b.TotalDelivery,
                status = b.status
            });
            return Json(result, JsonRequestBehavior.AllowGet);

        }


        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Bulks_Delete([DataSourceRequest]DataSourceRequest request, [Bind] Bulk bulk)
        {
            try
            {
                TempData["message"] = null;
                if (ModelState.IsValid)
                {
                    long? serviceId = bulk.ServiceId;
                    if (serviceId.HasValue)
                    {
                        var service = db.vw_servicesServicesInfo.Where(o => o.Id == serviceId.Value).FirstOrDefault();
                        if (service != null && !string.IsNullOrEmpty(service.ServiceCode))
                        {
                            SharedLibrary.Models.ServiceModel.SharedServiceEntities entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode);
                            string cmd = "delete from " + service.databaseName + ".dbo.EventbaseMessagesBuffer where BulkId =" + bulk.Id.ToString()
                                + " and (ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.InQueue).ToString()
                                + "         or ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Paused).ToString()
                                + "         or ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString() + ")";
                            entityService.Database.CommandTimeout = 300;
                            entityService.Database.ExecuteSqlCommand(cmd);

                            db.Entry(bulk).State = System.Data.Entity.EntityState.Deleted;
                            db.SaveChanges();
                            //TempData["message"] = "حذف انجام شد";

                        }
                        else
                        {
                            ModelState.AddModelError("Service Code", "ServiceCode is not specified for" + serviceId.Value.ToString());
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Service Id", "سرویس Bulk مشخص نشده است");

                    }

                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("exception", e.Message);
            }
            return Json(new[] { bulk }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Bulks_Edit(string id)
        {
            using (SharedLibrary.Models.PortalEntities entity = new SharedLibrary.Models.PortalEntities())
            {
                var services = entity.Services.Select(o => new { Id = o.Id, ServiceCode = o.ServiceCode, ServiceCaption = o.Name }).OrderBy(o=>o.ServiceCode).ToList();
                List<SelectListItem> servicesList = new List<SelectListItem>();
                foreach (var service in services)
                {
                    servicesList.Add(new SelectListItem { Text = service.ServiceCode + "(" + service.ServiceCaption + ")", Value = service.Id.ToString() });
                }
                ViewBag.Services = servicesList;

                int bulkIdInt;
                if (!string.IsNullOrEmpty(id) && int.TryParse(id, out bulkIdInt))
                {
                    var portal = new SharedLibrary.Models.PortalEntities();
                    bool exists = portal.Bulks.Any(o => o.Id == bulkIdInt);
                    if (exists)
                    {
                        return View("Bulks_Edit", new Models.Bulks_EditViewModel(bulkIdInt));
                    }
                }
                return View("Bulks_Edit", new Models.Bulks_EditViewModel());
            }
        }

        [HttpPost]
        public ActionResult Bulks_Edit_DeleteNumbers(Models.Bulks_EditViewModel model)
        {
            return View("Bulks_Edit", model);
        }

        [HttpPost]
        public ActionResult Bulks_Edit(Models.Bulks_EditViewModel model, string save, string deleteNumbers)
        {
            if (string.IsNullOrEmpty(deleteNumbers))
            {
                return fnc_save(model);
            }
            else
            {
                return fnc_deleteNumbers(model);
            }
        }

        private ActionResult fnc_deleteNumbers(Models.Bulks_EditViewModel model)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                TempData["message"] = null;

                if (model != null && model.BulkId.HasValue)
                {
                    int serviceId = int.Parse(model.service);
                    var service = portal.vw_servicesServicesInfo.Where(o => o.Id == serviceId).FirstOrDefault();
                    if (service != null)
                    {
                        if (!string.IsNullOrEmpty(service.ServiceCode))
                        {

                            if (!string.IsNullOrEmpty(service.databaseName))
                            {
                                SharedLibrary.Models.ServiceModel.SharedServiceEntities entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode);
                                entityService.Database.CommandTimeout = 300;
                                entityService.Database.ExecuteSqlCommand("Delete from " + service.databaseName + ".dbo.EventbaseMessagesBuffer where BulkId=" + model.BulkId.ToString() + " and (ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.InQueue).ToString()
                                + "         or ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Paused).ToString()
                                + "         or ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString() + ")");
                            }
                            else
                            {
                                ModelState.AddModelError("DatabaseName", "نام پایگاه داده برای سرویس" + serviceId.ToString() + " مشخص نشده است");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("ServiceCode", "ServiceCode برای سرویس" + serviceId.ToString() + " مشخص نشده است");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ServiceID", "سرویسی با شناسه " + serviceId.ToString() + " در پایگاه داده وجود ندارد");
                    }


                    if (ModelState.IsValid)
                        TempData["message"] = "حذف پیامها انجام شد";


                }

                SharedLibrary.Models.PortalEntities entity = new SharedLibrary.Models.PortalEntities();

                var services = entity.Services.Select(o => new { Id = o.Id, ServiceCode = o.ServiceCode, ServiceCaption = o.Name }).ToList();
                List<SelectListItem> servicesList = new List<SelectListItem>();
                foreach (var service in services)
                {
                    servicesList.Add(new SelectListItem { Text = service.ServiceCode + "(" + service.ServiceCaption + ")", Value = service.Id.ToString() });
                }
                ViewBag.Services = servicesList;


                //var modelNew = new Models.Bulks_EditViewModel(id);
                ModelState.Remove("Id");
                ModelState.Remove("DateCreated");
                ModelState.Remove("PersianDateCreated");
                ModelState.Remove("TotalMessages");
                ModelState.Remove("TotalSuccessfullySent");
                ModelState.Remove("TotalRetryCount");
                ModelState.Remove("TotalRetryCountUnique");
                ModelState.Remove("TotalFailed");

                Bulk bulk = portal.Bulks.Where(o => o.Id == model.BulkId).FirstOrDefault();
                if (bulk != null)
                {
                    model.BulkId = bulk.Id;
                    model.DateCreated = bulk.DateCreated.HasValue ? bulk.DateCreated.Value.ToString("yyyy/MM/dd HH:mm:ss") : "";
                    model.PersianDateCreated = bulk.PersianDateCreated;
                    model.TotalMessages = bulk.TotalMessages.HasValue ? bulk.TotalMessages.Value : 0;
                    model.TotalSuccessfullySent = bulk.TotalSuccessfullySent.HasValue ? bulk.TotalSuccessfullySent.Value : 0;
                    model.TotalRetryCount = bulk.TotalRetry.HasValue ? bulk.TotalRetry.Value : 0;
                    model.TotalRetryCountUnique = bulk.TotalRetryUnique.HasValue ? bulk.TotalRetryUnique.Value : 0;
                    model.TotalFailed = bulk.TotalFailed.HasValue ? bulk.TotalFailed.Value : 0;
                }

                return View("Bulks_Edit", model);
            }
        }

        private ActionResult fnc_save(Models.Bulks_EditViewModel model)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                TempData["message"] = null;
                SharedLibrary.Models.Bulk bulk = null;
                if (model != null)
                {
                    if (model.endTime <= model.startTime)
                    {
                        ModelState.AddModelError("Compare Times", "زمان پایان باید بعد از زمان شروع باشد");
                    }
                    if (model.readSize < 0 || model.readSize > int.MaxValue)
                    {
                        ModelState.AddModelError("Read Size", "Read Size should be between 0 to " + int.MaxValue.ToString());
                    }




                    if (!string.IsNullOrEmpty(model.BulkName))
                    {
                        int count = portal.Bulks.Where(o => o.bulkName == model.BulkName && (!model.BulkId.HasValue || (model.BulkId.HasValue && o.Id != model.BulkId))).Count();
                        if (count > 0)
                        {
                            ModelState.AddModelError("Bulk Name", "عنوان Bulk تکراری است");
                        }
                        //int count = portal.Bulks.Where(o => o.bulkName == model.BulkName && (model.Id == 0 || (model.Id > 0 && o.Id != model.Id))).Count();
                        //if (count > 0)
                        //{
                        //    ModelState.AddModelError("Bulk Name", "عنوان Bulk تکراری است");
                        //}
                    }

                    if (!model.BulkId.HasValue)
                    {
                        if (model.status == SharedLibrary.MessageHandler.BulkStatus.FinishedAll
                            || model.status == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime
                            || model.status == SharedLibrary.MessageHandler.BulkStatus.Running)
                        {
                            ModelState.AddModelError("Bulk Name", "امکان انتخاب وضعیت Running/FinishedAll/FinishedByTime توسط کاربر وجود ندارد");
                        }
                        if (model.startTime.HasValue && model.startTime.Value < DateTime.Now)
                        {
                            ModelState.AddModelError("passedTime", "زمان تغییرات به دلیل شروع Bulk به پایان رسیده است");
                        }
                        if (ModelState.IsValid)
                        {
                            bulk = new SharedLibrary.Models.Bulk();
                            bulk.bulkName = model.BulkName;
                            bulk.DateCreated = DateTime.Now;
                            bulk.endTime = model.endTime.Value;
                            bulk.message = model.message;
                            bulk.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            bulk.readSize = model.readSize;
                            bulk.retryCount = model.retryCount;
                            bulk.retryIntervalInSeconds = model.retryIntervalInSeconds;
                            bulk.resetVerySlowSending = model.resetVerySlowSending;
                            bulk.resetTooSlowSending = model.resetTooSlowSending;
                            bulk.ServiceId = int.Parse(model.service);
                            bulk.startTime = model.startTime.Value;
                            bulk.status = (int)model.status;
                            bulk.tps = model.tps;
                            portal.Bulks.Add(bulk);
                            portal.SaveChanges();

                            model.BulkId = bulk.Id;
                        }

                    }
                    else
                    {
                        bulk = portal.Bulks.Where(o => o.Id == model.BulkId).FirstOrDefault();
                        if (bulk != null)
                        {
                            if ((bulk.status != (int)model.status))
                            {
                                if (model.status == SharedLibrary.MessageHandler.BulkStatus.FinishedAll
                           || model.status == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime
                           || model.status == SharedLibrary.MessageHandler.BulkStatus.Running)
                                {

                                    ModelState.AddModelError("Bulk Name", "امکان انتخاب وضعیت Running/FinishedAll/FinishedByTime توسط کاربر وجود ندارد");
                                }
                            }

                            if (ModelState.IsValid)
                            {
                                bulk.bulkName = model.BulkName;
                                //bulk.DateCreated = DateTime.Now;
                                bulk.endTime = model.endTime.Value;
                                bulk.message = model.message;
                                //bulk.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                bulk.readSize = model.readSize;
                                bulk.retryCount = model.retryCount;
                                bulk.retryIntervalInSeconds = model.retryIntervalInSeconds;
                                bulk.resetVerySlowSending = model.resetVerySlowSending;
                                bulk.resetTooSlowSending = model.resetTooSlowSending;
                                bulk.ServiceId = int.Parse(model.service);
                                bulk.startTime = model.startTime.Value;
                                bulk.status = (int)model.status;
                                bulk.tps = model.tps;
                                portal.Entry(bulk).State = System.Data.Entity.EntityState.Modified;
                                portal.SaveChanges();

                            }
                        }
                    }
                    if (ModelState.IsValid)
                        TempData["message"] = "ثبت انجام شد. پس از زمان " + model.startTime + " (زمان شروع) امکان ثبت تغییرات جدید وجود ندارد";


                }

                SharedLibrary.Models.PortalEntities entity = new SharedLibrary.Models.PortalEntities();

                var services = entity.Services.Select(o => new { Id = o.Id, ServiceCode = o.ServiceCode, ServiceCaption = o.Name }).ToList();
                List<SelectListItem> servicesList = new List<SelectListItem>();
                foreach (var service in services)
                {
                    servicesList.Add(new SelectListItem { Text = service.ServiceCode + "(" + service.ServiceCaption + ")", Value = service.Id.ToString() });
                }
                ViewBag.Services = servicesList;
                //ModelState.Clear();

                //var modelNew = new Models.Bulks_EditViewModel(id);
                ModelState.Remove("Id");
                ModelState.Remove("BulkId");
                ModelState.Remove("DateCreated");
                ModelState.Remove("PersianDateCreated");
                ModelState.Remove("TotalMessages");
                ModelState.Remove("TotalSuccessfullySent");
                ModelState.Remove("TotalRetryCount");
                ModelState.Remove("TotalRetryCountUnique");
                ModelState.Remove("TotalFailed");
                if (bulk != null)
                {
                    
                    model.BulkId = bulk.Id;
                    model.DateCreated = bulk.DateCreated.HasValue ? bulk.DateCreated.Value.ToString("yyyy/MM/dd HH:mm:ss") : "";
                    model.PersianDateCreated = bulk.PersianDateCreated;
                    model.TotalMessages = bulk.TotalMessages.HasValue ? bulk.TotalMessages.Value : 0;
                    model.TotalSuccessfullySent = bulk.TotalSuccessfullySent.HasValue ? bulk.TotalSuccessfullySent.Value : 0;
                    model.TotalRetryCount = bulk.TotalRetry.HasValue ? bulk.TotalRetry.Value : 0;
                    model.TotalRetryCountUnique = bulk.TotalRetryUnique.HasValue ? bulk.TotalRetryUnique.Value : 0;
                    model.TotalFailed = bulk.TotalFailed.HasValue ? bulk.TotalFailed.Value : 0;
                }

                return View("Bulks_Edit", model);
            }
        }
        [HttpGet]
        public ActionResult Bulks_Numbers(string id)
        {
            this.sb_fillViewBags();
            int bulkIdInt;
            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out bulkIdInt))
            {
                using (var portal = new SharedLibrary.Models.PortalEntities())
                {
                    bool exists = portal.Bulks.Any(o => o.Id == bulkIdInt);
                    if (exists)
                    {
                        return View("Bulks_Numbers", new Models.Bulks_NumbersViewModel(bulkIdInt));
                    }
                }
            }
            return View("Bulks_Numbers", new Models.Bulks_NumbersViewModel());
        }


        [HttpPost]
        public ActionResult Bulks_Numbers(Models.Bulks_NumbersViewModel model)
        {
            try
            {
                ProgressHub progress = new ProgressHub();

                bool saved = false;
                int affectedRows = 0;
                string fileType = "";
                if (!model.BulkId.HasValue)
                {
                    ModelState.AddModelError("Id Error", "Model Id is not specified");
                }
                else
                {
                    var bulk = db.Bulks.Where(o => o.Id == model.BulkId).FirstOrDefault();
                    if (bulk != null)
                    {
                        if (bulk.startTime< DateTime.Now)
                        {
                            ModelState.AddModelError("passedTime", "زمان تغییرات پس از شروع برنامه Bulk به پایان رسیده است");
                        }
                    }
                }
                if (ModelState.IsValid)
                {
                    TempData["message"] = null;
                    saved = true;
                    if (model.bulkFileType == "sqlTable")
                    {
                        progress.SendMessage("Transfer MobileNumbers", 50);
                        affectedRows = this.fnc_transferMobileNumbers(model.BulkId.Value, model.sqlTableName, model.mobileNumberStartsWith98);
                        progress.SendMessage("Transfer Completed", 100);
                        fileType = "Table:" + model.sqlTableName;
                    }

                    else
                    {
                        string uniqueTableName;
                        if (model.destinationType == "createTable" || !model.save)
                        {
                            progress.SendMessage("Create Table", 10);
                            uniqueTableName = this.fnc_getUniqueTableName();
                            this.fnc_createTable(uniqueTableName);
                        }
                        else
                        {
                            uniqueTableName = model.destinationSqlTableName;
                            if (model.deleteOldData)
                            {
                                progress.SendMessage("Delete Old Data", 10);
                                this.fnc_deleteOldData(uniqueTableName);
                            }
                        }
                        if (model.bulkFileType == "file" || model.bulkFileType == "upload")
                        {
                            string fileName = "";
                            if (model.bulkFileType == "upload")
                            {
                                if (Request.Files.Count == 0 || Request.Files[0].ContentLength == 0)
                                {
                                    ModelState.AddModelError("No file specified", "فایلی مشخص نشده است");
                                    //TempData["message"] = "";
                                }
                                else
                                {
                                    progress.SendMessage("Upload File", 30);
                                    fileName = Server.MapPath("~/files/bulks") + "\\" + Request.Files[0].FileName;
                                    Request.Files[0].SaveAs(fileName);
                                }
                            }
                            else fileName = Server.MapPath("~/files/bulks") + "\\" + model.filePath;


                            fileType = "File:" + Path.GetFileName(fileName);
                            progress.SendMessage("Import File to Table", 50);
                            this.fnc_importFile(fileName, uniqueTableName, model.mobileNumberStartsWith98);

                        }
                        else if (model.bulkFileType == "list")
                        {
                            fileType = "List:";
                            progress.SendMessage("Import List to Table", 50);
                            this.fnc_importList(model.bulkList.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries), uniqueTableName, model.mobileNumberStartsWith98);


                        }
                        if (ModelState.IsValid)
                        {
                            progress.SendMessage("Import Numbers to BulkTable", 70);
                            affectedRows = this.fnc_transferMobileNumbers(model.BulkId.Value, uniqueTableName, model.mobileNumberStartsWith98);

                            if (!model.save)
                            {
                                progress.SendMessage("Delete temporary table", 90);
                                this.fnc_dropTable(uniqueTableName);
                            }
                        }
                        progress.SendMessage("Import is finished", 100);
                    }
                    if (saved)
                    {
                        using (var portal = new SharedLibrary.Models.PortalEntities())
                        {
                            var bulk = portal.Bulks.Where(o => o.Id == model.BulkId).FirstOrDefault();
                            if (bulk != null)
                            {
                                bulk.BulkFile = (string.IsNullOrEmpty(bulk.BulkFile) ? "" : bulk.BulkFile + ";") + fileType;
                                if (affectedRows > 0)
                                    bulk.TotalMessages = (!bulk.TotalMessages.HasValue ? 0 : bulk.TotalMessages) + affectedRows;
                                portal.Entry(bulk).State = System.Data.Entity.EntityState.Modified;
                                portal.SaveChanges();
                            }
                        }
                        TempData["message"] = ("تعداد " + (affectedRows == 0 ? "0" : affectedRows.ToString("#,##")) + " رکورد انتقال پیدا کرد");
                        TempData["saved"] = true;
                    }

                }

            }
            catch (Exception ex)
            {
                this.ModelState.AddModelError("Exception", "Exception has been occured!!! : " + ex.Message);
                logs.Error("Portal:Areas:Tools:Controllers:BulkController:Bulks_Numbers: Exception has been occured", ex);
            }
            this.sb_fillViewBags();
            return View("Bulks_Numbers", model);
        }

        private bool fnc_deleteOldData(string recommendedName)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                portal.Database.ExecuteSqlCommand("truncate TABLE dbo." + recommendedName);

            }
            return true;
        }
        private string fnc_getUniqueTableName()
        {
            string tableExists = "";
            string recommendedName = "bulkNumbers_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                do
                {
                    recommendedName = "bulkNumbers" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    tableExists = portal.Database.SqlQuery<string>("SELECT top 1 table_name FROM information_schema.tables WHERE (table_type = 'base table' and TABLE_NAME = '" + recommendedName + "')").FirstOrDefault();

                } while (!string.IsNullOrEmpty(tableExists));
            }
            return recommendedName;
        }

        private bool fnc_createTable(string recommendedName)
        {
            if (string.IsNullOrEmpty(recommendedName))
                throw new ArgumentException("Recommended Name of the table is not specified");

            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                string tableExists = portal.Database.SqlQuery<string>("SELECT top 1 table_name FROM information_schema.tables WHERE (table_type = 'base table' and TABLE_NAME = '" + recommendedName + "')").FirstOrDefault();
                if (!string.IsNullOrEmpty(tableExists))
                {
                    throw new ArgumentException("Recommended Name '" + recommendedName + "' exists in portal database");
                }
                //SqlCommand cmd = new SqlCommand();
                //cmd.Connection = new SqlConnection(portal.Database.Connection.ConnectionString);
                //cmd.CommandText = "CREATE TABLE dbo." + recommendedName + " (mobileNumber nvarchar(100) NULL )  ON[PRIMARY]";
                //cmd.Connection.Open();
                //cmd.ExecuteNonQuery();
                //cmd.CommandText = "CREATE NONCLUSTERED INDEX IX_ModileNumber_" + recommendedName + " ON dbo." + recommendedName + "(mobileNumber)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]";
                //cmd.ExecuteNonQuery();
                //cmd.Connection.Close();
                portal.Database.ExecuteSqlCommand("CREATE TABLE dbo." + recommendedName + " (mobileNumber nvarchar(100) NULL )  ON[PRIMARY]");
                portal.Database.ExecuteSqlCommand("CREATE NONCLUSTERED INDEX IX_ModileNumber_" + recommendedName + " ON dbo." + recommendedName + "(mobileNumber)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]");

            }
            return true;
        }
        private bool fnc_importFile(string filePath, string tableName, bool startWith98)
        {
            string cmd = "";
            if (startWith98)
                cmd = "BULK INSERT portal.dbo." + tableName + " FROM '" + filePath + "'"
                + "WITH(FORMATFILE = '" + Path.GetDirectoryName(filePath) + "\\Format98.xml')";
            else
                cmd = "BULK INSERT portal.dbo." + tableName + " FROM '" + filePath + "'"
                + "WITH(FORMATFILE = '" + Path.GetDirectoryName(filePath) + "\\Format.xml')";

            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                portal.Database.CommandTimeout = 180;
                portal.Database.ExecuteSqlCommand(cmd);
            }
            return true;
        }
        private bool fnc_importList(string[] mobileNumbers, string tableName, bool startWith98)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("MobileNumber"));
            if (startWith98)
            {
                for (int i = 0; i <= mobileNumbers.Length - 1; i++)
                    dt.Rows.Add("0" + mobileNumbers[i].Substring(2, 10));
            }
            else
            {
                for (int i = 0; i <= mobileNumbers.Length - 1; i++)
                    dt.Rows.Add(mobileNumbers[i].Substring(0, 11));
            }
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(portal.Database.Connection.ConnectionString);
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BatchSize = 10000;
                bulkCopy.BulkCopyTimeout = 180;
                bulkCopy.WriteToServer(dt);
            }
            return true;
        }
        private int fnc_transferMobileNumbers(int bulkId, string tableName, bool startWith98)
        {
            string cmd = "exec sp_transferBulkNumbers " + bulkId.ToString() + ",'" + tableName + "'," + (startWith98 ? "1" : "0");
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                portal.Database.CommandTimeout = 180;
                //return portal.Database.ExecuteSqlCommand(cmd);
                return portal.Database.SqlQuery<int>(cmd).FirstOrDefault();
            }
            return 0;
        }
        private bool fnc_dropTable(string tableName)
        {
            string cmd = "drop table [" + tableName + "]";
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                portal.Database.CommandTimeout = 180;
                portal.Database.ExecuteSqlCommand(cmd);
            }
            return true;
        }
        private void sb_fillViewBags()
        {
            List<string> files = Directory.GetFiles(Server.MapPath("~/files/bulks"), "*.txt").ToList();
            files.AddRange(Directory.GetFiles(Server.MapPath("~/files/bulks"), "*.csv").ToList());

            List<SelectListItem> fileNamesList = new List<SelectListItem>();
            foreach (var fileName in files)
            {
                fileNamesList.Add(new SelectListItem { Text = Path.GetFileName(fileName), Value = Path.GetFileName(fileName) });
            }
            ViewBag.serverFilesNames = fileNamesList;

            using (SharedLibrary.Models.PortalEntities db = new SharedLibrary.Models.PortalEntities())
            {
                var tableNamesAndViews = db.Database.SqlQuery<string>("SELECT table_name FROM information_schema.tables WHERE (table_type = 'base table' and TABLE_NAME like 'bulkNumbers%') or (table_type = 'view' and TABLE_NAME like 'vw_bulkNumbers%') order by table_name").ToList();
                List<SelectListItem> tablesAndViewsList = new List<SelectListItem>();
                foreach (var table in tableNamesAndViews)
                {
                    tablesAndViewsList.Add(new SelectListItem { Text = table, Value = table });
                }
                ViewBag.sqlTablesAndViews = tablesAndViewsList;

                var tableNames = db.Database.SqlQuery<string>("SELECT table_name FROM information_schema.tables WHERE (table_type = 'base table' and TABLE_NAME like 'bulkNumbers%') order by table_name").ToList();
                List<SelectListItem> tablesList = new List<SelectListItem>();
                foreach (var table in tableNames)
                {
                    tablesList.Add(new SelectListItem { Text = table, Value = table });
                }
                ViewBag.sqlTables = tablesList;
            }
        }
    }
}