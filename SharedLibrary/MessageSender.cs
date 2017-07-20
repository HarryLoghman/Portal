﻿using SharedLibrary.Models;
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
        public static async Task SendMesssagesToTelepromo(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                //var url = "http://10.20.9.159:8600" + "/samsson-sdp/transfer/send?";
                var url = "http://10.20.9.135:8600" + "/samsson-sdp/transfer/send?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];

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
                        else if (message.DateLastTried != null && message.DateLastTried > DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute))
                            continue;
                        var to = "98" + message.MobileNumber.TrimStart('0');
                        var messageContent = message.Content;
                        var messageId = Guid.NewGuid().ToString();
                        var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                                , sc, username, password, from, serviceId, to, messageContent, messageId);
                        if (message.ImiChargeKey != "FREE")
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
                            if (message.RetryCount == null)
                            {
                                message.RetryCount = 1;
                                message.DateLastTried = DateTime.Now;
                            }
                            else
                            {
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.RetryCount += 1;
                                message.DateLastTried = DateTime.Now;
                            }
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
                    if (message.RetryCount == null)
                    {
                        message.RetryCount = 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    else
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.RetryCount += 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    entity.Entry(message).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
        }

        public static async Task<dynamic> SendSinglechargeMesssageToTelepromo(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                //var url = "http://10.20.9.159:8600" + "/samsson-sdp/transfer/charge?";
                var url = "http://10.20.9.135:8600" + "/samsson-sdp/transfer/charge?";
                var sc = "Dehnad";
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var from = "98" + serviceAdditionalInfo["shortCode"];
                var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                using (var client = new HttpClient())
                {
                    var to = "98" + message.MobileNumber.TrimStart('0');
                    var messageContent = "InAppPurchase";
                    var messageId = Guid.NewGuid().ToString();
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

        private static async Task<Dictionary<string, string>> SendSingleMessageToTelepromo(HttpClient client, string url)
        {
            var result = new Dictionary<string, string>();
            result["status"] = "";
            result["message"] = "";
            try
            {
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
                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
            }
            return result;
        }

        public static async Task SendMesssagesToHub(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
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
                    if (message.RetryCount != null && message.RetryCount > retryCountMax)
                    {
                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        entity.Entry(message).State = EntityState.Modified;
                        continue;
                    }
                    else if (message.DateLastTried != null && message.DateLastTried > DateTime.Now.AddHours(retryPauseBeforeSendByMinute))
                        continue;

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
                            if (message.RetryCount == null)
                            {
                                message.RetryCount = 1;
                                message.DateLastTried = DateTime.Now;
                            }
                            else
                            {
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.RetryCount += 1;
                                message.DateLastTried = DateTime.Now;
                            }
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
                    if (message.RetryCount == null)
                    {
                        message.RetryCount = 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    else
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.RetryCount += 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    entity.Entry(message).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
        }

        public static async Task<dynamic> SendSinglechargeMesssageToHub(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, long installmentId = 0)
        {
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
                logs.Info(response);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response);
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

        public static async Task SendMesssagesToPardisPlatform(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
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
                    if (messages[index].DateLastTried != null && messages[index].DateLastTried > DateTime.Now.AddHours(retryPauseBeforeSendByMinute))
                        continue;
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

                var pardisClient = new SharedLibrary.PardisPlatformServiceReference.ServiceCallClient();
                var pardisResponse = pardisClient.ServiceSend(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], "pardis1", 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                logs.Info("pardis Response count: " + pardisResponse.Count());
                if (pardisResponse == null || pardisResponse.Count() < messagesCount)
                {
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
                    if (message.RetryCount == null)
                    {
                        message.RetryCount = 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    else
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.RetryCount += 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    entity.Entry(message).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
        }

        public static async Task SendMesssagesToPardisImi(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
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
                    if (messages[index].DateLastTried != null && messages[index].DateLastTried > DateTime.Now.AddHours(retryPauseBeforeSendByMinute))
                        continue;
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
                        if (message.RetryCount == null)
                        {
                            message.RetryCount = 1;
                            message.DateLastTried = DateTime.Now;
                        }
                        else
                        {
                            if (message.RetryCount > retryCountMax)
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                            message.RetryCount += 1;
                            message.DateLastTried = DateTime.Now;
                        }
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
                    if (message.RetryCount == null)
                    {
                        message.RetryCount = 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    else
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.RetryCount += 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    entity.Entry(message).State = EntityState.Modified;
                }
                entity.SaveChanges();
            }
        }
        public static async Task SendMesssagesToHamrahvas(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            List<string> mobileNumbers = new List<string>();
            List<string> contents = new List<string>();
            List<string> shortCodes = new List<string>();
            List<int> serviceIds = new List<int>();
            List<int> ImiMessageType = new List<int>();
            List<int> ImiChargeCode = new List<int>();
            List<string> messageIds = new List<string>();

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

        public static async Task SendMesssagesToMtn(dynamic entity, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            try
            {
                var messagesCount = messages.Count;
                if (messagesCount == 0)
                    return;
                var url = "http://92.42.55.180:8310/SendSmsService/services/SendSms";
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
                        else if (message.DateLastTried != null && message.DateLastTried > DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute))
                            continue;
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
                            if (message.RetryCount == null)
                            {
                                message.RetryCount = 1;
                                message.DateLastTried = DateTime.Now;
                            }
                            else
                            {
                                if (message.RetryCount > retryCountMax)
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                message.RetryCount += 1;
                                message.DateLastTried = DateTime.Now;
                            }
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
                    if (message.RetryCount == null)
                    {
                        message.RetryCount = 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    else
                    {
                        if (message.RetryCount > retryCountMax)
                            message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                        message.RetryCount += 1;
                        message.DateLastTried = DateTime.Now;
                    }
                    entity.Entry(message).State = EntityState.Modified;
                }
                entity.SaveChanges();
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
                        XmlNodeList successNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/ns1:sendSmsResponse");
                        if (successNode.Count > 0)
                        {
                            foreach (XmlNode success in successNode)
                            {
                                XmlNode successResultNode = success.SelectSingleNode("ns1:result");
                                result["message"] = successResultNode.InnerText;
                            }
                        }
                        else
                        {
                            XmlNodeList faultNode = xml.SelectNodes("/soapenv:Envelope/soapenv:Body/soapenv:Fault");
                            foreach (XmlNode fault in faultNode)
                            {
                                XmlNode faultCodeNode = fault.SelectSingleNode("faultcode");
                                XmlNode faultStringNode = fault.SelectSingleNode("faultstring");
                                result["message"] = faultCodeNode.InnerText + ": " + faultStringNode.InnerText;
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
    }
}