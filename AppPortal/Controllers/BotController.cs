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
            string result = "";
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "AppPortal:BotController:GetNotification");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    //string message = "";
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:GetNotification", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetUserWebService()
        {
            long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                    new Dictionary<string, string>() {
                        { "chatid", chatId.ToString()}}, null, "AppPortal:BotController:GetUserWebService");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                    {
                        var user = entity.Users.FirstOrDefault(o => o.ChatId == chatId);
                        result = JsonConvert.SerializeObject(user, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:GetUserWebService", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ServicesStatus()
        {
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, null, null, "AppPortal:BotController:ServicesStatus");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var list = DehnadNotificationService.ServiceChecker.GetDehnadServicesStatus();
                    result = JsonConvert.SerializeObject(list);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:ServicesStatus", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> StartService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicename",serviceName}}, null, "AppPortal:BotController:StartService");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    DehnadNotificationService.ServiceChecker.StartService(serviceName);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:StartService", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> StopService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicename",serviceName}}, null, "AppPortal:BotController:StopService");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    DehnadNotificationService.ServiceChecker.StopService(serviceName);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:StopService", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> RestartService()
        {
            string serviceName = HttpContext.Current.Request.Form["serviceName"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicename",serviceName } }, null, "AppPortal:BotController:RestartService");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    DehnadNotificationService.ServiceChecker.RestartService(serviceName);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:RestartService", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> KillProcess()
        {
            string processName = HttpContext.Current.Request.Form["processName"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "processname",processName}}, null, "AppPortal:BotController:KillProcess");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    DehnadNotificationService.ServiceChecker.KillProcess(processName);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:KillProcess", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ServiceInfo()
        {
            string serviceCode = HttpContext.Current.Request.Form["serviceCode"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, new Dictionary<string, string>()
                                                            { { "servicecode",serviceCode }}, null, "AppPortal:BotController:ServiceInfo");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var income = DehnadNotificationService.Income.GetServiceIncome(serviceCode);
                    result = JsonConvert.SerializeObject(income);
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:ServiceInfo", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> CreateNewUser()
        {
            string result = null;
            bool resultOk = true;
            try
            {
                string stringedTelegramBotResponse = HttpContext.Current.Request.Form["stringedTelegramBotResponse"];
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                    new Dictionary<string, string>() { { "stringedTelegramBotResponse", stringedTelegramBotResponse } }
                    , null, "AppPortal:BotController:CreateNewUser");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

                    using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                    {
                        var responseTelegram = Newtonsoft.Json.JsonConvert.DeserializeObject<DehnadNotificationService.Models.TelegramBotResponse>(stringedTelegramBotResponse);
                        var message = (Message)responseTelegram.Message;
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
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:CreateNewUser", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveMessageWebService()
        {
            string result = "";
            bool resultOk = true;
            try
            {
                long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
                string text = HttpContext.Current.Request.Form["text"];
                string channel = HttpContext.Current.Request.Form["channel"];
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() {
                        { "chatId", chatId.ToString() }
                        ,{"channel", channel}
                        ,{"text" ,text } }, null, "AppPortal:BotController:SaveMessageWebService");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {


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
                        result = JsonConvert.SerializeObject(userMessage.Id, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:SaveMessageWebService", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> GetUnsendMessages()
        {
            string channel = HttpContext.Current.Request.Form["channel"];
            string result = null;
            bool resultOk = true;
            try
            {
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                    , new Dictionary<string, string>() {
                        {"channel", channel} }
                        , null, "AppPortal:BotController:GetUnsendMessages");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                    {
                        var messages = entity.SentMessages.Where(o => o.IsSent == false && o.Channel == channel).ToList();
                        result = JsonConvert.SerializeObject(messages, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:GetUnsendMessages", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChangeMessageStatusToSended()
        {
            string result = "";
            bool resultOk = true;

            try
            {
                var messageIdsStr = HttpContext.Current.Request.Form["messageIds"];
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current
                     , new Dictionary<string, string>() {
                        { "messageIds", messageIdsStr.ToString() } }
                        , null, "AppPortal:BotController:ChangeMessageStatusToSended");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var messageIds = JsonConvert.DeserializeObject<List<long>>(messageIdsStr);
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
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:ChangeMessageStatusToSended", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ChangeReceivedMessageStatusToProcessed()
        {
            string result = "";
            bool resultOk = true;
            try
            {
                var messageIdStr = HttpContext.Current.Request.Form["messageId"];
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                     new Dictionary<string, string>() {
                        { "messageId", messageIdStr.ToString() } }
                     , null, "AppPortal:BotController:ChangeReceivedMessageStatusToProcessed");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {
                    var messageId = JsonConvert.DeserializeObject<long>(messageIdStr);
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
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:ChangeReceivedMessageStatusToProcessed", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveContact()
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string mobileNumber = HttpContext.Current.Request.Form["mobileNumber"];
                string firstName = HttpContext.Current.Request.Form["firstName"];
                string lastName = HttpContext.Current.Request.Form["lastName"];
                long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
                long userId = Convert.ToInt64(HttpContext.Current.Request.Form["userId"]);
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current,
                     new Dictionary<string, string>() {
                        { "mobile", mobileNumber }
                     ,{ "chatid", chatId.ToString()}
                     ,{"userid",userId.ToString() } }
                     , null, "AppPortal:BotController:SaveContact");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

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
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:SaveContact", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> SaveLastStep()
        {
            string result = "";
            bool resultOk = true;
            try
            {
                string lastStep = HttpContext.Current.Request.Form["lastStep"];
                long chatId = Convert.ToInt64(HttpContext.Current.Request.Form["chatId"]);
                string tpsRatePassed = SharedLibrary.Security.fnc_tpsRatePassed(HttpContext.Current, 
                    new Dictionary<string, string>() {
                        { "chatid", chatId.ToString()}
                     ,{"laststep",lastStep.ToString() } }
                    , null, "AppPortal:BotController:SaveLastStep");
                if (!string.IsNullOrEmpty(tpsRatePassed))
                {
                    result = tpsRatePassed;
                    resultOk = false;
                }
                else
                {

                    using (var entity = new DehnadNotificationService.Models.NotificationEntities())
                    {
                        var user = entity.Users.FirstOrDefault(o => o.ChatId == chatId);
                        user.LastStep = lastStep;
                        entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("AppPortal:BotController:SaveLastStep", e);
                resultOk = false;
                //result = e.Message;
                result = "Exception has been occured!!! Contact Administrator";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (!resultOk)
                response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}
