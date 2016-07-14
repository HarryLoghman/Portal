using Portal.Models;
using System.Collections.Generic;

namespace Portal.Services.Danestan
{
    public class ContentManager
    {
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
        }
    }
}