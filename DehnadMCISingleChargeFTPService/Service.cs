using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DehnadMCISingleChargeFTPService
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        private Thread downloaderThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        protected override void OnStart(string[] args)
        {
            downloaderThread = new Thread(downloaderFunction);
            downloaderThread.IsBackground = true;
            downloaderThread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                shutdownEvent.Set();
                if (!downloaderThread.Join(3000))
                {
                    downloaderThread.Abort();
                }

            }
            catch (Exception exp)
            {
                Program.logs.Info("Exception in thread termination ");
                Program.logs.Error("Exception in thread termination " + exp);
            }
        }

        private void downloaderFunction()
        {
            while (!shutdownEvent.WaitOne(0))
            {
                bool isInMaintenanceTime = false;
                try
                {

                    if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")) || isInMaintenanceTime == true)
                    {
                        Program.logs.Info("isInMaintenanceTime:" + isInMaintenanceTime);
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Program.logs.Info("downloaderFunction: started");

                        downloader down = new downloader();
                        down.updateSingleCharge();

                        Thread.Sleep(1000 * 60 * 30);
                        Program.logs.Info("downloaderFunction: ended");

                    }
                }
                catch (Exception e)
                {
                    Program.logs.Error("Exception in downloaderFunction: ", e);
                    Thread.Sleep(1000);
                }
            }

        }
    }
}
