using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DehnadReceiveProcessorService
{
    class Irancell
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public object HttpUtility { get; private set; }

        public async void GetMo()
        {
            try
            {
                var timeStamp = SharedLibrary.Aggregators.AggregatorMTN.MTNTimestamp(DateTime.Now);
                var servicesInfoList = SharedLibrary.ServiceHandler.GetAllServicesByAggregatorId(7); // 7 = Irancell
                var spId = "980110006379";
                foreach (var serviceInfo in servicesInfoList)
                {
                    string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/receive/v2_2/local"">     <soapenv:Header>        <v2:RequestSOAPHeader>           <v2:spId>{3}</v2:spId>           <v2:serviceId>{0}</v2:serviceId>           <v2:timeStamp>{1}</v2:timeStamp>        </v2:RequestSOAPHeader>     </soapenv:Header>     <soapenv:Body>        <loc:getReceivedSms>            <loc:registrationIdentifier>{2}</loc:registrationIdentifier>        </loc:getReceivedSms>     </soapenv:Body>  </soapenv:Envelope> "
    , serviceInfo.AggregatorServiceId, timeStamp, serviceInfo.ShortCode, spId);

                    //string url = "http://92.42.55.180:8310/ReceiveSmsService/services/ReceiveSms/getReceivedSmsRequest";
                    string url = SharedLibrary.HelpfulFunctions.fnc_getServerActionURL(SharedLibrary.HelpfulFunctions.enumServers.MTN, SharedLibrary.HelpfulFunctions.enumServersActions.MTNGetReceivedSms);
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                    using (var client = new HttpClient())
                    {
                        using (var httpClientResponse = await client.SendAsync(request))
                        {
                            if (httpClientResponse.IsSuccessStatusCode || httpClientResponse.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                string httpResult = httpClientResponse.Content.ReadAsStringAsync().Result;
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml(httpResult);
                                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/sms/receive/v2_2/local");
                                XmlNodeList messages = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:getReceivedSmsResponse/ns1:result", manager);
                                foreach (XmlNode message in messages)
                                {
                                    var newMessage = new MessageObject();
                                    XmlNode contentNode = message.SelectSingleNode("message");
                                    XmlNode mobileNumberNode = message.SelectSingleNode("senderAddress");
                                    XmlNode shortCodeNode = message.SelectSingleNode("smsServiceActivationNumber");
                                    newMessage.Content = contentNode.InnerText;
                                    newMessage.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(mobileNumberNode.InnerText.Replace("tel:", "").Trim());
                                    newMessage.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(shortCodeNode.InnerText.Replace("tel:", "").Trim());
                                    newMessage.ReceivedFrom = "92.42.55.180";
                                    SharedLibrary.MessageHandler.SaveReceivedMessage(newMessage);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in getReceivedSms: " + e);
            }
        }
    }
}
