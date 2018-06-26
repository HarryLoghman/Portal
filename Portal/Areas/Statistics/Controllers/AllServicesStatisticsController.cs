using System;
using System.Linq;
using System.Web.Mvc;
using SharedLibrary;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Data.Entity;
using System.Dynamic;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Portal.Areas.Statistics.Controllers
{
    public class AllServicesStatisticsController : Controller
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Statistics/AllServicesStatistics
        private SharedLibrary.Models.PortalEntities db = new SharedLibrary.Models.PortalEntities();

        public ActionResult Index()
        {
            ViewBag.ServiceName = "گزارش جامع سرویس ها";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Data_Read([DataSourceRequest]DataSourceRequest request)
        {
            try
            {
                using (var entity = new SharedLibrary.Models.PortalEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    dynamic result = new ExpandoObject();
                    List<dynamic> income = new List<dynamic>();
                    List<dynamic> subscriptions = new List<dynamic>();
                    List<dynamic> activeSubscriptions = new List<dynamic>();
                    List<dynamic> aggregateIncome = new List<dynamic>();
                    var allStats = entity.vw_DehnadAllServicesStatistics.ToList();
                    var services = allStats.Select(o => o.ServiceName).Distinct().ToList();
                    foreach (var service in services)
                    {
                        var serviceData = allStats.Where(o => o.ServiceName == service);
                        dynamic incomeItem = new ExpandoObject();
                        incomeItem.Name = serviceData.FirstOrDefault().ServiceName;
                        var incomeData = serviceData.Where(o => o.SumOfSinglechargeSuccessfulCharge != null).Select(o => new { Date = SharedLibrary.Date.DateTimeToUnixTimestampLocal(o.Date), Charge = Convert.ToDouble(o.SumOfSinglechargeSuccessfulCharge) }).OrderBy(o => o.Date).ToList();
                        List<dynamic> incomeList = new List<dynamic>();
                        foreach (var item in incomeData)
                        {
                            double[] incomeItemArray = new double[2];
                            incomeItemArray[0] = item.Date;
                            incomeItemArray[1] = item.Charge;
                            incomeList.Add(incomeItemArray);
                        }
                        incomeItem.Data = incomeList;
                        income.Add(incomeItem);
                        dynamic subItem = new ExpandoObject();
                        subItem.Name = incomeItem.Name;
                        var subscriptionData = serviceData.Select(o => new { Date = SharedLibrary.Date.DateTimeToUnixTimestampLocal(o.Date), Sub = Convert.ToDouble(o.NumberOfSubscriptions) }).OrderBy(o => o.Date).ToList();
                        var unSubscriptionData = serviceData.Select(o => new { Date = SharedLibrary.Date.DateTimeToUnixTimestampLocal(o.Date), Usub = Convert.ToDouble(o.NumberOfUnsubscriptions) }).OrderBy(o => o.Date).ToList();
                        List<dynamic> subscriptionDataList = new List<dynamic>();
                        List<dynamic> unSubscriptionDataList = new List<dynamic>();
                        foreach (var item in subscriptionData)
                        {
                            double[] itemArray = new double[2];
                            itemArray[0] = item.Date;
                            itemArray[1] = item.Sub;
                            subscriptionDataList.Add(itemArray);
                        }
                        foreach (var item in unSubscriptionData)
                        {
                            double[] itemArray = new double[2];
                            itemArray[0] = item.Date;
                            itemArray[1] = item.Usub;
                            unSubscriptionDataList.Add(itemArray);
                        }
                        subItem.SubscriptionData = subscriptionDataList;
                        subItem.UnsubscriptionData = unSubscriptionDataList;
                        subscriptions.Add(subItem);
                    }

                    foreach (var service in services)
                    {
                        var serviceData = allStats.Where(o => o.ServiceName == service);
                        dynamic subItem = new ExpandoObject();
                        subItem.Name = serviceData.FirstOrDefault().ServiceName;
                        var subscriptionData = serviceData.Select(o => new { Date = SharedLibrary.Date.DateTimeToUnixTimestampLocal(o.Date), Sub = Convert.ToDouble(o.TotalSubscribers) }).OrderBy(o => o.Date).ToList();
                        List<dynamic> subscriptionDataList = new List<dynamic>();
                        foreach (var item in subscriptionData)
                        {
                            double[] itemArray = new double[2];
                            itemArray[0] = item.Date;
                            itemArray[1] = item.Sub;
                            subscriptionDataList.Add(itemArray);
                        }
                        subItem.Data = subscriptionDataList;
                        activeSubscriptions.Add(subItem);
                    }

                    var aggregateIncomeData = allStats.Where(o => o.SumOfSinglechargeSuccessfulCharge != null).GroupBy(o => o.Date).Select(o => new { Date = SharedLibrary.Date.DateTimeToUnixTimestampLocal(o.Key), Charge = Convert.ToDouble(o.Sum(x => x.SumOfSinglechargeSuccessfulCharge)) }).OrderBy(o => o.Date).ToList();
                    List<dynamic> aggergateIncomeList = new List<dynamic>();
                    foreach (var item in aggregateIncomeData)
                    {
                        double[] incomeItemArray = new double[2];
                        incomeItemArray[0] = item.Date;
                        incomeItemArray[1] = item.Charge;
                        aggergateIncomeList.Add(incomeItemArray);
                    }

                    aggregateIncome.Add(aggergateIncomeList);
                    result.income = income;
                    result.subscriptions = subscriptions;
                    result.activeSubscriptions = activeSubscriptions;
                    result.aggregateIncome = aggregateIncome;
                    var json = JsonConvert.SerializeObject(result);
                    return Content(json);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in Data_Read:", e);
            }
            return Json("", JsonRequestBehavior.AllowGet);
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