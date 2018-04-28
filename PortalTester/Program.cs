using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PortalTester
{
    class Program
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            RunTest();
            Console.WriteLine("end...");
            Console.ReadLine();
        }

        public static void RunTest()
        {
            Console.WriteLine("enter mobile number");
            var mobile = Console.ReadLine();
            //MapfaOtpRequest(mobile);
            //MapfaOtpConfirm(mobile);
            var list = new List<string>();
            for (int i = 1; i <= 3000; i++)
                list.Add(mobile);
            var refrence = TelepromoOtpRequest(mobile);
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "start");
            for (int i = 0; i < list.Count; i += 100)
            {
                var receivedChunk = list.Skip(i).Take(100).ToList();
                List<Task> TaskList = new List<Task>();
                foreach (var message in receivedChunk)
                {
                    TaskList.Add(TelepromoOtpRequest(mobile));
                }
                Task.WaitAll(TaskList.ToArray());
            }
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "end");
            //var refrence = TelepromoOtpRequest(mobile);
            //TelepromoOtpConfirm(mobile, refrence);
        }

        public static async Task<string> TelepromoOtpRequest(string mobile)
        {
            var url = "http://10.20.9.135:8600/samsson-sdp/pin/generate?";
            var sc = "Dehnad";
            var username = "Dehnad";
            var password = "dehnad@123";
            var from = "98" + "307235";
            var serviceId = "441faa36103e44b2b2d69de90d195356";
            Random rnd = new Random();
            var refrence = "";
            using (var client = new HttpClient())
            {
                var to = "98" + mobile.TrimStart('0');
                var messageContent = "InAppPurchase";
                var contentId = rnd.Next(00001, 99999).ToString();
                var messageId = rnd.Next(1000000, 9999999).ToString();
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}&contentId={8}&chargingCode={9}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId, contentId, "TELSUBCTELTAMLY500");

                var result = "";
                logs.Info(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Telepromo OtpRequest Start: " + mobile); ;
                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:dd:ss") + "Telepromo OtpRequest Start: " + mobile);
                using (var response = client.GetAsync(new Uri(urlWithParameters)).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        dynamic results = JsonConvert.DeserializeObject<dynamic>(httpResult);
                        refrence = results["messageId"] + "_" + results["transactionId"];
                        result = httpResult;
                    }
                }
                logs.Info(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Telepromo OtpRequest End: " + mobile + "  -  " + result);
                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:dd:ss") + "Telepromo OtpRequest End: " + mobile + "  -  " + result);
                return refrence;
            }
        }
        public static void TelepromoOtpConfirm(string mobile, string refrenceId)
        {
            Console.WriteLine("enter tamly500 confirm code");
            var confirmcode = Console.ReadLine();
            var url = "http://10.20.9.135:8600/samsson-sdp/pin/confirm?";
            var sc = "Dehnad";
            var username = "Dehnad";
            var password = "dehnad@123";
            var from = "98" + "307235";
            var serviceId = "441faa36103e44b2b2d69de90d195356";
            using (var client = new HttpClient())
            {
                var to = "98" + mobile.TrimStart('0');
                var messageContent = "InAppPurchase";
                string otpIds = refrenceId;
                var optIdsSplitted = otpIds.Split('_');
                var messageId = optIdsSplitted[0];
                var transactionId = optIdsSplitted[1];
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}&transactionId={8}&pin={9}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId, transactionId, confirmcode);
                var result = "";
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Telepromo OtpConfirm Start: " + mobile);
                using (var response = client.GetAsync(new Uri(urlWithParameters)).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string httpResult = response.Content.ReadAsStringAsync().Result;
                        result = httpResult;
                    }
                }
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Telepromo OtpConfirm End: " + mobile + "  -  " + result);
            }
        }
        public static void MapfaOtpRequest(string mobile)
        {
            var username = "dehnad";
            var password = "4aa71a7447f5f8b050441e3019afdf4c";
            var aggregatorId = "8";
            var channelType = 1;
            var domain = "";
            if (aggregatorId == "3")
                domain = "pardis1";
            else
                domain = "alladmin";
            var mobileNumber = "98" + mobile.TrimStart('0');
            var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient();
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Mapfa OtpRequest Start: " + mobile);
            var result = client.sendVerificationCode(username, password, domain, channelType, mobileNumber, "dehnad_fantom_392");
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Mapfa OtpRequest End: " + mobile + "  -  " + result);
        }

        public static void MapfaOtpConfirm(string mobile)
        {
            Console.WriteLine("enter phantom confirm code");
            var confirmcode = Console.ReadLine();
            var username = "dehnad";
            var password = "4aa71a7447f5f8b050441e3019afdf4c";
            var aggregatorId = "8";
            var channelType = 1;
            var domain = "";
            if (aggregatorId == "3")
                domain = "pardis1";
            else
                domain = "alladmin";
            var mobileNumber = "98" + mobile.TrimStart('0');
            var client = new SharedLibrary.MobinOneMapfaChargingServiceReference.ChargingClient();
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Mapfa OtpConfirm Start: " + mobile);
            var result = client.verifySubscriber(username, password, domain, channelType, mobileNumber, "dehnad_fantom_392", confirmcode);
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + "Mapfa OtpConfirm End: " + mobile + "  -  " + result);
        }
    }
}
