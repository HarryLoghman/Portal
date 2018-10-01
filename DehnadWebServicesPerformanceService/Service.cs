using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace DehnadWebServicesPerformanceService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread pingThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            pingThread = new Thread(PingWorkerThread);
            pingThread.IsBackground = true;
            pingThread.Start();
        }

        protected override void OnStop()
        {
            try
            {

                shutdownEvent.Set();
                if (!pingThread.Join(3000))
                {
                    pingThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }

        }

        
        private void PingWorkerThread()
        {
            //var pingClass = new Ping();
            //while (!shutdownEvent.WaitOne(0))
            //{
            //    Thread.Sleep(1000);
            //}
        }
    }
}
