using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChargingLibrary
{
    public class SingleChargeThread
    {

        public static void SinglechargeInstallmentWorkerThread(TimeSpan illegalStartTime, TimeSpan illegalEndTime
            , long serviceId, int maxTries, string notifIcon, bool resetVerySlowCharging, bool resetTooSlowCharging)
        {

            bool canStartCharging = false;
            bool isInMaintenanceTime = false;
            TimeSpan timeSpanNow = DateTime.Now.TimeOfDay;
            int sleepTime;
            try
            {
                if (!ServiceCharge.fnc_isChargingLegalTime(illegalStartTime, illegalEndTime, out sleepTime))
                {
                    if (sleepTime < 1000 * 60)
                    {
                        //if we have only one minute only sleep second by second
                        sleepTime = 1000;
                    }
                    else
                    {
                        Program.logs.Info("Charging is stopped for " + sleepTime / 1000 + " seconds");
                    }
                    Thread.Sleep(sleepTime);
                }
                else
                {

                    using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                    {
                        var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == serviceId);
                        if (entryService == null)
                        {
                            throw new Exception("ServiceId " + serviceId + " is not defined");
                        }

                        using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(entryService.ServiceCode))
                        {
                            var isInMaintenace = entityService.Settings.FirstOrDefault(o => o.Name == "IsInMaintenanceTime" && o.Value.ToLower() == "true");
                            if (isInMaintenace != null)
                                isInMaintenanceTime = true;
                        }
                        if (isInMaintenanceTime)
                        {
                            Program.logs.Info("isInMaintenanceTime:" + isInMaintenanceTime);
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            string aggregatorNameLower = entryService.aggregatorName.ToLower();
                            if (aggregatorNameLower == "Hamrahvas".ToLower()
                            || aggregatorNameLower == "PardisImi".ToLower()
                            || aggregatorNameLower == "PardisPlatform".ToLower()
                            || aggregatorNameLower == "Telepromo".ToLower()
                            || aggregatorNameLower == "Hub".ToLower()
                            || aggregatorNameLower == "MTN".ToLower()
                            || aggregatorNameLower == "MobinOne".ToLower()
                            || aggregatorNameLower == "SamssonTci".ToLower()
                            || aggregatorNameLower == "MciDirect".ToLower()
                            || aggregatorNameLower == "Jhoobin".ToLower())
                            {
                                throw new Exception("There is no charging implementation for " + entryService.aggregatorName);
                            }
                            else if (aggregatorNameLower == "MobinOneMapfa".ToLower()
                                || aggregatorNameLower == "TelepromoMapfa".ToLower())
                            {
                            }
                            else
                            {
                                throw new Exception("Unknown aggregator" + entryService.aggregatorName);
                            }


                            ServiceCharge serviceCharge = null;

                            TimeSpan ts = DateTime.Now.TimeOfDay;
                            string day = ((int)DateTime.Now.DayOfWeek).ToString();
                            string strDate = DateTime.Now.ToString("yyyy-MM-dd");

                            var entryServiceCycle = entityPortal.serviceCyclesNews.FirstOrDefault(o => o.startTime <= ts && ts <= o.endTime && (o.servicesIDs == serviceId.ToString() || o.servicesIDs.StartsWith(serviceId.ToString() + ";") || o.servicesIDs.Contains(";" + serviceId.ToString() + ";") || o.servicesIDs.EndsWith(";" + serviceId.ToString())) && (o.daysOfWeekOrDate == strDate));
                            if (entryServiceCycle == null)
                            {
                                entryServiceCycle = entityPortal.serviceCyclesNews.FirstOrDefault(o => o.startTime <= ts && ts <= o.endTime && (o.servicesIDs == serviceId.ToString() || o.servicesIDs.StartsWith(serviceId.ToString() + ";") || o.servicesIDs.Contains(";" + serviceId.ToString() + ";") || o.servicesIDs.EndsWith(";" + serviceId.ToString())) && o.daysOfWeekOrDate.Contains(day));

                            }
                            if (entryServiceCycle != null)
                            {

                                int cycleNumber;
                                //v_startTimeTicks = DateTime.Now.Ticks;

                                int? tpsTotal = entityPortal.Services.Where(o => o.Id == serviceId).Select(o => o.tps).FirstOrDefault();
                                if (!tpsTotal.HasValue) tpsTotal = 20;


                                cycleNumber = entryServiceCycle.cycleNumber;
                                string servicesIDs = entryServiceCycle.servicesIDs;
                                string minTPSs = entryServiceCycle.minTPSs;
                                string cycleChargePrices = entryServiceCycle.cycleChargePrices;
                                string[] servicesIDsArr = servicesIDs.Split(';');
                                string[] minTPSsArr = minTPSs.Split(';');
                                string[] cycleChargePricesArr = cycleChargePrices.Split(';');
                                string aggregatorServiceId;
                                if (servicesIDsArr.Length != minTPSsArr.Length)
                                {
                                    Program.logs.Error("Number of services (" + servicesIDsArr.Length + ") does not match the number of TPSs (" + minTPSsArr.Length + ")");
                                    return;
                                }
                                if (servicesIDsArr.Length != cycleChargePricesArr.Length)
                                {
                                    Program.logs.Error("Number of services (" + servicesIDsArr.Length + ") does not match the number of cycleChargePrice (" + cycleChargePricesArr.Length + ")");
                                    return;
                                }
                                string notStartReason;
                                int tempServiceId;
                                for (int i = 0; i <= servicesIDsArr.Length - 1; i++)
                                {
                                    if (!int.TryParse(servicesIDsArr[i], out tempServiceId))
                                        continue;
                                    //serviceId = int.Parse(servicesIDsArr[i]);
                                    aggregatorServiceId = entityPortal.ServiceInfoes.Where(o => o.ServiceId == serviceId).Select(o => o.AggregatorServiceId).FirstOrDefault();
                                    if (string.IsNullOrEmpty(aggregatorServiceId))
                                        continue;
                                    if (servicesIDsArr[i] == serviceId.ToString())
                                    {

                                        if (aggregatorNameLower == "MobinOneMapfa".ToLower())
                                        {
                                            serviceCharge = new ServiceChargeMobinOneMapfa(serviceId, int.Parse(minTPSsArr[i])
                                             , maxTries, cycleNumber, int.Parse(cycleChargePricesArr[i]), notifIcon
                                             , illegalStartTime, illegalEndTime);

                                        }
                                        else if (aggregatorNameLower == "TelepromoMapfa".ToLower())
                                        {
                                            serviceCharge = new ServiceChargeTelepromoMapfa(serviceId, int.Parse(minTPSsArr[i])
                                            , maxTries, cycleNumber, int.Parse(cycleChargePricesArr[i]), notifIcon
                                            , illegalStartTime, illegalEndTime);
                                        }
                                        if (serviceCharge != null)
                                        {
                                            if (!serviceCharge.fnc_canStartCharging(cycleNumber, illegalStartTime, illegalEndTime, out notStartReason))
                                            {
                                                Program.logs.Warn(serviceCharge.prp_service.ServiceCode + " is not started because of : " + notStartReason);
                                                Thread.Sleep(1000);
                                                canStartCharging = false;
                                                break;
                                            }
                                            else
                                            {
                                                canStartCharging = true;
                                                break;
                                            }
                                        }

                                    }

                                    else continue;

                                }


                                if (canStartCharging && entryServiceCycle != null && serviceCharge != null)
                                {
                                    DateTime startTime = DateTime.Now;
                                    ChargingController chargingController = new ChargingController();
                                    chargingController.sb_chargeAll(tpsTotal.Value, new List<ServiceCharge> { serviceCharge }, startTime.Ticks, entryService.ServiceCode, notifIcon
                                        , resetVerySlowCharging, resetTooSlowCharging);

                                    Program.logs.Info("installmentCycleNumber: ended");
                                    Program.logs.Info("InstallmentJob ended!");

                                }

                            }
                        }
                    }

                }

            }

            catch (Exception e)
            {
                Program.logs.Error("Exception in SinglechargeInstallmentWorkerThread: ", e);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, (string.IsNullOrEmpty(notifIcon) ? "" : notifIcon) + "Exception in SinglechargeInstallmentWorkerThread: (" + e.Message + ")");
                Thread.Sleep(1000);
            }


        }

    }
}
