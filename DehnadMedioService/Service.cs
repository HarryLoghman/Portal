using ChargingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace DehnadMedioService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread prepareAutochargeThread;
        private Thread prepareEventbaseThread;
        private Thread sendMessageThread;
        private Thread statisticsThread;
        private Thread timedThread;
        private Thread informApplicationThread;
        private Thread singlechargeInstallmentThread;
        private Thread singlechargeInstallmentBalancerThread;
        private Thread singlechargeQueueThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        internal List<ServiceCharge> v_lst_services;
        internal static long v_startTimeTicks;
        internal int v_maxTries = 8;
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //prepareAutochargeThread = new Thread(AutochargeWorkerThread);
            //prepareAutochargeThread.IsBackground = true;
            //prepareAutochargeThread.Start();

            prepareEventbaseThread = new Thread(EventbaseWorkerThread);
            prepareEventbaseThread.IsBackground = true;
            prepareEventbaseThread.Start();

            sendMessageThread = new Thread(SendMessageWorkerThread);
            sendMessageThread.IsBackground = true;
            sendMessageThread.Start();

            statisticsThread = new Thread(StatisticsWorkerThread);
            statisticsThread.IsBackground = true;
            statisticsThread.Start();

            //timedThread = new Thread(TiemdWorkerThread);
            //timedThread.IsBackground = true;
            //timedThread.Start();

            //informApplicationThread = new Thread(InformApplicationWorkerThread);
            //informApplicationThread.IsBackground = true;
            //informApplicationThread.Start();

            singlechargeInstallmentThread = new Thread(SinglechargeInstallmentWorkerThread);
            singlechargeInstallmentThread.IsBackground = true;
            singlechargeInstallmentThread.Start();

            //singlechargeInstallmentBalancerThread = new Thread(SinglechargeInstallmentBalancerWorkerThread);
            //singlechargeInstallmentBalancerThread.IsBackground = true;
            //singlechargeInstallmentBalancerThread.Start();

            singlechargeQueueThread = new Thread(SinglechargeQueueWorkerThread);
            singlechargeQueueThread.IsBackground = true;
            singlechargeQueueThread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                //shutdownEvent.Set();
                //if (!prepareAutochargeThread.Join(3000))
                //{
                //    prepareAutochargeThread.Abort();
                //}

                shutdownEvent.Set();
                if (!prepareEventbaseThread.Join(3000))
                {
                    prepareEventbaseThread.Abort();
                }

                shutdownEvent.Set();
                if (!sendMessageThread.Join(3000))
                {
                    sendMessageThread.Abort();
                }

                shutdownEvent.Set();
                if (!statisticsThread.Join(3000))
                {
                    statisticsThread.Abort();
                }

                //shutdownEvent.Set();
                //if (!timedThread.Join(3000))
                //{
                //    timedThread.Abort();
                //}

                //shutdownEvent.Set();
                //if (!informApplicationThread.Join(3000))
                //{
                //    informApplicationThread.Abort();
                //}

                shutdownEvent.Set();
                if (!singlechargeInstallmentThread.Join(3000))
                {
                    singlechargeInstallmentThread.Abort();
                }

                //shutdownEvent.Set();
                //if (!singlechargeInstallmentBalancerThread.Join(3000))
                //{
                //    singlechargeInstallmentBalancerThread.Abort();
                //}

                shutdownEvent.Set();
                if (!singlechargeQueueThread.Join(3000))
                {
                    singlechargeQueueThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }

        }

        private void AutochargeWorkerThread()
        {
            var autoCharge = new Autocharge();
            while (!shutdownEvent.WaitOne(0))
            {
                autoCharge.Job();
                Thread.Sleep(1000);
            }
        }

        private void EventbaseWorkerThread()
        {
            var eventbase = new Eventbase();
            while (!shutdownEvent.WaitOne(0))
            {
                //eventbase.InsertEventbaseMessagesToQueue();
                Thread.Sleep(1000);
            }
        }

        private void SendMessageWorkerThread()
        {
            var messageSender = new Sender();
            while (!shutdownEvent.WaitOne(0))
            {
                messageSender.SendHandler();
                Thread.Sleep(1000);
            }
        }


        private void StatisticsWorkerThread()
        {
            var statistic = new Statistic();
            while (!shutdownEvent.WaitOne(0))
            {
                statistic.Process();
                Thread.Sleep(10000);
            }
        }

        private void TiemdWorkerThread()
        {
            var timed = new Timed();
            while (!shutdownEvent.WaitOne(0))
            {
                timed.ProcessTempMessageBufferTable();
                Thread.Sleep(60000);
            }
        }

        private void InformApplicationWorkerThread()
        {
            //var inform = new InformApplication();
            //while (!shutdownEvent.WaitOne(0))
            //{
            //    inform.Inform();
            //    Thread.Sleep(1000);
            //}
        }
        private void SinglechargeInstallmentWorkerThread()
        {
            bool isInMaintenanceTime = false;
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {

                    if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("07:00:00")) || isInMaintenanceTime == true)
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

                            var serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime && (o.servicesIDs == "10030" || o.servicesIDs.StartsWith("10030;") || o.servicesIDs.Contains(";10030;") || o.servicesIDs.EndsWith("10030;")) && (o.daysOfWeekOrDate == strDate)).Select(o => o);
                            if (serviceCycles.Count() == 0)
                            {
                                serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime && (o.servicesIDs == "10030" || o.servicesIDs.StartsWith("10030;") || o.servicesIDs.Contains(";10030;") || o.servicesIDs.EndsWith("10030;")) && o.daysOfWeekOrDate.Contains(day)).Select(o => o);
                            }
                            if (serviceCycles.Count() >= 1)
                            {
                                int cycleNumber, element;
                                List<SharedLibrary.Models.serviceCyclesNew> lstServiceCycles = serviceCycles.ToList();

                                v_lst_services = new List<ServiceCharge>();
                                v_startTimeTicks = DateTime.Now.Ticks;
                                int? tpsTotal = portal.Services.Where(o => o.Id == 10030).Select(o => o.tps).FirstOrDefault();
                                if (!tpsTotal.HasValue) tpsTotal = 20;


                                for (element = 0; element <= lstServiceCycles.Count - 1; element++)
                                {
                                    cycleNumber = lstServiceCycles[element].cycleNumber;
                                    string servicesIDs = lstServiceCycles[element].servicesIDs;
                                    string minTPSs = lstServiceCycles[element].minTPSs;
                                    string cycleChargePrices = lstServiceCycles[element].cycleChargePrices;
                                    string[] servicesIDsArr = servicesIDs.Split(';');
                                    string[] minTPSsArr = minTPSs.Split(';');
                                    string[] cycleChargePricesArr = cycleChargePrices.Split(';');
                                    string aggregatorServiceId;
                                    int serviceId;

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

                                    for (int i = 0; i <= servicesIDsArr.Length - 1; i++)
                                    {
                                        if (servicesIDsArr[i] == "")
                                            continue;
                                        serviceId = int.Parse(servicesIDsArr[i]);
                                        aggregatorServiceId = portal.ServiceInfoes.Where(o => o.ServiceId == serviceId).Select(o => o.AggregatorServiceId).FirstOrDefault();
                                        if (string.IsNullOrEmpty(aggregatorServiceId))
                                            continue;
                                        if (servicesIDsArr[i] == "10030")
                                        {
                                            ServiceChargeMobinOneMapfa sc = new ServiceChargeMobinOneMapfa(int.Parse(servicesIDsArr[i]), int.Parse(minTPSsArr[i]), v_maxTries, cycleNumber, int.Parse(cycleChargePricesArr[i]));
                                            if (!sc.fnc_canStartCharging(cycleNumber, out notStartReason))
                                            {
                                                Program.logs.Warn(sc.prp_service.ServiceCode + " is not started because of : " + notStartReason);
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
                                    ChargingController cs = new ChargingController();
                                    cs.sb_chargeAll(tpsTotal.Value, v_lst_services, v_startTimeTicks, "Medio");
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
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_SingleChargeGang(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Exception in SinglechargeInstallmentWorkerThread: (" + e.Message + ")");
                    Thread.Sleep(1000);
                }

            }
        }
        private void SinglechargeInstallmentWorkerThreadOld()
        {
            var singlechargeInstallment = new SinglechargeInstallmentClass();
            int installmentCycleNumber = 1;
            var entityType = typeof(SharedLibrary.Models.ServiceModel.SharedServiceEntities);
            var cycleType = typeof(SharedLibrary.Models.ServiceModel.InstallmentCycle);
            TimeSpan timeDiffs = TimeSpan.FromSeconds(1);
            if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("9:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("11:00:00"))
                installmentCycleNumber = 2;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("11:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("13:00:00"))
                installmentCycleNumber = 3;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("13:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
                installmentCycleNumber = 4;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:00:00"))
                installmentCycleNumber = 5;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("18:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:00:00"))
                installmentCycleNumber = 6;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("22:00:00"))
                installmentCycleNumber = 7;
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("22:00:00"))
                installmentCycleNumber = 8;
            else
                installmentCycleNumber = 1;
            //if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("9:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("11:00:00"))
            //    installmentCycleNumber = 2;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("11:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("13:00:00"))
            //    installmentCycleNumber = 3;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("13:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
            //    installmentCycleNumber = 4;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:00:00"))
            //    installmentCycleNumber = 5;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("18:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:59:00"))
            //    installmentCycleNumber = 6;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("19:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("19:59:00"))
            //    installmentCycleNumber = 7;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:59:00"))
            //    installmentCycleNumber = 8;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("21:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("21:59:00"))
            //    installmentCycleNumber = 9;
            //else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("22:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("22:59:00"))
            //    installmentCycleNumber = 10;
            //else
            //    installmentCycleNumber = 1;
            while (!shutdownEvent.WaitOne(0))
            {
                bool isInMaintenanceTime = false;
                try
                {
                    using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(Properties.Settings.Default.ServiceCode))
                    {
                        var isInMaintenace = entity.Settings.FirstOrDefault(o => o.Name == "IsInMaintenanceTime");
                        if (isInMaintenace != null)
                            isInMaintenanceTime = isInMaintenace.Value == "True" ? true : false;
                    }
                    if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("07:03:00")) || isInMaintenanceTime == true)
                    {
                        installmentCycleNumber = 1;
                        Thread.Sleep(/*50 * 60 * */1000);
                    }
                    else
                    {
                        var startTime = DateTime.Now;
                        if (installmentCycleNumber == 1 && DateTime.Now.TimeOfDay < TimeSpan.Parse("09:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 2 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("09:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("11:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 3 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("11:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("13:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 4 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("13:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 5 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 6 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("18:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 7 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("22:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else if (installmentCycleNumber == 8 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("22:00:00"))
                        {
                            var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                            var endTime = DateTime.Now;
                            var duration = endTime - startTime;
                            SharedLibrary.InstallmentHandler.InstallmentCycleToDb(Properties.Settings.Default.ServiceCode, entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
                            installmentCycleNumber++;
                        }
                        else
                            Thread.Sleep(1000);
                        //if (installmentCycleNumber == 1 && DateTime.Now.TimeOfDay < TimeSpan.Parse("09:00:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 2 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("09:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("11:00:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 3 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("11:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("13:00:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 4 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("13:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 5 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:00:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 6 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("18:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("18:59:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 7 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("19:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("19:59:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 8 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:59:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 9 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("21:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("21:59:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                        //else if (installmentCycleNumber == 10 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("22:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("22:59:00"))
                        //{
                        //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
                        //    var endTime = DateTime.Now;
                        //    var duration = endTime - startTime;
                        //    InstallmentCycleToDb(installmentCycleNumber, (long)duration.TotalSeconds, income);
                        //    installmentCycleNumber++;
                        //}
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SinglechargeInstallmentWorkerThread: ", e);
                    Thread.Sleep(1000);
                }
            }
        }

        private void InstallmentCycleToDb(int installmentCycleNumber, long totalSeconds, int income)
        {
            throw new NotImplementedException();
        }

        //private void SinglechargeInstallmentBalancerWorkerThread()
        //{
        //    while (!shutdownEvent.WaitOne(0))
        //    {
        //        var singlechargeInstallment = new SinglechargeInstallmentClass();
        //        if (DateTime.Now.Hour == 0 && DateTime.Now.Minute > 13 && DateTime.Now.Minute < 17)
        //        {
        //            singlechargeInstallment.InstallmentDailyBalance();
        //            Thread.Sleep(60 * 60 * 1000);
        //        }
        //        Thread.Sleep(1000);
        //    }
        //}

        private void SinglechargeQueueWorkerThread()
        {
            var singlechargeQueue = new SinglechargeQueue();
            while (!shutdownEvent.WaitOne(0))
            {
                singlechargeQueue.ProcessQueue();
                Thread.Sleep(1000);
            }
        }
    }
}
