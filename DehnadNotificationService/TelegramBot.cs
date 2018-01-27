using DehnadNotificationService.Models;
using Newtonsoft.Json;
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
        public static TelegramBotClient Bot;
        public static Telegram.Bot.Types.User botInfo;
        public static bool StartBot()
        {
            bool isBotRunning = false;
            
            Bot = new TelegramBotClient(Properties.Settings.Default.BotId);
            while (true)
            {
                if (isBotRunning == false)
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
                var userParams = new Dictionary<string, string>();
                var responseObject = new DehnadNotificationService.Models.TelegramBotResponse();
                responseObject.OutPut = new List<DehnadNotificationService.Models.TelegramBotOutput>();
                responseObject.Message = messageEventArgs.Message;
                responseObject.Message.Text = TelegramBotHelper.NormalizeContent(messageEventArgs.Message.Text);
                var serializedresponseObject = JsonConvert.SerializeObject(responseObject);

                if (responseObject.Message == null) return;

                User user;
                userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() } };
                user = await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("GetUserWebService", userParams);
                if (user == null)
                {
                    userParams = new Dictionary<string, string>() { { "stringedTelegramBotResponse", serializedresponseObject } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("CreateNewUser", userParams);

                    userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    user = await SharedLibrary.UsefulWebApis.NotificationBotApi<User>("GetUserWebService", userParams);
                }
                if (responseObject.Message.Type == MessageType.TextMessage)
                {
                    userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() }, { "text", responseObject.Message.Text } };
                    SharedLibrary.UsefulWebApis.NotificationBotApi<User>("SaveMessageWebService", userParams);
                }
                if (user.LastStep == "Started")
                {
                    responseObject = await BotManager.NewlyStartedUser(user, responseObject);
                }
                else if (user.LastStep.Contains("MobileConfirmation") && responseObject.Message.Type == MessageType.ContactMessage)
                {
                    responseObject = await BotManager.ContactReceived(user, responseObject);
                }
                else if (responseObject.Message.Text.ToLower().Contains("help"))
                {
                    if (user.LastStep.Contains("Admin"))
                        responseObject = await BotManager.AdminHelp(user, responseObject);
                    else if (user.LastStep.Contains("Member"))
                        responseObject = await BotManager.MemberHelp(user, responseObject);
                }
                else if (user.LastStep == "Admin-Registered")
                {
                    responseObject = await BotManager.AdminHelp(user, responseObject);
                }
                else if (user.LastStep == "Member-Registered")
                {
                    responseObject = await BotManager.MemberHelp(user, responseObject);
                }
                else
                {
                    if (responseObject.Message.Text == "IWantToBeAdmin!!" || responseObject.Message.Text == "SignMeToDehnadNotification")
                    {
                        responseObject = await BotManager.NewlyStartedUser( user, responseObject);
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

        public static async void TelegramSendMessage(TelegramBotResponse message, UserType userType)
        {
            try
            {
                using (var entity = new NotificationEntities())
                {
                    entity.Configuration.AutoDetectChangesEnabled = false;
                    List<long> chatIds = new List<long>();
                    if (userType == UserType.AdminOnly)
                        chatIds = entity.Users.Where(o => o.LastStep.Contains("Admin")).Select(o => o.ChatId).ToList();
                    else if (userType == UserType.MemberOnly)
                        chatIds = entity.Users.Where(o => o.LastStep.Contains("Member")).Select(o => o.ChatId).ToList();
                    else
                        chatIds = entity.Users.Where(o => o.LastStep.Contains("Admin") || o.LastStep.Contains("Member")).Select(o => o.ChatId).ToList();
                    foreach (var chatId in chatIds)
                    {
                        foreach (var response in message.OutPut)
                        {
                            if (response.Text != null && response.Text != "")
                            {
                                if (response.keyboard != null)
                                    await Bot.SendTextMessageAsync(chatId, response.Text, replyMarkup: response.keyboard);
                                else
                                    await Bot.SendTextMessageAsync(chatId, response.Text, replyMarkup: new ReplyKeyboardRemove() { RemoveKeyboard = true });
                            }
                            Service.SaveSendedMessageToDB(response.Text, "telegram", userType.ToString(), chatId, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelegramSendMessage: " + e);
            }
        }

    }

    public enum UserType
    {
        AdminOnly = 1,
        MemberOnly = 2,
        All = 3,
    }
}
