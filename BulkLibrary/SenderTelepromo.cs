using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace BulkLibrary
{
    class SenderTelepromo : Sender
    {

        string v_url;
        string v_userName;
        string v_password;

        public SenderTelepromo(SharedLibrary.Models.vw_servicesServicesInfo service) : base(service)
        {
            this.v_url = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.telepromoJson, SharedLibrary.HelpfulFunctions.enumServersActions.sendmessage);
            //this.v_urlDelivery = SharedLibrary.HelpfulFunctions.fnc_getServerURL(SharedLibrary.HelpfulFunctions.enumServers.MCI, SharedLibrary.HelpfulFunctions.enumServersActions.dehnadt);
            this.v_userName = "dehnad";
            this.v_password = "D4@Hn!";
        }
        public override void sb_send(EventbaseMessagesBufferExtended eventbase)
        {
            this.sb_sendToTelepromo(eventbase);
        }

        internal virtual void sb_sendToTelepromo(EventbaseMessagesBufferExtended eventbase)
        {

            System.Threading.Interlocked.Increment(ref BulkSenderController.v_taskCount);
            this.sb_sendMessageToTelepromoWithOutThread(eventbase);
        }

        internal virtual void sb_sendMessageToTelepromoWithOutThread(EventbaseMessagesBufferExtended eventbase)
        {
            //PorShetabLibrary.Models.Singlecharge singlecharge;
            #region prepare Request
            //var url = "http://172.17.251.18:8090" + "/SendSmsService/services/SendSms";
            var url = this.v_url;
            //var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            string shortCode =  "98" + this.prp_service.ShortCode;
            string description = "deliverychannel:WAP|discoverychannel:WAP|origin:" + shortCode + "|contentid:" + eventbase.MessageType;
            //string correlator = SharedLibrary. fnc_getCorrelator(shortcode, message, true);
            string json = SharedLibrary.MessageHandler.CreateTelepromoJsonString(this.v_userName, this.v_password
                                , this.prp_service.OperatorServiceId, this.prp_service.ShortCode, eventbase
                                , description, "", "0", "RLS", "1"
                                , this.prp_service.serviceName);
            
            #endregion

            try
            {
                eventbase.prp_IsSucceeded = false;
                eventbase.prp_payload = json;
                eventbase.prp_resultDescription = null;
                eventbase.prp_url = url;
                sendPostAsync(eventbase);
            }
            catch (Exception e)
            {
                //Program.logs.Info(this.prp_service.ServiceCode + " : " + payload);
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in SendMessageToMTNSubscriber: ", e);
                eventbase.prp_resultDescription = e.Message + "\r\n" + e.StackTrace;
                eventbase.prp_IsSucceeded = false;
                this.sb_saveResponseToDB(eventbase);
            }

        }

        internal void sendPostAsync(EventbaseMessagesBufferExtended eventbase)
        {

            try
            {
                Uri uri = new Uri(eventbase.prp_url, UriKind.Absolute);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Headers.Add("cache-control", "no-cache");
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "application/json;charset=\"utf-8\"";
                webRequest.Accept = "application/json";
                webRequest.Method = "POST";

                webRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallBack), new object[] { webRequest, eventbase });
            }
            catch (Exception ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in SendPostAsync: ", ex);
                eventbase.prp_resultDescription = ex.Message + "\r\n" + ex.StackTrace;
                eventbase.prp_IsSucceeded = false;
                eventbase.ReferenceId = null;
                this.sb_saveResponseToDB(eventbase);
            }
        }

        private void GetRequestStreamCallBack(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;

            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            EventbaseMessagesBufferExtended eventbase = (EventbaseMessagesBufferExtended)objs[1];

            try
            {


                using (Stream stream = webRequest.EndGetRequestStream(parameters))
                {
                    Byte[] bts = UnicodeEncoding.UTF8.GetBytes(eventbase.prp_payload);
                    stream.Write(bts, 0, bts.Count());
                }

                webRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), new object[] { webRequest, eventbase });

            }
            catch (System.Net.WebException ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack1: ", ex);
                eventbase.prp_IsSucceeded = false;
                eventbase.prp_resultDescription = null;
                eventbase.ReferenceId = null;
                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            eventbase.prp_resultDescription = rd.ReadToEnd();
                        }
                        ex.Response.Close();
                    }
                    else eventbase.prp_resultDescription = ex.Message + "\r\n" + ex.StackTrace;
                }
                catch (Exception e1)
                {
                    Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetRequestStreamCallBack inner try: ", e1);
                    eventbase.prp_resultDescription = e1.Message + "\r\n" + e1.StackTrace;
                }

                this.sb_saveResponseToDB(eventbase);
            }
            catch (Exception ex1)
            {
                eventbase.prp_IsSucceeded = false;
                eventbase.prp_resultDescription = ex1.Message + "\r\n" + ex1.StackTrace;
                eventbase.ReferenceId = null;
                this.sb_saveResponseToDB(eventbase);

            }

        }

        private void GetResponseCallback(IAsyncResult parameters)
        {
            object[] objs = (object[])parameters.AsyncState;
            bool isSucceeded = false;
            string parseResult;
            HttpWebRequest webRequest = (HttpWebRequest)objs[0];
            EventbaseMessagesBufferExtended eventbase = (EventbaseMessagesBufferExtended)objs[1];

            try
            {

                HttpWebResponse response = (HttpWebResponse)webRequest.EndGetResponse(parameters);
                string result = "";
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
                if (response.StatusCode == HttpStatusCode.OK)
                    isSucceeded = true;
                response.Close();
                //response.StatusCode.ToString();
                parseResult = this.parseTelepromo_JsonResult(result);

                if (isSucceeded)
                {
                    eventbase.prp_resultDescription = "Success";
                    eventbase.ReferenceId = parseResult;
                }
                else
                {
                    eventbase.prp_resultDescription = parseResult;
                }
                eventbase.prp_IsSucceeded = isSucceeded;

            }
            catch (System.Net.WebException ex)
            {
                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1: ", ex);
                eventbase.prp_IsSucceeded = false;
                eventbase.ReferenceId = null;
                eventbase.prp_resultDescription = ex.Message + "\r\n" + ex.StackTrace;

                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            eventbase.prp_resultDescription = this.parseTelepromo_JsonResult(rd.ReadToEnd());
                        }
                        ex.Response.Close();
                    }
                    //else eventbase.prp_resultDescription = ex.Message +"\r\n" + ex.StackTrace;
                }
                catch (Exception ex1)
                {
                    Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback1 inner try: ", ex1);
                }

            }
            catch (Exception ex1)
            {
                eventbase.prp_IsSucceeded = false;
                eventbase.ReferenceId = null;
                eventbase.prp_resultDescription = ex1.Message + "\r\n" + ex1.StackTrace;

                Program.logs.Error(this.prp_service.ServiceCode + " : Exception in GetResponseCallback2: ", ex1);

            }
            this.sb_saveResponseToDB(eventbase);
            this.sb_afterSend(eventbase, isSucceeded);


        }

        protected string parseTelepromo_JsonResult(string jsonResult)
        {
            //isSucceeded = false;
            var result = new Dictionary<string, string>();
            string resultDescription = "";
            if (!string.IsNullOrEmpty(jsonResult))
            {
                dynamic results = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                result["status_code"] = results["status_code"];
                result["status_txt"] = results["status_txt"];
                result["result"] = results["data"]["result"];
                result["success"] = results["data"]["success"];

                if (result["status_code"] == "0" && result["status_txt"].ToLower().Contains("ok"))
                {
                    resultDescription = result["result"];       
                }
                else
                {
                    Program.logs.Info("SendMesssagesToTelepromo Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                }
            }
            return resultDescription;



        }
    }
}

