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
    public class CampaignsChargesController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private PortalEntities db = new PortalEntities();
        // GET: Tools/CampaignsCharges
        public ActionResult Index()
        {
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CampaignsCharges_Read([DataSourceRequest]DataSourceRequest request)
        {

            var campaigns = (from c in db.CampaignsCharges
                             join sers in db.Services on c.ServiceId equals sers.Id
                             select new
                             {
                                 Id = c.Id,
                                 campaignName = c.campaignName,
                                 campaingType = c.campaignType,
                                 serviceName = sers.Name,
                                 ServiceId = c.ServiceId,
                                 price = c.price,
                                 keyword = c.keyword,
                                 message = c.message,
                                 status = c.status,
                                 startTime = c.startTime,
                                 endTime = c.endTime,
                                 replaceWelcomeMessage = c.replaceWelcomeMessage,
                                 TotalRequests = db.CampaignsMobileNumbers.Count(o => o.campaignId == c.Id && o.campaignType.ToLower() == "charges"),
                                 TotalPaid = db.CampaignsMobileNumbers.Count(o => o.campaignId == c.Id && o.campaignType.ToLower() == "charges" && o.paid.HasValue && o.paid.Value),
                             }).OrderBy(o => o.startTime).ThenBy(o => o.campaignName);
            DataSourceResult result = campaigns.ToDataSourceResult(request, c => new
            {
                Id = c.Id,
                campaignName = c.campaignName,
                campaignType = c.campaingType,
                serviceName = c.serviceName,
                ServiceId = c.ServiceId,
                price = c.price,
                keyword = c.keyword,
                message = c.message,
                status = c.status,
                startTime = c.startTime.HasValue ? c.startTime.Value.ToString("yyyy/MM/dd HH:mm:ss") : null,
                endTime = c.endTime.HasValue ? c.endTime.Value.ToString("yyyy/MM/dd HH:mm:ss") : null,
                replaceWelcomeMessage = c.replaceWelcomeMessage,
                TotalRequests = c.TotalRequests,
                TotalPaid = c.TotalPaid,
            });
            return Json(result, JsonRequestBehavior.AllowGet);

        }


        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult CampaignsCharges_Delete([DataSourceRequest]DataSourceRequest request, [Bind] CampaignsCharge campaign)
        {
            try
            {
                TempData["message"] = null;
                if (ModelState.IsValid)
                {
                    var cnt = db.CampaignsMobileNumbers.Count(o => o.campaignType == "charges" && o.campaignId == campaign.Id);
                    if (cnt > 0)
                    {
                        TempData["message"] = "برای کمپین انتخاب شده تعداد " + cnt.ToString() + " در خواست شارژ آمده و امکان حذف کمپین وجود ندارد";
                    }
                    else
                    {
                        db.Entry(campaign).State = System.Data.Entity.EntityState.Deleted;
                        db.SaveChanges();
                    }

                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("exception", e.Message);
            }
            return Json(new[] { campaign }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult CampaignsCharges_Edit(string id)
        {
            using (SharedLibrary.Models.PortalEntities entity = new SharedLibrary.Models.PortalEntities())
            {
                var services = entity.Services.Select(o => new { Id = o.Id, ServiceCode = o.ServiceCode, ServiceCaption = o.Name }).OrderBy(o => o.ServiceCode).ToList();
                List<SelectListItem> servicesList = new List<SelectListItem>();
                foreach (var service in services)
                {
                    servicesList.Add(new SelectListItem { Text = service.ServiceCode + "(" + service.ServiceCaption + ")", Value = service.Id.ToString() });
                }
                ViewBag.Services = servicesList;

                int campaignIdInt;
                if (!string.IsNullOrEmpty(id) && int.TryParse(id, out campaignIdInt))
                {
                    var portal = new SharedLibrary.Models.PortalEntities();
                    bool exists = portal.CampaignsCharges.Any(o => o.Id == campaignIdInt);
                    if (exists)
                    {
                        return View("CampaignsCharges_Edit", new Models.CampaignsCharges_EditViewModel(campaignIdInt));
                    }
                }
                return View("CampaignsCharges_Edit", new Models.CampaignsCharges_EditViewModel());
            }
        }


        [HttpPost]
        public ActionResult CampaignsCharges_Edit(Models.CampaignsCharges_EditViewModel model)
        {
            return fnc_save(model);

        }

        private ActionResult fnc_save(Models.CampaignsCharges_EditViewModel model)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                TempData["message"] = null;
                SharedLibrary.Models.CampaignsCharge campaigns = null;
                if (model != null)
                {
                    if (model.endTime.HasValue && model.startTime.HasValue)
                    {
                        if (model.endTime <= model.startTime)
                        {
                            ModelState.AddModelError("Compare Times", "زمان پایان باید بعد از زمان شروع باشد");
                        }
                    }


                    if (!string.IsNullOrEmpty(model.CampaignName))
                    {
                        int count = portal.CampaignsCharges.Where(o => o.campaignName == model.CampaignName && (!model.CampaignId.HasValue || (model.CampaignId.HasValue && o.Id != model.CampaignId))).Count();
                        if (count > 0)
                        {
                            ModelState.AddModelError("Campaign Name", "عنوان کمپین تکراری است");
                        }
                        //int count = portal.Bulks.Where(o => o.bulkName == model.BulkName && (model.Id == 0 || (model.Id > 0 && o.Id != model.Id))).Count();
                        //if (count > 0)
                        //{
                        //    ModelState.AddModelError("Bulk Name", "عنوان Bulk تکراری است");
                        //}
                    }

                    if (!model.CampaignId.HasValue)
                    {

                        if (ModelState.IsValid)
                        {
                            campaigns = new SharedLibrary.Models.CampaignsCharge();
                            campaigns.campaignName = model.CampaignName;
                            campaigns.campaignType = (int)model.CampaignType;
                            campaigns.endTime = model.endTime;
                            campaigns.keyword = model.keyword;
                            campaigns.message = model.message;
                            campaigns.price = model.price;
                            campaigns.replaceWelcomeMessage = model.replaceWelcomeMessage;
                            campaigns.ServiceId = int.Parse(model.service);
                            campaigns.startTime = model.startTime;
                            campaigns.status = (int)model.status;
                            portal.CampaignsCharges.Add(campaigns);
                            portal.SaveChanges();

                            model.CampaignId = campaigns.Id;
                        }

                    }
                    else
                    {
                        campaigns = portal.CampaignsCharges.Where(o => o.Id == model.CampaignId).FirstOrDefault();
                        if (campaigns != null)
                        {
                            if (ModelState.IsValid)
                            {
                                campaigns.campaignName = model.CampaignName;
                                campaigns.campaignType = (int)model.CampaignType;
                                campaigns.endTime = model.endTime;
                                campaigns.keyword = model.keyword;
                                campaigns.message = model.message;
                                campaigns.price = model.price;
                                campaigns.replaceWelcomeMessage = model.replaceWelcomeMessage;
                                campaigns.ServiceId = int.Parse(model.service);
                                campaigns.startTime = model.startTime;
                                campaigns.status = (int)model.status;
                                portal.Entry(campaigns).State = System.Data.Entity.EntityState.Modified;
                                portal.SaveChanges();

                            }
                        }
                    }
                    if (ModelState.IsValid)
                        TempData["message"] = "ثبت انجام شد";


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
                ModelState.Remove("CampaignId");
                ModelState.Remove("TotalRequests");
                ModelState.Remove("TotalPaid");
                if (campaigns != null)
                {

                    model.CampaignId = campaigns.Id;
                    model.TotalRequests = portal.CampaignsMobileNumbers.Count(o => o.campaignId == campaigns.Id && o.campaignType.ToLower() == "charges");
                    model.TotalPaid = portal.CampaignsMobileNumbers.Count(o => o.campaignId == campaigns.Id && o.campaignType.ToLower() == "charges"
                     && o.paid.HasValue && o.paid.Value);
                }

                return View("CampaignsCharges_Edit", model);
            }
        }
    }
}