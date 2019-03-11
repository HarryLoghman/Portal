using SharedLibrary.Models;
using System;
using System.Collections.Generic;
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
        SharedLibrary.Models.ServiceModel.SharedServiceEntities v_sharedServiceEntity;
        SharedLibrary.Models.PortalEntities v_portalEntities;

        public vw_servicesServicesInfo prp_service { get; set; }
        public SharedLibrary.Models.Bulk v_entryBulk;

        public int prp_rowCount { get; set; }
        protected List<SharedLibrary.Models.ServiceModel.EventbaseMessagesBuffer> v_lstEventbase;
        public int prp_rowIndex { get; set; }

        internal ServicePointSettings v_spSettings;
        double v_intervalInMillisecond;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        public static int v_taskCount;
        private long v_notifTime;

        public SharedLibrary.Aggregators.Aggregator prp_aggregator { get; }
        #endregion

        #region subs and functions
        public BulkController(SharedLibrary.Models.Bulk entryBulk)
        {
            string exceptionStr;
            if (entryBulk == null)
            {
                exceptionStr = "BulkExecuter:BulkControler:BulkControler,entryBulk is not specified";
                Program.logs.Error(exceptionStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                return;
            }
            this.v_entryBulk = entryBulk;
            this.v_portalEntities = new PortalEntities();
            //using (var portal = new SharedLibrary.Models.PortalEntities())
            //{
            var entryService = this.v_portalEntities.vw_servicesServicesInfo.Where(o => o.Id == entryBulk.ServiceId).FirstOrDefault();
            if (entryService == null)
            {
                this.v_portalEntities.Dispose();
                exceptionStr = "BulkExecuter:BulkControler:BulkControler,serviceId is invalid (bulkId=" + entryBulk.Id + ", ServiceId=" + entryBulk.ServiceId + ")";
                Program.logs.Error(exceptionStr);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
            }
            else
            {
                this.prp_service = entryService;
            }

            //}

            this.prp_aggregator = SharedLibrary.SharedVariables.fnc_getAggregator(this.prp_service.aggregatorName);
            this.v_sharedServiceEntity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(this.prp_service.ServiceCode);
        }

        private void sb_fill(bool getFailedOnes)
        {
            DateTime retryTimeOut = DateTime.Now;
            if (this.v_entryBulk.retryIntervalInSeconds.HasValue)
                retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);

            if (!getFailedOnes)
            {
                if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                   && (o.DateLastTried == null) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                   && (o.RetryCount == null || o.RetryCount == 0)).ToList();
                else
                {
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                  && (o.DateLastTried == null) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                  && (o.RetryCount == null || o.RetryCount == 0)).Take(this.v_entryBulk.readSize.Value).ToList();
                }
            }
            else
            {
                if (!this.v_entryBulk.readSize.HasValue || this.v_entryBulk.readSize.Value == 0)
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                   && (o.DateLastTried == null || o.DateLastTried < retryTimeOut) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                   && (o.RetryCount == null || o.RetryCount < this.v_entryBulk.retryCount)).ToList();
                else
                {
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                  && (o.DateLastTried == null || o.DateLastTried < retryTimeOut) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                  && (o.RetryCount == null || o.RetryCount < this.v_entryBulk.retryCount)).Take(this.v_entryBulk.readSize.Value).ToList();
                }
            }

        }
        /// <summary>
        /// this function get total rows without considering the timeout. it only returns the rows that should be served(including not processed rows and those which their retrycount&lt;maxTries)"
        /// </summary>
        /// <returns></returns>
        private int fnc_getTotalRowCount()
        {
            DateTime retryTimeOut = DateTime.Now.AddSeconds(-1 * this.v_entryBulk.retryIntervalInSeconds.Value);
            return v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.bulkId == this.v_entryBulk.Id && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
           && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
           && (o.RetryCount == null || o.RetryCount < this.v_entryBulk.retryCount)).Count();
        }

        public virtual void sb_startSending()
        {
            object lockObj = new object();
            this.v_spSettings = new ServicePointSettings();
            ServicePoint sp;

            ThreadPoolSettings thread = new ThreadPoolSettings();
            thread.Assign();

            this.v_intervalInMillisecond = 1000.00 / this.v_entryBulk.tps;
            this.v_ticksStart = DateTime.Now.Ticks;
            this.v_ticksPrevious = null;
            lock (lockObj) { v_taskCount = 0; }


            #region checkServicePoint
            sp = this.v_spSettings.GetServicePoint();

            if (sp != null)
            {
                Program.logs.Info("Connection Limit to:" + sp.ConnectionLimit.ToString());
                Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
            }
            else
            {
                Program.logs.Error("Connection Limit is Default");
                Console.WriteLine("Connection Limit to:" + sp.ConnectionLimit.ToString());
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Connection Limit is Default for " + this.prp_service.ServiceCode);

            }
            #endregion

            int totalRowCount = this.fnc_getTotalRowCount();
            if (totalRowCount == 0)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "Sending Bulk : No rows specified for service " + this.prp_service.ServiceCode);
                Program.logs.Info("No rows specified in sending Bulk");
                return;
            }
            Program.logs.Info("startTime:" + DateTime.Now.ToString("HH:mm:ss,fff"));
            Program.logs.Info("interval:" + this.v_intervalInMillisecond + " totalRowCount " + totalRowCount);
            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Sending Bulk : " + this.prp_service.ServiceCode + " is started with TotalRow = " + totalRowCount.ToString());
            Program.logs.Info("------------------------------------------------------");
            Program.logs.Info("------------------------------------------------------");

            this.v_notifTime = 0;
            this.v_spSettings.Assign();
            bool getRetryOnes = false;
            int loopNo = 0;
            try
            {
                while (true)
                {
                    this.sb_fill(getRetryOnes);

                    if (this.v_lstEventbase.Count == 0)
                    {
                        //there is no rows remain which are not retried
                        getRetryOnes = true;
                        totalRowCount = this.fnc_getTotalRowCount();
                        if (totalRowCount == 0)
                        {
                            //all rows are processed or their retryCount<maxRetries
                            break;
                        }
                        else
                        {
                            //we have rows that are fully retried
                            //wait till the retry timeout of the rows passed
                            Thread.Sleep(1000 * this.v_entryBulk.retryIntervalInSeconds.Value);
                            continue;
                        }
                    }

                    //this.sb_sendList();

                    loopNo++;
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Sending Bulk : " + loopNo.ToString() + (loopNo == 1 ? "st" : (loopNo == 2 ? "nd" : (loopNo == 3 ? "rd" : "th"))) + " loop " + this.prp_service.ServiceCode
                        + (getRetryOnes ? "(Retry Rows : " + this.v_lstEventbase.Where(o => o.RetryCount != null && o.RetryCount > 0).Count() + ")" : ""));
                    Program.logs.Info("Sending Bulk : " + loopNo.ToString() + (loopNo == 1 ? "st" : (loopNo == 2 ? "nd" : (loopNo == 3 ? "rd" : "th"))) + " loop " + this.prp_service.ServiceCode
                        + (getRetryOnes ? "(Retry Rows : " + this.v_lstEventbase.Where(o => o.RetryCount != null && o.RetryCount > 0).Count() + ")" : ""));
                    if (this.fnc_sendList())
                    { break; }
                }
            }
            catch (Exception e)
            {
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error
                    , "Bulk Sending Exception in sb_startSending :" + e.Message);
                Program.logs.Error(e);
            }
            SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Informational, "Sending Bulk : " + this.prp_service.ServiceCode + " is finished");
            return;

        }


        public static bool fnc_canStartSendingList(int bulkId, out string reason, out SharedLibrary.MessageHandler.BulkStatus bulkStatus)
        {
            using (var entityPortal = new SharedLibrary.Models.PortalEntities())
            {
                var entryBulk = entityPortal.Bulks.Where(o => o.Id == bulkId).FirstOrDefault();
                reason = "";
                bulkStatus = SharedLibrary.MessageHandler.BulkStatus.Enabled;
                if (entryBulk == null)
                {
                    reason = "Bulk " + bulkId + " does not exist anymore";
                }
                else
                {
                    bulkStatus = (SharedLibrary.MessageHandler.BulkStatus)entryBulk.status;
                    if (entryBulk.startTime > DateTime.Now)
                    {
                        reason = bulkId + " startTime is " + entryBulk.startTime.ToString("yyyy-MM-dd HH:mm:ss") + ". Its execution period is not started yet.";
                    }
                    else if (entryBulk.endTime < DateTime.Now)
                    {
                        reason = "Bulk " + bulkId + " endTime is " + entryBulk.startTime.ToString("yyyy-MM-dd HH:mm:ss") + ". Its execution period is passed.";
                    }
                    else
                    {
                        if (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Disabled
                        || bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedAll
                        || bulkStatus == SharedLibrary.MessageHandler.BulkStatus.FinishedByTime
                        || bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Stopped)
                            reason = "Bulk " + bulkId + " status is " + bulkStatus.ToString();
                    }
                }
            }
            return string.IsNullOrEmpty(reason);

        }
        private bool fnc_sendList()
        {
            string exceptionStr = "";
            SharedLibrary.MessageHandler.BulkStatus bulkStatus;
            for (int i = 0; i <= this.v_lstEventbase.Count - 1; i++)
            {
                #region checkBulkStatus
                if (i % 1000 == 0)
                {
                    //check status for a thousand process
                    bool canStart = fnc_canStartSendingList(this.v_entryBulk.Id, out exceptionStr, out bulkStatus);
                    if (!canStart)
                    {
                        exceptionStr = "BulkExecuter:BulkController:fnc_sendList," + exceptionStr;
                        Program.logs.Error(exceptionStr);
                        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                        return false;
                    }
                    while (bulkStatus == SharedLibrary.MessageHandler.BulkStatus.Paused)
                    {
                        exceptionStr = "";
                        canStart = fnc_canStartSendingList(this.v_entryBulk.Id, out exceptionStr, out bulkStatus);
                        if (!canStart)
                        {
                            exceptionStr = "BulkExecuter:BulkController:fnc_sendList,inner while " + exceptionStr;
                            Program.logs.Error(exceptionStr);
                            SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, exceptionStr);
                            return false;
                        }
                    }
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
                this.prp_aggregator.sb_sendMessage(this.prp_service, this.v_lstEventbase[i].Id
                    , this.v_lstEventbase[i].MobileNumber, SharedLibrary.MessageHandler.MessageType.EventBase
                    , this.v_entryBulk.retryCount.Value, this.v_lstEventbase[i].Content, DateTime.Now, null, this.v_lstEventbase[i].ImiChargeKey);

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
            Thread.Sleep(10000);//wait for saving to db is finished
            //this.sb_finish();
            return true;
        }


        private void sb_notifyLongSending()
        {
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - this.v_ticksStart);
            if (ts.TotalMinutes / 120 > 1)
            {
                if ((new TimeSpan(DateTime.Now.Ticks - this.v_notifTime)).Minutes > 1)
                {
                    string connectionLimitStr = "Null";
                    ServicePoint sp = this.v_spSettings.GetServicePoint();
                    if (sp != null)
                    {
                        connectionLimitStr = sp.ConnectionLimit.ToString();
                    }
                    else
                    {
                        connectionLimitStr = "Null";

                    }

                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Sending Bulk : " + this.prp_service.ServiceCode + " Long Sending:" + ts.ToString("c") + " (Task Remain:" + v_taskCount.ToString() + ")" + "(Connection Limit:" + connectionLimitStr + ")");
                    this.v_notifTime = DateTime.Now.Ticks;
                }

            }
        }
        #endregion
    }
}
