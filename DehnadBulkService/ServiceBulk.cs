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

namespace DehnadBulkService
{
    public partial class ServiceBulk : ServiceBase
    {
        private Thread bulkStartThread;
        private Thread bulkStatusCheck;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public ServiceBulk()
        {
            InitializeComponent();
        }

        internal void StartDebugging(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {

            bulkStartThread = new Thread(sb_startBulks);
            bulkStartThread.IsBackground = true;
            bulkStartThread.Start();

            bulkStatusCheck = new Thread(sb_statusCheck);
            bulkStatusCheck.IsBackground = true;
            bulkStatusCheck.Start();
        }

        protected override void OnStop()
        {
            try
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_AtomicWarning(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Service DehnadBulkService has been stopped");
                shutdownEvent.Set();

                if (!bulkStartThread.Join(3000))
                {
                    bulkStartThread.Abort();
                }

                if (!bulkStatusCheck.Join(3000))
                {
                    bulkStatusCheck.Abort();
                }

            }
            catch (Exception ex)
            {
                Program.logs.Info("Exception in thread termination ");
                Program.logs.Error("Exception in thread termination " + ex);
            }

        }

        private string fnc_getExecuterTemplatePath()
        {
            string dirPath = Properties.Settings.Default.BulkExecuterTemplateFolderPath + "\\BulkExecuterTemplate";
            if (!Directory.Exists(dirPath))
            {
                return "";
            }
            else return dirPath;
        }

        private void sb_startBulks()
        {
            Process process;
            string dirSrcName;
            string dirDesName;
            string exeName;
            string strNotif;
            while (!shutdownEvent.WaitOne(0))
            {
                try
                {
                    dirSrcName = this.fnc_getExecuterTemplatePath();
                    if (string.IsNullOrEmpty(dirSrcName))
                    {
                        strNotif = "There is no directory with the name of BulkExecuterTemplate in the " + Properties.Settings.Default.BulkExecuterTemplateFolderPath;
                        strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                        Program.logs.Error(strNotif);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                        Thread.Sleep(60 * 1000); //give one miunte to solve the problem
                        continue;
                    }


                    var arrProcess = Process.GetProcesses();
                    using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                    {
                        var lstEntryBulks = entityPortal.Bulks.Where(o => (o.status != (int)SharedLibrary.MessageHandler.BulkStatus.Disabled
                        && o.status != (int)SharedLibrary.MessageHandler.BulkStatus.Stopped
                        //&& o.status != (int)SharedLibrary.MessageHandler.BulkStatus.Paused
                        )
                        && o.startTime <= DateTime.Now && DateTime.Now <= o.endTime).ToList();

                        foreach (var entryBulk in lstEntryBulks)
                        {
                            var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == entryBulk.ServiceId);
                            if (entryService == null)
                            {
                                strNotif = "Service of the Bulk cannot be found bulkId=" + entryBulk.Id.ToString()
                                    + ",ServiceId = " + entryBulk.ServiceId.ToString();

                                strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                                Program.logs.Error(strNotif);
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                                continue;
                            }

                            //exeName = "BulkExecuter_" + entryService.ServiceCode + "_" + entryBulk.Id;

                            exeName = "BulkExecuter";
                            dirDesName = Properties.Settings.Default.BulkExecuterTemplateFolderPath + "\\BulkExecuter_" + entryService.ServiceCode + "_" + entryBulk.Id;

                            process = arrProcess.FirstOrDefault(o => o.ProcessName.ToLower() == exeName.ToLower()
                             && Path.GetDirectoryName(o.MainModule.FileName).ToLower() == dirDesName.ToLower());
                            if (process == null)
                            {
                                
                                //there is no process with the name "BulkExecuter_Achar_1025"
                                using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(entryService.ServiceCode))
                                {
                                    entityService.Database.CommandTimeout = 600;
                                    var cnt = entityService.EventbaseMessagesBuffers.Count(o =>
                                    (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend
                                      || o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.InQueue)
                                     //&& o.ProcessStatus != (int)SharedLibrary.MessageHandler.ProcessStatus.Success
                                     && o.bulkId == entryBulk.Id);
                                    if (cnt > 0)
                                    {
                                        #region delete old files
                                        //if (Directory.Exists(dirDesName))
                                        //{
                                        //    try
                                        //    {
                                        //        Directory.Delete(dirDesName);
                                        //    }
                                        //    catch (Exception ex)
                                        //    {
                                        //        strNotif = "Cannot delete " + Path.GetDirectoryName(dirDesName) + "," + ex.Message;
                                        //        strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                                        //        Program.logs.Error(strNotif);
                                        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                                        //        continue;
                                        //    }
                                        //}
                                        #endregion

                                        #region create and copy files from template
                                        try
                                        {
                                            if (!Directory.Exists(dirDesName))
                                                Directory.CreateDirectory(dirDesName);
                                            this.sb_copyDirectory(dirSrcName, dirDesName);
                                            this.sb_renameExeFilesAndSetServicePointSettings(dirDesName, exeName, entryService.aggregatorName);
                                        }
                                        catch (Exception ex)
                                        {
                                            strNotif = "Copy Directory Exception " + dirDesName + "," + ex.Message;
                                            strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                                            Program.logs.Error(strNotif, ex);
                                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                                            continue;
                                        }
                                        #endregion

                                        #region start bulk
                                        try
                                        {
                                            Program.logs.Info(exeName);
                                            Program.logs.Info(dirDesName);

                                            Process.Start(dirDesName + "\\" + exeName + ".exe", entryBulk.Id.ToString());
                                        }
                                        catch (Exception ex)
                                        {
                                            strNotif = "Cannot Start the process " + exeName + "," + ex.Message;
                                            strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                                            Program.logs.Error(strNotif, ex);
                                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                                            continue;
                                        }
                                        #endregion
                                    }
                                }
                            }
                            else
                            {
                                //it is running already
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    strNotif = ex.Message;
                    strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks,OuterException " + strNotif;
                    Program.logs.Error(strNotif, ex);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                    continue;
                }
                Thread.Sleep(10000);
            }

        }

        private void sb_statusCheck()
        {
            string strNotif;
            while (!shutdownEvent.WaitOne())
            {
                try
                {
                    using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                    {
                        var lstBulks = entityPortal.Bulks.Where(o => (o.status == (int)SharedLibrary.MessageHandler.BulkStatus.Running
                         || o.status == (int)SharedLibrary.MessageHandler.BulkStatus.Paused)
                         && o.startTime <= DateTime.Now && DateTime.Now <= o.endTime).ToList();
                        if (lstBulks.Count > 0)
                        {

                            var arrProcess = Process.GetProcesses();
                            Process process;
                            int i;
                            string exeName;
                            for (i = 0; i <= lstBulks.Count - 1; i++)
                            {
                                var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == lstBulks[i].ServiceId);
                                if (entryService == null)
                                {
                                    continue;
                                }


                                exeName = "BulkExecuter";
                                process = arrProcess.FirstOrDefault(o => o.ProcessName.ToLower() == exeName.ToLower());

                                strNotif = exeName + " is not listed in Windows Running Processes."
                                     + " bulk Status is " + ((SharedLibrary.MessageHandler.BulkStatus)lstBulks[i].status).ToString();
                                Program.logs.Error(strNotif);
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, strNotif);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    strNotif = ex.Message;
                    strNotif = "DehnadBulkService:ServiceBulk:sb_statusCheck,OuterException " + strNotif;
                    Program.logs.Error(strNotif, ex);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);

                }
                Thread.Sleep(1000 * 60 * 30);
            }
        }

        private void sb_renameExeFilesAndSetServicePointSettings(string dirDesPath, string exeName, string aggregatorName)
        {
            SharedLibrary.HelpfulFunctions.enumServers enumServer = SharedLibrary.HelpfulFunctions.fnc_getEnumServerByAggregatorName(aggregatorName);
            string serverUrl = SharedLibrary.HelpfulFunctions.fnc_getServerURL(true, enumServer);
            string str = File.ReadAllText(dirDesPath + "\\BulkExecuter.exe.config");
            str = str.Replace("<ServicePointSettings Uri=\"http://92.42.55.180:8310/\" ConnectionLimit=\"4000\" Expect100Continue=\"false\" UseNagleAlgorithm=\"false\">"
                , "<ServicePointSettings Uri=\"\" ConnectionLimit=\"4000\" Expect100Continue=\"false\" UseNagleAlgorithm=\"false\">");
            str = str.Replace("<ServicePointSettings Uri=\"\" ConnectionLimit=\"4000\" Expect100Continue=\"false\" UseNagleAlgorithm=\"false\">"
                , "<ServicePointSettings Uri=\"" + serverUrl + "\" ConnectionLimit=\"4000\" Expect100Continue=\"false\" UseNagleAlgorithm=\"false\">");

            File.WriteAllText(dirDesPath + "\\BulkExecuter.exe.config", str);
            //File.Move(dirDesPath + "\\BulkExecuter.exe", dirDesPath + "\\" + exeName + ".exe");
            //File.Move(dirDesPath + "\\BulkExecuter.exe.config", dirDesPath + "\\" + exeName + ".exe.config");
            //File.Move(dirDesPath + "\\BulkExecuter.pdb", dirDesPath + "\\" + exeName + ".pdb");
        }
        private void sb_copyDirectory(string dirSrc, string dirDes)
        {
            DirectoryInfo diSrc = new DirectoryInfo(dirSrc);
            DirectoryInfo diDes = new DirectoryInfo(dirDes);
            foreach (var file in diDes.GetFiles())
            {
                //Program.logs.Info(file.FullName);
                File.Delete(file.FullName);
                //Program.logs.Info("--------------------------" + file.FullName + "-----------------");

            }
            //foreach (var directory in diDes.GetDirectories())
            //{
            //    directory.Delete();
            //}
            foreach (var file in Directory.GetFiles(diSrc.FullName))
            {
                File.Copy(file, diDes.FullName + "\\" + Path.GetFileName(file), true);
            }
            if (!Directory.Exists(diDes + "\\Logs"))
                Directory.CreateDirectory(diDes + "\\Logs");
        }
    }
}
