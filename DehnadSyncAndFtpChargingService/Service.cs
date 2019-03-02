using DehnadSyncAndFtpChargingService.MCI;
using DehnadSyncAndFtpChargingService.MobinOne;
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

namespace DehnadSyncAndFtpChargingService
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        private Thread downloaderMCIThread;
        private Thread syncMCIThread;
        private Thread syncMobinOneThread;
        //private Thread syncNotChargedThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        protected override void OnStart(string[] args)
        {
            downloaderMCIThread = new Thread(downloaderMCI);
            downloaderMCIThread.IsBackground = true;
            downloaderMCIThread.Start();

            syncMCIThread = new Thread(syncMCI);
            syncMCIThread.IsBackground = true;
            syncMCIThread.Start();

            syncMobinOneThread = new Thread(syncMobinOne);
            syncMCIThread.IsBackground = true;
            syncMCIThread.Start();
            //syncNotChargedThread = new Thread(syncNotChargedFunction);
            //syncNotChargedThread.IsBackground = true;
            //syncNotChargedThread.Start();
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
                if (!downloaderMCIThread.Join(3000))
                {
                    downloaderMCIThread.Abort();
                }
                if (!syncMCIThread.Join(3000))
                {
                    syncMCIThread.Abort();
                }

                //if (!syncThread.Join(3000))
                //{
                //    syncNotChargedThread.Abort();
                //}
            }
            catch (Exception exp)
            {
                Program.logs.Info("Exception in thread termination ");
                Program.logs.Error("Exception in thread termination " + exp);
            }
        }

        private void syncMobinOne()
        {
            SyncMobinOne sync = new SyncMobinOne();
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
                        TimeSpan ts = DateTime.Now.TimeOfDay;
                        string day = ((int)DateTime.Now.DayOfWeek).ToString();
                        string strDate = DateTime.Now.ToString("yyyy-MM-dd");

                        using (var portal = new SharedLibrary.Models.PortalEntities())
                        {
                            var serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime
                            && ((o.servicesIDs.ToLower() == "MobinOneSync".ToLower() || o.servicesIDs.ToLower().StartsWith("MobinOneSync;".ToLower()) || o.servicesIDs.ToLower().Contains(";MobinOneSync;".ToLower()) || o.servicesIDs.ToLower().EndsWith(";MobinOneSync".ToLower())))
                                && (o.daysOfWeekOrDate == strDate)).Select(o => o);
                            if (serviceCycles.Count() == 0)
                            {
                                serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime
                                && ((o.servicesIDs.ToLower() == "MobinOneSync".ToLower() || o.servicesIDs.ToLower().StartsWith("MobinOneSync;".ToLower()) || o.servicesIDs.ToLower().Contains(";MobinOneSync;".ToLower()) || o.servicesIDs.ToLower().EndsWith(";MobinOneSync".ToLower())))
                                && o.daysOfWeekOrDate.Contains(day)).Select(o => o);
                            }
                            if (serviceCycles.Count() >= 1)
                            {
                                sync.sb_sync(DateTime.Now.AddDays(-1 + -1 * Properties.Settings.Default.MobinOneSyncNDaysBefore)
                                    , DateTime.Now.AddDays(-1));
                                Thread.Sleep(1000 * 60 * 10);
                            }
                        }
                    }
                    Thread.Sleep(1000 * 60);

                }
                catch (Exception e)
                {
                    Program.logs.Error("MCIFtpSync:SyncFunction: ", e);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MCIFtpSync:SyncFunction: (" + e.Message + ")");
                    Thread.Sleep(1000);
                }
            }
        }

        private void syncMCI()
        {
            SyncSubscription sync = new SyncSubscription();
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
                        TimeSpan ts = DateTime.Now.TimeOfDay;
                        string day = ((int)DateTime.Now.DayOfWeek).ToString();
                        string strDate = DateTime.Now.ToString("yyyy-MM-dd");

                        using (var portal = new SharedLibrary.Models.PortalEntities())
                        {
                            var serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime
                            && ((o.servicesIDs.ToLower() == "MCIFtpSync".ToLower() || o.servicesIDs.ToLower().StartsWith("MCIFtpSync;".ToLower()) || o.servicesIDs.ToLower().Contains(";MCIFtpSync;".ToLower()) || o.servicesIDs.ToLower().EndsWith(";MCIFtpSync".ToLower())))
                                && (o.daysOfWeekOrDate == strDate)).Select(o => o);
                            if (serviceCycles.Count() == 0)
                            {
                                serviceCycles = portal.serviceCyclesNews.Where(o => o.startTime <= ts && ts <= o.endTime
                                && ((o.servicesIDs.ToLower() == "MCIFtpSync".ToLower() || o.servicesIDs.ToLower().StartsWith("MCIFtpSync;".ToLower()) || o.servicesIDs.ToLower().Contains(";MCIFtpSync;".ToLower()) || o.servicesIDs.ToLower().EndsWith(";MCIFtpSync".ToLower())))
                                && o.daysOfWeekOrDate.Contains(day)).Select(o => o);
                            }
                            if (serviceCycles.Count() >= 1)
                            {
                                sync.syncSubscription();
                                Thread.Sleep(1000 * 60 * 10);
                            }
                        }
                    }
                    Thread.Sleep(1000 * 60);

                }
                catch (Exception e)
                {
                    Program.logs.Error("MCIFtpSync:SyncFunction: ", e);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "MCIFtpSync:SyncFunction: (" + e.Message + ")");
                    Thread.Sleep(1000);
                }
            }

        }

        //private void syncNotChargedFunction()
        //{
        //    //SyncNotCharged sync = new SyncNotCharged();
        //    //while (!shutdownEvent.WaitOne(0))
        //    //{
        //    //    sync.deactivatedNotCharged();
        //    //    int timeInterval = int.Parse(Properties.Settings.Default.DeactiveNotChargedIntervalInSeconds.ToString());
        //    //    //if (!Properties.Settings.Default.SyncIntervalInSeconds, out timeInterval))
        //    //    //{
        //    //    //    timeInterval = 60 * 30;
        //    //    //}
        //    //    Thread.Sleep(1000 * timeInterval);
        //    //}
        //}
        private void downloaderMCI()
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

                        #region parameter.txt check
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
                                        down.updateSingleChargeAndSubscription(directoryNameInFormatYYYYMMDD, operatorsSID, true);
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
                        #endregion

                        //check today
                        down.updateSingleChargeAndSubscription();

                        //int nDaysBefore = 0;
                        //if (int.TryParse(Properties.Settings.Default.CheckNDaysBefore, out nDaysBefore))
                        //{
                        //    if (nDaysBefore != 0)
                        //    {
                        //        int i;
                        //        for (i = 1; i <= nDaysBefore; i++)
                        //        {
                        //            Program.logs.Info("downloaderFunction:started:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                        //            down.updateSingleChargeAndSubscription(DateTime.Now.AddDays(-1 * i).ToString("yyyyMMdd"), null, false);
                        //            Program.logs.Info("downloaderFunction:ended:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                        //        }
                        //    }
                        //}
                        int nDaysBefore = Properties.Settings.Default.CheckNDaysBefore;
                        if (nDaysBefore != 0)
                        {
                            int i;
                            for (i = 1; i <= nDaysBefore; i++)
                            {
                                Program.logs.Info("downloaderFunction:started:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                                down.updateSingleChargeAndSubscription(DateTime.Now.AddDays(-1 * i).ToString("yyyyMMdd"), null, false);
                                Program.logs.Info("downloaderFunction:ended:" + DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd"));
                            }
                        }

                        Program.logs.Info("downloaderFunction: Ended");
                        int timeInterval = int.Parse(Properties.Settings.Default.DownloadIntervalInSeconds.ToString());
                        //if (!int.TryParse(Properties.Settings.Default.DownloadIntervalInSeconds, out timeInterval))
                        //{
                        //    timeInterval = 60 * 30;
                        //}
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
