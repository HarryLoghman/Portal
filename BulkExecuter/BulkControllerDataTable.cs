using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BulkExecuter
{
    public class BulkControllerDataTable
    {
        #region properties and variables

        vw_servicesServicesInfo v_entryService;
        public SharedLibrary.Models.Bulk v_entryBulk;

        public int prp_rowCount { get; set; }
        protected DataTable v_dtEventbase;
        public int prp_rowIndex { get; set; }

        internal ServicePointSettings v_spSettings;
        double v_intervalInMillisecond;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        public int v_taskCount;
        private static long v_notifTimeTicks;

        public SharedLibrary.Aggregators.Aggregator prp_aggregator { get; }

        #endregion

        #region subs and functions


        public BulkControllerDataTable(int bulkId)
        {
            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                var entryBulk = entityPortal.Bulks.FirstOrDefault(o => o.Id == bulkId);

                string exceptionStr;
                if (entryBulk == null)
                {
                    exceptionStr = "BulkExecuter:BulkController:BulkController,bulkId=" + bulkId + " does not exist";
                    Program.logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(1);
                    return;
                }
                this.v_entryBulk = entryBulk;


                //using (var portal = new SharedLibrary.Models.PortalEntities())
                //{

                var entryService = entityPortal.vw_servicesServicesInfo.Where(o => o.Id == entryBulk.ServiceId).FirstOrDefault();
                if (entryService == null)
                {
                    exceptionStr = "BulkExecuter:BulkController:BulkController,serviceId is invalid (bulkId=" + entryBulk.Id + ", ServiceId=" + entryBulk.ServiceId + ")";
                    Program.logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(1);
                    return;
                }
                else
                {
                    this.v_entryService = entryService;
                }

                //}
                var arrProcess = Process.GetProcesses();
                //var exeName = "BulkExecuter_" + entryService.ServiceCode + "_" + entryBulk.Id;
                var exeName = "BulkExecuter";
                int currentProcessId = Process.GetCurrentProcess().Id;
                string currentWorkingDirectory = Process.GetCurrentProcess().MainModule.FileName;
                Process process = arrProcess.FirstOrDefault(o => o.ProcessName.ToLower() == exeName.ToLower()
                && o.MainModule.FileName == currentWorkingDirectory
                && o.Id != currentProcessId);
                if (process != null)
                {
                    exceptionStr = "BulkExecuter:BulkController:BulkController," + exeName + " is already started"
                        + " WorkDirectory = " + currentWorkingDirectory
                        + ",Pid=" + process.Id;
                    Program.logs.Error(exceptionStr);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                    Environment.Exit(1);
                    return;
                }
                this.prp_aggregator = SharedLibrary.SharedVariables.fnc_getAggregator(this.v_entryService.aggregatorName);


            }
        }

        public virtual void sb_startSending()
        {
            string notifStr;

            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            if (!fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus))
            {
                notifStr = "Cannot start Bulk " + this.v_entryBulk.Id + " because " + notifStr;
                Program.logs.Info(notifStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                Environment.Exit(0);
                return;
            }

            object lockObj = new object();
            this.v_spSettings = new ServicePointSettings();
            ServicePoint sp;

            #region threadPoolSetting
            ThreadPoolSettings thread = new ThreadPoolSettings();
            thread.Assign();
            #endregion

            this.v_intervalInMillisecond = 1000.00 / this.v_entryBulk.tps;
            this.v_ticksStart = DateTime.Now.Ticks;
            this.v_ticksPrevious = null;
            lock (lockObj) { v_taskCount = 0; }


            #region checkServicePoint
            sp = this.v_spSettings.GetServicePoint();

            if (sp == null)
            //{
            //    Program.logs.Info("Connection Limit to:" + sp.ConnectionLimit.ToString());
            //    Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
            //}
            //else
            {
                Program.logs.Error("Connection Limit is Default");
                Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Connection Limit is Default for " + this.v_entryService.ServiceCode);

            }
            #endregion

            int totalRowCount = this.fnc_getTotalRowCount();

            if (totalRowCount == 0)
            {
                notifStr = "No bulk message found for bulkId " + this.v_entryBulk.Id.ToString() + " to send. Bulk will be finished";
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                Program.logs.Info(notifStr);
                this.sb_setBulkStatus(SharedLibrary.MessageHandler.BulkStatus.FinishedAll);
                Environment.Exit(0);
                return;
            }

            notifStr = "Bulk " + this.v_entryBulk.Id.ToString() + " related to " + this.v_entryService.ServiceCode + " is started with " + totalRowCount + " rows";
            Program.logs.Info(notifStr);
            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, notifStr);
            this.sb_setBulkStatus(SharedLibrary.MessageHandler.BulkStatus.Running);

            Program.logs.Info("startTime:" + DateTime.Now.ToString("HH:mm:ss,fff"));
            Program.logs.Info("interval:" + this.v_intervalInMillisecond + " totalRowCount " + totalRowCount);
            Program.logs.Info("------------------------------------------------------");
            Program.logs.Info("------------------------------------------------------");

            v_notifTimeTicks = 0;
            this.v_spSettings.Assign();

            sp = this.v_spSettings.GetServicePoint();
            Program.logs.Error("Connection Limit value is :" + sp.ConnectionLimit.ToString()
                + " and Uri : " + sp.Address.ToString());
            Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());

            bool getRetryOnes = false;
            int loopNo = 0;

            try
            {
                while (true)
                {
                    if (!fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus))
                    {
                        notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                        Program.logs.Info(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                        if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                            this.sb_setBulkStatus(bulkStatus, false);
                        Environment.Exit(0);
                        break;
                    }
                    if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                    {
                        notifStr = "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " is paused";
                        Program.logs.Info(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                        while (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                        {
                            notifStr = "";
                            bool canStart = fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                            if (!canStart)
                            {
                                notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                                Program.logs.Error(notifStr);
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                                if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                                    this.sb_setBulkStatus(bulkStatus, false);
                                Environment.Exit(0);
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                        //after pause set tickstart again

                    }
                    this.sb_fill(getRetryOnes);
                    if (this.v_dtEventbase.Rows.Count == 0)
                    {
                        //there is no rows remain which are their first time
                        getRetryOnes = true;
                        totalRowCount = this.fnc_getTotalRowCount();
                        if (totalRowCount == 0)
                        {
                            //all rows are processed or their retryCount<maxRetries
                            this.sb_setBulkStatus(SharedLibrary.MessageHandler.BulkStatus.FinishedAll);
                            break;
                        }
                        else
                        {
                            //all rows tried once
                            //wait till the retry timeout of the rows passed
                            Thread.Sleep(1000 * this.v_entryBulk.retryIntervalInSeconds.Value);
                            continue;
                        }
                        
                    }

                    //this.sb_sendList();
                    loopNo++;
                    notifStr = "Sending Bulk " + this.v_entryBulk.Id.ToString() + " for " + this.v_entryService.ServiceCode
                        + ": " + loopNo.ToString() + (loopNo == 1 ? "st" : (loopNo == 2 ? "nd" : (loopNo == 3 ? "rd" : "th"))) + " loop "
                        + " Total Rows " + this.v_dtEventbase.Rows.Count.ToString()
                        + (getRetryOnes ? "(Retry Rows : " + this.v_dtEventbase.Select("RetryCount is not null and RetryCount > 0").Length + ")" : "");
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational
                        , notifStr);
                    Program.logs.Info(notifStr);
                    //reset start time for proper long sending notification
                    this.v_ticksStart = DateTime.Now.Ticks;

                    if (this.v_entryService.aggregatorName.ToLower() == "mobinone"
                       && Properties.Settings.Default.UsePackForSending)
                    {
                        Program.logs.Warn("listList");
                        if (!this.fnc_sendListMobinOne())
                        { break; }
                    }
                    else
                    {
                        Program.logs.Warn("normalnormal");
                        if (!this.fnc_sendList())
                        { break; }
                    }
                    //if (!this.fnc_sendList())
                    //{ break; }

                }
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , "Bulk Sending Exception in sb_startSending :" + e.Message);
                Program.logs.Error(e);
            }
            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Sending Bulk : " + this.v_entryService.ServiceCode + " is finished");
            return;

        }

        private bool fnc_sendList()
        {
            string notifStr = "";

            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            int j = -1;
            int pausedRowCount = 0;
            for (int i = 0; i <= this.v_dtEventbase.Rows.Count - 1; i++)
            {
                j++;//bacause of devision overhead we use to reset periodically

                #region checkBulkStatus
                if (j == Properties.Settings.Default.CheckBulkStatusRowSize)
                {

                    //check status after a CheckBulkStatusRowSize messages processed
                    bool canContinueSending = fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                    if (!canContinueSending)
                    {
                        notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                        Program.logs.Error(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                        if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                            this.sb_setBulkStatus(bulkStatus, false);
                        Environment.Exit(0);
                        return false;
                    }
                    if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                    {
                        notifStr = "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " is paused";
                        Program.logs.Info(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);

                        while (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                        {
                            notifStr = "";
                            canContinueSending = fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                            if (!canContinueSending)
                            {
                                notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                                Program.logs.Error(notifStr);
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                                if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                                    this.sb_setBulkStatus(bulkStatus, false);
                                Environment.Exit(0);
                                return false;
                            }
                            Thread.Sleep(1000);
                        }
                        //after pause set tickstart again
                        this.v_ticksStart = DateTime.Now.Ticks;
                        pausedRowCount = i;
                    }
                    j = 0;
                }
                #endregion

                #region throttle request
                if (this.v_ticksPrevious.HasValue)
                {
                    DateTime previousTime = new DateTime(this.v_ticksPrevious.Value);
                    //this.v_ticksPrevious = DateTime.Now.Ticks;
                    //previousTime 
                    TimeSpan span = DateTime.Now - previousTime;
                    if (span.TotalMilliseconds < this.v_intervalInMillisecond)
                    {
                        //Program.logs.Info("fast loop" + span.TotalMilliseconds);
                        while (span.TotalMilliseconds < this.v_intervalInMillisecond)
                        {
                            span = DateTime.Now - previousTime;
                        }
                    }
                }
                #endregion

                DateTime timeStart = DateTime.Now;
                //while (v_taskCount == this.v_entryBulk.tps)
                //{
                //    //if all created tasks are in process, wait till one of them stops and we have free thread
                //}

                //EventbaseMessagesBufferExtended eventbase = new EventbaseMessagesBufferExtended(this.v_lstEventbase[i]);

                System.Threading.Interlocked.Add(ref this.v_taskCount, 1);

                int? retryCount = null;
                if (this.v_dtEventbase.Rows[i]["RetryCount"] != null && this.v_dtEventbase.Rows[i]["RetryCount"] != Convert.DBNull)
                {
                    retryCount = int.Parse(this.v_dtEventbase.Rows[i]["RetryCount"].ToString());
                }
                this.prp_aggregator.sb_sendMessage(this.v_entryService, long.Parse(this.v_dtEventbase.Rows[i]["Id"].ToString())
                    , this.v_dtEventbase.Rows[i]["MobileNumber"].ToString(), SharedLibrary.MessageHandler.MessageType.EventBase
                    , this.v_entryBulk.retryCount.Value, this.v_dtEventbase.Rows[i]["Content"].ToString(), DateTime.Now, 0
                    , this.v_dtEventbase.Rows[i]["ImiChargeKey"].ToString(), v_entryBulk.Id, Properties.Settings.Default.UseBulk
                    , retryCount
                    , new EventHandler(this.sb_sendingMessageIsFinished));

                DateTime timeEnd = DateTime.Now;
                TimeSpan span2 = timeEnd - (this.v_ticksPrevious.HasValue ? (new DateTime(this.v_ticksPrevious.Value)) : timeEnd);
                this.v_ticksPrevious = DateTime.Now.Ticks;
                //Program.logs.Info(" diff:" + span2.ToString("c"));

                sb_notifyLongSending(this.v_entryBulk, this.v_dtEventbase.Rows.Count - pausedRowCount + 1, this.v_ticksStart, this.v_taskCount);
            }


            Program.logs.Info("----------------------------------------");
            Program.logs.Info("----------------------------------------");

            //Program.logs.Info("total:" + ((new DateTime(this.v_ticksPrevious.Value)) - (new DateTime(this.v_ticksStart))).TotalMilliseconds);
            Console.WriteLine("taskRemain:" + v_taskCount);
            Program.logs.Info("taskRemain:" + v_taskCount);


            int taskCount = 0;
            while (v_taskCount > 0)
            {
                if (taskCount != v_taskCount)
                {
                    object obj = new object();
                    lock (obj) { taskCount = v_taskCount; }
                    Console.WriteLine("taskRemain:" + v_taskCount);
                    Program.logs.Warn("taskRemain:" + v_taskCount);
                }
                sb_notifyLongSending(this.v_entryBulk, this.v_dtEventbase.Rows.Count - pausedRowCount + 1, this.v_ticksStart, this.v_taskCount);
            }
            Thread.Sleep(10 * 1000);//wait for saving to db is finished
            
            //this.sb_finish();
            return true;
        }

        private bool fnc_sendListMobinOne()
        {
            string notifStr = "";

            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            int j = -1;
            int pausedRowCount = 0;
            List<string> mobileNumbersArr, contentsArr, imiChargeCodeArr;
            List<int?> priceArr;
            List<DateTime> correlatorArr;
            List<long> idsArr;
            mobileNumbersArr = new List<string>();
            contentsArr = new List<string>();
            imiChargeCodeArr = new List<string>();
            priceArr = new List<int?>();
            correlatorArr = new List<DateTime>();
            idsArr = new List<long>();
            for (int i = 0; i <= this.v_dtEventbase.Rows.Count - 1; i++)
            {
                j++;//bacause of devision overhead we use to reset periodically

                #region checkBulkStatus
                if (j == Properties.Settings.Default.CheckBulkStatusRowSize)
                {

                    //check status after a CheckBulkStatusRowSize messages processed
                    bool canContinueSending = fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                    if (!canContinueSending)
                    {
                        notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                        Program.logs.Error(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                        if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                            this.sb_setBulkStatus(bulkStatus, false);
                        Environment.Exit(0);
                        return false;
                    }
                    if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                    {
                        notifStr = "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " is paused";
                        Program.logs.Info(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);

                        while (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                        {
                            notifStr = "";
                            canContinueSending = fnc_checkBulkStatus(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                            if (!canContinueSending)
                            {
                                notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notifStr;
                                Program.logs.Error(notifStr);
                                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                                if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                                    this.sb_setBulkStatus(bulkStatus, false);
                                Environment.Exit(0);
                                return false;
                            }
                            Thread.Sleep(1000);
                        }
                        //after pause set tickstart again
                        this.v_ticksStart = DateTime.Now.Ticks;
                        pausedRowCount = i;
                    }
                    j = 0;
                }
                #endregion

                #region throttle request
                if (this.v_ticksPrevious.HasValue)
                {
                    DateTime previousTime = new DateTime(this.v_ticksPrevious.Value);
                    //this.v_ticksPrevious = DateTime.Now.Ticks;
                    //previousTime 
                    TimeSpan span = DateTime.Now - previousTime;
                    if (span.TotalMilliseconds < this.v_intervalInMillisecond)
                    {
                        //Program.logs.Info("fast loop" + span.TotalMilliseconds);
                        while (span.TotalMilliseconds < this.v_intervalInMillisecond)
                        {
                            span = DateTime.Now - previousTime;
                        }
                    }
                }
                #endregion

                DateTime timeStart = DateTime.Now;
                //while (v_taskCount == this.v_entryBulk.tps)
                //{
                //    //if all created tasks are in process, wait till one of them stops and we have free thread
                //}

                //EventbaseMessagesBufferExtended eventbase = new EventbaseMessagesBufferExtended(this.v_lstEventbase[i]);



                //this.prp_aggregator.sb_sendMessage(this.v_entryService, this.v_lstEventbase[i].Id
                //    , this.v_lstEventbase[i].MobileNumber, SharedLibrary.MessageHandler.MessageType.EventBase
                //    , this.v_entryBulk.retryCount.Value, this.v_lstEventbase[i].Content, DateTime.Now, 0
                //    , this.v_lstEventbase[i].ImiChargeKey, v_entryBulk.Id, true, this.v_lstEventbase[i].RetryCount, new EventHandler(this.sb_sendingMessageIsFinished));

                idsArr.Add(long.Parse(this.v_dtEventbase.Rows[i]["Id"].ToString()));
                mobileNumbersArr.Add(this.v_dtEventbase.Rows[i]["MobileNumber"].ToString());
                contentsArr.Add(this.v_dtEventbase.Rows[i]["Content"].ToString());
                correlatorArr.Add(DateTime.Now);
                priceArr.Add(0);
                imiChargeCodeArr.Add(this.v_dtEventbase.Rows[i]["ImiChargeKey"].ToString());

                if (mobileNumbersArr.Count == 90 || i == this.v_dtEventbase.Rows.Count - 1)
                {
                    //this.prp_aggregator.sb_sendMessage(this.v_entryService, this.v_lstEventbase[i].Id
                    //    , this.v_lstEventbase[i].MobileNumber, SharedLibrary.MessageHandler.MessageType.EventBase
                    //    , this.v_entryBulk.retryCount.Value, this.v_lstEventbase[i].Content, DateTime.Now, 0
                    //    , this.v_lstEventbase[i].ImiChargeKey, v_entryBulk.Id, true, this.v_lstEventbase[i].RetryCount, new EventHandler(this.sb_sendingMessageIsFinished));

                    int? retryCount = null;
                    if (this.v_dtEventbase.Rows[i]["RetryCount"] != null && this.v_dtEventbase.Rows[i]["RetryCount"] != Convert.DBNull)
                    {
                        retryCount = int.Parse(this.v_dtEventbase.Rows[i]["RetryCount"].ToString());
                    }
                    System.Threading.Interlocked.Add(ref this.v_taskCount, 1);
                    this.prp_aggregator.sb_sendMessage(this.v_entryService, idsArr.ToArray()
                          , mobileNumbersArr.ToArray(), SharedLibrary.MessageHandler.MessageType.EventBase
                          , this.v_entryBulk.retryCount.Value, contentsArr.ToArray(), correlatorArr.ToArray(), priceArr.ToArray()
                          , imiChargeCodeArr.ToArray(), v_entryBulk.Id , Properties.Settings.Default.UseBulk, retryCount
                          , new EventHandler(this.sb_sendingMessageIsFinished));
                    mobileNumbersArr.Clear();
                    idsArr.Clear();
                    contentsArr.Clear();
                    correlatorArr.Clear();
                    priceArr.Clear();
                    imiChargeCodeArr.Clear();

                    DateTime timeEnd = DateTime.Now;
                    TimeSpan span2 = timeEnd - (this.v_ticksPrevious.HasValue ? (new DateTime(this.v_ticksPrevious.Value)) : timeEnd);
                    this.v_ticksPrevious = DateTime.Now.Ticks;
                    //Program.logs.Info(" diff:" + span2.ToString("c"));

                    sb_notifyLongSending(this.v_entryBulk, this.v_dtEventbase.Rows.Count - pausedRowCount + 1, this.v_ticksStart, this.v_taskCount);

                }
                else
                {
                    continue;
                }
                
            }


            Program.logs.Info("----------------------------------------");
            Program.logs.Info("----------------------------------------");

            //Program.logs.Info("total:" + ((new DateTime(this.v_ticksPrevious.Value)) - (new DateTime(this.v_ticksStart))).TotalMilliseconds);
            Console.WriteLine("taskRemain:" + v_taskCount);
            Program.logs.Info("taskRemain:" + v_taskCount);


            int taskCount = 0;
            while (v_taskCount > 0)
            {
                if (taskCount != v_taskCount)
                {
                    object obj = new object();
                    lock (obj) { taskCount = v_taskCount; }
                    Console.WriteLine("taskRemain:" + v_taskCount);
                    Program.logs.Warn("taskRemain:" + v_taskCount);
                }
                sb_notifyLongSending(this.v_entryBulk, this.v_dtEventbase.Rows.Count - pausedRowCount + 1, this.v_ticksStart, this.v_taskCount);
            }
            Thread.Sleep(4000);//wait for saving to db is finished
            //this.sb_finish();
            return true;
        }
        public void sb_sendingMessageIsFinished(object sender, EventArgs e)
        {
            System.Threading.Interlocked.Decrement(ref this.v_taskCount);
        }

        private void sb_fill(bool getRetryOnes)
        {
            SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            SqlDataAdapter adp = new SqlDataAdapter(cmd);
            this.v_dtEventbase = new DataTable();

            DateTime retryTimeOut = DateTime.Now;
            if (this.v_entryBulk.retryIntervalInSeconds.HasValue)
                retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);

            if (!getRetryOnes)
            {
                if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                    cmd.CommandText = "Select * from " + this.v_entryService.databaseName + ".dbo.EventbaseMessagesBuffer where bulkId = " + this.v_entryBulk.Id.ToString()
                    + " and messageType = " + ((int)SharedLibrary.MessageHandler.MessageType.EventBase).ToString()
                    + " and DateLastTried is null"
                    + " and processStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString()
                    + " and isnull(retryCount,0) =0 ";
                else
                    cmd.CommandText = "Select top " + this.v_entryBulk.readSize.ToString() + " * from " + this.v_entryService.databaseName + ".dbo.EventbaseMessagesBuffer where bulkId = " + this.v_entryBulk.Id.ToString()
                    + " and messageType = " + ((int)SharedLibrary.MessageHandler.MessageType.EventBase).ToString()
                    + " and DateLastTried is null"
                    + " and processStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString()
                    + " and isnull(retryCount,0) =0 ";
            }
            else
            {
                if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                    cmd.CommandText = "Select * from " + this.v_entryService.databaseName + ".dbo.EventbaseMessagesBuffer where bulkId = " + this.v_entryBulk.Id.ToString()
                    + " and messageType = " + ((int)SharedLibrary.MessageHandler.MessageType.EventBase).ToString()
                    + " and (DateLastTried is null or  DateLastTried < '" + retryTimeOut.ToString("yyyy-MM-dd HH:mm:ss.fff") + "')"
                    + " and processStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString()
                    + " and isnull(retryCount,0)<=" + this.v_entryBulk.retryCount.ToString();
                else
                {
                    cmd.CommandText = "Select top " + this.v_entryBulk.readSize.ToString() + " * from " + this.v_entryService.databaseName + ".dbo.EventbaseMessagesBuffer where bulkId = " + this.v_entryBulk.Id.ToString()
                + " and messageType = " + ((int)SharedLibrary.MessageHandler.MessageType.EventBase).ToString()
                + " and (DateLastTried is null or  DateLastTried < '" + retryTimeOut.ToString("yyyy-MM-dd HH:mm:ss.fff") + "')"
                + " and processStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString()
                + " and isnull(retryCount,0)<=" + this.v_entryBulk.retryCount.ToString();

                }
            }

            //Program.logs.Error(cmd.CommandText);
            cnn.Open();
            adp.Fill(this.v_dtEventbase);
            cnn.Close();


        }
        /// <summary>
        /// this function get total rows without considering the timeout. it only returns the rows that should be served(including not processed rows and those which their retrycount&lt;maxTries)"
        /// </summary>
        /// <returns></returns>
        private int fnc_getTotalRowCount()
        {
            //DateTime retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);
            SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            cmd.CommandText = "Select count(*) from " + this.v_entryService.databaseName + ".dbo.EventbaseMessagesBuffer "
                    + "where bulkId = " + this.v_entryBulk.Id.ToString()
                    + " and messageType = " + ((int)SharedLibrary.MessageHandler.MessageType.EventBase).ToString()
                    + " and processStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend).ToString()
                    + " and isnull(retryCount,0)<=" + this.v_entryBulk.retryCount.ToString();
            //Program.logs.Error(cmd.CommandText);
            cnn.Open();
            object rowCount = cmd.ExecuteScalar();
            
            cnn.Close();
            if (rowCount == null || rowCount == DBNull.Value)
                return 0;
            else return int.Parse(rowCount.ToString());
        }

        private void sb_notifyLongSending(Bulk entryBulk, int totalRowCount, long timeStartTick
            , int taskCountRemain)
        {
            //if (this.v_lstEventbase == null || this.v_lstEventbase.Count == 0) return;
            if (totalRowCount == 0) return;
            enum_sendingSpeed chargingSpeed = enum_sendingSpeed.normal;
            //int totalRowCount = this.v_lstEventbase.Count();
            double bestSpeed;
            if (totalRowCount < entryBulk.tps)
                bestSpeed = 1;
            else bestSpeed = ((double)totalRowCount / entryBulk.tps);
            if (bestSpeed == 0) return;
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - timeStartTick);

            if (ts.TotalSeconds <= bestSpeed)
                return;
            else if (bestSpeed < ts.TotalSeconds &&
                ts.TotalSeconds <= bestSpeed * (int)enum_sendingSpeed.slow * 1.2)
            {
                chargingSpeed = enum_sendingSpeed.slow;
                return;
            }
            else if (bestSpeed * (int)enum_sendingSpeed.slow * 1.2 < ts.TotalSeconds &&
                ts.TotalSeconds <= bestSpeed * (int)enum_sendingSpeed.verySlow * 1.2)
            {
                chargingSpeed = enum_sendingSpeed.verySlow;
            }
            else
            {
                chargingSpeed = enum_sendingSpeed.tooSlow;
            }

            Program.logs.Info(chargingSpeed.ToString() + " starttime:" + (new DateTime(timeStartTick)).ToString("HH:mm:ss.fff")
                + " datetimeNow" + DateTime.Now.ToString("HH:mm:ss.fff")
                + " rowCount = " + totalRowCount.ToString()
                + " difference = " + ts.ToString("c")
                + " bestSpeed=" + bestSpeed.ToString());
            ServicePoint sp = this.v_spSettings.GetServicePoint();
            int? connectionLimit = null;
            if (sp != null)
            {
                connectionLimit = sp.ConnectionLimit;
            }
            if ((new TimeSpan(DateTime.Now.Ticks - v_notifTimeTicks)).Minutes > 1)
            {
                //notif every one minute
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , Program.v_currentDirecoty + " is " + chargingSpeed.ToString() + ":" + ts.ToString("c")
                    + " (Task Remain:" + taskCountRemain.ToString() + ")" + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                v_notifTimeTicks = DateTime.Now.Ticks;
                if ((chargingSpeed == BulkExecuter.enum_sendingSpeed.verySlow && (entryBulk.resetVerySlowSending.HasValue && entryBulk.resetVerySlowSending.Value))
                || (chargingSpeed == BulkExecuter.enum_sendingSpeed.tooSlow && (entryBulk.resetVerySlowSending.HasValue && entryBulk.resetVerySlowSending.Value)))
                {
                    if (taskCountRemain == 0) return;
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                        , Program.v_currentDirecoty + " is " + chargingSpeed.ToString()
                        + ts.ToString("c") + " (Task Remain:" + taskCountRemain.ToString() + ")"
                        + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                    Environment.Exit(2);
                }
            }

        }


        public static bool fnc_checkBulkStatus(int bulkId, out string reason
          , out SharedLibrary.MessageHandler.BulkStatus bulkStatus)
        {
            reason = "";
            bulkStatus = SharedLibrary.MessageHandler.BulkStatus.Enabled;
            bool bulkExists = true;
            DateTime startTime, endTime;
            startTime = DateTime.MaxValue;
            endTime = DateTime.MinValue;
            //if (useAdoNet)
            //{
            SqlCommand cmd = new SqlCommand("Select top 1 * from portal.dbo.bulks where Id= " + bulkId.ToString());
            SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
            cmd.Connection = cnn;
            cnn.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader == null || !reader.HasRows)
            {
                bulkExists = false;

            }
            else
            {
                reader.Read();
                bulkStatus = (SharedLibrary.MessageHandler.BulkStatus)reader["Status"];
                startTime = DateTime.Parse(reader["startTime"].ToString());
                endTime = DateTime.Parse(reader["endTime"].ToString());
            }
            cnn.Close();

            //}
            //else
            //{
            //    using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            //    {
            //        var entryBulk = entityPortal.Bulks.Where(o => o.Id == bulkId).FirstOrDefault();

            //        if (entryBulk == null)
            //        {
            //            bulkExists = false;
            //        }
            //        else
            //        {
            //            bulkStatus = (SharedLibrary.MessageHandler.BulkStatus)entryBulk.status;
            //            startTime = entryBulk.startTime;
            //            endTime = entryBulk.endTime;
            //        }
            //    }
            //}

            if (!bulkExists)
                reason = "Bulk " + bulkId + " does not exist anymore";
            else if (startTime > DateTime.Now)
            {
                reason = "Bulk " + bulkId + " startTime is " + startTime.ToString("yyyy-MM-dd HH:mm:ss") + ". Its execution period is not started yet.";
            }
            else if (endTime < DateTime.Now)
            {
                reason = "Bulk " + bulkId + " endTime is " + endTime.ToString("yyyy-MM-dd HH:mm:ss") + ". Its execution period is passed.";
            }
            else
            {
                if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Disabled
                //|| bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedAll
                //|| bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime
                || bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Stopped)
                    reason = "Bulk " + bulkId + " status is " + bulkStatus.ToString();
            }

            return string.IsNullOrEmpty(reason);

        }
        private void sb_setBulkStatus(SharedLibrary.MessageHandler.BulkStatus newBulkStatus, bool notif = true)
        {
            SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
            SqlCommand cmd = new SqlCommand("select top 1 * from portal.dbo.bulks where id = " + this.v_entryBulk.Id);

            cmd.Connection = cnn;
            cnn.Open();
            var reader = cmd.ExecuteReader();
            if (reader == null || !reader.HasRows)
            {
                cnn.Close();
                return;
            }
            else
            {
                reader.Read();
                int oldStatus = int.Parse(reader["status"].ToString());
                cnn.Close();
                if (oldStatus == (int)newBulkStatus)
                {
                    return;
                }

                this.v_entryBulk.status = (int)newBulkStatus;
                using (var entityPortal = new SharedLibrary.Models.PortalEntities())
                {
                    entityPortal.Entry(this.v_entryBulk).State = System.Data.Entity.EntityState.Modified;
                    entityPortal.SaveChanges();
                }
                //cmd.CommandText = "update portal.dbo.bulks set status = " + ((int)newBulkStatus).ToString() + " where id = " + bulkId.ToString();
                //cnn.Open();
                //cmd.ExecuteNonQuery();
                //cnn.Close();
                //string notifStr = "Bulk " + bulkId + " related to " + this.v_entryService.ServiceCode + " new state is " + newBulkStatus.ToString();
                string notifStr = Program.v_currentDirecoty + " new state is " + newBulkStatus.ToString();
                if (notif)
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, notifStr);
                Program.logs.Info(notifStr);
            }

        }


        #endregion
    }
}
