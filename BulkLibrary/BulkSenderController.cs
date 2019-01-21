using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BulkLibrary
{
    public class BulkSenderController
    {
        #region properties and variables
        SharedShortCodeServiceLibrary.SharedModel.ShortCodeServiceEntities v_sharedServiceEntity;
        SharedLibrary.Models.PortalEntities v_portalEntities;
        public int prp_bulkId { get; set; }
        /// <summary>
        /// tells us how many rows should load in every fetch; 0 means all rows
        /// </summary>
        public int prp_readSize { get; set; }
        public vw_servicesServicesInfo prp_service { get; set; }
        public int prp_tps { get; }
        public virtual int prp_maxTries { get; }
        public virtual int prp_retryIntervalInSeconds { get; }
        public int prp_rowCount { get; set; }
        protected List<SharedShortCodeServiceLibrary.SharedModel.EventbaseMessagesBuffer> v_lstEventbase;
        public int prp_rowIndex { get; set; }

        internal ServicePointSettings v_spSettings;
        double v_intervalInMillisecond;
        long v_ticksStart;
        Nullable<long> v_ticksPrevious;
        public static int v_taskCount;
        private long v_notifTime;

        public Sender prp_sender { get; }
        #endregion

        #region subs and functions
        public BulkSenderController(int serviceId, int contentId, int fetchCapacity, int tps, int maxTries, Sender sender)
        {
            this.v_portalEntities = new PortalEntities();
            //using (var portal = new SharedLibrary.Models.PortalEntities())
            //{
            var vw = this.v_portalEntities.vw_servicesServicesInfo.Where(o => o.Id == serviceId).FirstOrDefault();
            if (vw == null)
            {
                this.v_portalEntities.Dispose();
                throw new Exception("There is no service with serviceId=" + serviceId.ToString());
            }
            else
            {
                this.prp_service = vw;
            }
            //}
            this.prp_bulkId = contentId;
            this.prp_readSize = fetchCapacity;
            this.prp_tps = tps;
            this.prp_maxTries = maxTries;
            this.prp_sender = sender;
            this.v_sharedServiceEntity = new SharedShortCodeServiceLibrary.SharedModel.ShortCodeServiceEntities("Shared" + this.prp_service.ServiceCode + "Entities");
        }

        private void sb_fill(bool getRetryOnes)
        {
            DateTime retryTimeOut = DateTime.Now.AddSeconds(-1 * this.prp_retryIntervalInSeconds);
            if (!getRetryOnes)
            {
                if (this.prp_readSize == 0)
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.bulkId == this.prp_bulkId && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                   && (o.DateLastTried == null) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                   && (o.RetryCount == null || o.RetryCount == 0)).ToList();
                else
                {
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.prp_bulkId && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                  && (o.DateLastTried == null) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                  && (o.RetryCount == null || o.RetryCount == 0)).Take(this.prp_readSize).ToList();
                }
            }
            else
            {
                if (this.prp_readSize == 0)
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.prp_bulkId && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                   && (o.DateLastTried == null || o.DateLastTried < retryTimeOut) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                   && (o.RetryCount == null || o.RetryCount < this.prp_maxTries)).ToList();
                else
                {
                    this.v_lstEventbase = v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.prp_bulkId && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
                  && (o.DateLastTried == null || o.DateLastTried < retryTimeOut) && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
                  && (o.RetryCount == null || o.RetryCount < this.prp_maxTries)).Take(this.prp_readSize).ToList();
                }
            }

        }
        /// <summary>
        /// this function get total rows without considering the timeout. it only returns the rows that should be served(including not processed rows and those which their retrycount&lt;maxTries)"
        /// </summary>
        /// <returns></returns>
        private int fnc_getTotalRowCount()
        {
            DateTime retryTimeOut = DateTime.Now.AddSeconds(-1 * this.prp_retryIntervalInSeconds);
            return v_sharedServiceEntity.EventbaseMessagesBuffers.Where(o => o.ContentId == this.prp_bulkId && o.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase
           && (o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend)
           && (o.RetryCount == null || o.RetryCount < this.prp_maxTries)).Count();
        }

        public virtual void sb_startSending()
        {
            object lockObj = new object();
            this.v_spSettings = new ServicePointSettings();
            ServicePoint sp;

            ThreadPoolSettings thread = new ThreadPoolSettings();
            thread.Assign();

            this.v_intervalInMillisecond = 1000.00 / this.prp_tps;
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
                            //all rows are processed or their retryCount&gt;maxRetries
                            break;
                        }
                        else
                        {
                            //we have rows that are fully retired
                            //wait till the retry timeout of the rows passed
                            Thread.Sleep(1000 * this.prp_retryIntervalInSeconds);
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

        private bool fnc_sendList()
        {
            for (int i = 0; i <= this.v_lstEventbase.Count - 1; i++)
            {
                #region checkMonitoringStatus
                int? bulkStatus = this.v_portalEntities.Bulks.Where(o => o.Id == this.prp_bulkId).Select(o => o.status).FirstOrDefault();
                if (!bulkStatus.HasValue)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Error, "Bulk Status is not specified for bulkId" + this.prp_bulkId.ToString());
                    return false;
                }
                if (bulkStatus == (int)SharedLibrary.MessageHandler.BulkStatus.Disabled
                     || bulkStatus == (int)SharedLibrary.MessageHandler.BulkStatus.FinishedAll
                      || bulkStatus == (int)SharedLibrary.MessageHandler.BulkStatus.FinishedByTime
                      || bulkStatus == (int)SharedLibrary.MessageHandler.BulkStatus.Stopped)
                {
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DLog(System.Diagnostics.Eventing.Reader.StandardEventLevel.Warning, "Bulk Status =" + ((SharedLibrary.MessageHandler.BulkStatus)bulkStatus).ToString()
                        + " for bulkId" + this.prp_bulkId.ToString());
                    return false;
                }
                while (bulkStatus == (int)SharedLibrary.MessageHandler.BulkStatus.Paused)
                {
                    bulkStatus = this.v_sharedServiceEntity.MessagesMonitorings.Where(o => o.ContentId == this.prp_bulkId).Select(o => o.Status).FirstOrDefault();
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
                while (v_taskCount == this.prp_tps)
                {
                    //if all created tasks are in process. wait till one of them stops and we have free thread
                }
                EventbaseMessagesBufferExtended eventbase = new EventbaseMessagesBufferExtended(this.v_lstEventbase[i]);
                this.prp_sender.sb_send(eventbase);
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
