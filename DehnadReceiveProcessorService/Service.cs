using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadReceiveProcessorService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread processThread;
        private Thread getIrancellsMoThread;
        private Thread getFtpThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            processThread = new Thread(MessageProcessorWorkerThread);
            processThread.IsBackground = true;
            processThread.Start();

            getIrancellsMoThread = new Thread(IrancellMoWorkerThread);
            getIrancellsMoThread.IsBackground = true;
            getIrancellsMoThread.Start();

            getFtpThread = new Thread(FtpWorkerThread);
            getFtpThread.IsBackground = true;
            getFtpThread.Start();
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
                if (!getIrancellsMoThread.Join(3000))
                {
                    getIrancellsMoThread.Abort();
                }
                if (!getFtpThread.Join(3000))
                {
                    getFtpThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }

        }

        private void MessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.Process();
                Thread.Sleep(1000);
            }
        }

        private void IrancellMoWorkerThread()
        {
            var irancell = new Irancell();
            while (!shutdownEvent.WaitOne(0))
            {
                irancell.GetMo();
                Thread.Sleep(5000);
            }
        }

        private void FtpWorkerThread()
        {
            var ftp = new Ftp();
            while (!shutdownEvent.WaitOne(0))
            {
                if (DateTime.Now.Hour == 10 && DateTime.Now.Minute > 5 && DateTime.Now.Minute < 10)
                {
                    ftp.ProcessTelepromoFtpFiles();
                    Thread.Sleep(23 * 60 * 60 * 1000);
                }
                Thread.Sleep(2 * 60 * 1000);
            }
        }
    }
}
