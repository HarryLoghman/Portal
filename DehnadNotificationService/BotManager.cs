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

        public static async Task<TelegramBotResponse> ContactReceived( User user, TelegramBotResponse responseObject)
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

        public static async Task<TelegramBotResponse> AdminHelp(User user, TelegramBotResponse responseObject)
        {
            var outputItem = new TelegramBotOutput();
            var keyboardButtonsList = new List<string>();
            keyboardButtonsList.Add("Normal-راهنما");
            outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList, true, true);
            outputItem.Text = @"شما به عنوان مدیر عضو سیستم نوتیفیکیشن دهناد هستید.";
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
