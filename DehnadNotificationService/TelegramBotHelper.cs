using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace DehnadNotificationService
{
    public class TelegramBotHelper
    {
        public static ReplyKeyboardMarkup GenerateKeybaord(int rows, int columns, List<string> buttons, bool resizeKeyboard, bool oneTimeKeywboard)
        {
            var keyboardButtons = new List<List<KeyboardButton>>();
            var i = 0;
            for (int row = 1; row <= rows; row++)
            {
                var rowButton = new List<KeyboardButton>();
                for (int column = 1; column <= columns; column++)
                {
                    string selectedButton = buttons[i];
                    bool isLocation = false;
                    bool isContact = false;
                    if (selectedButton.StartsWith("Location"))
                    {
                        selectedButton = selectedButton.Replace("Location-", "");
                        isLocation = true;
                    }
                    else if (selectedButton.StartsWith("Contact"))
                    {
                        selectedButton = selectedButton.Replace("Contact-", "");
                        isContact = true;
                    }
                    else
                    {
                        selectedButton = selectedButton.Replace("Normal-", "");
                    }
                    var button = new KeyboardButton(selectedButton);
                    button.Text = selectedButton;
                    if (isContact)
                        button.RequestContact = true;
                    else if (isLocation)
                        button.RequestLocation = true;
                    rowButton.Add(button);
                    i++;
                }
                keyboardButtons.Add(rowButton);
            }

            var buttonsArray = keyboardButtons.Select(row => row.ToArray()).ToArray();
            var keyboard = new ReplyKeyboardMarkup(buttonsArray);
            keyboard.ResizeKeyboard = resizeKeyboard;
            keyboard.OneTimeKeyboard = oneTimeKeywboard;
            return keyboard;

            //var categories = new[] { "skills", "about me" };
            //var buttons = categories.Select(category => new[] { new KeyboardButton(category) })
            //    .ToArray();
            //var replyMarkup = new ReplyKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup GenerateInlineKeybaord(Dictionary<string,string> buttons)
        {
            var buttonsArray = new InlineKeyboardCallbackButton[buttons.Keys.Count];
            int i = 0;
            foreach (var item in buttons.Keys)
            {
                buttonsArray[i] = new InlineKeyboardCallbackButton(item, buttons[item]);
                i++;
            }
            InlineKeyboardMarkup menu = new InlineKeyboardMarkup(buttonsArray);
            return menu;
        }


        public static void SaveLastStep(Type entityType, dynamic user, string text)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                user.LastStep = text;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }

        }

        public static void SaveMobileNumber(Type entityType, dynamic user, string mobile)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                user.MobileNumber = mobile;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
        }

        public static void SaveContact(Type entityType, dynamic user, string mobile, string firstName, string lastName, long userId)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                user.MobileNumber = mobile;
                if (firstName != "")
                    user.Firstname = firstName;
                if (lastName != "")
                    user.Lastname = lastName;
                user.UserId = userId;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
        }

        public static void SaveContact(Type entityType, dynamic user, string mobile)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                user.MobileNumber = mobile;
                entity.Entry(user).State = System.Data.Entity.EntityState.Modified;
                entity.SaveChanges();
            }
        }

        public static void SaveMessage(Type entityType, Type userMessageType, dynamic user, string text)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                dynamic userMessage = Activator.CreateInstance(userMessageType);
                userMessage.ChatId = user.ChatId;
                userMessage.DateReceived = DateTime.Now;
                userMessage.PersianDateReceived = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                userMessage.Message = text;
                userMessage.Channel = "Telegram";
                entity.UserMessages.Add(userMessage);
                entity.SaveChanges();
            }
        }

        public static void CreateNewUser(Type entityType, Type userType, Message message, string promotedServiceCode = null)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                var parameters = message.Text.Replace("/start", "").Trim();
                if (parameters != null && parameters != "")
                {
                    //Get Params
                }
                dynamic newUser = Activator.CreateInstance(userType);
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

        public static string NormalizeContent(string content)
        {
            if (content == null)
                content = "";
            else
            {
                content = content.Trim();
                content = Regex.Replace(content, @"\s+", " ");
                content = content.Replace('ك', 'ک');
                content = content.Replace('ي', 'ی');
                content = content.Replace("‏۱", "1");
                content = content.Replace('۱', '1');
                content = content.Replace('١', '1');
                content = content.Replace('٢', '2');
                content = content.Replace('۲', '2');
                content = content.Replace('۳', '3');
                content = content.Replace('٣', '3');
                content = content.Replace("‏۳", "3");
                content = content.Replace("‏‏٣", "3");
                content = content.Replace("‏۴", "4");
                content = content.Replace('۴', '4');
                content = content.Replace('٤', '4');
                content = content.Replace("‏۵", "5");
                content = content.Replace('۵', '5');
                content = content.Replace('٥', '5');
                content = content.Replace('۶', '6');
                content = content.Replace("‏۶", "6");
                content = content.Replace('٦', '6');
                content = content.Replace('٧', '7');
                content = content.Replace('۷', '7');
                content = content.Replace('٨', '8');
                content = content.Replace('۸', '8');
                content = content.Replace('۹', '9');
                content = content.Replace('٩', '9');
                content = content.Replace('٠', '0');
                content = content.Replace('۰', '0');
            }
            return content;
        }
    }
}
