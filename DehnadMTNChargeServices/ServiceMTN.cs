using log4net.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DehnadMTNChargeServices
{
    public partial class ServiceMTN : ServiceBase
    {
        public ServiceMTN()
        {
            InitializeComponent();
        }

        internal List<ServiceCharge> v_lst_services;
        internal int v_maxTries = 4;
        private Thread singlechargeInstallmentThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        internal static long v_startTimeTicks;
        protected override void OnStart(string[] args)
        {
            Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Service DehnadMTNChargeServices has been started");
            singlechargeInstallmentThread = new Thread(SinglechargeInstallmentWorkerThread);
            singlechargeInstallmentThread.IsBackground = true;
            singlechargeInstallmentThread.Start();
        }
        internal void StartDebugging(string[] args)
        {
            Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Service DehnadMTNChargeServices has been started");
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }
        protected override void OnStop()
        {
            try
            {
                Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Service DehnadMTNChargeServices has been stopped");
                shutdownEvent.Set();
                if (!singlechargeInstallmentThread.Join(3000))
                {
                    singlechargeInstallmentThread.Abort();
                }

            }
            catch (Exception exp)
            {
                Program.logs.Info("Exception in thread termination ");
                Program.logs.Error("Exception in thread termination " + exp);
            }

        }

        static internal void StopService(string reason)
        {
            Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, reason);
            ServiceController sc = new ServiceController("DehnadMTNChargeServices");
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));

        }

        private void SinglechargeInstallmentWorkerThread()
        {

            while (!shutdownEvent.WaitOne(0))
            {
                Program.sb_setLoggerQuarterNumber();
                bool isInMaintenanceTime = false;
                try
                {

                    if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")) || isInMaintenanceTime == true)
                    {
                        Program.logs.Info("isInMaintenanceTime:" + isInMaintenanceTime);
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        TimeSpan ts = DateTime.Now.TimeOfDay;
                        string day = ((int)DateTime.Now.DayOfWeek).ToString();
                        string strDate = DateTime.Now.ToString("yyyy-MM-dd");
                        
                        using (var portal = new SharedLibrary.Models.PortalEntities())
                        {
                            var serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime && (o.daysOfWeekOrDate == strDate)).Select(o => o);
                            if (serviceCycles.Count() == 0)
                            {
                                serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime && o.daysOfWeekOrDate.Contains(day)).Select(o => o);
                            }
                            if (serviceCycles.Count() >= 1)
                            {
                                int cycleNumber, element;
                                List<SharedLibrary.Models.serviceCyclesNew> lstServiceCycles = serviceCycles.ToList();

                                v_lst_services = new List<ServiceCharge>();
                                v_startTimeTicks = DateTime.Now.Ticks;
                                int? tpsTotal = portal.Operators.Where(o => o.OperatorName == "MTN").Select(o => o.tps).FirstOrDefault();
                                if (!tpsTotal.HasValue) tpsTotal = 180;


                                for (element = 0; element <= lstServiceCycles.Count - 1; element++)
                                {
                                    cycleNumber = lstServiceCycles[element].cycleNumber;
                                    string servicesIDs = lstServiceCycles[element].servicesIDs;
                                    string minTPSs = lstServiceCycles[element].minTPSs;
                                    string[] servicesIDsArr = servicesIDs.Split(';');
                                    string[] minTPSsArr = minTPSs.Split(';');
                                    string aggregatorServiceId;
                                    int serviceId;

                                    if (servicesIDsArr.Length != minTPSsArr.Length)
                                    {
                                        Program.logs.Error("Number of services (" + servicesIDsArr.Length + ") does not match the number of TPSs (" + minTPSsArr.Length + ")");
                                        return;
                                    }
                                    string notStartReason;

                                    for (int i = 0; i <= servicesIDsArr.Length - 1; i++)
                                    {
                                        if (servicesIDsArr[i] == "")
                                            continue;
                                        serviceId = int.Parse(servicesIDsArr[i]);
                                        aggregatorServiceId = portal.ServiceInfoes.Where(o => o.ServiceId == serviceId).Select(o => o.AggregatorServiceId).FirstOrDefault();
                                        if (string.IsNullOrEmpty(aggregatorServiceId))
                                            continue;
                                        if (servicesIDsArr[i] == "10025")
                                        {
                                            ServiceChargeTahchin sc = new ServiceChargeTahchin(int.Parse(servicesIDsArr[i]), int.Parse(minTPSsArr[i]), aggregatorServiceId, v_maxTries, cycleNumber);
                                            if (!sc.fnc_canStartCharging(cycleNumber, out notStartReason))
                                            {
                                                Program.logs.Warn(sc.prp_serviceCode + " is not started because of : " + notStartReason);
                                                Thread.Sleep(1000);
                                            }
                                            else v_lst_services.Add(sc);
                                        }
                                        else if (servicesIDsArr[i] == "10036")
                                        {
                                            ServiceChargeDambel sc = new ServiceChargeDambel(int.Parse(servicesIDsArr[i]), int.Parse(minTPSsArr[i]), aggregatorServiceId, v_maxTries, cycleNumber);
                                            if (!sc.fnc_canStartCharging(cycleNumber, out notStartReason))
                                            {
                                                Program.logs.Warn(sc.prp_serviceCode + " is not started because of : " + notStartReason);
                                                Thread.Sleep(1000);
                                            }
                                            else v_lst_services.Add(sc);
                                        }
                                        else if (servicesIDsArr[i] == "10028")
                                        {
                                            ServiceChargeMusicYad sc = new ServiceChargeMusicYad(int.Parse(servicesIDsArr[i]), int.Parse(minTPSsArr[i]), aggregatorServiceId, v_maxTries, cycleNumber);
                                            if (!sc.fnc_canStartCharging(cycleNumber, out notStartReason))
                                            {
                                                Program.logs.Warn(sc.prp_serviceCode + " is not started because of : " + notStartReason);
                                                Thread.Sleep(1000);
                                            }
                                            else v_lst_services.Add(sc);
                                        }
                                        else if (servicesIDsArr[i] == "10039")
                                        {
                                            ServiceChargePorshetab sc = new ServiceChargePorshetab(int.Parse(servicesIDsArr[i]), int.Parse(minTPSsArr[i]), aggregatorServiceId, v_maxTries, cycleNumber);
                                            if (!sc.fnc_canStartCharging(cycleNumber, out notStartReason))
                                            {
                                                Program.logs.Warn(sc.prp_serviceCode + " is not started because of : " + notStartReason);
                                                Thread.Sleep(1000);
                                            }
                                            else v_lst_services.Add(sc);
                                        }
                                        else continue;

                                    }
                                }


                                if (v_lst_services.Count > 0)
                                {
                                    DateTime startTime = DateTime.Now;
                                    chargeServices cs = new chargeServices();
                                    cs.sb_chargeAll(tpsTotal.Value, v_lst_services, v_startTimeTicks);
                                    //while (!cs.prp_finished)
                                    //{

                                    //}

                                    Program.logs.Info("installmentCycleNumber: ended");
                                    Program.logs.Info("InstallmentJob ended!");
                                    
                                }

                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Program.logs.Error("Exception in SinglechargeInstallmentWorkerThread: ", e);
                    Program.sb_sendNotification(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Exception in SinglechargeInstallmentWorkerThread: (" + e.Message + ")");
                    Thread.Sleep(1000);
                }
            }
        }

    }
}
