using System;
using System.Linq;
using SharedLibrary.Models;
using DehnadNotificationService.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DehnadNotificationService
{
    public class BotManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static Dictionary<string, string> userParams = new Dictionary<string, string>();
        public static async Task<TelegramBotResponse> NewlyStartedUser(User user, TelegramBotResponse responseObject)
        {
            if (user.MobileNumber != null)
            {
                if (responseObject.Message.Text == "IWantToBeAdmin!!")
                {
                    userParams = new Dictionary<string, string>() { { "lastStep", "Admin" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Normal-");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
                    outputItem.Text = @"شما ادمین سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
                else if (responseObject.Message.Text == "SignMeToDehnadNotification")
                {
                    userParams = new Dictionary<string, string>() { { "lastStep", "Member" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Normal-");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
                    outputItem.Text = @"شما عضو سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
            }
            else
            {
                if (responseObject.Message.Text == "IWantToBeAdmin!!")
                {
                    userParams = new Dictionary<string, string>() { { "lastStep", "Admin-MobileConfirmation" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Contact-ارسال شماره موبایل");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
                    outputItem.Text = @"لطفا با استفاده از دکمه دریافت شماره، شماره خود را ثبت کنید.";
                    responseObject.OutPut.Add(outputItem);
                }
                else if (responseObject.Message.Text == "SignMeToDehnadNotification")
                {
                    userParams = new Dictionary<string, string>() { { "lastStep", "Member-MobileConfirmation" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Contact-ارسال شماره موبایل");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
                    outputItem.Text = @"شما عضو سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
            }
            return responseObject;
        }

        public static async Task<TelegramBotResponse> ContactReceived(User user, TelegramBotResponse responseObject)
        {
            var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(responseObject.Message.Contact.PhoneNumber.Replace(" ", ""));
            string firstName = ""; string lastName = "";
            if (responseObject.Message.Contact.FirstName != null)
                firstName = responseObject.Message.Contact.FirstName;
            if (responseObject.Message.Contact.LastName != null)
                lastName = responseObject.Message.Contact.LastName;
            userParams = new Dictionary<string, string>() { { "mobileNumber", mobileNumber }, { "chatId", responseObject.Message.Chat.Id.ToString() }, { "firstName", firstName }, { "lastName", lastName }, { "userId", responseObject.Message.Contact.UserId.ToString() } };
            await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveContact", userParams);
            if (user.LastStep.Contains("Admin"))
            {
                userParams = new Dictionary<string, string>() { { "lastStep", "Admin-Registered" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                responseObject = await AdminHelp(user, responseObject);
            }
            else if (user.LastStep.Contains("Member"))
            {
                userParams = new Dictionary<string, string>() { { "lastStep", "Member-Registered" }, { "chatId", responseObject.Message.Chat.Id.ToString() } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveLastStep", userParams);
                responseObject = await AdminHelp(user, responseObject);
            }
            return responseObject;
        }

        public static async Task<TelegramBotResponse> SerivcesStatus(User user, TelegramBotResponse responseObject)
        {
            var outputItem = new TelegramBotOutput();
            var message = @"شما به عنوان مدیر عضو سیستم نوتیفیکیشن دهناد هستید.";
            try
            {
                userParams = new Dictionary<string, string>() { };
                var services = await SharedLibrary.UsefulWebApis.NotificationBotApi<List<string>>("ServicesStatus", userParams);
                message = "";
                int i = 1;
                foreach (var service in services)
                {
                    if (!service.Contains("Running"))
                        message += "<b>" + i + "." + service + "</b>" + Environment.NewLine;
                    else
                        message += i + "." + service.ToLower() + Environment.NewLine;
                    i++;
                }
            }
            catch (Exception e)
            {
                message = "خطا در دریافت وضعیت ویندوز سرویس ها";
                logs.Error("Exception in SerivcesStatus: ", e);
            }
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = message;
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> StartService(User user, TelegramBotResponse responseObject)
        {
            try
            {
                var splittedText = responseObject.Message.Text.Split(' ');
                var serviceName = splittedText[2];
                userParams = new Dictionary<string, string>() { { "serviceName", serviceName } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("StartService", userParams);
            }
            catch (Exception e)
            {
                logs.Error("Exception in StartService: ", e);
            }
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"دستور استارت سرویس به سرور ارسال شد.";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> StopService(User user, TelegramBotResponse responseObject)
        {
            try
            {
                var splittedText = responseObject.Message.Text.Split(' ');
                var serviceName = splittedText[2];
                userParams = new Dictionary<string, string>() { { "serviceName", serviceName } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("StopService", userParams);
            }
            catch (Exception e)
            {
                logs.Error("Exception in StopService: ", e);
            }
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"دستور توفق سرویس به سرور ارسال شد.";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> RestartService(User user, TelegramBotResponse responseObject)
        {
            try
            {
                var splittedText = responseObject.Message.Text.Split(' ');
                var serviceName = splittedText[2];
                userParams = new Dictionary<string, string>() { { "serviceName", serviceName } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("RestartService", userParams);
            }
            catch (Exception e)
            {
                logs.Error("Exception in RestartService: ", e);
            }
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"دستور ریستارت سرویس به سرور ارسال شد.";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> KillProcess(User user, TelegramBotResponse responseObject)
        {
            try
            {
                var splittedText = responseObject.Message.Text.Split(' ');
                var processName = splittedText[2];
                userParams = new Dictionary<string, string>() { { "processName", processName } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("KillProcess", userParams);
            }
            catch (Exception e)
            {
                logs.Error("Exception in KillProcess: ", e);
            }
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"دستور توقف پروسس به سرور ارسال شد.";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> AdminHelp(User user, TelegramBotResponse responseObject)
        {
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-وضعیت ویندوز سرویس ها");
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(2, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"شما به عنوان مدیر عضو سیستم نوتیفیکیشن دهناد هستید.
            برای استارت یا استاپ یا ریستارت و یا توقف یک پروسس به صورت زیر عمل کنید
            start service servicename
            stop service servicename
            restart service servicename
            kill process processname";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

        public static async Task<TelegramBotResponse> MemberHelp(User user, TelegramBotResponse responseObject)
        {
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"شما عضو سیستم نوتیفیکیشن دهناد هستید.";
            responseObject.OutPut.Add(outputItem);
            return responseObject;
        }

    }
}
