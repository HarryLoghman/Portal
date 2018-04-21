using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadNotificationService.Models
{
    public class TelegramBotResponse
    {
        public List<TelegramBotOutput> OutPut { get; set; }
        public Telegram.Bot.Types.Message Message { get; set; }
    }

    public class TelegramBotOutput
    {
        public string Text { get; set; }
        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup keyboard { get; set; }
        public Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup inlineKeyboard { get; set; }
        public byte[] photo { get; set; }
        public string photoName { get; set; }
    }
}
