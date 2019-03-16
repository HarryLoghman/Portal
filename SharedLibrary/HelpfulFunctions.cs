using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharedLibrary
{
    public class HelpfulFunctions
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetAllTheNumbersFromComplexString(string input)
        {
            return new String(input.Where(Char.IsDigit).ToArray());
        }

        public static bool IsPropertyExist(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject is ExpandoObject)
                return ((IDictionary<string, object>)dynamicObject).ContainsKey(propertyName);

            return dynamicObject.GetType().GetProperty(propertyName) != null;
        }

        public static bool DownloadFileFromWeb(string uri, string filePath)
        {
            var fileName = uri.Remove(0, uri.LastIndexOf('/'));
            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFile(uri, filePath + fileName);
                return true;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response != null)
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return true;
                }
                logs.Error("WebException in DownloadFileFromWeb-" + uri + ":", e);
                return false;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DownloadFileFromWeb-" + uri + ":", e);
                return false;
            }
        }

        public static bool DeleteFile(string fileUri)
        {
            try
            {
                File.Delete(fileUri);
                return true;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DeleteFile-" + fileUri + ":", e);
                return false;
            }
        }

        public static bool DecompressFromBZ2File(string fileUri)
        {
            try
            {
                FileInfo zipFile = new FileInfo(fileUri);
                using (FileStream fileToDecompressAsStream = zipFile.OpenRead())
                {
                    string decompressedFileName = fileUri.Replace(".bz2", "");
                    using (FileStream decompressedStream = File.Create(decompressedFileName))
                    {
                        try
                        {
                            BZip2.Decompress(fileToDecompressAsStream, decompressedStream, true);
                            return true;
                        }
                        catch (Exception e)
                        {
                            logs.Error("Exception in DecompressFromBZ2File-" + fileUri + ":", e);
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in outerlayer of DecompressFromBZ2File-" + fileUri + ":", e);
                return false;
            }
        }

        public static List<ImiData> ReadImiDataFile(string fileUri)
        {
            var preparedLine = "";
            var imiDataList = new List<ImiData>();
            try
            {
                var file = File.ReadAllLines(fileUri);
                foreach (var line in file)
                {
                    preparedLine = "";
                    preparedLine = line.Replace("trans-id", "transId");
                    preparedLine = preparedLine.Replace("base-price-point", "basePricePoint");
                    preparedLine = preparedLine.Replace("next_renewal_date", "nextRenewalDate");
                    preparedLine = preparedLine.Replace("trans-status", "transStatus");
                    preparedLine = preparedLine.Replace("billed-price-point", "billedPricePoint");
                    preparedLine = preparedLine.Replace("event-type", "eventType");
                    var imiDataObj = JsonConvert.DeserializeObject<ImiData>(preparedLine);
                    imiDataObj.keyword = HttpUtility.UrlDecode(imiDataObj.keyword, System.Text.UnicodeEncoding.Default);
                    imiDataList.Add(imiDataObj);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ReadImiDataFile-" + fileUri + ":", e);
            }
            return imiDataList;
        }

        public static List<ImiData> ReadImiDataFromMemory(string serviceCode, List<string> data)
        {
            var preparedLine = "";
            var imiDataList = new List<ImiData>();
            try
            {
                foreach (var line in data)
                {
                    preparedLine = "";
                    preparedLine = line.Replace("trans-id", "transId");
                    preparedLine = preparedLine.Replace("base-price-point", "basePricePoint");
                    preparedLine = preparedLine.Replace("next_renewal_date", "nextRenewalDate");
                    preparedLine = preparedLine.Replace("trans-status", "transStatus");
                    preparedLine = preparedLine.Replace("billed-price-point", "billedPricePoint");
                    preparedLine = preparedLine.Replace("event-type", "eventType");
                    var imiDataObj = JsonConvert.DeserializeObject<ImiData>(preparedLine);
                    imiDataObj.keyword = HttpUtility.UrlDecode(imiDataObj.keyword, System.Text.UnicodeEncoding.Default);
                    imiDataList.Add(imiDataObj);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ReadImiDataMemory-" + serviceCode + ":", e);
            }
            return imiDataList;
        }

        public static Dictionary<string, int> GetIncomeAndSubscriptionsFromImiDataFile(List<ImiData> imiDatas, List<SharedLibrary.Models.OperatorsPrefix> operatorPrefixes)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            result["prepaidSubscriptions"] = 0;
            result["postpaidSubscriptions"] = 0;
            result["prepaidUnsubscriptions"] = 0;
            result["postpaidUnsubscriptions"] = 0;
            result["prepaidCharges"] = 0;
            result["postpaidCharges"] = 0;
            result["sumOfCharges"] = 0;
            try
            {
                foreach (var data in imiDatas)
                {
                    if (data.eventType == "1.5")
                    {
                        if (data.status != 0)
                            continue;
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message, operatorPrefixes);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidCharges"] += data.billedPricePoint.Value;
                        else
                            result["prepaidCharges"] += data.billedPricePoint.Value;
                        result["sumOfCharges"] += data.billedPricePoint.Value;
                    }
                    else if (data.eventType == "1.2")
                    {
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message, operatorPrefixes);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidUnsubscriptions"] += 1;
                        else
                            result["prepaidUnsubscriptions"] += 1;
                    }
                    else if (data.eventType == "1.1")
                    {
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message, operatorPrefixes);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidSubscriptions"] += 1;
                        else
                            result["prepaidSubscriptions"] += 1;
                    }
                }
                if (result["postpaidCharges"] > 0)
                    result["postpaidCharges"] /= 10;
                if (result["prepaidCharges"] > 0)
                    result["prepaidCharges"] /= 10;
                if (result["sumOfCharges"] > 0)
                    result["sumOfCharges"] /= 10;
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetIncomeAndSubscriptionsFromImiDataFile:", e);
                result["prepaidSubscriptions"] = 0;
                result["postpaidSubscriptions"] = 0;
                result["prepaidUnsubscriptions"] = 0;
                result["postpaidUnsubscriptions"] = 0;
                result["prepaidCharges"] = 0;
                result["postpaidCharges"] = 0;
                result["sumOfCharges"] = 0;
            }
            return result;
        }

        public static bool IsPropertyExistInDynamicObject(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject is ExpandoObject)
                return ((IDictionary<string, object>)dynamicObject).ContainsKey(propertyName);

            return dynamicObject.GetType().GetProperty(propertyName) != null;
        }

        public static string IrancellSignatureGenerator(string authorizationKey, string cpId, string serviceId, string price, string timestamp, string requestId)
        {
            string result = "";
            try
            {
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                var key = cpId + serviceId + price + timestamp + requestId;
                key = key.ToLower();
                HMACSHA1 hmac = new HMACSHA1(ConvertHexStringToByteArray(authorizationKey));
                hmac.Initialize();
                byte[] buffer1 = encoding.GetBytes(key.ToLower());
                result = BitConverter.ToString(hmac.ComputeHash(buffer1)).Replace("-", "").ToLower();
            }
            catch (Exception e)
            {
                logs.Error("Exception in IrancellSignatureGenerator: ", e);
            }
            return result;
        }

        public static string IrancellEncryptedResponse(string encryptedText, string authorizationKey)
        {
            string cryptTxt = "";
            try
            {
                cryptTxt = encryptedText.Replace(" ", "+");
                byte[] bytesBuff = Convert.FromBase64String(cryptTxt);

                string key = authorizationKey;

                using (Aes aes = Aes.Create())
                {
                    Rfc2898DeriveBytes crypto = new Rfc2898DeriveBytes(key, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    aes.Key = crypto.GetBytes(32); aes.IV = crypto.GetBytes(16);
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        using (CryptoStream cStream = new CryptoStream(mStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cStream.Write(bytesBuff, 0, bytesBuff.Length); cStream.Close();
                        }
                        cryptTxt = Encoding.Unicode.GetString(mStream.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IrancellEncryptedResponse: ", e);
            }
            return cryptTxt;
        }

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            byte[] HexAsBytes = null;
            try
            {
                if (hexString.Length % 2 != 0)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
                }
                HexAsBytes = new byte[hexString.Length / 2];
                for (int index = 0; index < HexAsBytes.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ConvertHexStringToByteArray: ", e);
            }
            return HexAsBytes;
        }

        public static int CaluculatePercentageDifference(int value1, int value2)
        {
            value1 = value1 == 0 ? 1 : value1;
            var percent = (((decimal)value2 - (decimal)value1) / (decimal)value1) * (decimal)100;
            return Convert.ToInt32(percent);
        }
        private static string fnc_getNotificationIcon(StandardEventLevel level)
        {
            string icon = "";
            if (level == StandardEventLevel.Critical)
            {
                icon = "🆘";
            }
            else if (level == StandardEventLevel.Error)
            {
                icon = "🔴";
            }
            else if (level == StandardEventLevel.Informational)
            {
                icon = "✅";
            }
            else if (level == StandardEventLevel.Warning)
            {
                icon = "⚠";
            }
            return icon;
        }
        
            public static void sb_sendNotification_AtomicWarning(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n4.php";

            string url = "";
            string icon = "";

            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationAtomicWarning);
                icon = fnc_getNotificationIcon(level);
                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e);
            }
        }
        public static void sb_sendNotification_SingleChargeGang(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n6.php";

            string url = "";
            string icon = "";

            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationSingleChargeGang);
                icon = fnc_getNotificationIcon(level);
                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e);
            }
        }
        public static void sb_sendNotification_DEmergency(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n3.php";
            string url = "";
            string icon = "";

            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationDEmergency);
                icon = fnc_getNotificationIcon(level);

                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e + url + icon + HttpUtility.UrlEncode(message));
            }
        }
        public static void sb_sendNotification_DLog(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n5.php";
            string url = "";
            string icon = "";
            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationDLog);
                icon = fnc_getNotificationIcon(level);

                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e + url + icon + HttpUtility.UrlEncode(message));
            }
        }

        public static void sb_sendNotification_DRequestLog(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n7.php";
            string url = "";
            string icon = "";
            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationDResuestLog);
                icon = fnc_getNotificationIcon(level);

                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e + url + icon + HttpUtility.UrlEncode(message));
            }
        }

        public static void sb_sendNotification_Monitoring(StandardEventLevel level, string message)
        {
            //string url = "http://84.22.102.27/notif/n6.php";
            string url = "";
            string icon = "";

            try
            {
                url = fnc_getServerActionURL(enumServers.dehnadNotification, enumServersActions.dehnadNotificationSingleChargeGang);
                icon = fnc_getNotificationIcon(level);
                Uri uri = new Uri(url + icon + HttpUtility.UrlEncode(message), UriKind.Absolute);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Timeout = 60 * 1000;

                //webRequest.Headers.Add("SOAPAction", action);
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "Get";
                WebResponse webResponse = webRequest.GetResponse();
                webResponse.Close();
            }
            catch (Exception e)
            {
                logs.Error(e);
            }
        }
        private static string telepromoIp = "http://10.20.9.135:8600"; // "http://10.20.9.157:8600" "http://10.20.9.159:8600"
        private static string telepromoIpJSON = "http://10.20.9.187:8700";
        private static string telepromoMapfaIp = "http://10.20.9.188:9090";
        private static string mtnIp = "http://92.42.55.180:8310";
        private static string mtnIpGet = "http://92.42.51.91";
        private static string mciIp = "http://172.17.251.18:8090";
        private static string sammsonTciIP = "https://www.tci.ir";
        private static string mobinOneMapfaIP = "http://10.20.9.8:9005";
        private static string dehnadNotificationIP = "http://84.22.102.27";
        private static string dehnadAppPortalIP = "http://79.175.164.51:8093";
        private static string dehnadReceivePortalIP = "http://79.175.164.51:200";
        private static string dehnadReceivePortalOnTohidIP = "http://10.20.96.65:8090";
        private static string mobinOneSyncIP = "http://cp.mobinone.org:33065";
        private static string MCIFtpSyncIP = "ftp://172.17.252.201";

        public enum enumServers
        {
            telepromo = 0,
            telepromoJson = 1,
            SamssonTci = 2,
            MTN = 3,
            MTNGet = 6,
            MCI = 4,
            TelepromoMapfa = 5,
            mobinOneMapfa = 7,
            mobinOne = 8,
            dehnadNotification = 9,
            dehnadAppPortal = 10,
            dehnadReceivePortal = 11,
            dehnadReceivePortalOnTohid = 12,
            mobinOneSync = 13,
            MCIFtpSync = 14,
        }

        public enum enumServersActions
        {
            sendmessage = 0,
            bulk = 1,
            charge = 2,
            otpRequest = 3,
            otpConfirm = 4,
            chargeCancel = 5,
            MTNUnsubGet = 6,
            MTNOtpGet = 7,
            MTNGetReceivedSms = 9,
            MTNSmsNotification = 10,
            MTNSmsStopNotification = 11,
            ftp = 8,
            dehnadNotificationSingleChargeGang = 12,
            dehnadNotificationDEmergency = 13,
            dehnadNotificationDLog = 14,
            dehnadMTNDelivery = 15,
            dehnadMCIDelivery = 16,
            dehnadBot = 17,
            dehnadNotificationDResuestLog = 18,
            mobinOneSync = 19,
            MCISync = 20,
            dehnadNotificationAtomicWarning = 21,

        }
        public static List<Models.ServersIP> fnc_getLocalServers()
        {
            using (var portal = new PortalEntities())
            {
                var serverIPEntity = portal.ServersIPs.Where(o => (
                o.ServerName == enumServers.dehnadAppPortal.ToString()
                || o.ServerName == enumServers.dehnadNotification.ToString()
                || o.ServerName == enumServers.dehnadReceivePortal.ToString()
                || o.ServerName == enumServers.dehnadReceivePortalOnTohid.ToString())
                && o.state == 1 && !string.IsNullOrEmpty(o.IP)).OrderBy(o => o.priority).ToList();
                return serverIPEntity;
            }
        }

        public static enumServers fnc_getEnumServerByAggregatorName(string aggregatorName)
        {
            if (aggregatorName.ToLower() == "MciDirect".ToLower())
            {
                return enumServers.MCI;
            }
            else if (aggregatorName.ToLower() == "MobinOne".ToLower())
            {
                return enumServers.mobinOne;
            }
            else if (aggregatorName.ToLower() == "MobinOneMapfa".ToLower())
            {
                return enumServers.mobinOneMapfa;
            }
            else if (aggregatorName.ToLower() == "MTN".ToLower())
            {
                return enumServers.MTN;
            }
            else if (aggregatorName.ToLower() == "SamssonTci".ToLower())
            {
                return enumServers.SamssonTci;
            }
            else if (aggregatorName.ToLower() == "Telepromo".ToLower())
            {
                return enumServers.telepromoJson;
            }
            else if (aggregatorName.ToLower() == "TelepromoMapfa".ToLower())
            {
                return enumServers.TelepromoMapfa;
            }
            else throw new Exception("There is no URL defined for the AggregatorName " + aggregatorName);

        }
        public static string fnc_getServerURL(enumServers server)
        {
            string userName = null;
            string pwd = null;
            return fnc_getServerURL(server, out userName, out pwd);
        }
        public static string fnc_getServerURL(enumServers server, out string userName, out string pwd)
        {
            userName = null;
            pwd = null;
            try
            {
                string serverURL = "";
                var serverName = server.ToString().ToLower();
                using (var portal = new PortalEntities())
                {
                    var entryServersIP = portal.ServersIPs.Where(o => o.ServerName == serverName && o.state == 1 && !string.IsNullOrEmpty(o.IP)).OrderBy(o => o.priority).FirstOrDefault();

                    if (entryServersIP != null)
                    {
                        userName = entryServersIP.userName;
                        pwd = entryServersIP.pwd;

                        serverURL = entryServersIP.IP;
                        if (!serverURL.StartsWith("http://") && !serverURL.StartsWith("ftp://"))
                            serverURL = "http://" + serverURL;
                        if (serverURL.EndsWith("/"))
                            serverURL = serverURL.Remove(serverURL.Length - 1, 1);

                        return serverURL;
                       
                    }
                    else
                    {
                        logs.Error("Error in fnc_getServerURL : No serverIP found for =" + server.ToString());
                        switch (server)
                        {
                            case enumServers.telepromo:
                                serverURL = telepromoIp;
                                break;
                            case enumServers.telepromoJson:
                                serverURL = telepromoIpJSON;
                                break;
                            case enumServers.SamssonTci:
                                serverURL = sammsonTciIP;
                                break;
                            case enumServers.MTN:
                                serverURL = mtnIp;
                                break;
                            case enumServers.MTNGet:
                                serverURL = mtnIpGet;
                                break;
                            case enumServers.MCI:
                                serverURL = mciIp;
                                break;
                            case enumServers.TelepromoMapfa:
                                serverURL = telepromoMapfaIp;
                                break;
                            case enumServers.mobinOneMapfa:
                                serverURL = mobinOneMapfaIP;
                                break;
                            case enumServers.dehnadNotification:
                                serverURL = dehnadNotificationIP;
                                break;
                            case enumServers.dehnadAppPortal:
                                serverURL = dehnadAppPortalIP;
                                break;
                            case enumServers.dehnadReceivePortal:
                                serverURL = dehnadReceivePortalIP;
                                break;
                            case enumServers.dehnadReceivePortalOnTohid:
                                serverURL = dehnadReceivePortalOnTohidIP;
                                break;
                            case enumServers.mobinOneSync:
                                serverURL = mobinOneSyncIP;
                                userName = "dehnad";
                                pwd = "OC56i4yA";
                                break;
                            case enumServers.MCIFtpSync:
                                serverURL = MCIFtpSyncIP;
                                userName = "DEH";
                                pwd = "d9H&*&123";
                                break;
                            default:
                                logs.Error("Error in fnc_getServerURL : Unknown Server " + server.ToString());
                                return "";
                        }
                        return serverURL;
                    }
                }
                //logs.Error("Error in fnc_getServerURL : Unknown Match server= " + server.ToString() + " and action = " + action.ToString());
                //return "";
            }
            catch (Exception e)
            {
                logs.Error("Error in fnc_getServerURL", e);
                return "";
            }

        }
        public static string fnc_getServerActionURL(enumServers server, enumServersActions action)
        {
            string userName, pwd;
            return fnc_getServerActionURL(server, action, out userName, out pwd);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string fnc_getServerActionURL(enumServers server, enumServersActions action, out string userName, out string pwd)
        {
            userName = null;
            pwd = null;
            try
            {
                string serverURL = "";
                var serverName = server.ToString().ToLower();
                var actionName = action.ToString().ToLower();
                serverURL = fnc_getServerURL(server, out userName, out pwd);
                using (var portal = new PortalEntities())
                {
                    
                    var entryServersIP = portal.ServersIPs.Where(o => o.ServerName == serverName && o.state == 1 && !string.IsNullOrEmpty(o.IP)).OrderBy(o => o.priority).FirstOrDefault();
                    if (entryServersIP != null)
                    {
                        var url = portal.ServersActions.Where(o => o.serverId == entryServersIP.Id && o.Action == actionName && o.state == 1 && !string.IsNullOrEmpty(o.URL)).OrderBy(o => o.priority).Select(o => o.URL).FirstOrDefault();
                        if (!string.IsNullOrEmpty(url))
                        {
                            if (url.StartsWith("/"))
                                url = url.Remove(0, 1);
                            if (string.IsNullOrEmpty(entryServersIP.ports))
                                return serverURL + "/" + url;
                            else return serverURL + ":" + entryServersIP.ports.Split(';')[0] + "/" + url;
                        }
                        else
                        {
                            logs.Error("Error in fnc_getServerURL : No Match found for server=" + server.ToString() + " and action=" + action.ToString());
                        }
                    }
                    else
                    {
                        logs.Error("Error in fnc_getServerURL : No serverIP found for =" + server.ToString());
                        
                        switch (server)
                        {
                            case enumServers.telepromo:
                                switch (action)
                                {
                                    case enumServersActions.sendmessage:
                                        return serverURL + "/" + "samsson-sdp/transfer/send?";
                                    case enumServersActions.bulk:
                                        return serverURL + "/" + "samsson-sdp/jtransfer/qsend?";
                                    case enumServersActions.charge:
                                        return serverURL + "/" + "samsson-sdp/transfer/charge?";
                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "samsson-sdp/pin/generate?";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "samsson-sdp/pin/confirm?";
                                    case enumServersActions.ftp:
                                        return serverURL + "/" + "ftp/{0}-{1}.txt.bz2";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.telepromoJson:
                                switch (action)
                                {
                                    case enumServersActions.sendmessage:
                                        return serverURL + "/" + "samsson-gateway/sendmessage/";
                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "samsson-gateway/otp-generation/";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "samsson-gateway/otp-confirmation/";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.SamssonTci:
                                switch (action)
                                {
                                    case enumServersActions.charge:
                                        return serverURL + "/" + "api/v1/GuestMode/Bill/{0}/{1}/{2}/{3}";
                                    case enumServersActions.chargeCancel:
                                        return serverURL + "/" + "api/v1/GuestMode/cancel/{0}/{1}/{2}/";
                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "api/v1/GuestMode/AddPhone/{0}";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "api/v1/GuestMode/Verify/{0}/{1}";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.MTN:
                                switch (action)
                                {
                                    case enumServersActions.sendmessage:
                                        return serverURL + "/" + "SendSmsService/services/SendSms";
                                    case enumServersActions.charge:
                                        return serverURL + "/" + "AmountChargingService/services/AmountCharging";
                                    case enumServersActions.MTNGetReceivedSms:
                                        return serverURL + "/" + "ReceiveSmsService/services/ReceiveSms/getReceivedSmsRequest";
                                    case enumServersActions.MTNSmsNotification:
                                        return serverURL + "/" + "SmsNotificationManagerService/services/SmsNotificationManager/startSmsNotificationRequest";
                                    case enumServersActions.MTNSmsStopNotification:
                                        return serverURL + "/" + "SmsNotificationManagerService/services/SmsNotificationManager/stopSmsNotificationRequest";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.MTNGet:
                                switch (action)
                                {
                                    case enumServersActions.MTNOtpGet:
                                        return serverURL + "/" + "CGGateway/Default.aspx?Timestamp={0}&RequestID={1}&pageno={2}&Callback={3}&Sign={4}&mode={5}";
                                    case enumServersActions.MTNUnsubGet:
                                        return serverURL + "/" + "CGGateway/UnSubscribe.aspx?Timestamp={0}&RequestID={1}&CpCode={2}&Callback={3}&Sign={4}&mode={5}";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.MCI:
                                switch (action)
                                {
                                    case enumServersActions.sendmessage:
                                        return serverURL + "/" + "parlayxsmsgw/services/SendSmsService";
                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "apigw/charging/pushotp";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "apigw/charging/chargeotp";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.TelepromoMapfa:
                                switch (action)
                                {
                                    case enumServersActions.sendmessage:
                                        return serverURL + "/" + "samsson-gateway/sendmessagepardis/";
                                    case enumServersActions.charge:
                                        return serverURL + "/" + "samsson-gateway/chargingpardis/";
                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "samsson-gateway/otp-generationpardis/";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "samsson-gateway/otp-confirmationpardis/";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.mobinOneMapfa:
                                switch (action)
                                {
                                    case enumServersActions.charge:
                                        return serverURL + "/" + "charging_websrv/services/Charging?wsdl";
                                    default:
                                        break;
                                }
                                break;
                            case enumServers.dehnadNotification:
                                switch (action)
                                {
                                    case enumServersActions.dehnadNotificationDEmergency:
                                        return serverURL + "/" + "notif/n3.php?msg=";
                                    case enumServersActions.dehnadNotificationDLog:
                                        return serverURL + "/" + "notif/n5.php?msg=";
                                    case enumServersActions.dehnadNotificationSingleChargeGang:
                                        return serverURL + "/" + "notif/n6.php?msg=";
                                    case enumServersActions.dehnadNotificationDResuestLog:
                                        return serverURL + "/" + "notif/n7.php?msg=";
                                    case enumServersActions.dehnadNotificationAtomicWarning:
                                        return serverURL + "/" + "notif/n4.php?msg=";
                                }
                                break;
                            case enumServers.dehnadAppPortal:
                                switch (action)
                                {

                                    case enumServersActions.otpRequest:
                                        return serverURL + "/" + "api/App/OtpCharge";
                                    case enumServersActions.otpConfirm:
                                        return serverURL + "/" + "api/App/OtpConfirm";
                                    case enumServersActions.dehnadBot:
                                        return serverURL + "/" + "api/Bot/";

                                }
                                break;
                            case enumServers.dehnadReceivePortal:
                                switch (action)
                                {
                                    case enumServersActions.dehnadMTNDelivery:
                                        return serverURL + "/" + "api/Mtn/Delivery";


                                }
                                break;

                            case enumServers.dehnadReceivePortalOnTohid:
                                switch (action)
                                {
                                    case enumServersActions.dehnadMCIDelivery:
                                        return serverURL + "/" + "api/Mci/Delivery";

                                }
                                break;
                            case enumServers.mobinOneSync:
                                switch (action)
                                {
                                    case enumServersActions.mobinOneSync:
                                        return serverURL + "/?";

                                }
                                break;
                            case enumServers.MCIFtpSync:
                                switch (action)
                                {
                                    case enumServersActions.MCISync:
                                        return serverURL + "/";

                                }
                                break;
                            default:
                                break;

                        }
                    }
                }
                logs.Error("Error in fnc_getServerURL : Unknown Match server= " + server.ToString() + " and action = " + action.ToString());
                return "";
            }
            catch (Exception e)
            {
                logs.Error("Error in fnc_getServerURL", e);
                return "";
            }

        }

        //public static string fnc_getConnectionStringInAppConfig(SharedLibrary.Models.vw_servicesServicesInfo service)
        //{

        //    return "Shared" + service.databaseName + "Entities";

        //}
        //public static string fnc_getConnectionStringInAppConfig(string serviceCode)
        //{
        //    vw_servicesServicesInfo service = ServiceHandler.GetServiceFromServiceCode(serviceCode);
        //    return fnc_getConnectionStringInAppConfig(service);

        //}

        public static string test()
        {
            return "testlo";
        }
    }
}
