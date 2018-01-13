using DehnadNotificationService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DehnadNotificationService
{
    public class TelegramBot
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly TelegramBotClient Bot = new TelegramBotClient(Properties.Settings.Default.BotId);
        public static Telegram.Bot.Types.User botInfo;
        public static bool StartBot()
        {
            bool isBotRunning = false;
            while (true)
            {
                try
                {
                    Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
                    Bot.OnMessage += BotOnMessageReceived;
                    Bot.OnMessageEdited += BotOnMessageReceived;
                    //Bot.OnInlineQuery += BotOnInlineQueryReceived;
                    Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
                    Bot.OnReceiveError += BotOnReceiveError;
                    botInfo = Bot.GetMeAsync().Result;
                    logs.Info(botInfo.Username + " started");
                    Bot.StartReceiving();
                    isBotRunning = true;
                }
                catch (Exception e)
                {
                    logs.Error("Exception In Starting Notification Telegram Bot:", e);
                    isBotRunning = false;
                    Thread.Sleep(60 * 1000);
                }
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            logs.Error(" BotOnReceiveError called: " + receiveErrorEventArgs.ApiRequestException);
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            logs.Info(" BotOnChosenInlineResultReceived called: " + $"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                Type entityType = typeof(NotificationEntities);
                Type userType = typeof(User);
                Type userMessageType = typeof(UserMessage);
                var responseObject = new TelegramBotResponse();
                responseObject.OutPut = new List<TelegramBotOutput>();
                responseObject.Message = messageEventArgs.Message;
                if (responseObject.Message == null) return;

                User user;
                using (var entity = new NotificationEntities())
                {
                    user = entity.Users.FirstOrDefault(o => o.ChatId == responseObject.Message.Chat.Id);
                }
                if (user == null)
                {
                    TelegramBotHelper.CreateNewUser(entityType, userType, responseObject.Message);
                    using (var entity = new NotificationEntities())
                    {
                        user = entity.Users.FirstOrDefault(o => o.ChatId == responseObject.Message.Chat.Id);
                    }
                }
                if (responseObject.Message.Type == MessageType.TextMessage)
                {
                    TelegramBotHelper.SaveMessage(entityType, userMessageType, user, responseObject.Message.Text);
                }
                if (user.LastStep == "Started")
                {
                    responseObject = await BotManager.NewlyStartedUser(entityType, user, responseObject);
                }
                else if (responseObject.Message.Text.Contains("MobileConfirmation") && responseObject.Message.Type == MessageType.ContactMessage)
                {
                    responseObject = await BotManager.ContactReceived(entityType, user, responseObject);
                }
                else if (responseObject.Message.Text.ToLower().Contains("help"))
                {
                    if (user.LastStep.Contains("Admin"))
                        responseObject = await BotManager.AdminHelp(entityType, user, responseObject);
                    else if (user.LastStep.Contains("Member"))
                        responseObject = await BotManager.MemberHelp(entityType, user, responseObject);
                }
                else if (user.LastStep == "Admin-Registered")
                {
                    responseObject = await BotManager.AdminHelp(entityType, user, responseObject);
                }
                else if (user.LastStep == "Member-Registered")
                {
                    responseObject = await BotManager.MemberHelp(entityType, user, responseObject);
                }
                else
                {
                    if (responseObject.Message.Text == "IWantToBeAdmin!!" || responseObject.Message.Text == "SignMeToDehnadNotification")
                    {
                        responseObject = await BotManager.NewlyStartedUser(entityType, user, responseObject);
                    }
                }
                foreach (var response in responseObject.OutPut)
                {
                    if (response.Text != null && response.Text != "")
                    {
                        if (response.keyboard != null)
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: response.keyboard);
                        else
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: new ReplyKeyboardRemove() { RemoveKeyboard = true });
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("error: " + e);
            }
        }
    }
}
