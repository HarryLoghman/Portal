using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadSinglechargeDeliveryProcessorService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread processThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            processThread = new Thread(SinglechargeDeliveryProcessorWorkerThread);
            processThread.IsBackground = true;
            processThread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                shutdownEvent.Set();
                if (!processThread.Join(3000))
                {
                    processThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }

        }

        private void SinglechargeDeliveryProcessorWorkerThread()
        {
            var SinglechargeDeliveryProcessor = new DeliveryProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                SinglechargeDeliveryProcessor.Process();
                Thread.Sleep(1000);
            }

        }
    }
}
