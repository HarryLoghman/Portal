using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DehnadMCIFtpChargingService
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


        internal void StartDebugging(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
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

            downloader down = new downloader();
            while (!shutdownEvent.WaitOne(0))
            {
                bool isInMaintenanceTime = false;
                try
                {

                    if ((DateTime.Now.TimeOfDay >= TimeSpan.Parse("23:45:00") || DateTime.Now.TimeOfDay < TimeSpan.Parse("00:01:00")) || isInMaintenanceTime == true)
                    {
                        isInMaintenanceTime = true;
                        Program.logs.Info("isInMaintenanceTime:" + isInMaintenanceTime);
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Program.logs.Info("downloaderFunction: started");

                        try
                        {
                            string parameterPath = "parameter\\parameter.txt";
                            if (File.Exists(parameterPath))
                            {
                                Program.logs.Info("start of reading parameter file");
                                string[] datesAndOperatorSID = File.ReadAllLines(parameterPath);
                                string[] operatorsSID;
                                DateTime date;
                                string directoryNameInFormatYYYYMMDD;
                                int i;
                                for (i = 0; i <= datesAndOperatorSID.Length - 1; i++)
                                {
                                    if (string.IsNullOrEmpty(datesAndOperatorSID[i]))
                                    {
                                        continue;
                                    }
                                    operatorsSID = datesAndOperatorSID[i].Split(';');
                                    if (operatorsSID.Length == 0) continue;
                                    directoryNameInFormatYYYYMMDD = operatorsSID[0];
                                    if (DateTime.TryParse(directoryNameInFormatYYYYMMDD.Substring(0, 4) + "/" + directoryNameInFormatYYYYMMDD.Substring(4, 2) + "/" + directoryNameInFormatYYYYMMDD.Substring(6, 2) + " 00:00:00 ", out date))
                                    {
                                        if (operatorsSID.Length > 1)
                                        {
                                            operatorsSID = operatorsSID.Skip(1).ToArray();
                                        }
                                        else operatorsSID = null;
                                        down.updateSingleCharge(directoryNameInFormatYYYYMMDD, operatorsSID, true);
                                    }
                                    else
                                    {
                                        Program.logs.Error("Date should be in format of yyyyMMDD, the given value is " + operatorsSID[0]);
                                    }
                                }

                                File.Move(parameterPath, parameterPath.Remove(parameterPath.Length - 4, 4) + "_processed" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                                Program.logs.Info("end of reading parameter file");
                            }
                        }
                        catch (Exception ex)
                        {
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "Exception in downloaderFunction reading parameter file: " + ex.Message);
                            Program.logs.Error("Exception in downloaderFunction reading parameter file: ", ex);
                        }

                        //check today
                        down.updateSingleCharge();
                        int nDaysBefore = 0;
                        if (int.TryParse(Properties.Settings.Default.CheckNDaysBefore, out nDaysBefore))
                        {
                            if (nDaysBefore != 0)
                            {
                                int i;
                                for (i = 1; i <= nDaysBefore; i++)
                                {
                                    Program.logs.Info("downloaderFunction:started:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                                    down.updateSingleCharge(DateTime.Now.AddDays(-1 * i).ToString("yyyyMMdd"), null, false);
                                    Program.logs.Info("downloaderFunction:ended:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                                }
                            }
                        }


                        Program.logs.Info("downloaderFunction: Ended");
                        int timeInterval;
                        if (!int.TryParse(Properties.Settings.Default.TimeIntervalInSecond, out timeInterval))
                        {
                            timeInterval = 60 * 30;
                        }
                        Thread.Sleep(1000 * timeInterval);


                    }
                }
                catch (Exception e)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "MCIFtpDownloader:" + "Exception in downloaderFunction: " + e.Message);
                    Program.logs.Error("Exception in downloaderFunction: ", e);
                    Thread.Sleep(1000);
                }
            }

        }
    }
}
