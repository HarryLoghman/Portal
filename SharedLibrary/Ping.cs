using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Ping
    {
        public static long PingTelepromoMessageSending()
        {
            long result = 0;
            var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Tamly500", "Telepromo");
            var telepromoIp = SharedLibrary.MessageSender.telepromoIp;
            var url = telepromoIp + "/samsson-sdp/transfer/send?";
            var sc = "Dehnad";
            var username = serviceAdditionalInfo["username"];
            var password = serviceAdditionalInfo["password"];
            var from = "98" + serviceAdditionalInfo["shortCode"];
            var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
            Random rnd = new Random();
            try
            {
                var message = new Models.MessageObject();
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                var to = "98" + message.MobileNumber.TrimStart('0');
                var messageContent = message.Content;

                var messageId = rnd.Next(1000000, 9999999).ToString();
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId);
                if (message.Price > 0)
                    urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    using (var response = client.GetAsync(new Uri(url)).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result = duration;
                }
            }
            catch
            {
                result = -1;
            }
            return result;
        }

        public static long PingTelepromoOtp()
        {
            long result = 0;
            var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("Tamly500", "Telepromo");
            var telepromoIp = SharedLibrary.MessageSender.telepromoIp;
            var url = telepromoIp + "/samsson-sdp/transfer/send?";
            var sc = "Dehnad";
            var username = serviceAdditionalInfo["username"];
            var password = serviceAdditionalInfo["password"];
            var from = "98" + serviceAdditionalInfo["shortCode"];
            var serviceId = serviceAdditionalInfo["aggregatorServiceId"];
            Random rnd = new Random();
            try
            {
                var message = new Models.MessageObject();
                message.MobileNumber = "09900000000";
                message.Content = "ping";
                var to = "98" + message.MobileNumber.TrimStart('0');
                var messageContent = message.Content;

                var messageId = rnd.Next(1000000, 9999999).ToString();
                var urlWithParameters = url + String.Format("sc={0}&username={1}&password={2}&from={3}&serviceId={4}&to={5}&message={6}&messageId={7}"
                                                        , sc, username, password, from, serviceId, to, messageContent, messageId);
                if (message.Price > 0)
                    urlWithParameters += String.Format("&chargingCode={0}", message.ImiChargeKey);
                using (var client = new HttpClient())
                {
                    var now = DateTime.Now;
                    using (var response = client.GetAsync(new Uri(url)).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string httpResult = response.Content.ReadAsStringAsync().Result;
                        }
                    }
                    var after = DateTime.Now;
                    var duration = (long)((after - now).TotalMilliseconds);
                    result = duration;
                }
            }
            catch
            {
                result = -1;
            }
            return result;
        }
    }
}
