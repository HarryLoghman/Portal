using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadAvvalPodService
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
            //var singlechargeInstallment = new SinglechargeInstallmentClass();
            //int installmentCycleNumber = 1;
            //while (!shutdownEvent.WaitOne(0))
            //{
            //    if (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 15)
            //    {
            //        installmentCycleNumber = 1;
            //        Thread.Sleep(50 * 60 * 1000);
            //    }
            //    else
            //    {
            //        if (DateTime.Now.Hour >= 7)
            //        {
            //            singlechargeInstallment.ProcessInstallment(installmentCycleNumber);
            //            installmentCycleNumber++;
            //        }
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
                Thread.Sleep(60 * 1000);
            }
        }
    }
}
