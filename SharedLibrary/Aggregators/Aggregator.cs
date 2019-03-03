using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Aggregators
{
    public class Aggregator : IDisposable
    {
        #region general
        internal virtual WebRequestProcess prp_webRequestProcess { get; }
        internal virtual string prp_userName { get; set; }
        internal virtual string prp_password { get; set; }
        public Dictionary<string, string> prp_aggregatorErrors { get; set; }
        public delegate void hanlder_requestFinished(Aggregator agg, WebRequestParameter parameter);
        public event hanlder_requestFinished ev_requestFinished;

        public string prp_url_sendMessage { get; set; }
        public string prp_url_delivery { get; set; }

        public bool prp_addErrorDescription { get; set; }
        protected Aggregator() : this(false)
        {
        }
        protected Aggregator(bool addErrorDescription)
        {
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var agg = portal.Aggregators.Select(o => o).FirstOrDefault();
                if (agg != null)
                {
                    this.prp_userName = agg.AggregatorUsername;
                    this.prp_password = agg.AggregatorPassword;
                }
            }

            //this.prp_userName = userName;
            //this.prp_password = password;
            //this.prp_aggregatorErrors = aggregatorErrors;
            this.prp_url_delivery = "";
            this.prp_url_sendMessage = "";
            this.prp_webRequestProcess = new WebRequestProcess();
        }

        protected virtual HttpWebRequest fnc_createWebRequestHeader(SharedLibrary.Models.vw_servicesServicesInfo service, string url)
        {
            Uri uri = new Uri(url, UriKind.Absolute);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = 60 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";

            return webRequest;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="logs"></param>
        /// <param name="url">url for the action e.g. sendmessageUrl or otpUrl</param>
        /// <param name="urlDelivery"></param>

        public void Dispose()
        {

        }


        internal virtual void sb_finishRequest(WebRequestParameter parameter, Exception ex)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                parameter.prp_isSucceeded = false;
                if (ex is WebException)
                {
                    SharedVariables.logs.Error(parameter.prp_service.ServiceCode + " : Exception in SendingMessage: ", ex);
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    WebException webException = (WebException)ex;
                    try
                    {
                        if (webException.Response != null)
                        {
                            using (StreamReader rd = new StreamReader(webException.Response.GetResponseStream()))
                            {
                                bool isSucceeded;
                                parameter.prp_result = ex.Message + ":" + this.fnc_sendMessage_parseResult(parameter.prp_service, rd.ReadToEnd(), out isSucceeded);
                            }
                            webException.Response.Close();
                        }
                        //else eventbase.prp_resultDescription = ex.Message +"\r\n" + ex.StackTrace;
                    }
                    catch (Exception ex1)
                    {
                        SharedVariables.logs.Error(parameter.prp_service.ServiceCode + " : Exception in SendingMessage inner try: ", ex1);
                    }
                }
                else
                {
                    //eventbase.ReferenceId = null;
                    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

                    SharedVariables.logs.Error(parameter.prp_service.ServiceCode + " : Exception in SendingMessage2: ", ex);
                }
            }
            this.sb_saveResponseToDB(parameter);
        }
        internal virtual void sb_finishRequest(WebRequestParameter parameter, bool httpOK, string result)
        {
            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                bool isSucceeded;
                parameter.prp_result = this.fnc_sendMessage_parseResult(parameter.prp_service, result, out isSucceeded);
                if (isSucceeded)
                {
                    parameter.prp_isSucceeded = isSucceeded;
                    parameter.prp_result = "Success";
                }
                else
                {
                    parameter.prp_isSucceeded = isSucceeded;
                    parameter.prp_result = result;
                }

            }
            this.sb_saveResponseToDB(parameter);
        }

        internal virtual void sb_saveResponseToDB(WebRequestParameter parameter)
        {


            if (parameter.prp_webRequestType == enum_webRequestParameterType.message)
            {
                WebRequestParameterMessage parameterMessage = (WebRequestParameterMessage)parameter;
                string tableName = "";
                switch (parameterMessage.prp_messageType)
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
                try
                {

                    DateTime now = DateTime.Now;
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = new SqlConnection(parameterMessage.prp_cnnStrService);
                    if (parameter.prp_isSucceeded)
                    {
                        cmd.CommandText = "update " + parameter.prp_service.databaseName + ".dbo." + tableName + " "
                             + "set ProcessStatus=" + ((int)SharedLibrary.MessageHandler.ProcessStatus.Success).ToString()
                             + ",ReferenceId=" + (string.IsNullOrEmpty(parameterMessage.prp_referenceId) ? "Null" : "'" + parameterMessage.prp_referenceId + "'")
                             + ",SentDate='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                             + ",PersianSentDate='" + SharedLibrary.Date.GetPersianDateTime(now) + "'"
                             + ",SendResult=" + (string.IsNullOrEmpty(parameterMessage.prp_result) ? "Null" : "'" + parameterMessage.prp_result + "'")
                             + " where id = " + parameterMessage.prp_id.ToString();
                    }
                    else
                    {
                        SharedLibrary.MessageHandler.ProcessStatus processStatus = SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                        if (parameterMessage.prp_retryCount.HasValue && parameterMessage.prp_retryCount.Value >= parameterMessage.prp_maxTries)
                        {
                            processStatus = SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        }
                        cmd.CommandText = "update " + parameter.prp_service.databaseName + ".dbo." + tableName + " "
                          + "set DateLastTried='" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                          + ",RetryCount=IsNull(RetryCount,0)+1"
                          + ",SendResult=" + (string.IsNullOrEmpty(parameterMessage.prp_result) ? "Null" : "'" + parameterMessage.prp_result + "'")
                          + (processStatus == SharedLibrary.MessageHandler.ProcessStatus.Failed ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                          //+ (parameterMessage.prp_retryCount > parameterMessage.prp_maxTries ? ",ProcessStatus = " + ((int)SharedLibrary.MessageHandler.ProcessStatus.Failed).ToString() : "")
                          + " where id = " + parameterMessage.prp_id.ToString();

                    }
                    SharedVariables.logs.Info(parameter.prp_service.ServiceCode + " : " + cmd.CommandText);
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                    if (parameterMessage.prp_messagePoint.HasValue && parameterMessage.prp_messagePoint.Value > 0)
                    {
                        SharedLibrary.MessageHandler.SetSubscriberPoint(parameterMessage.prp_mobileNumber, parameter.prp_service.Id, parameterMessage.prp_messagePoint.Value);
                    }
                }
                catch (Exception ex1)
                {
                    SharedVariables.logs.Error(parameter.prp_service.ServiceCode + " : Exception in saving to " + tableName + ": ", ex1);
                    SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "sb_saveResponseToDB : " + parameter.prp_service.ServiceCode + " Exception in saving to " + tableName + "(" + ex1.Message + ")");
                    //Program.logs.Error("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                    //Program.logs.Warn("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                    //Program.logs.Info("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");


                }
            }
            if (ev_requestFinished != null)
            {
                this.ev_requestFinished(this, parameter);
            }
        }
        protected virtual string fnc_getAggregatorErrorDescription(string errorNameOrId)
        {
            string errorDesc = "";
            if (this.prp_aggregatorErrors != null && this.prp_addErrorDescription)
            {
                var errorsDesc = this.prp_aggregatorErrors.Where(o => o.Key == errorNameOrId).ToList();
                foreach (var err in errorsDesc)
                {
                    errorDesc = errorDesc + err + "|";
                }

                if (errorDesc != "")
                {
                    errorDesc = errorDesc.Remove(errorDesc.Length - 1, 1);
                    errorDesc = "(" + errorDesc + ")";
                }
            }
            return errorDesc;
        }
        #endregion
        #region charging
        #endregion
        #region sendMessage


        public void sb_sendMessage(SharedLibrary.Models.vw_servicesServicesInfo service, long id, string mobileNumber, SharedLibrary.MessageHandler.MessageType messageType, int maxTries
            , string messageContent, DateTime dateTimeCorrelator
            , int? price, string chargeKey)
        {
            string requestBody = "";
            HttpWebRequest webRequest = this.fnc_createWebRequestHeader(service, this.prp_url_sendMessage);

            requestBody = this.fnc_sendMessage_createBodyString(service, messageType, mobileNumber, messageContent, dateTimeCorrelator
                , price, chargeKey);
            WebRequestParameterMessage parameter = new WebRequestParameterMessage(id, mobileNumber, maxTries, dateTimeCorrelator, messageContent
                , enum_webRequestParameterType.message, messageType, requestBody, service, SharedVariables.logs);

            //parameter.prp_bodyString = requestBody;
            this.prp_webRequestProcess.SendRequest(webRequest, requestBody, parameter, this);


        }

        internal virtual string fnc_sendMessage_createBodyString(SharedLibrary.Models.vw_servicesServicesInfo service,
            SharedLibrary.MessageHandler.MessageType messageType, string mobileNumber, string messageContent, DateTime dateTimeCorrelator
            , int? price, string imiChargeKey)
        {
            return "";
        }

        internal virtual string fnc_sendMessage_parseResult(SharedLibrary.Models.vw_servicesServicesInfo service, string result, out bool isSucceeded)
        {
            isSucceeded = false;
            return "";
        }
        #endregion


    }
}
