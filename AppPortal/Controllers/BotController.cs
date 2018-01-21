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
        public HttpResponseMessage CreateNewUserWebService()
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
        public async void SaveMessageWebService()
        {
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            string text = HttpContext.Current.Request.Form["text"];
            using (var entity = new DehnadNotificationService.Models.NotificationEntities())
            {
                var userMessage = new DehnadNotificationService.Models.UserMessage();
                userMessage.ChatId = chatId;
                userMessage.DateReceived = DateTime.Now;
                userMessage.PersianDateReceived = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                userMessage.Message = text;
                userMessage.Channel = "telegram";
                entity.UserMessages.Add(userMessage);
                entity.SaveChanges();
            }
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
