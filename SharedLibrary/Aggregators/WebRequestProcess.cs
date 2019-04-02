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
    class WebRequestProcess
    {

        internal void SendRequest(HttpWebRequest webRequestHeader, string body, WebRequestParameter parameter, Aggregator aggregator)
        {
            parameter.v_timings.Add("sendRequest", DateTime.Now);
            ServicePoint sp = ServicePointManager.FindServicePoint(new Uri(aggregator.prp_url_sendMessage));
            SharedVariables.logs.Warn("connectionLimit-" + aggregator.prp_url_sendMessage + "," + (sp != null ? sp.ConnectionLimit.ToString() : "Null"));
            webRequestHeader.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequestHeader, parameter, aggregator });
        }
        internal void SendRequest(HttpWebRequest webRequestHeader, string body, List<WebRequestParameterMessage> parameterArr, Aggregator aggregator)
        {
            webRequestHeader.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequestHeader, parameterArr, aggregator });
        }

        private void sb_addTiminigsToList(List<WebRequestParameterMessage> lst, string keyTime, DateTime dateTime)
        {
            foreach (var parameter in lst)
            {
                parameter.v_timings.Add(keyTime, dateTime);
            }
        }
        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            string bodyString;

            if (objs[1].GetType() == typeof(WebRequestParameter)
                || objs[1].GetType() == typeof(WebRequestParameterMessage))

            {
                bodyString = ((WebRequestParameter)objs[1]).prp_bodyString;
            }
            else
            {
                bodyString = ((List<WebRequestParameterMessage>)objs[1])[0].prp_bodyString;
                SharedVariables.logs.Info("bodyString    " + bodyString);
            }
            Aggregator aggregator = (Aggregator)objs[2];
            try
            {
                if (objs[1].GetType() == typeof(WebRequestParameter)
                  || objs[1].GetType() == typeof(WebRequestParameterMessage))

                {
                    ((WebRequestParameter)objs[1]).v_timings.Add("requestBefore", DateTime.Now);
                }
                else
                {
                    this.sb_addTiminigsToList(((List<WebRequestParameterMessage>)objs[1]), "requestBefore", DateTime.Now);
                }
                using (Stream stream = webRequest.EndGetRequestStream(parameters))
                {
                    Byte[] bts = UnicodeEncoding.UTF8.GetBytes(bodyString);
                    stream.Write(bts, 0, bts.Count());
                }

                if (objs[1].GetType() == typeof(WebRequestParameter)
                  || objs[1].GetType() == typeof(WebRequestParameterMessage))

                {
                    ((WebRequestParameter)objs[1]).v_timings.Add("requestAfter", DateTime.Now);
                }
                else
                {
                    this.sb_addTiminigsToList(((List<WebRequestParameterMessage>)objs[1]), "requestAfter", DateTime.Now);
                }

                webRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), new object[] { webRequest, objs[1], aggregator });

            }
            //catch (System.Net.WebException ex)
            //{
            //    aggregator.sb_finishRequest(webRequest, ex);

            //    this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack1: ", ex);
            //    parameter.prp_isSucceeded = false;
            //    parameter.prp_result = null;
            //    //message.ReferenceId = null;
            //    try
            //    {
            //        if (ex.Response != null)
            //        {
            //            using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
            //            {
            //                parameter.prp_result = rd.ReadToEnd();
            //            }
            //            ex.Response.Close();
            //        }
            //        else parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;
            //    }
            //    catch (Exception e1)
            //    {
            //        this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack inner try: ", e1);
            //        parameter.prp_result = e1.Message + "\r\n" + e1.StackTrace;
            //    }

            //    parameter.sb_save();
            //}
            //catch (Exception ex1)
            //{
            //    parameter.prp_isSucceeded = false;
            //    parameter.prp_result = ex1.Message + "\r\n" + ex1.StackTrace;
            //    //message.ReferenceId = null;
            //    parameter.sb_save();

            //}
            catch (Exception ex)
            {
                if (objs[1].GetType() == typeof(WebRequestParameterMessage))
                    aggregator.sb_finishRequest((WebRequestParameterMessage)objs[1], ex, true);
                else if (objs[1].GetType() == typeof(WebRequestParameter))
                    aggregator.sb_finishRequest((WebRequestParameter)objs[1], ex, true);
                else
                {
                    //SharedVariables.logs.Info("exception list1    " + bodyString);
                    var lstParameters = (List<WebRequestParameterMessage>)objs[1];
                    for (int i = 0; i <= lstParameters.Count - 1; i++)
                    {
                        aggregator.sb_finishRequest(lstParameters[i], ex, (i == 0));
                    }
                }


            }

        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            //bool isSucceeded = false;
            //string parsedResult = "";
            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            //WebRequestParameter parameter = (List<WebRequestParameter>)objs[1];
            Aggregator aggregator = (Aggregator)objs[2];
            try
            {
                if (objs[1].GetType() == typeof(WebRequestParameter)
                 || objs[1].GetType() == typeof(WebRequestParameterMessage))

                {
                    ((WebRequestParameter)objs[1]).v_timings.Add("responseBefore", DateTime.Now);
                }
                else
                {
                    this.sb_addTiminigsToList(((List<WebRequestParameterMessage>)objs[1]), "responseBefore", DateTime.Now);
                }
                HttpWebResponse response = (HttpWebResponse)webRequest.EndGetResponse(parameters);
                string result = "";
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }

                if (objs[1].GetType() == typeof(WebRequestParameter)
                || objs[1].GetType() == typeof(WebRequestParameterMessage))

                {
                    ((WebRequestParameter)objs[1]).v_timings.Add("responseAfter", DateTime.Now);
                }
                else
                {
                    this.sb_addTiminigsToList(((List<WebRequestParameterMessage>)objs[1]), "responseAfter", DateTime.Now);
                }

                bool httpOk = false;
                if (response.StatusCode == HttpStatusCode.OK)
                    httpOk = true;
                response.Close();

                if (objs[1].GetType() == typeof(WebRequestParameter)
                     || objs[1].GetType() == typeof(WebRequestParameterMessage))
                {

                    aggregator.sb_finishRequest((WebRequestParameter)objs[1], httpOk, result, true);
                }
                else
                {

                    //SharedVariables.logs.Info("getresponsecallback list2    ");
                    var lstParameters = (List<WebRequestParameterMessage>)objs[1];
                    for (int i = 0; i <= lstParameters.Count - 1; i++)
                    {
                        aggregator.sb_finishRequest(lstParameters[i], httpOk, result, (i == 0));
                    }
                }

                if (objs[1].GetType() == typeof(WebRequestParameter)
               || objs[1].GetType() == typeof(WebRequestParameterMessage))

                {
                    ((WebRequestParameter)objs[1]).v_timings.Add("responseEnd", DateTime.Now);
                    this.sb_saveTimings(((WebRequestParameterMessage)objs[1]));
                }
                else
                {
                    this.sb_addTiminigsToList(((List<WebRequestParameterMessage>)objs[1]), "responseEnd", DateTime.Now);
                    this.sb_saveTimings(((List<WebRequestParameterMessage>)objs[1]));
                }


                //switch (parameter.prp_webRequestType)
                //{
                //    case enum_webRequestParameterType.message:
                //        parsedResult = this.fnc_sendMessage_parseResult(result, out isSucceeded);
                //        break;
                //    case enum_webRequestParameterType.charge:
                //        break;
                //    default:
                //        break;
                //}
                ////parsedResult = message.fnc_parseResult(result, out isSucceeded);
                ////response.StatusCode.ToString();
                //parameter.prp_isSucceeded = isSucceeded;
                //if (isSucceeded)
                //{
                //    parameter.prp_result = "Success";
                //    parameter.prp_isSucceeded = isSucceeded;
                //    //message.ReferenceId = parsedResult;
                //}
                //else
                //{
                //    parameter.prp_result = parsedResult;
                //    parameter.prp_isSucceeded = false;
                //}


            }
            //catch (System.Net.WebException ex)
            //{
            //    this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1: ", ex);
            //    parameter.prp_isSucceeded = false;
            //    parameter.prp_result = ex.Message + "\r\n" + ex.StackTrace;

            //    //message.ReferenceId = null;


            //    try
            //    {
            //        if (ex.Response != null)
            //        {
            //            using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
            //            {
            //                switch (parameter.prp_webRequestType)
            //                {
            //                    case enum_webRequestParameterType.message:
            //                        parsedResult = this.fnc_sendMessage_parseResult(rd.ReadToEnd(), out isSucceeded);
            //                        parameter.prp_result = parsedResult;
            //                        break;
            //                    case enum_webRequestParameterType.charge:
            //                        break;
            //                    default:
            //                        break;
            //                }
            //            }
            //            ex.Response.Close();
            //        }
            //        //else eventbase.prp_resultDescription = ex.Message +"\r\n" + ex.StackTrace;
            //    }
            //    catch (Exception ex1)
            //    {
            //        this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1 inner try: ", ex1);
            //    }

            //}
            //catch (Exception ex1)
            //{
            //    parameter.prp_isSucceeded = false;
            //    //message.ReferenceId = null;
            //    parameter.prp_result = ex1.Message + "\r\n" + ex1.StackTrace;

            //    this.prp_logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback2: ", ex1);

            //}
            //parameter.sb_save();
            ////this.sb_afterSend(message, isSucceeded);
            catch (Exception ex)
            {
                if (objs[1].GetType() == typeof(WebRequestParameterMessage)
                    || objs[1].GetType() == typeof(WebRequestParameter))
                    aggregator.sb_finishRequest((WebRequestParameter)objs[1], ex, false);
                else
                {
                    //SharedVariables.logs.Info("exception list2    " + ex.Message);
                    var lstParameters = (List<WebRequestParameterMessage>)objs[1];
                    for (int i = 0; i <= lstParameters.Count - 1; i++)
                    {
                        aggregator.sb_finishRequest(lstParameters[i], ex, (i == 0));
                    }
                }

            }


        }

        private void sb_saveTimings(List<WebRequestParameterMessage> parameterMessageList)
        {
            foreach (var parameter in parameterMessageList)
            {
                this.sb_saveTimings(parameter);
            }
        }
        private void sb_saveTimings(WebRequestParameterMessage parameterMessage)
        {
            SqlCommand cmd = new SqlCommand();
            try
            {
                DateTime now = DateTime.Now;

                cmd.Connection = new SqlConnection(SharedVariables.v_cnnStr);
                cmd.CommandText = "insert into ftptemp.dbo.timings "
                + "(mobileNumber, start, afterHeader, afterBody, parameterConstruction,sendRequest, RequestBefore, RequestAfter, ResponseBefore, ResponseAfter" +
                    ", ResponseEnd, requestSent)"
                + " values"
                + "('" + parameterMessage.prp_mobileNumber + "'"
                + "," + (parameterMessage.v_timings.ContainsKey("start") ? "'" + parameterMessage.v_timings["start"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("afterHeader") ? "'" + parameterMessage.v_timings["afterHeader"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("afterBody") ? "'" + parameterMessage.v_timings["afterBody"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("parameterConstruction") ? "'" + parameterMessage.v_timings["parameterConstruction"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("sendRequest") ? "'" + parameterMessage.v_timings["sendRequest"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("requestBefore") ? "'" + parameterMessage.v_timings["requestBefore"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("requestAfter") ? "'" + parameterMessage.v_timings["requestAfter"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("responseBefore") ? "'" + parameterMessage.v_timings["responseBefore"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("responseAfter") ? "'" + parameterMessage.v_timings["responseAfter"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("responseEnd") ? "'" + parameterMessage.v_timings["responseEnd"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null")
                + "," + (parameterMessage.v_timings.ContainsKey("requestSent") ? "'" + parameterMessage.v_timings["requestSent"].ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" : "Null") + ")";

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();

            }
            catch (Exception ex1)
            {
                SharedVariables.logs.Error(cmd.CommandText);
                SharedVariables.logs.Error(parameterMessage.prp_service.ServiceCode + " : Exception in saving to  timinigs: ", ex1);
                SharedLibrary.HelpfulFunctions.sb_sendNotification_DEmergency(System.Diagnostics.Eventing.Reader.StandardEventLevel.Critical, "sb_saveResponseToDB : " + parameterMessage.prp_service.ServiceCode + " Exception in saving to timings(" + ex1.Message + ")");
                //Program.logs.Error("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Warn("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");
                //Program.logs.Info("**********************Application Stops because of DB Exception has been occured during the save operation to database**********************");


            }

            //if (ev_requestFinished != null)
            //{
            //    this.ev_requestFinished(this, parameter);
            //}
        }
    }
}
