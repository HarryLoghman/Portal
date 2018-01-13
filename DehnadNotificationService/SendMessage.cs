using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DehnadNotificationService
{
    public class SendMessage
    {
        public static void SendMessageBySms(string text)
        {
            var url = string.Format("http://37.130.202.188/class/sms/webservice/send_url.php?from=985000145&to=09125612694&msg={0}&uname=APDehnad&pass=Dehnad@66", text);
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage() { RequestUri = new Uri(url) };
                using (var response = client.SendAsync(request).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        //string httpResult = response.Content.ReadAsStringAsync().Result;
                    }
                }
            }
        }
    }
}
