using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Portal.Controllers
{
    public class BotController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetNotification()
        {
            try
            {
                string message = "";
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in GetNotification method: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetUserWebService()
        {
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            string json = null;
            try
            {
                using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                {
                    var user = entity.Users.FirstOrDefault(o => o.ChatId == chatId);
                    json = JsonConvert.SerializeObject(user, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetUserWebService: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ServicesStatus()
        {
            string json = null;
            try
            {
                var list = DehnadNotificationService.ServiceChecker.GetDehnadServicesStatus();
                json = JsonConvert.SerializeObject(list);
            }
            catch (Exception e)
            {
                logs.Error("Exception in ServicesStatus: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> StartService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string json = null;
            try
            {
                DehnadNotificationService.ServiceChecker.StartService(serviceName);
            }
            catch (Exception e)
            {
                logs.Error("Exception in StartService: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> StopService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string json = null;
            try
            {
                DehnadNotificationService.ServiceChecker.StopService(serviceName);
            }
            catch (Exception e)
            {
                logs.Error("Exception in StopService: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> RestartService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string json = null;
            try
            {
                DehnadNotificationService.ServiceChecker.RestartService(serviceName);
            }
            catch (Exception e)
            {
                logs.Error("Exception in RestartService: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> KillProcess()
        {
            string processName = HttpContext.Current.Request.Form["processName"];
            string json = null;
            try
            {
                DehnadNotificationService.ServiceChecker.KillProcess(processName);
            }
            catch (Exception e)
            {
                logs.Error("Exception in KillProcess: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> CreateNewUser()
        {
            try
            {
                string stringedTelegramBotResponse = HttpContext.Current.Request.Form["stringedTelegramBotResponse"];
                using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<DehnadNotificationService.Models.TelegramBotResponse>(stringedTelegramBotResponse);
                    var message = (Message)response.Message;
                    var parameters = message.Text.Replace("/start", "").Trim();
                    string param1 = "";
                    if (parameters != null && parameters != "")
                    {
                        var splitedParameters = parameters.Split('=');
                        if (splitedParameters[0].ToLower() == "param1")
                            param1 = splitedParameters[1];
                    }
                    var newUser = new DehnadNotificationService.Models.User();
                    newUser.ChatId = message.Chat.Id;
                    newUser.DateCreated = DateTime.Now;
                    newUser.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    newUser.Username = message.Chat.Username == null ? null : message.Chat.Username;
                    newUser.Firstname = message.Chat.FirstName == null ? null : message.Chat.FirstName;
                    newUser.Lastname = message.Chat.LastName == null ? null : message.Chat.LastName;
                    newUser.Title = message.Chat.Title == null ? null : message.Chat.Title;
                    newUser.LastStep = "Started";
                    entity.Users.Add(newUser);
                    var setting = new DehnadNotificationService.Models.UserSetting();
                    setting.ChatId = message.Chat.Id;
                    setting.Name = "EnableSMS";
                    setting.Value = "0";
                    entity.UserSettings.Add(setting);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CreateNewUserWebService: ", e);
            }
            var responseCode = new HttpResponseMessage(HttpStatusCode.OK);
            return responseCode;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveMessageWebService()
        {
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            string text = HttpContext.Current.Request.Form["text"];
            string channel = HttpContext.Current.Request.Form["channel"];
            string json = "";
            using (var entity = new DehnadNotificationService.Models.NotificationEntities())
            {
                var userMessage = new DehnadNotificationService.Models.UserMessage();
                userMessage.ChatId = chatId;
                userMessage.DateReceived = DateTime.Now;
                userMessage.PersianDateReceived = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                userMessage.Message = text;
                userMessage.Channel = channel;
                userMessage.IsProcessed = false;
                entity.UserMessages.Add(userMessage);
                entity.SaveChanges();
                json = JsonConvert.SerializeObject(userMessage.Id, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetUnsendMessages()
        {
            string channel = HttpContext.Current.Request.Form["channel"];
            string json = null;
            try
            {
                using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                {
                    var messages = entity.SentMessages.Where(o => o.IsSent == false && o.Channel == channel).ToList();
                    json = JsonConvert.SerializeObject(messages, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetUnsendMessagesId: ", e);
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChangeMessageStatusToSended()
        {
            try
            {
                var messageIds = JsonConvert.DeserializeObject<List<long>>(HttpContext.Current.Request.Form["messageIds"]);
                using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                {
                    var messages = entity.SentMessages.Where(o => messageIds.Contains(o.Id)).ToList();
                    foreach (var message in messages)
                    {
                        message.IsSent = true;
                        message.DateSent = DateTime.Now;
                        entity.Entry(message).State = System.Data.Entity.EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChangeMessageStatusToSended: ", e);
            }
            var responseCode = new HttpResponseMessage(HttpStatusCode.OK);
            return responseCode;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChangeReceivedMessageStatusToProcessed()
        {
            try
            {
                var messageId = JsonConvert.DeserializeObject<long>(HttpContext.Current.Request.Form["messageId"]);
                using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                {
                    var message = entity.UserMessages.Where(o => o.Id == messageId).FirstOrDefault();
                    if (message != null)
                    {
                        message.IsProcessed = true;
                        message.DateProcessed = DateTime.Now;
                        entity.Entry(message).State = System.Data.Entity.EntityState.Modified;
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ChangeReceivedMessageStatusToProcessed: ", e);
            }
            var responseCode = new HttpResponseMessage(HttpStatusCode.OK);
            return responseCode;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveContact()
        {
            string mobileNumber = HttpContext.Current.Request.Form["mobileNumber"];
            string firstName = HttpContext.Current.Request.Form["firstName"];
            string lastName = HttpContext.Current.Request.Form["lastName"];
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            long userId = Convert.ToInt64(HttpContext.Current.Request.Form["userId"]);
            using (var entity = new DehnadNotificationService.Models.NotificationEntities())
            {
                var user = entity.Users.FirstOrDefault(o => o.ChatId == chatId);
                user.MobileNumber = mobileNumber;
                if (firstName != "")
                    user.Firstname = firstName;
                if (lastName != "")
                    user.Lastname = lastName;
                user.UserId = userId;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
            var responseCode = new HttpResponseMessage(HttpStatusCode.OK);
            return responseCode;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveLastStep()
        {
            string lastStep = HttpContext.Current.Request.Form["lastStep"];
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            using (var entity = new DehnadNotificationService.Models.NotificationEntities())
            {
                var user = entity.Users.FirstOrDefault(o => o.ChatId == chatId);
                user.LastStep = lastStep;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
            var responseCode = new HttpResponseMessage(HttpStatusCode.OK);
            return responseCode;
        }
    }
}
