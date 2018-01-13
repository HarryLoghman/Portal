using System;
using System.ServiceProcess;
using System.Threading;

namespace DehnadNotificationService
{
    public partial class Service : ServiceBase
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Thread telegramBotThread;
        private Thread incomeThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            telegramBotThread = new Thread(TelegramBotWorkerThread);
            telegramBotThread.IsBackground = true;
            telegramBotThread.Start();

            incomeThread = new Thread(IncomeWorkerThread);
            incomeThread.IsBackground = true;
            incomeThread.Start();

        }

        protected override void OnStop()
        {
            try
            {
                shutdownEvent.Set();
                if (!telegramBotThread.Join(3000))
                {
                    telegramBotThread.Abort();
                }
                if (!incomeThread.Join(3000))
                {
                    incomeThread.Abort();
                }
            }
            catch (Exception exp)
            {
                logs.Info(" Exception in thread termination ");
                logs.Error(" Exception in thread termination " + exp);
            }
        }

        private void TelegramBotWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                TelegramBot.StartBot();
                Thread.Sleep(1000);
            }
        }

        private void IncomeWorkerThread()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                Income.IncomeDiffrenceByHour();
                Thread.Sleep(60 * 1000);
            }
        }
    }
}
