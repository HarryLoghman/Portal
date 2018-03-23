using Newtonsoft.Json;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SharedLibrary
{
    public class MessageSender
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int retryCountMax = 15;
        public static int retryPauseBeforeSendByMinute = -1;
        public static string telepromoIp = "http://10.20.9.135:8600"; // "http://10.20.9.157:8600" "http://10.20.9.159:8600"
        public static string irancellIp = "http://92.42.55.180:8310";

        public static async Task SendMesssagesToTelepromo(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;

                    var url = telepromoIp + "/samsson-sdp/transfer/send?";
                    var sc = "Dehnad";
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var from = "98" + serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    Random rnd = new Random();
                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            if (message.RetryCount != null && message.RetryCount >= retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }

                            var to = "98" + message.MobileNumber.TrimStart('0');
                            var messageContent = message.Content;

                            var messageId = rnd.Next(1000000, 9999999).ToString();
                            var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                    , sc, username, password, from, serviceId, to, messageContent, messageId);
                            if (message.Price > 0)
                                urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);

                            var result = new Dictionary<string, string>();
                            result["status"] = "";
                            result["message"] = "";
                            try
                            {
                                result = await SendSingleMessageToTelepromo(client, urlWithParameters);
                            }
                            catch (Exception e)
                            {
                                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
                            }


                            if (result["status"] == "0")
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                message.ReferenceId = messageId;
                                message.SentDate = DateTime.Now;
                                message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                if (message.MessagePoint > 0)
                                    SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
                                entity.Entry(message).State = EntityState.Modified;
                            }
                            else
                            {
                                logs.Info("SendMesssagesToTelepromo Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.DateLastTried = DateTime.Now;
                                message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                                entity.Entry(message).State = EntityState.Modified;
                            }
                        }
                        entity.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMessagesToTelepromo: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }

        public static async Task<dynamic> SendSinglechargeMesssageToTelepromo(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var url = telepromoIp + "/samsson-sdp/transfer/charge?";
                    var sc = "Dehnad";
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var from = "98" + serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    using (var client = new HttpClient())
                    {
                        var to = "98" + message.MobileNumber.TrimStart('0');
                        var messageContent = "InAppPurchase";
                        Random rnd = new Random();
                        var messageId = rnd.Next(1000000, 9999999).ToString();
                        var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                , sc, username, password, from, serviceId, to, messageContent, messageId);
                        urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                        var result = new Dictionary<string, string>();
                        result["status"] = "";
                        result["message"] = "";
                        result = await SendSingleMessageToTelepromo(client, urlWithParameters);
                        if (result["status"] == "0" && result["message"].Contains("description=ACCEPTED"))
                            singlecharge.IsSucceeded = true;
                        else
                            singlecharge.IsSucceeded = false;

                        singlecharge.Description = result["message"];
                        singlecharge.ReferenceId = messageId;
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendSinglechargeMesssageToTelepromo: " + e);
                }
                try
                {
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "IsSucceeded") != true)
                        singlecharge.IsSucceeded = false;
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                        singlecharge.ReferenceId = "Exception occurred!";
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;

                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendSinglechargeMesssageToTelepromo on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> TelepromoOTPRequest(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var url = telepromoIp + "/samsson-sdp/pin/generate?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                Random rnd = new Random();
                using (var client = new HttpClient())
                {
                    var to = "98" + message.MobileNumber.TrimStart('0');
                    var messageContent = "InAppPurchase";
                    var contentId = rnd.Next(00001, 99999).ToString();
                    var messageId = rnd.Next(1000000, 9999999).ToString();
                    var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}&contentId={8}&chargingCode={9}"
                                                            , sc, username, password, from, serviceId, to, messageContent, messageId, contentId, message.ImiChargeKey);
                    var result = new Dictionary<string, string>();
                    result["status"] = "";
                    result["message"] = "";
                    result = await SendSingleMessageToTelepromoJsonResponse(client, urlWithParameters);
                    if (result["status"] == "0" && result["message"].Contains("SUCCESS"))
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                    else
                        singlecharge.Description = result["message"];

                    singlecharge.ReferenceId = result["messageId"] + "_" + result["transactionId"];
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoOTPRequest: " + e);
                singlecharge.Description = "Exception";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoOTPRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> TelepromoOTPConfirm(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var url = telepromoIp + "/samsson-sdp/pin/confirm?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                using (var client = new HttpClient())
                {
                    var to = "98" + message.MobileNumber.TrimStart('0');
                    var messageContent = "InAppPurchase";
                    string otpIds = singlecharge.ReferenceId;
                    var optIdsSplitted = otpIds.Split('_');
                    var messageId = optIdsSplitted[0];
                    var transactionId = optIdsSplitted[1];
                    var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}&transactionId={8}&pin={9}"
                                                            , sc, username, password, from, serviceId, to, messageContent, messageId, transactionId, confirmationCode);
                    var result = new Dictionary<string, string>();
                    result["status"] = "";
                    result["message"] = "";
                    result = await SendSingleMessageToTelepromoJsonResponse(client, urlWithParameters);
                    singlecharge.Description = result["message"] + "-code:" + confirmationCode;
                    if (result["status"] == "0" && result["message"].Contains("SUCCESS"))
                    {
                        singlecharge.IsSucceeded = true;
                        singlecharge.Description = "SUCCESS";
                        entity.Entry(singlecharge).State = EntityState.Modified;
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }

            return singlecharge;
        }

        public static async Task<Dictionary<string, string>> SendSingleMessageToTelepromo(HttpClient client, string url)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            result["transactionId"] = "";
            try
            {
                //HttpMethod method = HttpMethod.Head;
                //var httpRequest = new HttpRequestMessage() { RequestUri = new Uri(url), Method = method };
                //using (var response = client.SendAsync(httpRequest).Result)
                //{

                //}
                using (var response = client.GetAsync(new Uri(url)).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        XDocument xmlResult = XDocument.Parse(httpResult);
                        result["status"] = xmlResult.Root.Descendants("status").Select(e => e.Value).FirstOrDefault();
                        result["message"] = xmlResult.Root.Descendants("message").Select(e => e.Value).FirstOrDefault();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("request:" + url);
                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
            }
            return result;
        }

        private static async Task<Dictionary<string, string>> SendSingleMessageToTelepromoJsonResponse(HttpClient client, string url)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            result["transactionId"] = "";
            result["messageId"] = "";
            try
            {
                using (var response = client.GetAsync(new Uri(url)).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        dynamic results = JsonConvert.DeserializeObject<dynamic>(httpResult);
                        result["status"] = results.status;
                        result["message"] = results.message;
                        result["messageId"] = results["messageId"];
                        result["transactionId"] = results["transactionId"];
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
                result["status"] = "900";
                result["message"] = "Exception in calling aggregator webservice";
            }
            return result;
        }

        public static async Task SendMesssagesToHub(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                var waitingForRetryMobileNumbers = new List<string>();
                try
                {
                    await Task.Delay(10); // for making it async
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var aggregatorUsername = serviceAdditionalInfo["username"];
                    var aggregatorPassword = serviceAdditionalInfo["password"];
                    var from = serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    var subUnsubXmlStringList = new List<string>();

                    XmlDocument doc = new XmlDocument();
                    XmlElement root = doc.CreateElement("xmsrequest");
                    XmlElement userid = doc.CreateElement("userid");
                    XmlElement password = doc.CreateElement("password");
                    XmlElement action = doc.CreateElement("action");
                    XmlElement body = doc.CreateElement("body");
                    XmlElement type = doc.CreateElement("type");
                    type.InnerText = "oto";
                    body.AppendChild(type);
                    XmlElement serviceid = doc.CreateElement("serviceid");
                    serviceid.InnerText = serviceId;
                    body.AppendChild(serviceid);

                    foreach (var message in messages)
                    {
                        if (message.RetryCount != null && message.RetryCount >= retryCountMax)
                        {
                            waitingForRetryMobileNumbers.Add(message.MobileNumber);
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            entity.Entry(message).State = EntityState.Modified;
                            continue;
                        }
                        if (message.ImiChargeKey == "UnSubscription" || message.ImiChargeKey == "Register" || message.ImiChargeKey == "Renewal")
                        {
                            XmlDocument doc1 = new XmlDocument();
                            XmlElement root1 = doc1.CreateElement("xmsrequest");
                            XmlElement userid1 = doc1.CreateElement("userid");
                            XmlElement password1 = doc1.CreateElement("password");
                            XmlElement action1 = doc1.CreateElement("action");
                            XmlElement body1 = doc1.CreateElement("body");
                            XmlElement serviceid1 = doc1.CreateElement("serviceid");
                            XmlElement mobile1 = doc1.CreateElement("mobile");
                            XmlElement smsid = doc1.CreateElement("smsid");
                            XmlElement subUnSub;
                            serviceid1.InnerText = serviceId;
                            body1.AppendChild(serviceid1);

                            mobile1.InnerText = message.MobileNumber;
                            body1.AppendChild(mobile1);

                            smsid.InnerText = "-1";
                            body1.AppendChild(smsid);


                            if (message.ImiChargeKey == "UnSubscription")
                            {
                                subUnSub = doc1.CreateElement("sendgoodbye");
                                action1.InnerText = "vasremovemember";
                            }
                            else
                            {
                                subUnSub = doc1.CreateElement("sendwellcome");
                                action1.InnerText = "vasaddmember";
                            }
                            subUnSub.InnerText = "0";
                            body1.AppendChild(subUnSub);

                            userid1.InnerText = aggregatorUsername;
                            password1.InnerText = aggregatorPassword;

                            doc1.AppendChild(root1);
                            root1.AppendChild(userid1);
                            root1.AppendChild(password1);
                            root1.AppendChild(action1);
                            root1.AppendChild(body1);

                            subUnsubXmlStringList.Add(doc1.OuterXml);
                        }
                        XmlElement recipient = doc.CreateElement("recipient");
                        recipient.InnerText = message.Content;
                        body.AppendChild(recipient);

                        XmlAttribute mobile = doc.CreateAttribute("mobile");
                        recipient.Attributes.Append(mobile);
                        mobile.InnerText = message.MobileNumber;

                        XmlAttribute originator = doc.CreateAttribute("originator");
                        originator.InnerText = from;
                        recipient.Attributes.Append(originator);

                        XmlAttribute cost = doc.CreateAttribute("cost");
                        cost.InnerText = message.Price.ToString();
                        recipient.Attributes.Append(cost);

                        //XmlAttribute type1 = doc.CreateAttribute("type");
                        //type1.InnerText = "250";
                        //recipient.Attributes.Append(type1);
                    }

                    userid.InnerText = aggregatorUsername;
                    password.InnerText = aggregatorPassword;
                    action.InnerText = "smssend";
                    //
                    doc.AppendChild(root);
                    root.AppendChild(userid);
                    root.AppendChild(password);
                    root.AppendChild(action);
                    root.AppendChild(body);
                    //
                    string stringedXml = doc.OuterXml;
                    SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                    foreach (var subUnsubStringXml in subUnsubXmlStringList)
                    {
                        string subUnsubResponse = hubClient.XmsRequest(subUnsubStringXml).ToString();
                    }
                    string response = hubClient.XmsRequest(stringedXml).ToString();
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(response);
                    XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                    foreach (XmlNode error in OK)
                    {
                        if (error.InnerText != "" && error.InnerText != "ok")
                        {
                            logs.Error("Error in sending message to Hub");
                            foreach (var message in messages)
                            {
                                if (waitingForRetryMobileNumbers.Contains(message.MobileNumber))
                                    continue;
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.DateLastTried = DateTime.Now;
                                message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                                entity.Entry(message).State = EntityState.Modified;
                            }
                            entity.SaveChanges();
                        }
                        else
                        {
                            var i = 0;
                            XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                            foreach (XmlNode xn in xnList)
                            {
                                string responseCode = (xn.Attributes["status"].Value).ToString();
                                if (responseCode == "40")
                                {
                                    messages[i].ReferenceId = xn.InnerText;
                                    messages[i].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                    if (messages[i].MessagePoint > 0)
                                        SharedLibrary.MessageHandler.SetSubscriberPoint(messages[i].MobileNumber, messages[i].ServiceId, messages[i].MessagePoint);
                                }
                                else
                                {
                                    messages[i].ReferenceId = "failed:" + responseCode;
                                    if (messages[i].RetryCount == null)
                                    {
                                        messages[i].RetryCount = 1;
                                        messages[i].DateLastTried = DateTime.Now;
                                    }
                                    else
                                    {
                                        if (messages[i].RetryCount > retryCountMax)
                                            messages[i].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                        messages[i].RetryCount += 1;
                                        messages[i].DateLastTried = DateTime.Now;
                                    }
                                }
                                messages[i].SentDate = DateTime.Now;
                                messages[i].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                entity.Entry(messages[i]).State = EntityState.Modified;
                                i++;
                            }
                            entity.SaveChanges();
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMessagesToHub: " + e);
                    foreach (var message in messages)
                    {
                        if (waitingForRetryMobileNumbers.Contains(message.MobileNumber))
                            continue;
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }

        public static async Task<dynamic> SendSinglechargeMesssageToHub(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var aggregatorUsername = serviceAdditionalInfo["username"];
                    var aggregatorPassword = serviceAdditionalInfo["password"];
                    var from = serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                    XmlDocument doc = new XmlDocument();
                    XmlElement root = doc.CreateElement("xmsrequest");
                    XmlElement userid = doc.CreateElement("userid");
                    XmlElement password = doc.CreateElement("password");
                    XmlElement action = doc.CreateElement("action");
                    XmlElement body = doc.CreateElement("body");
                    XmlElement serviceid = doc.CreateElement("serviceid");
                    serviceid.InnerText = serviceId;
                    body.AppendChild(serviceid);

                    XmlElement recipient = doc.CreateElement("recipient");
                    body.AppendChild(recipient);

                    XmlAttribute mobile = doc.CreateAttribute("mobile");
                    recipient.Attributes.Append(mobile);
                    mobile.InnerText = message.MobileNumber;

                    XmlAttribute originator = doc.CreateAttribute("originator");
                    originator.InnerText = from;
                    recipient.Attributes.Append(originator);

                    XmlAttribute cost = doc.CreateAttribute("cost");
                    cost.InnerText = (message.Price * 10).ToString();
                    recipient.Attributes.Append(cost);

                    //XmlAttribute type1 = doc.CreateAttribute("type");
                    //type1.InnerText = "250";
                    //recipient.Attributes.Append(type1);

                    userid.InnerText = aggregatorUsername;
                    password.InnerText = aggregatorPassword;
                    action.InnerText = "singlecharge";
                    //
                    doc.AppendChild(root);
                    root.AppendChild(userid);
                    root.AppendChild(password);
                    root.AppendChild(action);
                    root.AppendChild(body);
                    //
                    string stringedXml = doc.OuterXml;
                    SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                    logs.Info(stringedXml);
                    string response = hubClient.XmsRequest(stringedXml).ToString();
                    XmlDocument xml = new XmlDocument();
                    XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                    foreach (XmlNode error in OK)
                    {
                        if (error.InnerText != "" && error.InnerText != "ok")
                        {
                            logs.Error("Error in Singlecharge using Hub");
                        }
                        else
                        {
                            var i = 0;
                            XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                            foreach (XmlNode xn in xnList)
                            {
                                string responseCode = (xn.Attributes["status"].Value).ToString();
                                if (responseCode == "40")
                                {
                                    singlecharge.IsSucceeded = false;
                                    singlecharge.Description = responseCode;
                                    singlecharge.ReferenceId = xn.InnerText;
                                }
                                else
                                {
                                    singlecharge.IsSucceeded = false;
                                    singlecharge.Description = responseCode;
                                    //singlecharge.ReferenceId = xn.InnerText;
                                }
                                i++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendSinglechargeMesssageToHub: " + e);
                }
                try
                {
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "IsSucceeded") != true)
                        singlecharge.IsSucceeded = false;
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                        singlecharge.ReferenceId = "Exception occurred!";
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;

                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendSinglechargeMesssageToHub on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> HubOtpChargeRequest(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var aggregatorUsername = serviceAdditionalInfo["username"];
                var aggregatorPassword = serviceAdditionalInfo["password"];
                var from = serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("xmsrequest");
                XmlElement userid = doc.CreateElement("userid");
                XmlElement password = doc.CreateElement("password");
                XmlElement action = doc.CreateElement("action");
                XmlElement body = doc.CreateElement("body");
                XmlElement serviceid = doc.CreateElement("serviceid");
                serviceid.InnerText = serviceId;
                body.AppendChild(serviceid);

                XmlElement recipient = doc.CreateElement("recipient");
                body.AppendChild(recipient);

                XmlAttribute mobile = doc.CreateAttribute("mobile");
                recipient.Attributes.Append(mobile);
                mobile.InnerText = message.MobileNumber;

                XmlAttribute originator = doc.CreateAttribute("originator");
                originator.InnerText = from;
                recipient.Attributes.Append(originator);

                XmlAttribute cost = doc.CreateAttribute("cost");
                if (message.Price == 5 || message.Price == 6) // 5 for Sub and 6 for Unsub price on otp
                    cost.InnerText = message.Price.ToString();
                else
                    cost.InnerText = (message.Price * 10).ToString();

                recipient.Attributes.Append(cost);

                var random = new Random();
                long doer = random.Next(10000000, 999999999);
                XmlAttribute doerid = doc.CreateAttribute("doerid");
                doerid.InnerText = doer.ToString();
                recipient.Attributes.Append(doerid);

                userid.InnerText = aggregatorUsername;
                password.InnerText = aggregatorPassword;
                action.InnerText = "PushOtp";
                //
                doc.AppendChild(root);
                root.AppendChild(userid);
                root.AppendChild(password);
                root.AppendChild(action);
                root.AppendChild(body);
                //
                string stringedXml = doc.OuterXml;
                SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                string response = hubClient.XmsRequest(stringedXml).ToString();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response);
                XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                foreach (XmlNode error in OK)
                {
                    if (error.InnerText != "" && error.InnerText != "ok")
                    {
                        logs.Error("Error in OtpChargeReuqest using Hub");
                    }
                    else
                    {
                        var i = 0;
                        XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                        foreach (XmlNode xn in xnList)
                        {
                            string responseCode = (xn.Attributes["status"].Value).ToString();
                            if (responseCode == "40")
                            {
                                singlecharge.Description = "SUCCESS-Pending Confirmation";
                                singlecharge.ReferenceId = xn.InnerText;
                            }
                            else
                            {
                                singlecharge.Description = responseCode;
                                //singlecharge.ReferenceId = xn.InnerText;
                            }
                            i++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in HubOtpChargeRequest: " + e);
                singlecharge.Description = "Exception Occurred";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                if (message.Price == 5 || message.Price == 6)
                    singlecharge.Price = 0;
                else
                    singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in HubOtpChargeRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> HubOTPConfirm(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var aggregatorUsername = serviceAdditionalInfo["username"];
                var aggregatorPassword = serviceAdditionalInfo["password"];
                var from = serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("xmsrequest");
                XmlElement userid = doc.CreateElement("userid");
                XmlElement password = doc.CreateElement("password");
                XmlElement action = doc.CreateElement("action");
                XmlElement body = doc.CreateElement("body");
                XmlElement serviceid = doc.CreateElement("serviceid");
                serviceid.InnerText = serviceId;
                body.AppendChild(serviceid);

                XmlElement recipient = doc.CreateElement("recipient");
                body.AppendChild(recipient);

                XmlAttribute mobile = doc.CreateAttribute("mobile");
                recipient.Attributes.Append(mobile);
                mobile.InnerText = message.MobileNumber;

                XmlAttribute originator = doc.CreateAttribute("originator");
                originator.InnerText = from;
                recipient.Attributes.Append(originator);

                var random = new Random();
                long doer = random.Next(10000000, 999999999);
                XmlAttribute doerid = doc.CreateAttribute("doerid");
                doerid.InnerText = doer.ToString();
                recipient.Attributes.Append(doerid);

                XmlAttribute pin = doc.CreateAttribute("pin");
                pin.InnerText = confirmationCode;
                recipient.Attributes.Append(pin);

                recipient.InnerText = singlecharge.ReferenceId;

                userid.InnerText = aggregatorUsername;
                password.InnerText = aggregatorPassword;
                action.InnerText = "chargeotp";
                //
                doc.AppendChild(root);
                root.AppendChild(userid);
                root.AppendChild(password);
                root.AppendChild(action);
                root.AppendChild(body);
                //
                string stringedXml = doc.OuterXml;
                SharedLibrary.HubServiceReference.SmsSoapClient hubClient = new SharedLibrary.HubServiceReference.SmsSoapClient();
                string response = hubClient.XmsRequest(stringedXml).ToString();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response);
                XmlNodeList OK = xml.SelectNodes("/xmsresponse/code");
                foreach (XmlNode error in OK)
                {
                    if (error.InnerText != "" && error.InnerText != "ok")
                    {
                        logs.Error("Error in HubOtpConfrim");
                    }
                    else
                    {
                        var i = 0;
                        XmlNodeList xnList = xml.SelectNodes("/xmsresponse/body/recipient");
                        foreach (XmlNode xn in xnList)
                        {
                            string responseCode = (xn.Attributes["status"].Value).ToString();
                            if (responseCode == "41")
                            {
                                singlecharge.Description = "SUCCESS";
                                singlecharge.ReferenceId = xn.InnerText;
                                singlecharge.IsSucceeded = true;
                                entity.Entry(singlecharge).State = EntityState.Modified;
                                entity.SaveChanges();
                            }
                            else
                            {
                                singlecharge.Description = responseCode;
                                //singlecharge.ReferenceId = xn.InnerText;
                            }
                            i++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in HubOtpConfrim: " + e);
                singlecharge.Description = "Exception Occurred";
            }
            return singlecharge;
        }

        public static async Task<dynamic> PardisImiOtpChargeRequest(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var SPID = "RESA";
                var otpRequest = new SharedLibrary.PardisOTPServiceReference.CPOTPRequest();
                string mobileNumber = message.MobileNumber;
                otpRequest.MobileNo = Convert.ToInt64("98" + mobileNumber.TrimStart('0'));
                otpRequest.ShortCode = Convert.ToInt64("98" + serviceAdditionalInfo["shortCode"]);
                otpRequest.ChargeCode = message.ImiChargeKey;

                var pardisClient = new SharedLibrary.PardisOTPServiceReference.OTPSoapClient();
                var pardisResponse = pardisClient.Request(SPID, otpRequest);
                if (pardisResponse.OTPTransactionId != null)
                {
                    singlecharge.Description = "SUCCESS-Pending Confirmation";
                    singlecharge.ReferenceId = pardisResponse.ReferenceCode + "_" + pardisResponse.OTPTransactionId;
                }
                else
                    singlecharge.Description = pardisResponse.ErrorCode + ":" + pardisResponse.ErrorMessage;
            }
            catch (Exception e)
            {
                logs.Error("Exception in PardisImiOtpChargeRequest: " + e);
                singlecharge.Description = "Exception Occurred";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in PardisImiOtpChargeRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> PardisImiOTPConfirm(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var SPID = "RESA";
                var otpConfirm = new SharedLibrary.PardisOTPServiceReference.CPOTPConfirm();
                string mobileNumber = message.MobileNumber;
                otpConfirm.MobileNo = Convert.ToInt64("98" + mobileNumber.TrimStart('0'));
                otpConfirm.ShortCode = Convert.ToInt64("98" + serviceAdditionalInfo["shortCode"]);
                otpConfirm.PIN = confirmationCode;
                string otpIds = singlecharge.ReferenceId;
                var optIdsSplitted = otpIds.Split('_');
                var messageId = optIdsSplitted[0];
                var transactionId = optIdsSplitted[1];
                otpConfirm.OTPTransactionId = Convert.ToInt64(transactionId);

                var pardisClient = new SharedLibrary.PardisOTPServiceReference.OTPSoapClient();
                var pardisResponse = pardisClient.Confirm(SPID, otpConfirm);
                if (pardisResponse.ReferenceCode != null)
                {
                    singlecharge.IsSucceeded = true;
                    singlecharge.Description = "SUCCESS";
                    entity.Entry(singlecharge).State = EntityState.Modified;
                    entity.SaveChanges();
                }
                else
                    singlecharge.Description = pardisResponse.ErrorCode + ":" + pardisResponse.ErrorMessage;
            }
            catch (Exception e)
            {
                logs.Error("Exception in PardisImiOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }

            return singlecharge;
        }

        public static async Task SendMesssagesToPardisPlatform(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";

                    string[] mobileNumbers = new string[messagesCount];
                    string[] shortCodes = new string[messagesCount];
                    string[] messageContents = new string[messagesCount];
                    string[] aggregatorServiceIds = new string[messagesCount];
                    string[] udhs = new string[messagesCount];
                    string[] mclass = new string[messagesCount];

                    foreach (var message in messages)
                    {
                        if (message.RetryCount != null && message.RetryCount > retryCountMax)
                        {
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }
                    entity.SaveChanges();

                    for (int index = 0; index < messagesCount; index++)
                    {
                        mobileNumbers[index] = "98" + messages[index].MobileNumber.TrimStart('0');
                        shortCodes[index] = "98" + paridsShortCodes.FirstOrDefault(o => o.Price == messages[index].Price).ShortCode;
                        messageContents[index] = messages[index].Content;
                        aggregatorServiceIds[index] = paridsShortCodes.FirstOrDefault(o => o.Price == messages[index].Price).PardisServiceId;
                        udhs[index] = "";
                        mclass[index] = "";
                    }
                    mobileNumbers = mobileNumbers.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    shortCodes = shortCodes.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    messageContents = messageContents.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    aggregatorServiceIds = aggregatorServiceIds.Where(o => !string.IsNullOrEmpty(o)).ToArray();
                    long[] pardisResponse;
                    if (aggregatorId == "3")
                    {
                        var pardisClient = new SharedLibrary.PardisPlatformServiceReference.SendClient();
                        pardisResponse = pardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                    }
                    else
                    {
                        var mobinonePardisClient = new SharedLibrary.MobinOneMapfaSendServiceReference.SendClient();
                        pardisResponse = mobinonePardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                    }
                    logs.Info("pardis Response count: " + pardisResponse.Count());
                    if (pardisResponse == null || pardisResponse.Count() < messagesCount)
                    {
                        foreach (var item in pardisResponse)
                        {
                            logs.Info("paridsResposne when count < messageCount: " + item);
                        }
                        pardisResponse = new long[messagesCount];
                    }
                    for (int index = 0; index < messagesCount; index++)
                    {
                        if (pardisResponse[index] == null)
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        else if (pardisResponse[index] <= 100)
                        {
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            messages[index].ReferenceId = pardisResponse[index].ToString();
                            if (messages[index].MessagePoint > 0)
                                SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                        }
                        else
                        {
                            messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                            messages[index].ReferenceId = pardisResponse[index].ToString();
                            if (messages[index].MessagePoint > 0)
                                SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                        }
                        messages[index].SentDate = DateTime.Now;
                        messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                        entity.Entry(messages[index]).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMesssagesToPardisPlatform: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }

        public static async Task<dynamic> MapfaOTPRequest(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.OrderByDescending(o => o.Price).FirstOrDefault().PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var channelType = (int)MessageHandler.MapfaChannels.SMS;
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    var client = new MobinOneMapfaChargingServiceReference.ChargingClient();
                    var result = client.sendVerificationCode(username, password, domain, channelType, mobileNumber, aggregatorServiceId);

                    if (result == 0)
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                    else
                        singlecharge.Description = result.ToString();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaOTPRequest: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {
                    singlecharge.IsSucceeded = false;
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = true;

                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaOTPRequest on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> MapfaOTPConfirm(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.OrderByDescending(o => o.Price).FirstOrDefault().PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var channelType = (int)MessageHandler.MapfaChannels.SMS;
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    var client = new MobinOneMapfaChargingServiceReference.ChargingClient();
                    var result = client.verifySubscriber(username, password, domain, channelType, mobileNumber, aggregatorServiceId, confirmationCode);
                    singlecharge.Description = result.ToString();
                    if (result == 0)
                    {
                        singlecharge.IsSucceeded = true;
                        singlecharge.Description = "SUCCESS";
                    }
                    entity.Entry(singlecharge).State = EntityState.Modified;
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaOTPConfirm: " + e);
                    singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> MapfaStaticPriceSinglecharge(Type entityType, Type singlechargeType, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                dynamic singlecharge = Activator.CreateInstance(singlechargeType);
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price.Value).PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var channelType = (int)MessageHandler.MapfaChannels.SMS;
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    var client = new MobinOneMapfaChargingServiceReference.ChargingClient();
                    var result = client.singleCharge(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    if (result > 10000)
                        singlecharge.IsSucceeded = true;
                    else
                        singlecharge.IsSucceeded = false;

                    singlecharge.Description = result.ToString();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaStaticPriceSinglecharge: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {

                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaStaticPriceSinglecharge on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> MapfaDynamicPriceSinglecharge(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price.Value).PardisServiceId;
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var aggregatorId = serviceAdditionalInfo["aggregatorId"];
                    var channelType = (int)MessageHandler.MapfaChannels.SMS;
                    var domain = "";
                    if (aggregatorId == "3")
                        domain = "pardis1";
                    else
                        domain = "alladmin";
                    var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    var price = Convert.ToInt64(message.Price.Value * 10);
                    var client = new MobinOneMapfaChargingServiceReference.ChargingClient();
                    var result = client.dynamicCharge(username, password, domain, channelType, mobileNumber, aggregatorServiceId, price);
                    if (result > 10000)
                        singlecharge.IsSucceeded = true;
                    else
                        singlecharge.IsSucceeded = false;

                    singlecharge.Description = result;
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaDynamicPriceSinglecharge: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in MapfaDynamicPriceSinglecharge on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task<dynamic> SamssonTciSinglecharge(Type entityType, Type singlechargeType, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, bool fromWeb, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                dynamic singlecharge = Activator.CreateInstance(singlechargeType);
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    var splitedToken = message.Token.Split(';');
                    singlecharge.UserToken = splitedToken[0];
                    var packageName = splitedToken[1];
                    var sku = splitedToken[2];
                    bool isCancel = false;
                    ServicePointManager.ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => true;
                    using (var client = new HttpClient())
                    {
                        var values = new Dictionary<string, string>
                        {
                        };

                        var content = new FormUrlEncodedContent(values);
                        var url = "";
                        HttpResponseMessage response = null;
                        if (message.Price >= 0)
                        {
                            url = string.Format("https://samssonsdp.com/api/v1/GuestMode/Bill/{0}/{1}/{2}/{3}", singlecharge.UserToken, packageName, sku, message.Price);
                            singlecharge.ReferenceId = "Charging";
                            logs.Info("samssonsinglecharge url: " + url);
                            response = await client.PostAsync(url, content);
                        }
                        else
                        {
                            url = string.Format("https://samssonsdp.com/api/v1/GuestMode/cancel/{0}/{1}/{2}/", singlecharge.UserToken, packageName, sku);
                            singlecharge.ReferenceId = "Unsubscribe";
                            message.Price = 0;
                            isCancel = true;
                            logs.Info("samssonsinglecharge url: " + url);
                            response = await client.GetAsync(url);
                        }

                        var responseString = await response.Content.ReadAsStringAsync();
                        logs.Info(responseString);
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                        if (jsonResponse.error == null)
                        {
                            string value = jsonResponse.value.ToString();
                            value = value.Replace("[\r\n", string.Empty).Replace("\r\n]", string.Empty);
                            dynamic valueObj = Newtonsoft.Json.JsonConvert.DeserializeObject(value);
                            if (isCancel == true)
                            {
                                if (valueObj.canceled == true)
                                {
                                    singlecharge.IsSucceeded = true;
                                    singlecharge.Description = "User Successfuly Canceled";
                                }
                                else
                                {
                                    singlecharge.IsSucceeded = false;
                                    singlecharge.Description = "User Does Not Canceled";
                                }
                            }
                            else
                            {
                                if (valueObj.status == true)
                                    singlecharge.IsSucceeded = true;
                                else
                                    singlecharge.IsSucceeded = false;
                                singlecharge.Description = value;
                            }
                        }
                        else
                        {
                            string e = jsonResponse.error.ToString();
                            e = e.Replace("[\r\n", string.Empty).Replace("\r\n]", string.Empty);
                            dynamic error = Newtonsoft.Json.JsonConvert.DeserializeObject(e);
                            singlecharge.Description = error.code.ToString() + "-" + error.message.ToString();
                            singlecharge.IsSucceeded = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SamssonTciSinglecharge: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {

                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;
                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SamssonTciSinglecharge on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task SendMesssagesToPardisImi(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var SPID = "RESA";
                    SharedLibrary.PardisImiServiceReference.SMS[] smsList = new SharedLibrary.PardisImiServiceReference.SMS[messagesCount];

                    foreach (var message in messages)
                    {
                        if (message.RetryCount != null && message.RetryCount > retryCountMax)
                        {
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }

                    for (int index = 0; index < messagesCount; index++)
                    {
                        smsList[index] = new SharedLibrary.PardisImiServiceReference.SMS()
                        {
                            Index = index + 1,
                            Addresses = "98" + messages[index].MobileNumber.TrimStart('0'),
                            ShortCode = "98" + serviceAdditionalInfo["shortCode"],
                            Message = messages[index].Content,
                            ChargeCode = messages[index].ImiChargeKey,
                            SubUnsubMoMessage = messages[index].SubUnSubMoMssage,
                            SubUnsubType = messages[index].SubUnSubType
                        };
                    }
                    smsList = smsList.Where(o => o.Index != null).ToArray();
                    var pardisClient = new SharedLibrary.PardisImiServiceReference.MTSoapClient();
                    var pardisResponse = pardisClient.SendSMS(SPID, smsList);
                    if (pardisResponse.Rows.Count == 0)
                    {
                        logs.Info("SendMessagesToPardisImi does not return response there must be somthing wrong with the parameters");
                        foreach (var sms in smsList)
                        {
                            logs.Info("Index: " + sms.Index);
                            logs.Info("Addresses: " + sms.Addresses);
                            logs.Info("ShortCode: " + sms.ShortCode);
                            logs.Info("Message: " + sms.Message);
                            logs.Info("ChargeCode: " + sms.ChargeCode);
                            logs.Info("SubUnsubMoMessage: " + sms.SubUnsubMoMessage);
                            logs.Info("SubUnsubType: " + sms.SubUnsubType);
                            logs.Info("++++++++++++++++++++++++++++++++++");
                        }
                        foreach (var message in messages)
                        {
                            if (message.RetryCount > retryCountMax)
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            message.DateLastTried = DateTime.Now;
                            message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                        entity.SaveChanges();
                        return;
                    }
                    for (int index = 0; index < messagesCount; index++)
                    {
                        messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                        messages[index].ReferenceId = pardisResponse.Rows[index]["Correlator"].ToString();
                        messages[index].SentDate = DateTime.Now;
                        messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                        if (messages[index].MessagePoint > 0)
                            SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                        entity.Entry(messages[index]).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMesssagesToPardisImi: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }
        public static async Task SendMesssagesToHamrahvas(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            List<string> mobileNumbers = new List<string>();
            List<string> contents = new List<string>();
            List<string> shortCodes = new List<string>();
            List<int> serviceIds = new List<int>();
            List<int> ImiMessageType = new List<int>();
            List<int> ImiChargeCode = new List<int>();
            List<string> messageIds = new List<string>();
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                foreach (var message in messages)
                {
                    mobileNumbers.Add(message.MobileNumber);
                    contents.Add(message.Content);
                    shortCodes.Add(serviceAdditionalInfo["shortCode"]);
                    ImiMessageType.Add(message.ImiMessageType);
                    ImiChargeCode.Add(message.ImiChargeCode);
                    serviceIds.Add(Convert.ToInt32(serviceAdditionalInfo["aggregatorServiceId"]));
                    //messageIds.Add(message.Id.ToString());
                }
                try
                {
                    //var hamrahClient = new HamrahServiceReference.SmsBufferSoapClient("SmsBufferSoap");
                    //var hamrahResult = hamrahClient.MessageListUploadWithServiceId(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], mobileNumbers.ToArray(), contents.ToArray(), shortCodes.ToArray(), serviceIds.ToArray(), ImiMessageType.ToArray(), ImiChargeCode.ToArray()/*, messageIds.ToArray()*/);
                    //if (mobileNumbers.Count > 1 && hamrahResult.Length == 1)
                    //{
                    //    logs.Info(" Hamrah Webservice returned an error: " + hamrahResult[0]);
                    //    return;
                    //}
                    //else
                    //{
                    //    int cntr = 0;
                    //    foreach (var item in messages)
                    //    {
                    //        if (hamrahResult[cntr].ToLower().Contains("success"))
                    //        {
                    //            if (item.MessagePoint != 0)
                    //            {
                    //                var subscriber = entity.SubscribersPoints.Where(o => o.MobileNumber == item.MobileNumber).FirstOrDefault();
                    //                if (subscriber != null)
                    //                {
                    //                    subscriber.Point += item.MessagePoint.GetValueOrDefault();
                    //                    entity.Entry(subscriber).State = EntityState.Modified;
                    //                }
                    //            }
                    //            item.ProcessStatus = (int)MessageHandler.ProcessStatus.Success;
                    //        }
                    //        else
                    //        {
                    //            if (!String.IsNullOrEmpty(hamrahResult[cntr]))
                    //                logs.Info("Error!!: " + hamrahResult[cntr]);
                    //            else
                    //                logs.Info("Error");
                    //                item.ProcessStatus = (int)MessageHandler.ProcessStatus.Failed;
                    //        }
                    //        item.ReferenceId = hamrahResult[cntr].Length > 50 ? hamrahResult[cntr].Substring(0, 49) : hamrahResult[cntr];
                    //        item.SentDate = DateTime.Now;
                    //        entity.MessagesBuffers.Attach(item);
                    //        entity.Entry(item).State = EntityState.Modified;
                    //        cntr++;
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    logs.Error("Exception in Calling Hamrah Webservice", e);
                }
                foreach (var item in messages)
                {
                    item.ProcessStatus = 3;
                    entity.Entry(item).State = EntityState.Modified;
                }
                entity.SaveChanges();
                logs.Info(" Send function ended ");
            }
        }

        public static async Task SendMesssagesToMtn(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    var url = irancellIp + "/SendSmsService/services/SendSms";
                    var username = serviceAdditionalInfo["username"];
                    var serviceId = serviceAdditionalInfo["serviceId"];
                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            if (message.RetryCount != null && message.RetryCount > retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }

                            var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
                            string payload = SharedLibrary.MessageHandler.CreateMtnSoapEnvelopeString(serviceAdditionalInfo["aggregatorServiceId"], timeStamp, message.MobileNumber, serviceAdditionalInfo["shortCode"], message.Content, serviceId);

                            var result = new Dictionary<string, string>();
                            result["status"] = "";
                            result["message"] = "";
                            try
                            {
                                result = await SendSingleMessageToMtn(client, url, payload);
                            }
                            catch (Exception e)
                            {
                                logs.Error("Exception in SendSingleMessageToMtn: " + e);
                            }


                            if (result["status"] == "OK")
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                message.ReferenceId = result["message"];
                                message.SentDate = DateTime.Now;
                                message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                if (message.MessagePoint > 0)
                                    SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
                                entity.Entry(message).State = EntityState.Modified;
                            }
                            else
                            {
                                logs.Info("SendMesssagesToMtn Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.DateLastTried = DateTime.Now;
                                message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                                entity.Entry(message).State = EntityState.Modified;
                            }
                        }
                        entity.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMessagesToMtn: " + e);
                    foreach (var message in messages)
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.DateLastTried = DateTime.Now;
                        message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        entity.Entry(message).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
        }

        public static async Task<Dictionary<string, string>> SendSingleMessageToMtn(HttpClient client, string url, string payload)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                using (var response = await client.SendAsync(request))
                {
                    result["status"] = response.StatusCode.ToString();
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(httpResult);
                        XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                        manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                        manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                        manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/sms/send/v2_2/local");
                        XmlNodeList successNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:sendSmsResponse", manager);
                        if (successNode.Count > 0)
                        {
                            foreach (XmlNode success in successNode)
                            {
                                XmlNode successResultNode = success.SelectSingleNode("ns1:result", manager);
                                result["message"] = successResultNode.InnerText.Trim();
                            }
                        }
                        else
                        {
                            manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                            XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                            foreach (XmlNode fault in faultNode)
                            {
                                XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                                XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                                result["message"] = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                            }
                        }
                    }
                    else
                        result["message"] = response.StatusCode.ToString();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendSingleMessageToMtn: " + e);
            }
            return result;
        }

        public static async Task<dynamic> ChargeMtnSubscriber(Type entityType, Type singlechargeType, MessageObject message, bool isRefund, bool isInAppPurchase, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                string charge = "";
                var spId = "980110006379";
                dynamic singlecharge = Activator.CreateInstance(singlechargeType);
                singlecharge.MobileNumber = message.MobileNumber;
                if (isRefund == true)
                    charge = "refundAmount";
                else
                    charge = "chargeAmount";
                var mobile = "98" + message.MobileNumber.TrimStart('0');
                var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
                int rialedPrice = message.Price.Value * 10;
                var referenceCode = Guid.NewGuid().ToString();
                var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
                string payload = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:loc=""http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local"">      <soapenv:Header>         <RequestSOAPHeader xmlns=""http://www.huawei.com.cn/schema/common/v2_1"">            <spId>{6}</spId>  <serviceId>{5}</serviceId>             <timeStamp>{0}</timeStamp>   <OA>{1}</OA> <FA>{1}</FA>        </RequestSOAPHeader>       </soapenv:Header>       <soapenv:Body>          <loc:{4}>             <loc:endUserIdentifier>{1}</loc:endUserIdentifier>             <loc:charge>                <description>charge</description>                <currency>IRR</currency>                <amount>{2}</amount>                </loc:charge>              <loc:referenceCode>{3}</loc:referenceCode>            </loc:{4}>          </soapenv:Body></soapenv:Envelope>"
    , timeStamp, mobile, rialedPrice, referenceCode, charge, serviceAdditionalInfo["aggregatorServiceId"], spId);
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);
                        var request = new HttpRequestMessage(HttpMethod.Post, url);
                        request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");
                        logs.Info("request: " + payload);
                        using (var response = await client.SendAsync(request))
                        {
                            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                string httpResult = response.Content.ReadAsStringAsync().Result;
                                logs.Info("response: " + httpResult);
                                XmlDocument xml = new XmlDocument();
                                xml.LoadXml(httpResult);
                                XmlNamespaceManager manager = new XmlNamespaceManager(xml.NameTable);
                                manager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                                manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                                manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/payment/amount_charging/v2_1/local");
                                XmlNode successNode = xml.SelectSingleNode("/soapenv:Envelope/soapenv:Body/ns1:chargeAmountResponse", manager);
                                if (successNode != null)
                                {
                                    singlecharge.IsSucceeded = true;
                                }
                                else
                                {
                                    singlecharge.IsSucceeded = false;
                                    manager.AddNamespace("ns1", "http://www.csapi.org/schema/parlayx/common/v2_1");
                                    XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault", manager);
                                    foreach (XmlNode fault in faultNode)
                                    {
                                        XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                                        XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                                        singlecharge.Description = faultCodeNode.InnerText.Trim() + ": " + faultStringNode.InnerText.Trim();
                                    }
                                }
                            }
                            else
                            {
                                singlecharge.IsSucceeded = false;
                                singlecharge.Description = response.StatusCode.ToString();
                            }
                            singlecharge.ReferenceId = referenceCode;
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in ChargeMtnSubscriber: " + e);
                    if (e.Message.Contains("TaskCanceledException"))
                        logs.Info("timeout");
                    else
                        singlecharge.Description = "Exception in ChargeMtnSubscriber";
                }
                try
                {
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "IsSucceeded") != true)
                        singlecharge.IsSucceeded = false;
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                        singlecharge.ReferenceId = "Exception occurred!";
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    if (isRefund == true)
                        singlecharge.Price = message.Price.GetValueOrDefault() * -1;
                    else
                        singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    if (installmentId != 0)
                        singlecharge.InstallmentId = installmentId;

                    singlecharge.IsCalledFromInAppPurchase = isInAppPurchase;

                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in ChargeMtnSubscriber on saving values to db: " + e);
                }
                return singlecharge;
            }
        }

        public static async Task SendMesssagesToMobinOne(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo, bool isBulk = false)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;
                    Random rnd = new Random();
                    foreach (var message in messages)
                    {
                        if (message.RetryCount != null && message.RetryCount > retryCountMax)
                        {
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }

                    var smsList = new MobinOneServiceReference.ArrayReq();
                    smsList.number = new string[messagesCount];
                    smsList.message = new string[messagesCount];
                    smsList.chargecode = new string[messagesCount];
                    smsList.amount = new string[messagesCount];
                    smsList.requestId = new string[messagesCount];

                    if (messages[0].MessageType == 2)
                        isBulk = true;

                    if (isBulk)
                        smsList.type = "bulk";
                    else
                        smsList.type = "mt";

                    smsList.username = serviceAdditionalInfo["username"];
                    smsList.password = serviceAdditionalInfo["password"];
                    smsList.shortcode = "98" + serviceAdditionalInfo["shortCode"];
                    smsList.servicekey = serviceAdditionalInfo["aggregatorServiceId"];

                    for (int index = 0; index < messagesCount; index++)
                    {
                        smsList.number[index] = "98" + messages[index].MobileNumber.TrimStart('0');
                        smsList.message[index] = messages[index].Content;
                        if (messages[index].Price == 0)
                            smsList.chargecode[index] = "";
                        else
                            smsList.chargecode[index] = messages[index].ImiChargeKey;
                        if (messages[index].Price == 0)
                            smsList.amount[index] = "";
                        else
                            smsList.amount[index] = messages[index].Price.ToString();

                        var messageId = rnd.Next(1000000, 9999999).ToString();
                        smsList.requestId[index] = messageId.ToString();
                    }
                    var mobineOneClient = new MobinOneServiceReference.tpsPortTypeClient();
                    var result = mobineOneClient.sendSms(smsList);
                    logs.Info("response:" + result);
                    if (result.Length == 0)
                    {
                        logs.Info("SendMesssagesToMobinOne does not return response there must be somthing wrong with the parameters");
                        foreach (var message in messages)
                        {
                            if (message.RetryCount > retryCountMax)
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                            message.DateLastTried = DateTime.Now;
                            entity.Entry(message).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        for (int index = 0; index < messagesCount; index++)
                        {
                            var res = result[index].Split('-');
                            if (res[0] == "Success")
                            {
                                messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                if (messages[index].MessagePoint > 0)
                                    SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                                messages[index].SentDate = DateTime.Now;
                                messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            }
                            else
                            {
                                if (messages[index].RetryCount > retryCountMax)
                                    messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                messages[index].RetryCount = messages[index].RetryCount == null ? 1 : messages[index].RetryCount + 1;
                                messages[index].DateLastTried = DateTime.Now;
                            }
                            entity.Entry(messages[index]).State = EntityState.Modified;
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendMesssagesToMobinOne: " + e);
                    foreach (var message in messages)
                    {
                        //if (message.RetryCount > retryCountMax)
                        //    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        //message.DateLastTried = DateTime.Now;
                        //message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                        if (message.MessagePoint > 0)
                            SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
                        message.SentDate = DateTime.Now;
                        message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                        entity.Entry(message).State = EntityState.Modified;
                    }
                }
                entity.SaveChanges();
            }
        }

        public static async Task<dynamic> MobinOneOTPRequest(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var shortCode = "98" + serviceAdditionalInfo["shortCode"];
                var mobile = "98" + message.MobileNumber.TrimStart('0');
                var stringedPrice = (message.Price * 10).ToString();
                if (message.Price == 0)
                    stringedPrice = "";
                var rnd = new Random();
                var requestId = rnd.Next(1000000, 9999999).ToString();

                var client = new MobinOneServiceReference.tpsPortTypeClient();
                var result = client.inAppCharge(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], shortCode, serviceAdditionalInfo["aggregatorServiceId"], message.ImiChargeKey, mobile, stringedPrice, requestId);
                var splitedResult = result.Split('-');

                if (splitedResult[0] == "Success")
                    singlecharge.Description = "SUCCESS-Pending Confirmation";
                else
                    singlecharge.Description = splitedResult[0] + "-" + splitedResult[1];

                singlecharge.ReferenceId = splitedResult[2] + "_" + splitedResult[3];
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneOTPRequest: " + e);
                singlecharge.Description = "Exception";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneOTPRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> MobinOneOTPConfirm(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                string otpIds = singlecharge.ReferenceId;
                var optIdsSplitted = otpIds.Split('_');
                var transactionId = optIdsSplitted[0];
                var txCode = optIdsSplitted[1];
                var client = new MobinOneServiceReference.tpsPortTypeClient();
                var result = client.inAppChargeConfirm(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], transactionId, txCode, confirmationCode);
                var splitedResult = result.Split('-');

                if (splitedResult[1] == "ACCEPTED")
                {
                    singlecharge.IsSucceeded = true;
                    singlecharge.Description = "SUCCESS";
                }
                else
                    singlecharge.Description = result;
                entity.Entry(singlecharge).State = EntityState.Modified;
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }
            return singlecharge;
        }

        public static async Task<dynamic> MobinOneCharge(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var shortCode = "98" + serviceAdditionalInfo["shortCode"];
                var mobile = "98" + message.MobileNumber.TrimStart('0');
                var stringedPrice = ""; // (message.Price * 10).ToString();
                if (message.Price == 0)
                    stringedPrice = "";
                var rnd = new Random();
                var requestId = rnd.Next(1000000, 9999999).ToString();

                var client = new MobinOneServiceReference.tpsPortTypeClient();
                var result = client.charge(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], shortCode, serviceAdditionalInfo["aggregatorServiceId"], message.ImiChargeKey, mobile, stringedPrice, requestId);
                var splitedResult = result.Split('-');

                if (splitedResult[0] == "Success")
                    singlecharge.IsSucceeded = true;
                else
                    singlecharge.IsSucceeded = false;

                singlecharge.Description = splitedResult[1] + "-" + splitedResult[2];
                singlecharge.ReferenceId = splitedResult[3];
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneCharge: " + e);
                singlecharge.Description = "Exception";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneCharge on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> SamssonTciOTPRequest(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                singlecharge.MobileNumber = message.MobileNumber;
                try
                {
                    using (var client = new HttpClient())
                    {
                        var values = new Dictionary<string, string>
                        {
                        };

                        var content = new FormUrlEncodedContent(values);
                        var url = string.Format("https://www.tci.ir/api/v1/GuestMode/AddPhone/{0}", message.MobileNumber);
                        var response = await client.PostAsync(url, content);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseString = await response.Content.ReadAsStringAsync();
                            logs.Info(responseString);
                            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                            if (jsonResponse.error == null)
                            {
                                singlecharge.Description = "SUCCESS-Pending Confirmation";
                                singlecharge.ReferenceId = jsonResponse.value.ToString();
                            }
                            else
                            {
                                string e = jsonResponse.error.ToString();
                                e = e.Replace("[\r\n", string.Empty).Replace("\r\n]", string.Empty);
                                dynamic error = Newtonsoft.Json.JsonConvert.DeserializeObject(e);
                                singlecharge.Description = error.code.ToString() + "-" + error.message.ToString();
                            }
                        }
                        else
                            singlecharge.Description = response.StatusCode;
                    }
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SamssonTciOTPRequest: " + e);
                    singlecharge.Description = "Exception";
                }
                try
                {
                    singlecharge.IsSucceeded = false;
                    if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                        singlecharge.ReferenceId = "Exception occurred!";
                    singlecharge.DateCreated = DateTime.Now;
                    singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlecharge.Price = message.Price.GetValueOrDefault();
                    singlecharge.IsApplicationInformed = false;
                    singlecharge.IsCalledFromInAppPurchase = true;

                    entity.Singlecharges.Add(singlecharge);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SamssonTciOTPRequest on saving values to db: " + e);
                }
            }
            return singlecharge;
        }

        public static async Task<dynamic> SamssonTciOTPConfirm(Type entityType, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    using (var client = new HttpClient())
                    {
                        var values = new Dictionary<string, string>
                        {
                        };

                        var content = new FormUrlEncodedContent(values);
                        var url = string.Format("https://www.tci.ir/api/v1/GuestMode/Verify/{0}/{1}", message.Token, message.ConfirmCode);
                        var response = await client.PostAsync(url, content);
                        var responseString = await response.Content.ReadAsStringAsync();
                        logs.Info(responseString);
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                        if (jsonResponse.error == null)
                        {
                            if (jsonResponse.value == true)
                            {
                                singlecharge.Description = "SUCCESS";
                                singlecharge.ReferenceId = "Register";
                                singlecharge.Price = 0;
                                singlecharge.IsSucceeded = true;
                                singlecharge.UserToken = message.Token;
                            }
                        }
                        else
                        {
                            string e = jsonResponse.error.ToString();
                            e = e.Replace("[\r\n", string.Empty).Replace("\r\n]", string.Empty);
                            dynamic error = Newtonsoft.Json.JsonConvert.DeserializeObject(e);
                            singlecharge.Description = error.code.ToString() + "-" + error.message.ToString();
                        }
                    }
                    entity.Entry(singlecharge).State = EntityState.Modified;
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SamssonTciOTPConfirm: " + e);
                    singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
                }
            }
            return singlecharge;
        }
    }
}
