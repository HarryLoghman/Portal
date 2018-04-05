using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadReceiveProcessorService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread processThread;
        private Thread telepromoProcessThread;
        private Thread hubProcessThread;
        private Thread irancellProcessThread;
        private Thread mobinoneProcessThread;
        private Thread mobinOneMapfaProcessThread;
        private Thread samssonTciProcessThread;
        private Thread pardisPlatformProcessThread;
        private Thread getIrancellsMoThread;
        private Thread getFtpThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //processThread = new Thread(MessageProcessorWorkerThread);
            //processThread.IsBackground = true;
            //processThread.Start();

            pardisPlatformProcessThread = new Thread(PardisPLatformMessageProcessorWorkerThread);
            pardisPlatformProcessThread.IsBackground = true;
            pardisPlatformProcessThread.Start();

            telepromoProcessThread = new Thread(TelepromoMessageProcessorWorkerThread);
            telepromoProcessThread.IsBackground = true;
            telepromoProcessThread.Start();

            hubProcessThread = new Thread(HubMessageProcessorWorkerThread);
            hubProcessThread.IsBackground = true;
            hubProcessThread.Start();

            irancellProcessThread = new Thread(IrancellMessageProcessorWorkerThread);
            irancellProcessThread.IsBackground = true;
            irancellProcessThread.Start();

            mobinoneProcessThread = new Thread(MobinOneMessageProcessorWorkerThread);
            mobinoneProcessThread.IsBackground = true;
            mobinoneProcessThread.Start();

            mobinOneMapfaProcessThread = new Thread(MobinOneMapfaMessageProcessorWorkerThread);
            mobinOneMapfaProcessThread.IsBackground = true;
            mobinOneMapfaProcessThread.Start();

            samssonTciProcessThread = new Thread(SamssonTciMessageProcessorWorkerThread);
            samssonTciProcessThread.IsBackground = true;
            samssonTciProcessThread.Start();

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
                //if (!processThread.Join(3000))
                //{
                //    processThread.Abort();
                //}
                if (!telepromoProcessThread.Join(3000))
                {
                    telepromoProcessThread.Abort();
                }
                if (!hubProcessThread.Join(3000))
                {
                    hubProcessThread.Abort();
                }
                if (!irancellProcessThread.Join(3000))
                {
                    irancellProcessThread.Abort();
                }
                if (!mobinoneProcessThread.Join(3000))
                {
                    mobinoneProcessThread.Abort();
                }
                if (!mobinOneMapfaProcessThread.Join(3000))
                {
                    mobinOneMapfaProcessThread.Abort();
                }
                if (!samssonTciProcessThread.Join(3000))
                {
                    samssonTciProcessThread.Abort();
                }
                if (!pardisPlatformProcessThread.Join(3000))
                {
                    pardisPlatformProcessThread.Abort();
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

        private void TelepromoMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.TelepromoProcess();
                Thread.Sleep(1000);
            }
        }

        private void HubMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.HubProcess();
                Thread.Sleep(1000);
            }
        }

        private void IrancellMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.IrancellProcess();
                Thread.Sleep(1000);
            }
        }

        private void MobinOneMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.MobinOneProcess();
                Thread.Sleep(1000);
            }
        }

        private void MobinOneMapfaMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.MobinOneMapfaProcess();
                Thread.Sleep(1000);
            }
        }

        private void SamssonTciMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.SamssonTciProcess();
                Thread.Sleep(1000);
            }
        }

        private void PardisPLatformMessageProcessorWorkerThread()
        {
            var messageProcessor = new MessageProcesser();
            while (!shutdownEvent.WaitOne(0))
            {
                messageProcessor.PardisPlatformProcess();
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
                if (DateTime.Now.Hour == 10 && DateTime.Now.Minute > 30 && DateTime.Now.Minute < 40)
                {
                    ftp.TelepromoIncomeReport();
                    Thread.Sleep(23 * 60 * 60 * 1000);
                }
                Thread.Sleep(2 * 60 * 1000);
            }
        }
    }
}
