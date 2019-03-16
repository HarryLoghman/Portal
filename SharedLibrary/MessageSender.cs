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
using System.ServiceModel;
using System.ServiceModel.Channels;
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
        //public static string telepromoIp = "http://10.20.9.135:8600"; // "http://10.20.9.157:8600" "http://10.20.9.159:8600"
        //public static string telepromoIpJSON = "http://10.20.9.187:8700";
        //public static string telepromoPardisIp = "http://10.20.9.188:9090";
        //public static string irancellIp = "http://92.42.55.180:8310";
        //public static string mciIp = "http://172.17.251.18:8090";

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> OTPRequestGeneral(string aggregatorName, SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            logs.Info("OTPRequestGeneral,Start," + message.ServiceCode + "," + message.MobileNumber + "," + aggregatorName);
            //Task<dynamic> result;
            string aggregatorNameLowerCase = aggregatorName.ToLower();
            if (aggregatorNameLowerCase == "mobinonemapfa")
            {
                return await MapfaOTPRequest(entity, singlecharge, message, serviceAdditionalInfo);

            }
            else if (aggregatorNameLowerCase == "mcidirect")
            {
                return await MciDirectOtpCharge(entity, singlecharge, message, serviceAdditionalInfo);
            }
            else if (aggregatorNameLowerCase == "telepromo")
            {
                //if (serviceAdditionalInfo["serviceCode"] == "JabehAbzar" || serviceAdditionalInfo["serviceCode"] == "ShenoYad"
                //   || serviceAdditionalInfo["serviceCode"] == "ShenoYad500" || serviceAdditionalInfo["serviceCode"] == "Halghe")
                //{
                return await TelepromoOTPRequestJSON(entity, singlecharge, message, serviceAdditionalInfo);
                //}
                //else
                //{
                //    return await TelepromoOTPRequest(entity, singlecharge, message, serviceAdditionalInfo);
                //}
            }
            else if (aggregatorNameLowerCase == "telepromomapfa")
            {
                return await TelepromoMapfaOtpCharge(entity, singlecharge, message, serviceAdditionalInfo);
            }
            else if (aggregatorNameLowerCase == "pardisimi")
            {
                return await PardisImiOtpChargeRequest(entity, singlecharge, message, serviceAdditionalInfo);
            }
            else if (aggregatorNameLowerCase == "mobinone")
            {
                return await MobinOneOTPRequest(entity, singlecharge, message, serviceAdditionalInfo);

            }
            else if (aggregatorNameLowerCase == "samssontci")
            {
                return await SamssonTciOTPRequest(entity, singlecharge, message, serviceAdditionalInfo);
            }
            else return null;
            //else return null;
            logs.Info("OTPRequestGeneral,End," + message.ServiceCode + "," + message.MobileNumber);
            //return result;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> OTPConfirmGeneral(string aggregatorName
            , SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmCode)
        {
            logs.Info("OTPConfirmGeneral,Start," + message.ServiceCode + "," + message.MobileNumber);
            Task<dynamic> result = null;
            string aggregatorNameLowerCase = aggregatorName.ToLower();
            if (aggregatorNameLowerCase == "mobinonemapfa")
            {
                return await MapfaOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else if (aggregatorNameLowerCase == "mcidirect")
            {
                return await MciDirectOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else if (aggregatorNameLowerCase == "telepromo")
            {
                //if (serviceAdditionalInfo["serviceCode"] == "JabehAbzar" || serviceAdditionalInfo["serviceCode"] == "ShenoYad"
                //   || serviceAdditionalInfo["serviceCode"] == "ShenoYad500")
                //{
                return await TelepromoOTPConfirmJson(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
                //}
                //else
                //{
                //    return await TelepromoOTPConfirmJson(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
                //}
            }
            else if (aggregatorNameLowerCase == "telepromomapfa")
            {
                return await TelepromoMapfaOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else if (aggregatorNameLowerCase == "pardisimi")
            {
                return await PardisImiOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else if (aggregatorNameLowerCase == "mobinone")
            {
                return await MobinOneOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else if (aggregatorNameLowerCase == "samssontci")
            {
                return await SamssonTciOTPConfirm(entity, singlecharge, message, serviceAdditionalInfo, confirmCode);
            }
            else return null;
            logs.Info("OTPConfirmGeneral,End," + message.ServiceCode + "," + message.MobileNumber);
            //return result;
        }


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

                    //var url = telepromoIp + "/samsson-sdp/transfer/send?";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromo, HelpfulFunctions.enumServersActions.sendmessage);
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

        public static async Task SendMesssagesToTelepromoJSON(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                var result = new Dictionary<string, string>();
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;

                    //var url = telepromoIpJSON + "/samsson-gateway/sendmessage/";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromoJson, HelpfulFunctions.enumServersActions.sendmessage);
                    var username = "dehnad";
                    var password = "D4@Hn!";
                    var operatorServiceId = serviceAdditionalInfo["OperatorServiceId"];
                    var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                    //var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                    //var description = "deliverychannel:WAP|discoverychannel:WAP|origin:"+shortcode+ "|contentid:"+message.MessageType
                    var chargeCode = "";
                    var amount = "0";
                    var currency = "RLS";
                    var isFree = "1";
                    //var correlator = shortcode + Guid.NewGuid().ToString().Replace("-", "");
                    var correlator = "";
                    var serviceName = "";
                    if (serviceAdditionalInfo["shortCode"] == "3072428")
                    {
                        serviceName = "CTELHALGHE";
                        chargeCode = "TELREWCTELHALG5000";
                    }
                    else if (serviceAdditionalInfo["shortCode"] == "307251")
                    {
                        serviceName = "CTELSHENOYAD5000";
                        chargeCode = "TELREWCTELSHEN5000";
                    }
                    else if (serviceAdditionalInfo["shortCode"] == "307251")
                    {
                        serviceName = "CTELSHENOYAD";
                        chargeCode = "TELRENCTELSHENOYAD";
                    }
                    else if (serviceAdditionalInfo["shortCode"] == "307236")
                    {
                        serviceName = "CTELJABEHABZAR";
                        chargeCode = "FREE";
                    }

                    string json = "";
                    Random rnd = new Random();
                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            correlator = fnc_getCorrelator(shortcode, message, true);
                            result["status_code"] = "";
                            result["status_txt"] = "";
                            result["result"] = "";
                            result["success"] = "";

                            if (message.RetryCount != null && message.RetryCount >= retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }

                            //var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                            var description = "deliverychannel:WAP|discoverychannel:WAP|origin:" + shortcode + "|contentid:" + message.MessageType;
                            //var messageContent = message.Content;

                            //Dictionary<string, string> dic = new Dictionary<string, string>()
                            //{
                            //    { "username" , username }
                            //    ,{ "password" , password }
                            //    ,{"serviceid" , serviceId.ToString() }
                            //    ,{"shortcode" , shortcode }
                            //    ,{ "msisdn" , mobileNumber }
                            //    ,{"description" , description }
                            //    ,{"chargecode" ,chargeCode }
                            //    ,{"amount" , amount }
                            //    ,{"currency" , currency }
                            //    ,{"message",  messageContent }
                            //    ,{"is_free",isFree }
                            //    ,{"correlator" , correlator }
                            //    ,{ "servicename" , serviceName }
                            //};
                            ////json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"serviceid\":\"" + serviceId.ToString() + "\""
                            ////    + ",\"shortcode\":\"" + shortcode + "\",\"msisdn\":\"" + mobileNumber + "\",\"description\":\"" + description + "\""
                            ////    + ",\"chargecode\":\"" + chargeCode + "\",\"amount\":\"" + amount + "\",\"currency\":\"" + currency + "\""
                            ////    + ",\"message\":\"" + messageContent + "\",\"is_free\":\"" + isFree + "\",\"correlator\":\"" + correlator + "\""
                            ////    + ",\"servicename\":\"" + serviceName + "\"" + "}";
                            //json = JsonConvert.SerializeObject(dic);

                            json = MessageHandler.CreateTelepromoJsonString(username, password
                                , operatorServiceId, shortcode, message, description, chargeCode, amount, currency, isFree
                                , serviceName);

                            var request = new HttpRequestMessage(HttpMethod.Post, url);
                            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                            request.Headers.Add("cache-control", "no-cache");

                            using (var response = await client.SendAsync(request))
                            {

                                if (response.IsSuccessStatusCode)
                                {
                                    string httpResult = response.Content.ReadAsStringAsync().Result;
                                    dynamic results = JsonConvert.DeserializeObject<dynamic>(httpResult);
                                    result["status_code"] = results["status_code"];
                                    result["status_txt"] = results["status_txt"];
                                    result["result"] = results["data"]["result"];
                                    result["success"] = results["data"]["success"];

                                }
                                else
                                {
                                }
                            }

                            if (result["status_code"] == "0" && result["status_txt"].ToLower().Contains("ok"))
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                message.ReferenceId = result["result"];
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
                    logs.Error("Exception in SendMesssagesToTelepromoJSON: " + e);
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

        public static async Task SendBulkMesssagesToTelepromo(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;

                    //var url = telepromoIp + "/samsson-sdp/jtransfer/qsend?";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromo, HelpfulFunctions.enumServersActions.bulk);
                    var sc = "Dehnad";
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var from = "98" + serviceAdditionalInfo["shortCode"];
                    var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
                    Random rnd = new Random();
                    var contentList = new List<TelepromoBulkMessage>();
                    bool isExceptionOccured = false;
                    var responseString = "";
                    try
                    {
                        List<string> refrenceIds = new List<string>();
                        foreach (var item in messages)
                        {
                            var message = new TelepromoBulkMessage();
                            message.to = "98" + item.MobileNumber.TrimStart('0');
                            message.message = item.Content;
                            message.messageId = rnd.Next(1000000, 9999999).ToString();
                            refrenceIds.Add(message.messageId);
                            contentList.Add(message);
                        }
                        var contents = JsonConvert.SerializeObject(contentList);
                        var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}", sc, username, password, from, serviceId);
                        if (messages[0].Price > 0)
                            urlWithParameters += String.Format("&chargingCode={0}", messages[0].ImiChargeKey);

                        using (var client = new HttpClient())
                        {
                            var content = new StringContent(contents, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(urlWithParameters, content);
                            responseString = await response.Content.ReadAsStringAsync();
                        }
                        dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                        int index = 0;
                        foreach (var item in jsonResponse)
                        {
                            if (item.ToString() == "0")
                            {
                                messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                messages[index].ReferenceId = refrenceIds[index];
                                messages[index].SentDate = DateTime.Now;
                                messages[index].PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                if (messages[index].MessagePoint > 0)
                                    SharedLibrary.MessageHandler.SetSubscriberPoint(messages[index].MobileNumber, messages[index].ServiceId, messages[index].MessagePoint);
                                entity.Entry(messages[index]).State = EntityState.Modified;
                            }
                            else
                            {
                                logs.Info("SendBulkMesssagesToTelepromo Message was not sended with status of: " + item.ToString());
                                if (messages[index].RetryCount > retryCountMax)
                                    messages[index].ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                messages[index].DateLastTried = DateTime.Now;
                                messages[index].RetryCount = messages[index].RetryCount == null ? 1 : messages[index].RetryCount + 1;
                                entity.Entry(messages[index]).State = EntityState.Modified;
                            }
                            index++;
                        }
                    }
                    catch (Exception e)
                    {
                        isExceptionOccured = true;
                        logs.Error("Exception in SendBulkMesssagesToTelepromo: " + e);
                    }

                    if (isExceptionOccured == true || responseString == "")
                    {
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
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SendBulkMessagesToTelepromo: " + e);
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
                    //var url = telepromoIp + "/samsson-sdp/transfer/charge?";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromo, HelpfulFunctions.enumServersActions.charge);
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
                //var url = telepromoIp + "/samsson-sdp/pin/generate?";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromo, HelpfulFunctions.enumServersActions.otpRequest);
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> TelepromoOTPRequestJSON(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            //logs.Info("TelepromoOTPRequestJSON");
            entity.Configuration.AutoDetectChangesEnabled = false;
            string description = "OTPRequest";
            var result = new Dictionary<string, string>();
            result["status_code"] = "";
            result["status_txt"] = "";

            result["statusCode"] = "";
            result["serverReferenceCode"] = "";
            result["OTPTransactionId"] = "";
            result["referenceCode"] = "";

            try
            {
                //var url = telepromoIpJSON + "/samsson-gateway/otp-generation/";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromoJson, HelpfulFunctions.enumServersActions.otpRequest);
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var username = "dehnad";
                var password = "D4@Hn!";

                //logs.Info("TelepromoOTPRequestJSON1");
                var serviceId = serviceAdditionalInfo["OperatorServiceId"];
                //logs.Info("TelepromoOTPRequestJSON2");
                var referenceCode = Guid.NewGuid().ToString();
                var shortCode = "98" + serviceAdditionalInfo["shortCode"];
                var chargeCode = "";
                var serviceName = "";
                if (serviceAdditionalInfo["shortCode"] == "3072428")
                {
                    serviceName = "CTELHALGHE";
                    chargeCode = "TELSUBCTELHALGHE";
                }
                else if (serviceAdditionalInfo["shortCode"] == "307251")
                {
                    serviceName = "CTELSHENOYAD5000";
                    chargeCode = "TELSUBCTELSHENOYA5";
                }
                else if (serviceAdditionalInfo["shortCode"] == "307251")
                {
                    serviceName = "CTELSHENOYAD";
                    chargeCode = "TELSUBCTELSHENOYA5";
                }
                else if (serviceAdditionalInfo["shortCode"] == "307236")
                {
                    serviceName = "CTELJABEHABZAR";
                    chargeCode = "TELSUBCTELJABEHABZ";
                }

                Dictionary<string, string> dic = new Dictionary<string, string>()
                            {
                                { "msisdn" , mobileNumber }
                                ,{ "username" , username }
                                ,{ "password" , password }
                                ,{ "servicename" , serviceName }
                                ,{"serviceid" , serviceId.ToString() }
                                ,{"referencecode",referenceCode }
                                ,{"shortcode" , shortCode }
                                ,{"contentid","48" }
                                , {"chargecode" ,chargeCode }
                                , {"description" ,description }
                                ,{ "amount" , message.Price.ToString() }

                };
                //json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\",\"serviceid\":\"" + serviceId.ToString() + "\""
                //    + ",\"shortcode\":\"" + shortcode + "\",\"msisdn\":\"" + mobileNumber + "\",\"description\":\"" + description + "\""
                //    + ",\"chargecode\":\"" + chargeCode + "\",\"amount\":\"" + amount + "\",\"currency\":\"" + currency + "\""
                //    + ",\"message\":\"" + messageContent + "\",\"is_free\":\"" + isFree + "\",\"correlator\":\"" + correlator + "\""
                //    + ",\"servicename\":\"" + serviceName + "\"" + "}";
                string json = JsonConvert.SerializeObject(dic);
                //string json = "{\"msisdn\":\"" + mobileNumber + "\",\"username\":\"" + username + "\",\"password\":\"" + password + "\""
                //    + ",\"servicename\":\"CTELHALGHE\",\"serviceid\":\"" + serviceId + "\",\"referencecode\":\"" + referenceCode + "\""
                //    + ",\"shortcode\":\"" + shortCode + "\",\"contentid\":\"48\",\"chargecode\":\"TELSUBCTELHALGHE\",\"description\":\"description\",\"amount\":\"0\""
                //    + "}";
                logs.Info("TelepromoOTPRequestJSON" + json);
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Add("cache-control", "no-cache");
                //request.Headers.Add("content-type", " multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW");

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var response = await client.SendAsync(request))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string httpResult = response.Content.ReadAsStringAsync().Result;
                                dynamic results = JsonConvert.DeserializeObject<dynamic>(httpResult);
                                result["status_code"] = results["status_code"];
                                result["status_txt"] = results["status_txt"];
                                //logs.Info("TelepromoOTPRequestJSONSave");
                                result["statusCode"] = results["data"]["statusInfo"]["statusCode"];
                                //result["serverReferenceCode"] = results["data"]["statusInfo"]["serverReferenceCode"];//equals to OTPTransactionId
                                result["OTPTransactionId"] = results["data"]["statusInfo"]["OTPTransactionId"];
                                result["referenceCode"] = results["data"]["statusInfo"]["referenceCode"];

                            }
                            else
                            {
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logs.Error("TelepromoOTPRequestJSON", e);
                    result["status_code"] = "900";
                    result["status_txt"] = "Exception in calling aggregator webservice";
                }

                if (result["status_code"] == "0" && result["status_txt"].ToLower().Contains("ok"))
                    description = "SUCCESS-Pending Confirmation";
                else
                    description = result["status_txt"];

                //singlecharge.ReferenceId = result["serverReferenceCode"] + "_" + result["OTPTransactionId"]+ "_"+ result["referenceCode"];

            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoOTPRequest: " + e);
                singlecharge.Description = "Exception";

            }
            try
            {
                singlecharge.MobileNumber = message.MobileNumber;

                singlecharge.ReferenceId = (result["referenceCode"] + result["OTPTransactionId"] != "" ? result["referenceCode"] + "_" + result["OTPTransactionId"] : "");
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsSucceeded = false;
                singlecharge.Description = description;
                singlecharge.IsApplicationInformed = false;
                singlecharge.InstallmentId = null;
                //singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoOTPRequestJSON on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<dynamic> TelepromoOTPConfirm(dynamic entity, dynamic singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                //var url = telepromoIp + "/samsson-sdp/pin/confirm?";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromo, HelpfulFunctions.enumServersActions.otpConfirm);
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> TelepromoOTPConfirmJson(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            logs.Info("TelepromoOTPConfirmJSON");
            var result = new Dictionary<string, string>();
            result["status_code"] = "";
            result["status_txt"] = "";

            result["statusCode"] = "";
            result["serverReferenceCode"] = "";
            result["OTPTransactionId"] = "";
            result["referenceCode"] = "";

            try
            {
                //var url = telepromoIpJSON + "/samsson-gateway/otp-confirmation/";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.telepromoJson, HelpfulFunctions.enumServersActions.otpConfirm);
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var username = "dehnad";
                var password = "D4@Hn!";
                //logs.Info("TelepromoOTPConfirmJson1");
                var serviceId = serviceAdditionalInfo["OperatorServiceId"];
                //logs.Info("TelepromoOTPConfirmJson2");
                string referenceId = singlecharge.ReferenceId;
                var referenceIdSplitted = referenceId.Split('_');
                var referencecode = referenceIdSplitted[0];

                var shortCode = "98" + serviceAdditionalInfo["shortCode"];
                var contentid = "48";
                //var confirmationCode=confimrationCode
                var OTPTransactionId = referenceIdSplitted[1];
                Dictionary<string, string> dic = new Dictionary<string, string>()
                            {
                                { "msisdn" , mobileNumber }
                                ,{ "username" , username }
                                ,{ "password" , password }
                                ,{"serviceid" , serviceId.ToString() }
                                ,{"referencecode",referencecode }
                                ,{"shortcode" , shortCode }
                                ,{"contentid",contentid }
                                ,{"message",confirmationCode}
                                ,{ "otptransaction" , OTPTransactionId }

                };
                string json = JsonConvert.SerializeObject(dic);
                //string json = "{\"msisdn\":\"" + mobileNumber + "\",\"username\":\"" + username + "\",\"password\":\"" + password + "\""
                // + ",\"serviceid\":\"" + serviceId + "\",\"referencecode\":\"" + referencecode + "\""
                // + ",\"shortcode\":\"" + shortCode + "\",\"contentid\":\"" + contentid + "\",\"message\":\"" + confirmationCode + "\",\"otptransaction\":\"" + OTPTransactionId + "\""
                // + "}";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Add("cache-control", "no-cache");
                //request.Headers.Add("content-type", " multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW");
                logs.Info("TelepromoOTPConfirmJson3" + json);
                using (var client = new HttpClient())
                {

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                            dynamic results = JsonConvert.DeserializeObject<dynamic>(httpResult);
                            result["status_code"] = results["status_code"];
                            result["status_txt"] = results["status_txt"];

                            result["statuscode"] = results["data"]["statusInfo"]["statuscode"];
                            result["serverreferencecode"] = results["data"]["statusInfo"]["serverreferencecode"];//equals to OTPTransactionId
                            //result["OTPTransactionId"] = results["data"]["statusInfo"]["OTPTransactionId"];
                            result["referencecode"] = results["data"]["statusInfo"]["referencecode"];

                        }
                        else
                        {
                        }
                    }

                    singlecharge.Description = result["statuscode"] + "-code:" + confirmationCode;
                    if (result["status_code"] == "0" && result["status_txt"].Contains("OK"))
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
                logs.Error("Exception in TelepromoOTPConfirmJSon: " + e);
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
            //there is no document to implement correlator
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> PardisImiOtpChargeRequest(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
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
                //singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in PardisImiOtpChargeRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> PardisImiOTPConfirm(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
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
            //there is no document to implement correlator
            await Task.Delay(10);
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
                        using (var pardisClient = new SharedLibrary.PardisPlatformServiceReference.SendClient())
                        {
                            pardisResponse = pardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                        }
                    }
                    else
                    {
                        using (var mobinonePardisClient = new SharedLibrary.MobinOneMapfaSendServiceReference.SendClient())
                        {
                            pardisResponse = mobinonePardisClient.ServiceSend(username, password, domain, 0, messageContents, mobileNumbers, shortCodes, udhs, mclass, aggregatorServiceIds);
                        }
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MapfaOTPRequest(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            logs.Info("MapfaOTPRequest:start" + message.ServiceCode + "," + message.MobileNumber);
            entity.Configuration.AutoDetectChangesEnabled = false;
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                singlecharge.IsSucceeded = false;
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                //singlecharge.IsCalledFromInAppPurchase = true;
                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();

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
                using (var client = new MobinOneMapfaChargingServiceReference.ChargingClient())
                {
                    logs.Info("Mapfa OtpCharge: " + mobileNumber);
                    var result = client.sendVerificationCode(username, password, domain, channelType, mobileNumber, aggregatorServiceId);
                    logs.Info("Mapfa OtpCharge: " + mobileNumber);
                    if (result == 0)
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                    else
                        singlecharge.Description = result.ToString();

                    entity.Entry(singlecharge).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in MapfaOTPRequest: " + e);
                singlecharge.Description = "Exception";
            }
            logs.Info("MapfaOTPRequest:end" + message.ServiceCode + "," + message.MobileNumber);
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MapfaOTPConfirm(
            SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
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
                using (var client = new MobinOneMapfaChargingServiceReference.ChargingClient())
                {
                    logs.Error("Mapfa OtpConfirm: " + mobileNumber);
                    var result = client.verifySubscriber(username, password, domain, channelType, mobileNumber, aggregatorServiceId, confirmationCode);
                    logs.Error("Mapfa OtpConfirm: " + mobileNumber);
                    singlecharge.Description = result.ToString();
                    if (result == 0)
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
                logs.Error("Exception in MapfaOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }
            return singlecharge;
        }

        public static async Task SendMesssagesToPardisImi(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            //there is no document to implement correlator
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
                        logs.Info("SendMessagesToPardisImi does not return response there must be something wrong with the parameters");
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
            //there is no document to implement correlator
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

        public static async Task SendMesssagesToMtnOld(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            //using (dynamic entity = Activator.CreateInstance(entityType))
            //{
            //    entity.Configuration.AutoDetectChangesEnabled = false;
            //    try
            //    {
            //        var messagesCount = messages.Count;
            //        if (messagesCount == 0)
            //            return;
            //        //var url = irancellIp + "/SendSmsService/services/SendSms";
            //        var url = HelpfulFunctions.fnc_getServerURL(HelpfulFunctions.enumServers.MTN, HelpfulFunctions.enumServersActions.sendmessage);
            //        var username = serviceAdditionalInfo["username"];
            //        var serviceId = serviceAdditionalInfo["serviceId"];
            //        using (var client = new HttpClient())
            //        {
            //            foreach (var message in messages)
            //            {
            //                if (message.RetryCount != null && message.RetryCount > retryCountMax)
            //                {
            //                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
            //                    entity.Entry(message).State = EntityState.Modified;
            //                    continue;
            //                }

            //                //var timeStamp = SharedLibrary.Date.MTNTimestamp(DateTime.Now);
            //                //string payload = SharedLibrary.MessageHandler.CreateMtnSoapEnvelopeString(serviceAdditionalInfo["aggregatorServiceId"], timeStamp, message.MobileNumber, serviceAdditionalInfo["shortCode"], message.Content, serviceId);
            //                string payload = SharedLibrary.Aggregators.MTN.CreateBodyStringForMessage(
            //                    serviceAdditionalInfo["aggregatorServiceId"], serviceAdditionalInfo["shortCode"], message.MobileNumber
            //                    , message.Content, message.DateAddedToQueue);
            //                var result = new Dictionary<string, string>();
            //                result["status"] = "";
            //                result["message"] = "";
            //                try
            //                {
            //                    result = await SendSingleMessageToMtn(client, url, payload);
            //                }
            //                catch (Exception e)
            //                {
            //                    logs.Error("Exception in SendSingleMessageToMtn: " + e);
            //                }


            //                if (result["status"] == "OK")
            //                {
            //                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
            //                    message.ReferenceId = result["message"];
            //                    message.SentDate = DateTime.Now;
            //                    message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            //                    if (message.MessagePoint > 0)
            //                        SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
            //                    entity.Entry(message).State = EntityState.Modified;
            //                }
            //                else
            //                {
            //                    logs.Info("SendMesssagesToMtn Message was not sended with status of: " + result["status"] + " - description: " + result["message"]);
            //                    if (message.RetryCount > retryCountMax)
            //                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
            //                    message.DateLastTried = DateTime.Now;
            //                    message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
            //                    entity.Entry(message).State = EntityState.Modified;
            //                }
            //            }
            //            entity.SaveChanges();
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        logs.Error("Exception in SendMessagesToMtn: " + e);
            //        foreach (var message in messages)
            //        {
            //            if (message.RetryCount > retryCountMax)
            //                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
            //            message.DateLastTried = DateTime.Now;
            //            message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
            //            entity.Entry(message).State = EntityState.Modified;
            //        }
            //        entity.SaveChanges();
            //    }
            //}
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
                var timeStamp = SharedLibrary.Aggregators.AggregatorMTN.MTNTimestamp(DateTime.Now);
                int rialedPrice = message.Price.Value * 10;
                var referenceCode = Guid.NewGuid().ToString();
                //var url = "http://92.42.55.180:8310" + "/AmountChargingService/services/AmountCharging";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.MTN, HelpfulFunctions.enumServersActions.charge);
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
                        smsList.requestId[index] = fnc_getCorrelator(smsList.shortcode, messages[index], true);
                    }

                    using (var mobineOneClient = new MobinOneServiceReference.tpsPortTypeClient())
                    {
                        //logs.Info("SendMesssagesToMobinOne5:beforesendsms");
                        //logs.Info("smsList.type:" + smsList.type);
                        //logs.Info("smsList.username:" + smsList.username);
                        //logs.Info("smsList.pass:" + smsList.password);
                        //logs.Info("smsList.shortcode:" + smsList.shortcode);
                        //logs.Info("smsList.servicekey:" + smsList.servicekey);

                        //for (int i = 0; i <= smsList.number.Length - 1; i++)
                        //{
                        //    logs.Info("smsList.number[" + i.ToString() + "]:" + smsList.number[i]
                        //        + ",smsList.message[" + i.ToString() + "]:" + smsList.message[i]
                        //        + ",smsList.chargeCode[" + i.ToString() + "]:" + smsList.chargecode[i]
                        //        + ",smsList.price[" + i.ToString() + "]:" + smsList.amount[i]
                        //        + ",smsList.requestId[" + i.ToString() + "]:" + smsList.requestId[i]);
                        //}
                        var result = mobineOneClient.sendSms(smsList);
                        logs.Info("response:" + result);
                        if (result.Length == 0)
                        {
                            logs.Info("SendMesssagesToMobinOne does not return response there must be something wrong with the parameters");
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MobinOneOTPRequest(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
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

                using (var client = new MobinOneServiceReference.tpsPortTypeClient())
                {
                    logs.Error("MobinOneOTPRequest:inAppCharge(\"" + serviceAdditionalInfo["username"] + "\",\"" + serviceAdditionalInfo["password"] + "\",\"" + shortCode + "\",\"" + serviceAdditionalInfo["aggregatorServiceId"] + "\",\"" + message.ImiChargeKey + "\",\"" + mobile + "\",\"" + stringedPrice + "\",\"" + requestId + "\")");
                    var result = client.inAppCharge(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], shortCode, serviceAdditionalInfo["aggregatorServiceId"], message.ImiChargeKey, mobile, stringedPrice, requestId);
                    logs.Error("MobinOneOTPRequest:Result:" + result);
                    var splitedResult = result.Split('-');

                    if (splitedResult[0] == "Success")
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                    else
                        singlecharge.Description = splitedResult[0] + "-" + splitedResult[1];

                    singlecharge.ReferenceId = splitedResult[2] + "_" + splitedResult[3];
                }
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
                //singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneOTPRequest on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MobinOneOTPConfirm(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            logs.Error("mobineoneotpConfirm");
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                string otpIds = singlecharge.ReferenceId;
                var optIdsSplitted = otpIds.Split('_');
                var transactionId = optIdsSplitted[0];
                var txCode = optIdsSplitted[1];
                using (var client = new MobinOneServiceReference.tpsPortTypeClient())
                {
                    logs.Error("MobinOneOTPConfirm:inAppChargeConfirm(\"" + serviceAdditionalInfo["username"] + "\",\"" + serviceAdditionalInfo["password"] + "\",\"" + transactionId + "\",\"" + txCode + "\",\"" + confirmationCode + "\")");
                    var result = client.inAppChargeConfirm(serviceAdditionalInfo["username"], serviceAdditionalInfo["password"], transactionId, txCode, confirmationCode);
                    logs.Error("MobinOneOTPConfirm:Result:" + result);
                    var splitedResult = result.Split('-');

                    if (splitedResult[1] == "ACCEPTED")
                    {
                        singlecharge.IsSucceeded = true;
                        singlecharge.Description = "SUCCESS";
                    }
                    else
                        singlecharge.Description = result;

                    logs.Error("mobineoneotpConfirm," + result);
                    entity.Entry(singlecharge).State = EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in MobinOneOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> SamssonTciOTPRequest(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
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
                    //var url = string.Format("https://www.tci.ir/api/v1/GuestMode/AddPhone/{0}", message.MobileNumber);
                    var url = string.Format(HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.SamssonTci, HelpfulFunctions.enumServersActions.otpRequest), message.MobileNumber);
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
                        singlecharge.Description = response.StatusCode.ToString();
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
                //singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in SamssonTciOTPRequest on saving values to db: " + e);
            }

            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> SamssonTciOTPConfirm(
            SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
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
                    //var url = string.Format("https://www.tci.ir/api/v1/GuestMode/Verify/{0}/{1}", message.Token, message.ConfirmCode);
                    var url = string.Format(HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.SamssonTci, HelpfulFunctions.enumServersActions.otpConfirm), message.Token, message.ConfirmCode);
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
                            //singlecharge.UserToken = message.Token;
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

            return singlecharge;
        }

       public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MciDirectOtpCharge(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                //var url = mciIp + "/apigw/charging/pushotp";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.MCI, HelpfulFunctions.enumServersActions.otpRequest);
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var rnd = new Random();
                var refrenceCode = rnd.Next(100000000, 999999999).ToString();
                var json = string.Format(@"{{
                            ""accesInfo"": {{
                                ""servicekey"": ""{0}"",
                                ""msisdn"": ""{1}"",
                                ""serviceName"": ""{5}"",
                                ""referenceCode"": ""{2}"",
                                ""shortCode"": ""{3}"",
                                ""contentId"":""1""
                            }} ,
                        ""charge"": {{
                                ""code"": ""{4}"",
                                ""amount"":{6},
                                ""description"": ""otp""
                            }}
                    }}", aggregatorServiceId, mobileNumber, refrenceCode, shortcode, message.ImiChargeKey, serviceAdditionalInfo["serviceName"], message.Price * 10);

                using (var client = new HttpClient())
                {
                    logs.Info("MCI Direct OTP Charge : " + url + ":" + json);


                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(url, content);
                    var responseString = await result.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);

                    //logs.Error("MCIDirectOTPCharge " + mobileNumber + responseString);
                    if (jsonResponse.statusInfo.statusCode.ToString() == "200")
                    {
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                        singlecharge.ReferenceId = refrenceCode + "_" + jsonResponse.statusInfo.OTPTransactionId.ToString();
                    }
                    else
                    {
                        singlecharge.Description = jsonResponse.statusInfo.errorInfo.errorCode.ToString() + " : " + jsonResponse.statusInfo.errorInfo.errorDescription.ToString();
                        singlecharge.ReferenceId = refrenceCode + "_";
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in MciDirectOtpCharge: " + e);
                singlecharge.Description = "Exception";
            }
            try
            {
                singlecharge.IsSucceeded = false;
                //if (HelpfulFunctions.IsPropertyExist(singlecharge, "ReferenceId") != true)
                //    singlecharge.ReferenceId = "Exception occurred!";
                singlecharge.DateCreated = DateTime.Now;
                singlecharge.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                singlecharge.Price = message.Price.GetValueOrDefault();
                singlecharge.IsApplicationInformed = false;
                //singlecharge.IsCalledFromInAppPurchase = true;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in MciDirectOtpCharge on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> MciDirectOTPConfirm(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                //var url = mciIp + "/apigw/charging/chargeotp";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.MCI, HelpfulFunctions.enumServersActions.otpConfirm);
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                string otpIds = singlecharge.ReferenceId;
                var optIdsSplitted = otpIds.Split('_');
                var referenceCode = optIdsSplitted[0];
                var otpTransactionId = optIdsSplitted[1];
                var json = string.Format(@"{{
                       ""accesInfo"":{{  
                          ""servicekey"":""{0}"",
                          ""msisdn"":""{1}"",
                          ""otpTransactionId"":""{2}"",
                          ""transactionPIN"":""{3}"",
                          ""referenceCode"":""{4}"",
                       ""contentId"":""1"",
                       ""shortCode"": ""{5}""
                       }}
                    }}", aggregatorServiceId, mobileNumber, otpTransactionId, confirmationCode, referenceCode, shortcode);
                //logs.Error("MCIDirectOTPConfirm" + json);
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(url, content);
                    var responseString = await result.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.statusInfo.statusCode.ToString() == "200")
                    {
                        singlecharge.IsSucceeded = true;
                        singlecharge.Description = "SUCCESS";
                        entity.Entry(singlecharge).State = EntityState.Modified;
                        entity.SaveChanges();
                    }
                    else
                        singlecharge.Description = jsonResponse.statusInfo.errorInfo.errorCode.ToString() + " : " + jsonResponse.statusInfo.errorInfo.errorDescription.ToString();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in MciDirectOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }

            return singlecharge;
        }

        public static async Task SendMesssagesToTelepromoMapfa(Type entityType, dynamic messages, Dictionary<string, string> serviceAdditionalInfo)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;
                try
                {
                    var messagesCount = messages.Count;
                    if (messagesCount == 0)
                        return;

                    //var url = telepromoPardisIp + "/samsson-gateway/sendmessagepardis/";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.TelepromoMapfa, HelpfulFunctions.enumServersActions.sendmessage);
                    var username = serviceAdditionalInfo["username"];
                    var password = serviceAdditionalInfo["password"];
                    var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                    var serivceId = Convert.ToInt32(serviceAdditionalInfo["serviceId"]);
                    var paridsShortCodes = ServiceHandler.GetPardisShortcodesFromServiceId(serivceId);
                    var serviceName = serviceAdditionalInfo["serviceName"];
                    var currency = "RLS";
                    var chargeCode = "";



                    var description = "";
                    using (var client = new HttpClient())
                    {
                        foreach (var message in messages)
                        {
                            var correlator = fnc_getCorrelator(shortcode, message, true);// shortcode + "-" + serviceAdditionalInfo["serviceCode"];
                            if (message.RetryCount != null && message.RetryCount >= retryCountMax)
                            {
                                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                entity.Entry(message).State = EntityState.Modified;
                                continue;
                            }

                            var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                            try
                            {
                                var isFree = true;
                                var amount = "0";
                                if (message.Price > 0)
                                {
                                    amount = (message.Price * 10).ToString();
                                    isFree = false;
                                }

                                var aggregatorServiceId = paridsShortCodes.FirstOrDefault(o => o.Price == message.Price).PardisServiceId;
                                message.Content = message.Content.ToString().Replace("\r\n", "\\n").Replace("\n", "\\n");

                                var json = string.Format(@"{{""username"":""{0}"",""password"":""{1}"",""serviceid"":""{2}"",""shortcode"":""{3}"", ""msisdn"": ""{4}"" , ""servicename"": ""{5}"", ""currency"": ""{6}"", ""chargecode"": ""{7}"", ""correlator"": ""{8}"" , ""is_free"": ""{9}"", ""description"": ""{10}"" , ""amount"": ""{11}"", ""message"":""{12}""}}"
                                                            , username, password, aggregatorServiceId, shortcode, mobileNumber, serviceName, currency, chargeCode, correlator, isFree, description, amount, message.Content);

                                var content = new StringContent(json, Encoding.UTF8, "application/json");
                                var result = await client.PostAsync(url, content);
                                var responseString = await result.Content.ReadAsStringAsync();
                                dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                                if (jsonResponse.data.ToString().Length > 4)
                                {
                                    message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Success;
                                    message.ReferenceId = jsonResponse.data.ToString();
                                    message.SentDate = DateTime.Now;
                                    message.PersianSentDate = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                                    if (message.MessagePoint > 0)
                                        SharedLibrary.MessageHandler.SetSubscriberPoint(message.MobileNumber, message.ServiceId, message.MessagePoint);
                                    entity.Entry(message).State = EntityState.Modified;
                                }
                                else
                                {
                                    logs.Info("SendMesssagesToTelepromoMapfa Message was not sended with data of: " + jsonResponse.data + " - description: " + jsonResponse.status_txt);
                                    if (message.RetryCount > retryCountMax)
                                        message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.Failed;
                                    message.DateLastTried = DateTime.Now;
                                    message.RetryCount = message.RetryCount == null ? 1 : message.RetryCount + 1;
                                    entity.Entry(message).State = EntityState.Modified;
                                }
                            }
                            catch (Exception e)
                            {
                                logs.Error("Exception in SendSingleMessageToTelepromo: " + e);
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
                    logs.Error("Exception in SendMesssagesToTelepromoMapfa: " + e);
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

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> TelepromoMapfaOtpCharge(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge, MessageObject message, Dictionary<string, string> serviceAdditionalInfo)
        {
            singlecharge.MobileNumber = message.MobileNumber;
            try
            {
                //var url = telepromoPardisIp + "/samsson-gateway/otp-generationpardis/";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.TelepromoMapfa, HelpfulFunctions.enumServersActions.otpRequest);
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var description = "otp";
                var json = string.Format(@"{{
                                ""username"": ""{0}"",
                                ""password"": ""{1}"",
                                ""serviceid"": ""{2}"",
                                ""msisdn"": ""{3}"",
                                ""description"": ""{4}""
                    }}", username, password, aggregatorServiceId, mobileNumber, description);
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(url, content);
                    var responseString = await result.Content.ReadAsStringAsync();
                    logs.Info("response:" + responseString);
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.data.ToString() == "0")
                    {
                        singlecharge.Description = "SUCCESS-Pending Confirmation";
                    }
                    else
                    {
                        singlecharge.Description = jsonResponse.data.ToString() + " : " + jsonResponse.status_txt.ToString() + " : " + jsonResponse.status_code.ToString();
                    }
                    singlecharge.ReferenceId = "";
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoMapfaOtpCharge: " + e);
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
                //singlecharge.IsCalledFromInAppPurchase = message.InAppPurchase;

                entity.Singlecharges.Add(singlecharge);
                entity.SaveChanges();
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoMapfaOtpCharge on saving values to db: " + e);
            }
            return singlecharge;
        }

        public static async Task<SharedLibrary.Models.ServiceModel.Singlecharge> TelepromoMapfaOTPConfirm(SharedLibrary.Models.ServiceModel.SharedServiceEntities entity
            , SharedLibrary.Models.ServiceModel.Singlecharge singlecharge
            , MessageObject message, Dictionary<string, string> serviceAdditionalInfo, string confirmationCode)
        {
            entity.Configuration.AutoDetectChangesEnabled = false;
            try
            {
                //var url = telepromoPardisIp + "/samsson-gateway/otp-confirmationpardis/";
                var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.TelepromoMapfa, HelpfulFunctions.enumServersActions.otpConfirm);
                var username = serviceAdditionalInfo["username"];
                var password = serviceAdditionalInfo["password"];
                var shortcode = "98" + serviceAdditionalInfo["shortCode"];
                var aggregatorServiceId = serviceAdditionalInfo["aggregatorServiceId"];
                var serviceId = serviceAdditionalInfo["serviceId"];
                var mobileNumber = "98" + message.MobileNumber.TrimStart('0');
                var json = string.Format(@"{{
                                ""username"": ""{0}"",
                                ""password"": ""{1}"",
                                ""serviceid"": ""{2}"",
                                ""msisdn"": ""{3}"",
                                ""message"": ""{4}""
                    }}", username, password, aggregatorServiceId, mobileNumber, confirmationCode);
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(url, content);
                    var responseString = await result.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse.data.ToString() == "0")
                    {
                        singlecharge.IsSucceeded = true;
                        singlecharge.Description = "SUCCESS";
                        entity.Entry(singlecharge).State = EntityState.Modified;
                        entity.SaveChanges();
                    }
                    else
                        singlecharge.Description = jsonResponse.data.ToString();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelepromoMapfaOTPConfirm: " + e);
                singlecharge.Description = "Exception Occured for" + "-code:" + confirmationCode;
            }

            return singlecharge;
        }

        public static string fnc_getCorrelator(string shortCode, long ticks, bool addShortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
            {
                throw new ArgumentException("ShortCode is not specified");
            }
            if (addShortCode)
            {
                if (!shortCode.StartsWith("98"))
                    shortCode = "98" + shortCode;
            }
            return shortCode + "s" + ticks.ToString();
        }

        public static void sb_processCorrelator(string correlator, ref string mobileNumber, out string shortCode)
        {
            if (mobileNumber.StartsWith("tel:98"))
                mobileNumber = mobileNumber.Remove(0, 6);
            if (!mobileNumber.StartsWith("0"))
                mobileNumber = "0" + mobileNumber;
            shortCode = (!string.IsNullOrEmpty(correlator) && correlator.Contains("s") ? correlator.Split('s')[0] : null);
            if (shortCode != null && shortCode.StartsWith("98"))
                shortCode = shortCode.Remove(0, 2);
        }


    }
}
