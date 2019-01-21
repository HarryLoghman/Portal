using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;
namespace BulkLibrary
{
    public class Sender : IDisposable
    {
        public virtual vw_servicesServicesInfo prp_service { get; }
        public virtual int prp_maxTries { get; }
        SharedServiceEntities v_serviceEntity;
        protected virtual string prp_imiChargeKey { get; }
        public Sender(vw_servicesServicesInfo service)
        {
            this.prp_service = service;
            this.v_serviceEntity = new SharedServiceEntities("Shared" + service.databaseName + "Entities");
            this.prp_imiChargeKey = this.v_serviceEntity.ImiChargeCodes.Where(o => o.Description == "Free").Select(o => o.ChargeKey).FirstOrDefault();

        }
        public virtual void sb_send(EventbaseMessagesBufferExtended eventbase)
        {

        }

        internal void sb_saveResponseToDB(EventbaseMessagesBufferExtended eventbase)
        {
            System.Threading.Interlocked.Decrement(ref BulkSenderController.v_taskCount);

            try
            {

                this.updateEventBase(eventbase);

                //this.insertSMSResult(eventbase);

            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + singleChargeReq.payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in Save to DB2: ", e);

            }
        }

        private void updateEventBase(EventbaseMessagesBufferExtended eventbase)
        {
            try
            {
                DateTime now = DateTime.Now;
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = new SqlConnection(Program.v_cnnStr);
                if (eventbase.prp_IsSucceeded)
                {
                    cmd.CommandText = "update " + this.prp_service.databaseName + ".dbo.EventbaseMessagesBuffer "
                         + "set ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.Success).ToString()
                         + ",ReferenceId='" + eventbase.ReferenceId + "'"
                         + ",SentDate='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                         + ",PersianSentDate='" + SharedLibrary.Date.GetPersianDateTime(now) + "'"
                         + "where id = " + eventbase.Id.ToString();
                }
                else
                {
                    SharedLibrary.MessageHandler.ProcessStatus processStatus = SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                    if (eventbase.RetryCount.HasValue && eventbase.RetryCount.Value >= this.prp_maxTries)
                    {
                        processStatus = SharedLibrary.MessageHandler.ProcessStatus.Failed;
                    }
                    cmd.CommandText = "update " + this.prp_service.databaseName + ".dbo.EventbaseMessagesBuffer "
                      + "set DateLastTried='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                      + ",RetryCount=IsNull(RetryCount,0)+1"
                      + (processStatus == SharedLibrary.MessageHandler.ProcessStatus.Failed ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                      + (eventbase.RetryCount > this.prp_maxTries ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                      + "where id = " + eventbase.Id.ToString();

                }
                Program.logs.Info(cmd.CommandText);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
                if (eventbase.MessagePoint.HasValue && eventbase.MessagePoint.Value > 0)
                {
                    SharedLibrary.MessageHandler.SetSubscriberPoint(eventbase.MobileNumber, eventbase.ServiceId, eventbase.MessagePoint.Value);
                }
            }
            catch (Exception ex1)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in saving to EventbaseMessagesBuffer: ", ex1);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Sending Bulk : " + this.prp_service.ServiceCode + " Exception in save to EventbaseMessagesBuffer(" + ex1.Message + ")");
                //Program.logs.Error("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Warn("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Info("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");


            }
        }

        //private void insertSMSResult(EventbaseMessagesBufferExtended eventbase)
        //{
        //    try
        //    {
        //        DateTime now = DateTime.Now;
        //        SqlCommand cmd = new SqlCommand();
        //        cmd.Connection = new SqlConnection(Program.v_cnnStr);
        //        cmd.CommandText = "insert into SMSResults(mobileNumber, referenceId, result, IsSucceeded, regDate, parentId, parentTableName)"
        //            + " values "
        //            + "('" + eventbase.MobileNumber + "' , '" + eventbase.ReferenceId + "' , '" + eventbase.prp_resultDescription + "'," + (eventbase.prp_IsSucceeded ? "1" : "0") + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
        //            + "," + eventbase.Id + ",'EventbaseMessagesBuffer')";

        //        Program.logs.Info(cmd.CommandText);
        //        cmd.Connection.Open();
        //        cmd.ExecuteNonQuery();
        //        cmd.Connection.Close();
        //        if (eventbase.MessagePoint.HasValue && eventbase.MessagePoint.Value > 0)
        //        {
        //            SharedLibrary.MessageHandler.SetSubscriberPoint(eventbase.MobileNumber, eventbase.ServiceId, eventbase.MessagePoint.Value);
        //        }
        //    }
        //    catch (Exception ex1)
        //    {
        //        Program.logs.Error(this.prp_service.ServiceCode + " : Exception in saving to SMSResults: ", ex1);
        //        SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Sending Bulk : " + this.prp_service.ServiceCode + " Exception in save to SMSResults(" + ex1.Message + ")");


        //    }
        //}

        protected void sb_afterSend(SharedLibrary.Models.EventbaseMessagesBuffer eventbase, bool isSucceeded)
        {

        }
        public void Dispose()
        {
            this.v_serviceEntity.Dispose();
        }
    }
}
