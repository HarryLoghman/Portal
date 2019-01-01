using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BulkLibrary
{
    public class SenderMTN : Sender
    {
        public SenderMTN(SharedLibrary.Models.vw_servicesServicesInfo service) : base(service)
        {

        }
        public override void sb_send(EventbaseMessagesBufferExtended eventbase)
        {
            this.sb_sendMTN(eventbase);
        }

        internal virtual void sb_sendMTN(EventbaseMessagesBufferExtended eventbase)
        {

            System.Threading.Interlocked.Increment(ref BulkSenderController.v_taskCount);
            this.sb_sendMessageToMtnSubscriberWithOutThread(eventbase);
        }

        internal virtual void sb_sendMessageToMtnSubscriberWithOutThread(EventbaseMessagesBufferExtended eventbase)
        {
            //PorShetabLibrary.Models.Singlecharge singlecharge;
            #region prepare Request
            var url = "http://92.42.55.180:8310" + "/SendSmsService/services/SendSms";
            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            string payload = SharedLibrary.MessageHandler.CreateMtnSoapEnvelopeString(this.prp_service.AggregatorServiceId
                , timeStamp, eventbase.MobileNumber, this.prp_service.ShortCode, eventbase.Content, this.prp_service.Id.ToString());
            #endregion

            try
            {
                eventbase.prp_IsSucceeded = false;
                eventbase.prp_payload = payload;
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
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
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
                response.StatusCode.ToString();
                parseResult = this.parseMTN_XMLResult(result);

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
                            eventbase.prp_resultDescription = this.parseMTN_XMLResult(rd.ReadToEnd());
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

        protected string parseMTN_XMLResult(string xmlResult)
        {
            //isSucceeded = false;
            string resultDescription = "";
            if (!string.IsNullOrEmpty(xmlResult))
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlResult);
                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/sms/send/v2_2/local");
                XmlNodeList successNodeList = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:sendSmsResponse", manager);
                if (successNodeList.Count > 0)
                {
                    foreach (XmlNode success in successNodeList)
                    {
                        XmlNode successResultNode = success.SelectSingleNode("ns1:result", manager);
                        resultDescription = successResultNode.InnerText.Trim();
                    }
                }
                else
                {
                    //isSucceeded = false;

                    manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                    XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                    foreach (XmlNode fault in faultNode)
                    {
                        XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                        XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                        resultDescription = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                    }
                }
            }
            return resultDescription;



        }
    }
}
