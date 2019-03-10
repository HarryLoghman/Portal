using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace DehnadMonitoringService
{
    class MonitoringCycles
    {
        static Dictionary<long, string> v_dicCheckedCycles;
        /// <summary>
        /// check if a charging cycle start at the specified time
        /// </summary>
        public static void MonitorCyclesStartingStatus()
        {
            if (v_dicCheckedCycles == null)
            {
                v_dicCheckedCycles = new Dictionary<long, string>();
            }
            TimeSpan ts = DateTime.Now.TimeOfDay;
            string day = ((int)DateTime.Now.DayOfWeek).ToString();
            string strDate = DateTime.Now.ToString("yyyy-MM-dd");
            int i;
            long serviceId;


            DateTime startTimeServiceCycle;

            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                var entryServiceCycle = entityPortal.serviceCyclesNews.FirstOrDefault(o => o.startTime <= ts && ts <= o.endTime && o.daysOfWeekOrDate == strDate);
                if (entryServiceCycle == null)
                {
                    entryServiceCycle = entityPortal.serviceCyclesNews.FirstOrDefault(o => o.startTime <= ts && ts <= o.endTime && o.daysOfWeekOrDate.Contains(day));
                }

                if (entryServiceCycle == null)
                    return;


                #region give cycle to start
                while (DateTime.Parse(DateTime.Now.Date.ToString("yyyy-MM-dd") + " " + entryServiceCycle.startTime.ToString("c"))
                    .AddMinutes(Properties.Settings.Default.CharingCycleWaitToStartInSeconds * 1000) > DateTime.Now)
                {
                    //wait one minute after cycle start time
                    Thread.Sleep(1000 * 60);
                }
                #endregion

                startTimeServiceCycle = DateTime.Parse(DateTime.Now.Date.ToString("yyyy-MM-dd") + " " + entryServiceCycle.startTime.ToString("c"));

                string serviceIdStr = entryServiceCycle.servicesIDs;
                if (string.IsNullOrEmpty(serviceIdStr))
                    return;
                string[] arrServiceIds = serviceIdStr.Split(';');
                using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
                {
                    for (i = 0; i <= arrServiceIds.Length - 1; i++)
                    {

                        if (!long.TryParse(arrServiceIds[i], out serviceId))
                        {
                            continue;
                        }
                        var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == serviceId);
                        if (entryService != null)
                        {
                            continue;
                        }
                        #region if we check service cycle and date before
                        var keyValue = v_dicCheckedCycles.Where(o => o.Key == serviceId).Select(o => (KeyValuePair<long, string>?)o).FirstOrDefault();
                        if (keyValue.HasValue && keyValue.Value.Value == DateTime.Now.ToString("yyyy-MM-dd") + "-" + entryServiceCycle.cycleNumber.ToString())
                            continue;
                        #endregion

                        using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(entryService.ServiceCode))
                        {
                            if (!entityService.Singlecharges.Any(o => o.CycleNumber == entryServiceCycle.cycleNumber
                              && o.Price > 0
                              && o.DateCreated >= startTimeServiceCycle))
                            {
                                #region if there is any row to charge
                                var maxTries = entityPortal.serviceCyclesNews.Count(o => o.state == 1 &&
                                    (o.servicesIDs == arrServiceIds[i] || o.servicesIDs.StartsWith(arrServiceIds[i] + ";")
                                    || o.servicesIDs.Contains(";" + arrServiceIds[i] + ";") || o.servicesIDs.EndsWith(";" + arrServiceIds[i]))
                                    && o.daysOfWeekOrDate == strDate);
                                if (maxTries == 0)
                                {
                                    entityPortal.serviceCyclesNews.Count(o => o.state == 1 &&
                                    (o.servicesIDs == arrServiceIds[i] || o.servicesIDs.StartsWith(arrServiceIds[i] + ";")
                                    || o.servicesIDs.Contains(";" + arrServiceIds[i] + ";") || o.servicesIDs.EndsWith(";" + arrServiceIds[i])));
                                }
                                if (maxTries > 0)
                                {
                                    int rowCount = SharedLibrary.ServiceHandler.getActiveSubscribersForCharge(entryService.databaseName, entryService.Id, DateTime.Now, entryService.chargePrice.Value
                                        , maxTries, entryServiceCycle.cycleNumber, false, false, null, null, null).Count();
                                    if (rowCount > 0)
                                    {
                                        SharedLibrary.HelpfulFunctions.sb_sendNotification_Monitoring(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Charging cycle of " + entryService.ServiceCode + " is not started after waiting for " + Properties.Settings.Default.CharingCycleWaitToStartInSeconds.ToString() + " seconds");
                                    }
                                }
                                #endregion

                            }
                            #region addCheckstatus
                            if (!keyValue.HasValue)
                            {
                                v_dicCheckedCycles.Add(serviceId, DateTime.Now.ToString("yyyy-MM-dd") + "-" + entryServiceCycle.cycleNumber.ToString());
                            }
                            else
                            {
                                v_dicCheckedCycles[serviceId] = DateTime.Now.ToString("yyyy-MM-dd") + "-" + entryServiceCycle.cycleNumber.ToString();
                            }
                            #endregion
                        }

                    }
                }
            }
        }
    }
}
