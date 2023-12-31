﻿using System;
using System.Data.Entity;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace DehnadTahChinService
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
        private DateTime v_lastExecutionTime;
        int v_cycleNumber = -1;
        public static string v_dbName = "Tahchin";
        public static int maxChargeLimit = 300;
        public static int maxServiceTries = 4;
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //prepareAutochargeThread = new Thread(AutochargeWorkerThread);
            //prepareAutochargeThread.IsBackground = true;
            //prepareAutochargeThread.Start();
            this.v_lastExecutionTime = DateTime.Now;

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
                shutdownEvent.Set();

                //if (!prepareAutochargeThread.Join(3000))
                //{
                //    prepareAutochargeThread.Abort();
                //}

                if (!prepareEventbaseThread.Join(3000))
                {
                    prepareEventbaseThread.Abort();
                }

                if (!sendMessageThread.Join(3000))
                {
                    sendMessageThread.Abort();
                }

                if (!statisticsThread.Join(3000))
                {
                    statisticsThread.Abort();
                }

                //if (!timedThread.Join(3000))
                //{
                //    timedThread.Abort();
                //}

                //if (!informApplicationThread.Join(3000))
                //{
                //    informApplicationThread.Abort();
                //}

                if (!singlechargeInstallmentThread.Join(3000))
                {
                    singlechargeInstallmentThread.Abort();
                }

                //shutdownEvent.Set();
                //if (!singlechargeInstallmentBalancerThread.Join(3000))
                //{
                //    singlechargeInstallmentBalancerThread.Abort();
                //}

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
            return;
            //int installmentCycleNumber = 1;
            ////TimeSpan timeDiffs = TimeSpan.FromSeconds(1);
            ////if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("10:30:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
            ////    installmentCycleNumber = 2;
            ////else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:00:00"))
            ////    installmentCycleNumber = 3;
            ////else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") /*&& DateTime.Now.TimeOfDay < TimeSpan.Parse("22:00:00")*/)
            ////    installmentCycleNumber = 4;
            //////else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("22:00:00"))
            //////    installmentCycleNumber = 5;
            ////else
            ////    installmentCycleNumber = 1;
            //int tps;
            //var entityType = typeof(TahChinLibrary.Models.TahChinEntities);
            //var cycleType = typeof(TahChinLibrary.Models.InstallmentCycle);

            //while (!shutdownEvent.WaitOne(0))
            //{
                
            //    bool isInMaintenanceTime = false;
            //    try
            //    {
            //        using (var entity = new TahChinLibrary.Models.TahChinEntities())
            //        {
            //            var isInMaintenace = entity.Settings.FirstOrDefault(o => o.Name == "IsInMaintenanceTime");
            //            if (isInMaintenace != null)
            //                isInMaintenanceTime = isInMaintenace.Value == "True" ? true : false;
            //        }
            //        if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")) || isInMaintenanceTime == true)
            //        {
            //            logs.Info("isInMaintenanceTime:" + isInMaintenanceTime);
            //            installmentCycleNumber = 1;
            //            Thread.Sleep(1000);
            //        }
            //        else
            //        {
            //            //var singlechargeInstallment = new SinglechargeInstallmentClassNew();
            //            //var income = singlechargeInstallment.ProcessInstallment(-1, SharedLibrary.ThrottleMTN.getOperatorTPS(), 45, 55);
            //            //Thread.Sleep(180 * 1000);
            //            var startTime = DateTime.Now;
            //            var singlechargeInstallment = new SinglechargeInstallmentClassNew();
            //            var serviceCode = Properties.Settings.Default.ServiceCode;
            //            string aggregatorName = SharedLibrary.ServiceHandler.GetAggregatorNameFromServiceCode(Properties.Settings.Default.ServiceCode); ;

            //            var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(serviceCode, aggregatorName);
            //            using (var portal = new SharedLibrary.Models.PortalEntities())
            //            {

            //                TimeSpan ts = DateTime.Now.TimeOfDay;
            //                string serviceId = serviceAdditionalInfo["serviceId"];
            //                string day = ((int)DateTime.Now.DayOfWeek).ToString();
            //                string strDate = DateTime.Now.ToString("yyyy-MM-dd");
            //                var serviceCycles = portal.serviceCycles.Where(o => o.serviceID.ToString() == serviceId && o.startTime <= ts && ts <= o.endTime && (o.daysOfWeek == strDate)).Select(o => o);
            //                if (serviceCycles.Count() == 0)
            //                    serviceCycles = portal.serviceCycles.Where(o => o.serviceID.ToString() == serviceId && o.startTime <= ts && ts <= o.endTime && o.daysOfWeek.Contains(day)).Select(o => o);
            //                if (serviceCycles.Count() == 1)
            //                {

            //                    bool forciblyExecute = false;
            //                    if (v_cycleNumber != serviceCycles.FirstOrDefault().cycleNumber)
            //                    {
            //                        v_cycleNumber = serviceCycles.FirstOrDefault().cycleNumber;
            //                        forciblyExecute = true;
            //                    }
            //                    tps = Properties.Settings.Default.DefaultSingleChargeTakeSize;

            //                    installmentCycleNumber = serviceCycles.FirstOrDefault().cycleNumber;
            //                    tps = serviceCycles.FirstOrDefault().minTPS.HasValue ? serviceCycles.FirstOrDefault().minTPS.Value : tps;

            //                    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber, SharedLibrary.ThrottleMTN.getOperatorTPS(), tps, this.v_lastExecutionTime,forciblyExecute);
            //                    var endTime = DateTime.Now;
            //                    var duration = endTime - startTime;
            //                    SharedLibrary.InstallmentHandler.InstallmentCycleToDb(entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
            //                    this.v_lastExecutionTime = DateTime.Now;

            //                }
            //                else Thread.Sleep(10000);
            //            }

            //            //if (installmentCycleNumber == 1 && DateTime.Now.TimeOfDay < TimeSpan.Parse("10:30:00"))
            //            //{
            //            //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
            //            //    var endTime = DateTime.Now;
            //            //    var duration = endTime - startTime;
            //            //    SharedLibrary.InstallmentHandler.InstallmentCycleToDb(entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
            //            //    installmentCycleNumber++;
            //            //}
            //            //else if (installmentCycleNumber == 2 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("10:30:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("16:00:00"))
            //            //{
            //            //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
            //            //    var endTime = DateTime.Now;
            //            //    var duration = endTime - startTime;
            //            //    SharedLibrary.InstallmentHandler.InstallmentCycleToDb(entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
            //            //    installmentCycleNumber++;
            //            //}
            //            //else if (installmentCycleNumber == 3 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("16:00:00") && DateTime.Now.TimeOfDay < TimeSpan.Parse("20:00:00"))
            //            //{
            //            //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
            //            //    var endTime = DateTime.Now;
            //            //    var duration = endTime - startTime;
            //            //    SharedLibrary.InstallmentHandler.InstallmentCycleToDb(entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
            //            //    installmentCycleNumber++;
            //            //}
            //            //else if (installmentCycleNumber == 4 && DateTime.Now.TimeOfDay >= TimeSpan.Parse("20:00:00") /*&& DateTime.Now.TimeOfDay < TimeSpan.Parse("22:00:00")*/)
            //            //{
            //            //    var income = singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
            //            //    var endTime = DateTime.Now;
            //            //    var duration = endTime - startTime;
            //            //    SharedLibrary.InstallmentHandler.InstallmentCycleToDb(entityType, cycleType, installmentCycleNumber, (long)duration.TotalSeconds, income);
            //            //    installmentCycleNumber++;
            //            //}
            //            //else
            //            //    Thread.Sleep(1000);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        logs.Error("Exception in SinglechargeInstallmentWorkerThread: ", e);
            //        Thread.Sleep(1000);
            //    }
            //}
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
