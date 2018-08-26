using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace DehnadJhoobinService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread getDataThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            getDataThread = new Thread(GetDataWorkerThread);
            getDataThread.IsBackground = true;
            getDataThread.Start();
        }

        protected override void OnStop()
        {
            try
            {

                shutdownEvent.Set();
                if (!getDataThread.Join(3000))
                {
                    getDataThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }

        }

        
        private void GetDataWorkerThread()
        {
            var dataClass = new GetData();
            while (!shutdownEvent.WaitOne(0))
            {
                dataClass.GetJoobinData();
                Thread.Sleep(1000);
            }
        }
    }
}
