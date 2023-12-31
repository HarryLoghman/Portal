﻿using DehnadNotificationService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
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
            string receivedmessageId = "";
            try
            {
                var userParams = new Dictionary<string, string>();
                var responseObject = new DehnadNotificationService.Models.TelegramBotResponse();
                responseObject.OutPut = new List<DehnadNotificationService.Models.TelegramBotOutput>();
                responseObject.Message = messageEventArgs.Message;
                responseObject.Message.Text = TelegramBotHelper.NormalizeContent(messageEventArgs.Message.Text);
                var serializedresponseObject = JsonConvert.SerializeObject(responseObject);
                if (responseObject.Message == null) return;

                Models.User user;
                userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() } };
                user = await SharedLibrary.UsefulWebApis.NotificationBotApi<Models.User>("GetUserWebService", userParams);
                if (user == null)
                {
                    userParams = new Dictionary<string, string>() { { "stringedTelegramBotResponse", serializedresponseObject } };
                    await SharedLibrary.UsefulWebApis.NotificationBotApi<Models.User>("CreateNewUser", userParams);

                    userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() } };
                    user = await SharedLibrary.UsefulWebApis.NotificationBotApi<Models.User>("GetUserWebService", userParams);
                }
                if (responseObject.Message.Type == MessageType.Text)
                {
                    userParams = new Dictionary<string, string>() { { "chatId", responseObject.Message.Chat.Id.ToString() }, { "text", responseObject.Message.Text }, { "channel", "telegram" } };
                    receivedmessageId =  await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("SaveMessageWebService", userParams);
                }
                if (user.LastStep == "Started")
                {
                    responseObject = await BotManager.NewlyStartedUser(user, responseObject);
                }
                else if (user.LastStep.Contains("MobileConfirmation") && responseObject.Message.Type == MessageType.Contact)
                {
                    responseObject = await BotManager.ContactReceived(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text == "وضعیت ویندوز سرویس ها")
                {
                    responseObject = await BotManager.SerivcesStatus(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text.ToLower().Contains("start service"))
                {
                    responseObject = await BotManager.StartService(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text.ToLower().Contains("stop service"))
                {
                    responseObject = await BotManager.StopService(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text.ToLower().Contains("restart service"))
                {
                    responseObject = await BotManager.RestartService(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text.ToLower().Contains("kill process"))
                {
                    responseObject = await BotManager.KillProcess(user, responseObject);
                }
                else if (user.LastStep.Contains("Admin") && responseObject.Message.Text.ToLower().Contains(" info"))
                {
                    responseObject = await BotManager.ServiceInfo(user, responseObject);
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
                        responseObject = await BotManager.NewlyStartedUser(user, responseObject);
                    }
                }
                foreach (var response in responseObject.OutPut)
                {
                    if (response.Text != null && response.Text != "")
                    {
                        if (response.photoName != null)
                        {
                            using (Stream stream = new MemoryStream(response.photo))
                            {
                                var file = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, response.photoName);
                                //file.Content = stream;
                                //file.Filename = response.photoName;
                                await Bot.SendPhotoAsync(responseObject.Message.Chat.Id, file, file.FileName);
                            }
                        }
                        if (response.keyboard != null && response.keyboard != null)
                        {
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: response.keyboard);
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, "در صورت نیاز به برگشت به منوی قبل از دکمه پایین صفحه استفاده نمایید.", replyMarkup: response.inlineKeyboard);
                        }
                        else if (response.keyboard != null)
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: response.keyboard);
                        else if (response.inlineKeyboard != null)
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: response.inlineKeyboard);
                        else
                            await Bot.SendTextMessageAsync(responseObject.Message.Chat.Id, response.Text, replyMarkup: new ReplyKeyboardRemove() {});
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("error: " + e);
            }
            if (receivedmessageId != "")
            {
                var userParams = new Dictionary<string, string>() { { "messageId", receivedmessageId } };
                await SharedLibrary.UsefulWebApis.NotificationBotApi<string>("ChangeReceivedMessageStatusToProcessed", userParams);
            }
        }

        public static void SaveTelegramMessageToQueue(string message, UserType userType)
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
                        var tMessage = new SentMessage();
                        tMessage.Channel = "telegram";
                        tMessage.TelegramKeyboardData = null;
                        tMessage.Content = message;
                        tMessage.DateCreated = DateTime.Now;
                        tMessage.IsSent = false;
                        tMessage.ChatId = chatId;
                        tMessage.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                        tMessage.UserType = userType.ToString();
                        entity.SentMessages.Add(tMessage);
                    }
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SaveTelegramMessageToQueue: " + e);
            }
        }

        public static async void TelegramSendMessage()
        {
            List<long> messageIdsThatSended = new List<long>();
            List<SentMessage> messagesToSend = new List<SentMessage>();
            try
            {
                if (Properties.Settings.Default.UseWebServiceForDbOperations == false)
                {
                    using (var entity = new NotificationEntities())
                    {
                        entity.Configuration.AutoDetectChangesEnabled = false;
                        messagesToSend = entity.SentMessages.Where(o => o.IsSent == false && o.Channel == "telegram").ToList();
                    }
                }
                else
                {
                    var userParams = new Dictionary<string, string>() { { "channel", "telegram" } };
                    messagesToSend = await SharedLibrary.UsefulWebApis.NotificationBotApi<List<SentMessage>>("GetUnsendMessages", userParams);
                }
                foreach (var message in messagesToSend)
                {
                    if (message.Content != null && message.Content != "")
                    {
                        if (message.TelegramKeyboardData != null)
                        {
                            //await Bot.SendTextMessageAsync(message.ChatId, message.Content, replyMarkup: response.keyboard);
                        }
                        else
                            await Bot.SendTextMessageAsync(message.ChatId, message.Content, Telegram.Bot.Types.Enums.ParseMode.Html, replyMarkup: new ReplyKeyboardRemove() {});
                    }
                    messageIdsThatSended.Add(message.Id);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TelegramSendMessage: " + e);
            }
            if (messageIdsThatSended.Count > 0)
                Service.ChangeMessageStatusToSended(messageIdsThatSended);
        }
    }

    public enum UserType
    {
        AdminOnly = 1,
        MemberOnly = 2,
        All = 3,
    }
}
