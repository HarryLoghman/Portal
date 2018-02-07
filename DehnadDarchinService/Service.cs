using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadDarchinService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread singlechargeInstallmentThread;
        private Thread singlechargeQueueThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            singlechargeInstallmentThread = new Thread(SinglechargeInstallmentWorkerThread);
            singlechargeInstallmentThread.IsBackground = true;
            singlechargeInstallmentThread.Start();

            singlechargeQueueThread = new Thread(SinglechargeQueueWorkerThread);
            singlechargeQueueThread.IsBackground = true;
            singlechargeQueueThread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                shutdownEvent.Set();

                if (!singlechargeInstallmentThread.Join(3000))
                {
                    singlechargeInstallmentThread.Abort();
                }

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
