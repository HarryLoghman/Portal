using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BulkExecuter
{
    public class BulkController
    {
        #region properties and variables
        public enum enum_sendingSpeed
        {
            normal = 1,
            slow = 2,
            verySlow = 4,
            tooSlow = 8
        }


        vw_servicesServicesInfo v_entryService;
        public SharedLibrary.Models.Bulk v_entryBulk;

        public int prp_rowCount { get; set; }
        protected List<SharedLibrary.Models.ServiceModel.EventbaseMessagesBuffer> v_lstEventbase;
        public int prp_rowIndex { get; set; }

        internal ServicePointSettings v_spSettings;
        double v_intervalInMillisecond;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        public int v_taskCount;
        private long v_notifTime;

        public SharedLibrary.Aggregators.Aggregator prp_aggregator { get; }
        #endregion

        #region subs and functions


        public BulkController(int bulkId)
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
            string notStartReason;
            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            if (!fnc_canStartSendingList(this.v_entryBulk.Id, out notStartReason, out bulkStatus))
            {
                notifStr = "Cannot start Bulk " + this.v_entryBulk.Id + " because " + notStartReason;
                Program.logs.Info(notifStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
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

            this.v_notifTime = 0;
            this.v_spSettings.Assign();

            Program.logs.Error("Connection Limit value is :" + this.v_spSettings.ConnectionLimit.ToString());
            Console.WriteLine("Connection Limit to:" + this.v_spSettings.ConnectionLimit.ToString());

            bool getRetryOnes = false;
            int loopNo = 0;
            try
            {
                while (true)
                {
                    if (!fnc_canStartSendingList(this.v_entryBulk.Id, out notStartReason, out bulkStatus))
                    {
                        notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notStartReason;
                        Program.logs.Info(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, notifStr);
                        break;
                    }

                    this.sb_fill(getRetryOnes);

                    if (this.v_lstEventbase.Count == 0)
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
                        + " Total Rows " + this.v_lstEventbase.Count().ToString()
                        + (getRetryOnes ? "(Retry Rows : " + this.v_lstEventbase.Where(o => o.RetryCount != null && o.RetryCount > 0).Count() + ")" : "");

                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational
                        , notifStr);
                    Program.logs.Info(notifStr);
                    //reset start time for proper long sending notification
                    this.v_ticksStart = DateTime.Now.Ticks;

                    if (!this.fnc_sendList())
                    { break; }
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
            string notStartReason;
            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            int j = -1;
            for (int i = 0; i <= this.v_lstEventbase.Count - 1; i++)
            {
                j++;//bacause of devision overhead we use to reset periodically
                #region checkBulkStatus
                if (j == Properties.Settings.Default.CheckBulkStatusRowSize)
                {
                    //check status after a thousand messages processed
                    bool canStart = fnc_canStartSendingList(this.v_entryBulk.Id, out notStartReason, out bulkStatus);
                    if (!canStart)
                    {
                        notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notStartReason;
                        Program.logs.Error(notifStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                        if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                        {
                            this.sb_setBulkStatus(bulkStatus);
                        }
                        return false;
                    }
                    while (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                    {
                        notifStr = "";
                        canStart = fnc_canStartSendingList(this.v_entryBulk.Id, out notifStr, out bulkStatus);
                        if (!canStart)
                        {
                            notifStr = "Cannot continue Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " bacause " + notStartReason;
                            Program.logs.Error(notifStr);
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, notifStr);
                            if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime)
                            {
                                this.sb_setBulkStatus(bulkStatus);
                            }
                            return false;
                        }
                        Thread.Sleep(1000);
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
                while (v_taskCount == this.v_entryBulk.tps)
                {
                    //if all created tasks are in process, wait till one of them stops and we have free thread
                }
                //EventbaseMessagesBufferExtended eventbase = new EventbaseMessagesBufferExtended(this.v_lstEventbase[i]);

                System.Threading.Interlocked.Add(ref this.v_taskCount, 1);

                this.prp_aggregator.sb_sendMessage(this.v_entryService, this.v_lstEventbase[i].Id
                    , this.v_lstEventbase[i].MobileNumber, SharedLibrary.MessageHandler.MessageType.EventBase
                    , this.v_entryBulk.retryCount.Value, this.v_lstEventbase[i].Content, DateTime.Now, 0
                    , this.v_lstEventbase[i].ImiChargeKey, v_entryBulk.Id, false, this.v_lstEventbase[i].RetryCount, new EventHandler(this.sb_sendingMessageIsFinished));

                DateTime timeEnd = DateTime.Now;
                TimeSpan span2 = timeEnd - (this.v_ticksPrevious.HasValue ? (new DateTime(this.v_ticksPrevious.Value)) : timeEnd);
                this.v_ticksPrevious = DateTime.Now.Ticks;
                //Program.logs.Info(" diff:" + span2.ToString("c"));

                this.sb_notifyLongSending();
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

            }
            Thread.Sleep(60 * 1000);//wait for saving to db is finished
            //this.sb_finish();
            return true;
        }

        public void sb_sendingMessageIsFinished(object sender, EventArgs e)
        {
            System.Threading.Interlocked.Decrement(ref this.v_taskCount);
        }
        private void sb_fill(bool getRetryOnes)
        {
            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(this.v_entryService.ServiceCode))
            {
                DateTime retryTimeOut = DateTime.Now;
                if (this.v_entryBulk.retryIntervalInSeconds.HasValue)
                    retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);

                if (!getRetryOnes)
                {
                    if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                        this.v_lstEventbase = entityService.EventbaseMessagesBuffers.AsNoTracking().Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                       && (o.DateLastTried == null)
                       && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                       && (o.RetryCount == null || o.RetryCount == 0)).ToList();
                    else
                    {
                        this.v_lstEventbase = entityService.EventbaseMessagesBuffers.AsNoTracking().Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                      && (o.DateLastTried == null)
                      && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                      && (o.RetryCount == null || o.RetryCount == 0)).Take(this.v_entryBulk.readSize.Value).ToList();
                    }
                }
                else
                {
                    if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                        this.v_lstEventbase = entityService.EventbaseMessagesBuffers.AsNoTracking().Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                       && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                       && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                       && (o.RetryCount == null || o.RetryCount <= this.v_entryBulk.retryCount)).ToList();
                    else
                    {
                        this.v_lstEventbase = entityService.EventbaseMessagesBuffers.AsNoTracking().Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                      && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)
                      && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                      && (o.RetryCount == null || o.RetryCount <= this.v_entryBulk.retryCount)).Take(this.v_entryBulk.readSize.Value).ToList();
                    }
                }
            }

        }
        /// <summary>
        /// this function get total rows without considering the timeout. it only returns the rows that should be served(including not processed rows and those which their retrycount&lt;maxTries)"
        /// </summary>
        /// <returns></returns>
        private int fnc_getTotalRowCount()
        {
            //DateTime retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);
            using (var entityService = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(this.v_entryService.ServiceCode))
            {
                return entityService.EventbaseMessagesBuffers.AsNoTracking().Where(o => o.bulkId == this.v_entryBulk.Id
                && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                && (o.RetryCount == null || o.RetryCount <= this.v_entryBulk.retryCount)).Count();
            }
        }

        public static bool fnc_canStartSendingList(int bulkId, out string reason
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



        private void sb_notifyLongSending()
        {
            if (this.v_lstEventbase == null || this.v_lstEventbase.Count == 0) return;

            enum_sendingSpeed chargingSpeed = enum_sendingSpeed.normal;
            int totalRowCount = this.v_lstEventbase.Count();
            double bestSpeed;
            if (totalRowCount < this.v_entryBulk.tps)
                bestSpeed = 1;
            else bestSpeed = ((double)totalRowCount / this.v_entryBulk.tps);
            if (bestSpeed == 0) return;
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - this.v_ticksStart);

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

            Program.logs.Info(chargingSpeed.ToString() + " starttime:" + (new DateTime(this.v_ticksStart)).ToString("HH:mm:ss.fff")
                + " datetimeNow" + DateTime.Now.ToString("HH:mm:ss.fff")
                + " rowCount = " + totalRowCount.ToString()
                + " difference = " + ts.ToString("c")
                + " bestSpeed=" + bestSpeed.ToString());
            int? connectionLimit = null;
            ServicePoint sp = this.v_spSettings.GetServicePoint();
            if (sp != null)
            {
                connectionLimit = sp.ConnectionLimit;
            }
            if ((new TimeSpan(DateTime.Now.Ticks - this.v_notifTime)).Minutes > 1)
            {
                //notif every one minute
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.serviceName + " is " + chargingSpeed.ToString() + ":" + ts.ToString("c")
                    + " (Task Remain:" + v_taskCount.ToString() + ")" + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                this.v_notifTime = DateTime.Now.Ticks;
                if ((chargingSpeed == BulkController.enum_sendingSpeed.verySlow /*&& this.prp_resetVerySlowCharging*/)
                || (chargingSpeed == BulkController.enum_sendingSpeed.tooSlow /*&& this.prp_resetTooSlowCharging*/))
                {
                    if (v_taskCount == 0) return;
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                        , "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.serviceName + " is " + chargingSpeed.ToString()
                        + ts.ToString("c") + " (Task Remain:" + v_taskCount.ToString() + ")"
                        + "(Connection Limit:" + (connectionLimit.HasValue ? connectionLimit.Value.ToString() : "Null") + ")");
                    //Environment.Exit(1);
                }
            }

        }

        private void sb_setBulkStatus(SharedLibrary.MessageHandler.BulkStatus newBulkStatus, bool notif = true)
        {
            SqlConnection cnn = new SqlConnection(SharedLibrary.SharedVariables.v_cnnStr);
            SqlCommand cmd = new SqlCommand("select top 1 * from bulks where id = " + this.v_entryBulk.Id);

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
                string notifStr = "Bulk " + this.v_entryBulk.Id + " related to " + this.v_entryService.ServiceCode + " new state is " + newBulkStatus.ToString();
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, notifStr);
                Program.logs.Info(notifStr);
            }




        }
        #endregion
    }
}
