using System;
using System.Linq;
using SharedLibrary.Models;
using DehnadNotificationService.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DehnadNotificationService
{
    public class BotManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task<TelegramBotResponse> NewlyStartedUser(Type entityType, User user, TelegramBotResponse responseObject)
        {
            if (user.MobileNumber != null)
            {
                if (responseObject.Message.Text == "IWantToBeAdmin!!")
                {
                    TelegramBotHelper.SaveLastStep(entityType, user, "Admin");
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Normal-");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList);
                    outputItem.Text = @"شما ادمین سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
                else if (responseObject.Message.Text == "SignMeToDehnadNotification")
                {
                    TelegramBotHelper.SaveLastStep(entityType, user, "Member");
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Normal-");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList);
                    outputItem.Text = @"شما عضو سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
            }
            else
            {
                if (responseObject.Message.Text == "IWantToBeAdmin!!")
                {
                    TelegramBotHelper.SaveLastStep(entityType, user, "Admin-MobileConfirmation");
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Contact-ثبت شماره موبایل");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList);
                    outputItem.Text = @"لطفا با استفاده از دکمه دریافت شماره، شماره خود را ثبت کنید.";
                    responseObject.OutPut.Add(outputItem);
                }
                else if (responseObject.Message.Text == "SignMeToDehnadNotification")
                {
                    TelegramBotHelper.SaveLastStep(entityType, user, "Member-MobileConfirmation");
                    var outputItem = new TelegramBotOutput();
                    var keyboardButtonsList = new List<string>();
                    keyboardButtonsList.Add("Contact-ثبت شماره موبایل");
                    outputItem.keyboard = TelegramBotHelper.GenerateKeybaord(1, 1, keyboardButtonsList);
                    outputItem.Text = @"شما عضو سیستم نوتیفیکیشن دهناد شدید.";
                    responseObject.OutPut.Add(outputItem);
                }
            }
            return responseObject;
        }

        public static async Task<TelegramBotResponse> ContactReceived(Type entityType, User user, TelegramBotResponse responseObject)
        {
            var mobileNumber = SharedLibrary.MessageHandler.ValidateNumber(responseObject.Message.Contact.PhoneNumber.Replace(" ", ""));
            string firstName = ""; string lastName = "";
            if (responseObject.Message.Contact.FirstName != null)
                firstName = responseObject.Message.Contact.FirstName;
            if (responseObject.Message.Contact.LastName != null)
                lastName = responseObject.Message.Contact.LastName;
            TelegramBotHelper.SaveContact(entityType, user, mobileNumber, firstName, lastName, responseObject.Message.Contact.UserId);
            if (user.LastStep.Contains("Admin"))
            {
                TelegramBotHelper.SaveLastStep(entityType, user, "Admin-Registered");
            }
            else if (user.LastStep.Contains("Member"))
            {
                TelegramBotHelper.SaveLastStep(entityType, user, "Member-Registered");
            }
            return responseObject;
        }

        public static async Task<TelegramBotResponse> AdminHelp(Type entityType, User user, TelegramBotResponse responseObject)
        {
            throw new NotImplementedException();
        }

        public static async Task<TelegramBotResponse> MemberHelp(Type entityType, User user, TelegramBotResponse responseObject)
        {
            throw new NotImplementedException();
        }
    }
}
