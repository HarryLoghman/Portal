using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Aggregators
{
    internal class WebRequestParameterMessage : WebRequestParameter
    {
        internal WebRequestParameterMessage(long id, string mobileNumber, int maxTries, DateTime dateTimeCorrelator, string messageContent
           , enum_webRequestParameterType webRequestType, SharedLibrary.MessageHandler.MessageType messageType, string bodyString, SharedLibrary.Models.vw_servicesServicesInfo service, log4net.ILog logs) :
            base(webRequestType, bodyString, service, logs)
        {
            this.prp_id = id;
            this.prp_maxTries = maxTries;
            this.prp_mobileNumber = mobileNumber;
            this.prp_messageType = messageType;
            this.prp_dateTimeCorrelator = dateTimeCorrelator;
            this.prp_messageContent = messageContent;
        }

        internal SharedLibrary.MessageHandler.MessageType prp_messageType { get; set; }
        internal long prp_id { get; }
        internal int? prp_retryCount { get; set; }
        internal string prp_referenceId { get; set; }
        internal int prp_maxTries { get; }
        internal int? prp_messagePoint { get; set; }
        internal string prp_mobileNumber { get; }

        internal DateTime prp_dateTimeCorrelator { get; set; }

        
        internal string prp_messageContent { get; set; }
        internal override void sb_save()
        {
            try
            {
                string tableName = "";
                switch (this.prp_messageType)
                {
                    case SharedLibrary.MessageHandler.MessageType.OnDemand:
                        tableName = "OnDemandMessagesBuffer";
                        break;
                    case SharedLibrary.MessageHandler.MessageType.EventBase:
                        tableName = "EventbaseMessagesBuffer";
                        break;
                    case SharedLibrary.MessageHandler.MessageType.AutoCharge:
                        tableName = "AutochargeMessagesBuffer";
                        break;
                    default:
                        return;
                }
                DateTime now = DateTime.Now;
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = new SqlConnection(this.prp_cnnStrService);
                if (this.prp_isSucceeded)
                {
                    cmd.CommandText = "update " + this.prp_databaseName + ".dbo." + tableName + " "
                         + "set ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.Success).ToString()
                         + ",ReferenceId=" + (string.IsNullOrEmpty(this.prp_referenceId) ? "Null" : "'" + this.prp_referenceId.ToLower() + "'")
                         + ",SentDate='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                         + ",PersianSentDate='" + SharedLibrary.Date.GetPersianDateTime(now) + "'"
                         + "where id = " + this.prp_id.ToString();
                }
                else
                {
                    SharedLibrary.MessageHandler.ProcessStatus processStatus = SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                    if (this.prp_retryCount.HasValue && this.prp_retryCount.Value >= this.prp_maxTries)
                    {
                        processStatus = SharedLibrary.MessageHandler.ProcessStatus.Failed;
                    }
                    cmd.CommandText = "update " + this.prp_databaseName + ".dbo." + tableName + " "
                      + "set DateLastTried='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                      + ",RetryCount=IsNull(RetryCount,0)+1"
                      + (processStatus == SharedLibrary.MessageHandler.ProcessStatus.Failed ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                      + (this.prp_retryCount > this.prp_maxTries ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                      + "where id = " + this.prp_id.ToString();

                }
                this.prp_logs.Info(cmd.CommandText);
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
                if (this.prp_messagePoint.HasValue && this.prp_messagePoint.Value > 0)
                {
                    SharedLibrary.MessageHandler.SetSubscriberPoint(this.prp_mobileNumber, this.prp_service.Id, this.prp_messagePoint.Value);
                }
            }
            catch (Exception ex1)
            {
                this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in saving to EventbaseMessagesBuffer: ", ex1);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "Sending Bulk : " + this.prp_service.ServiceCode + " Exception in save to EventbaseMessagesBuffer(" + ex1.Message + ")");
                //Program.logs.Error("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Warn("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Info("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");


            }
        }

        internal override string fnc_parseResult(string result, out bool isSucceeded)
        {
            return base.fnc_parseResult(result, out isSucceeded);
        }
    }
}
