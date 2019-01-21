using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorsLibrary
{
    class WebRequestProcess
    {
        
        internal void SendRequest(HttpWebRequest webRequestHeader, string body, WebRequestParameter parameter, Aggregator aggregator)
        {
            webRequestHeader.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequestHeader, parameter ,aggregator });
        }
        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            WebRequestParameter parameter = (WebRequestParameter)objs[1];
            Aggregator aggregator = (Aggregator)objs[2];
            try
            {
                using (Stream stream = webRequest.EndGetRequestStream(parameters))
                {
                    Byte[] bts = UnicodeEncoding.UTF8.GetBytes(parameter.prp_bodyString);
                    stream.Write(bts, 0, bts.Count());
                }
                webRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), new object[] { webRequest, parameter , aggregator });

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
            catch(Exception ex)
            {
                aggregator.sb_finishRequest(parameter, ex);
            }

        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            //bool isSucceeded = false;
            //string parsedResult = "";
            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            WebRequestParameter parameter = (WebRequestParameter)objs[1];
            Aggregator aggregator = (Aggregator)objs[2];
            try
            {
                HttpWebResponse response = (HttpWebResponse)webRequest.EndGetResponse(parameters);
                string result = "";
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
                bool httpOk = false;
                if (response.StatusCode == HttpStatusCode.OK)
                    httpOk = true;
                response.Close();
                aggregator.sb_finishRequest(parameter, httpOk, result);
                

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
            catch(Exception ex)
            {
                aggregator.sb_finishRequest(parameter, ex);
            }


        }
    }
}
