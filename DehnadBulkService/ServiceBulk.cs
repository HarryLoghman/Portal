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
        private Thread bulkThread;
        private ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        public ServiceBulk()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            bulkThread = new Thread(sb_checkBulks);
            bulkThread.IsBackground = true;
            bulkThread.Start();
        }

        protected override void OnStop()
        {
            try
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_AtomicWarning(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Service DehnadBulkService has been stopped");
                shutdownEvent.Set();
                if (!bulkThread.Join(3000))
                {
                    bulkThread.Abort();
                }

            }
            catch (Exception exp)
            {
                Program.logs.Info("Exception in thread termination ");
                Program.logs.Error("Exception in thread termination " + exp);
            }

        }

        private string fnc_getExecuterTemplateExistence()
        {
            string dirPath = Directory.GetCurrentDirectory() + "\\BulkExecuterTemplate";
            if (!Directory.Exists(dirPath))
            {
                return "";
            }
            else return dirPath;
        }
        private void sb_checkBulks()
        {
            while (!shutdownEvent.WaitOne())
            {
                Process process;
                string dirSrcName;
                string dirDesName;
                string exeName;
                string strNotif;
                dirSrcName = this.fnc_getExecuterTemplateExistence();
                if (string.IsNullOrEmpty(dirSrcName))
                {
                    strNotif = "There is no directory with the name of BulkExecuterTemplate";
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
                    && o.status != (int)SharedLibrary.MessageHandler.BulkStatus.Paused) && o.startTime >= DateTime.Now && o.endTime <= DateTime.Now).ToList();

                    foreach (var entryBulk in lstEntryBulks)
                    {
                        var entryService = entityPortal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == entryBulk.ServiceId);
                        if (entryService == null) continue;

                        exeName = "BulkExecuter_" + entryService.ServiceCode + "_" + entryBulk.Id;
                        dirDesName = Directory.GetCurrentDirectory() + "\\Bulk_" + entryService.ServiceCode + "_" + entryBulk.Id;

                        process = arrProcess.FirstOrDefault(o => o.ProcessName.ToLower() == exeName);
                        if (process == null)
                        {
                            //there is no process with the name "BulkExecuter_Achar_1025"
                            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(entryService.ServiceCode))
                            {
                                var cnt = entityService.EventbaseMessagesBuffers.Count(o => o.ProcessStatus != (int)SharedLibrary.MessageHandler.ProcessStatus.Failed
                                 && o.ProcessStatus != (int)SharedLibrary.MessageHandler.ProcessStatus.Finished
                                 && o.ProcessStatus != (int)SharedLibrary.MessageHandler.ProcessStatus.Success
                                 && o.bulkId == entryBulk.Id);
                                if (cnt > 0)
                                {
                                    if (Directory.Exists(dirDesName))
                                    {
                                        try
                                        {
                                            Directory.Delete(dirDesName);
                                        }
                                        catch (Exception ex)
                                        {
                                            strNotif = "Cannot delete " + dirDesName + "," + ex.Message;
                                            strNotif = "DehnadBulkService:ServiceBulk:sb_checkBulks," + strNotif;
                                            Program.logs.Error(strNotif);
                                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, strNotif);
                                            continue;
                                        }
                                    }
                                    Directory.CreateDirectory(dirDesName);
                                    this.sb_copyDirectory(dirSrcName, dirDesName);
                                    rename exe file
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

        }

        private void sb_copyDirectory(string dirSrc,string dirDes)
        {
            DirectoryInfo diSrc = new DirectoryInfo(dirSrc);
            DirectoryInfo diDes = new DirectoryInfo(dirDes);
            foreach (var file in diDes.GetFiles())
            {
                file.Delete();
            }
            foreach (var directory in diDes.GetDirectories())
            {
                directory.Delete();
            }
            foreach (var file in Directory.GetFiles(diSrc.FullName))
            {
                File.Copy(file, diDes.FullName + "\\" + Path.GetFileName(file));
            }
            Directory.CreateDirectory(diDes + "\\Logs");
        }
    }
}
