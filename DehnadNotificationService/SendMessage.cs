using DehnadNotificationService.Models;
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
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SendMessageBySms(string text, UserType userType)
        {
            try
            {
                using (var entity = new NotificationEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    List<string> mobileNumbers = new List<string>();
                    if (userType == UserType.AdminOnly)
                        mobileNumbers = entity.Users.Where(o => o.LastStep.Contains("Admin") && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();
                    else if (userType == UserType.MemberOnly)
                        mobileNumbers = entity.Users.Where(o => o.LastStep.Contains("Member") && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();
                    else
                        mobileNumbers = entity.Users.Where(o => (o.LastStep.Contains("Admin") || o.LastStep.Contains("Member")) && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();
                    foreach (var mobileNumber in mobileNumbers)
                    {
                        var url = string.Format("http://37.130.202.188/class/sms/webservice/send_url.php?from=985000145&to={1}&msg={0}&uname=APDehnad&pass=Dehnad@66", text, mobileNumber);
                        using (var client = new HttpClient())
                        {
                            var request = new HttpRequestMessage() { RequestUri = new Uri(url) };
                            using (var response = client.SendAsync(request).Result)
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    string httpResult = response.Content.ReadAsStringAsync().Result;
                                    logs.Info("Notification SendMessageBySms result: " + httpResult);
                                }
                                else
                                {
                                    logs.Info("Notification SendMessageBySms Not Successful with http stauts code: " + response.StatusCode);
                                }
                            }
                        }
                        Service.SaveSendedMessageToDB(text, "sms", userType.ToString(), null, mobileNumber);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMessageBySms: " + e);
            }
        }
    }
}
