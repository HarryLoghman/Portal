using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class UsefulWebApis
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task<dynamic> MciOtpSendActivationCode(string serviceCode, string mobileNumber, string price)
        {
            dynamic result = new ExpandoObject();
            result.Status = "Error";
            try
            {
                logs.Error("MyError1:OtpCharge" + serviceCode + mobileNumber);
                var accessKey = Encrypt.GetHMACSHA256Hash("OtpCharge" + serviceCode + mobileNumber);
                logs.Info("OtpCharge" + serviceCode + mobileNumber);
                var localCallCrypt = Encrypt.EncryptString_RijndaelManaged("localcall");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var values = new Dictionary<string, string>
                {
                   { "AccessKey", accessKey },
                   { "MobileNumber", mobileNumber },
                   { "Price", price },
                   { "ServiceCode", serviceCode },
                   { "ExtraParameter", localCallCrypt }
                };
                    
                    var content = new FormUrlEncodedContent(values);
                    
                    //var url = "http://79.175.164.51:8093/api/App/OtpCharge";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.dehnadAppPortal, HelpfulFunctions.enumServersActions.otpRequest);
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    logs.Error("MyError2:" + responseString);
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    result = jsonResponse;

                }
            }
            catch (Exception e)
            {
                result.Status = "Error";
                logs.Error("Exception in MciOtpSendActivationCode: ", e);
            }
            return result;
        }
        public static async Task<dynamic> MciOtpSendConfirmCode(string serviceCode, string mobileNumber, string confirmCode)
        {
            dynamic result = new ExpandoObject();
            result.Status = "Error";
            try
            {
                var accessKey = Encrypt.GetHMACSHA256Hash("OtpConfirm" + serviceCode + mobileNumber);
                var localCallCrypt = Encrypt.EncryptString_RijndaelManaged("localcall");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var values = new Dictionary<string, string>
                {
                   { "AccessKey", accessKey },
                   { "MobileNumber", mobileNumber },
                   { "ConfirmCode", confirmCode },
                   { "ServiceCode", serviceCode },
                   { "ExtraParameter", localCallCrypt }
                };

                    var content = new FormUrlEncodedContent(values);
                    //var url = "http://79.175.164.51:8093/api/App/OtpConfirm";
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.dehnadAppPortal, HelpfulFunctions.enumServersActions.otpConfirm);
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    result = jsonResponse;
                }
            }
            catch (Exception e)
            {
                result.Stauts = "Error";
                logs.Error("Exception in MciOtpSendConfirmCode: ", e);
            }
            return result;
        }
        static HttpClient v_client;
        public static async Task<dynamic> DanoopReferral(string ipAndMethodName, string parameters)
        {
            dynamic result = new ExpandoObject();
            result.status = "Error";
            try
            {
                if (v_client == null)
                {
                    v_client = new HttpClient();
                    v_client.Timeout = TimeSpan.FromSeconds(20);
                }

                var url = string.Format("{0}?{1}", ipAndMethodName, parameters);
                logs.Info("danoop request:" + url);
                var response = v_client.GetAsync(new Uri(url)).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    logs.Info("danoop response:" + responseString);
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                    result = jsonResponse;
                }
                else
                {
                    result.stauts = "Error";
                    result.description = "exception";
                    logs.Error("DanoopReferral returned status: " + response.StatusCode);
                }

            }
            catch (Exception e)
            {
                result.stauts = "Error";
                result.description = "exception";
                logs.Error("Exception in DanoopReferral: ", e);
            }
            return result;
        }

        public static string DanoopReferralWithWebRequest(string ipAndMethodName, string parameters)
        {
            bool internalServerError = false;
            WebExceptionStatus status = WebExceptionStatus.Success;
            var url = string.Format("{0}?{1}", ipAndMethodName, parameters);
            Uri uri = new Uri(url, UriKind.Absolute);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = 20 * 1000;

            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/html;charset=\"utf-8\"";
            webRequest.Accept = "text/html";
            webRequest.Method = "Get";

            //using (Stream stream = webRequest.GetRequestStream())
            //{
            //    //Byte[] bts = UnicodeEncoding.UTF8.GetBytes(content);
            //    //stream.Write(bts, 0, bts.Count());
            //}

            string result;
            try
            {
                var response = webRequest.GetResponse();
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
                response.Close();
            }
            catch (System.Net.WebException ex)
            {
                status = ex.Status;
                if (ex.Response != null)
                {
                    internalServerError = true;
                    using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                else result = "";
                ex.Response.Close();
            }

            return result;
        }

        public static string sendPostWithWebRequest(string url, string content, out bool internalServerError , out WebExceptionStatus status )
        {
            internalServerError = false;
            status = WebExceptionStatus.Success;
            
            Uri uri = new Uri(url, UriKind.Absolute);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Timeout = 60 * 1000;
            
            //webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            
            using (Stream stream = webRequest.GetRequestStream())
            {
                Byte[] bts = UnicodeEncoding.UTF8.GetBytes(content);
                stream.Write(bts, 0, bts.Count());
            }
            
            string result;
            try
            {
                var response = webRequest.GetResponse();
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
                response.Close();
            }
            catch (System.Net.WebException ex)
            {
                status = ex.Status;
                if (ex.Response != null)
                { 
                    internalServerError = true;
                    using (StreamReader rd = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                else result = "";

            }
            
            return result;
        }
        public static async Task<T> NotificationBotApi<T>(string methodName, Dictionary<string, string> parameters) where T : class
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(parameters);
                    //var url = "http://79.175.164.51:8093/api/Bot/" + methodName;
                    var url = HelpfulFunctions.fnc_getServerActionURL(HelpfulFunctions.enumServers.dehnadAppPortal, HelpfulFunctions.enumServersActions.dehnadBot); 
                    var response = await client.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode)
                        return null;
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseString,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in BotApis: ", e);
            }
            return null;
        }
    }
}
