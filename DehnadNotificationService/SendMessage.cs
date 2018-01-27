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
        public static async void SendMessageBySms()
        {
            List<long> messageIdsThatSended = new List<long>();
            List<SentMessage> smsToSend = new List<SentMessage>();
            try
            {
                if (Properties.Settings.Default.IsBotServer == false)
                {
                    using (var entity = new NotificationEntities())
                    {
                        entity.Configuration.AutoDetectChangesEnabled = false;
                        smsToSend = entity.SentMessages.Where(o => o.IsSent == false && o.Channel == "telegram").ToList();
                    }
                }
                else
                {
                    var userParams = new Dictionary<string, string>() { { "channel", "sms" } };
                    smsToSend = await SharedLibrary.UsefulWebApis.NotificationBotApi<List<SentMessage>>("GetUnsendMessages", userParams);
                }
                foreach (var sms in smsToSend)
                {
                    var url = string.Format("http://37.130.202.188/class/sms/webservice/send_url.php?from=985000145&to={1}&msg={0}&uname=APDehnad&pass=Dehnad@66", sms.Content, sms.MobileNumber);
                    using (var client = new HttpClient())
                    {
                        var request = new HttpRequestMessage() { RequestUri = new Uri(url) };
                        using (var response = client.SendAsync(request).Result)
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string httpResult = response.Content.ReadAsStringAsync().Result;
                                logs.Info("Notification SendMessageBySms result: " + httpResult);
                                messageIdsThatSended.Add(sms.Id);
                            }
                            else
                            {
                                logs.Info("Notification SendMessageBySms Not Successful with http stauts code: " + response.StatusCode);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SendMessageBySms: " + e);
            }
            Service.ChangeMessageStatusToSended(messageIdsThatSended);
        }

        public static void SaveSmsMessageToQueue(string message, UserType userType)
        {
            try
            {
                using (var entity = new NotificationEntities())
                {
                    List<string> mobileNumbers = new List<string>();
                    if (userType == UserType.AdminOnly)
                        mobileNumbers = entity.Users.Where(o => o.LastStep.Contains("Admin") && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();
                    else if (userType == UserType.MemberOnly)
                        mobileNumbers = entity.Users.Where(o => o.LastStep.Contains("Member") && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();
                    else
                        mobileNumbers = entity.Users.Where(o => (o.LastStep.Contains("Admin") || o.LastStep.Contains("Member")) && o.MobileNumber != null).Select(o => o.MobileNumber).ToList();

                    foreach (var mobile in mobileNumbers)
                    {
                        var sms = new SentMessage();
                        sms.Channel = "sms";
                        sms.Content = message;
                        sms.DateCreated = DateTime.Now;
                        sms.IsSent = false;
                        sms.MobileNumber = mobile;
                        sms.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                        sms.UserType = userType.ToString();
                        entity.SentMessages.Add(sms);
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SaveSmsMessageToQueue: " + e);
            }
        }
    }
}
