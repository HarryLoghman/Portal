using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;
using SepidRoodLibrary.Models;
using System.Text.RegularExpressions;

namespace SepidRoodLibrary
{
    public class ContentManager
    {
        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            if (message.Content == "2")
            {
                message = ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriber.Id);
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "5")
            {
                message = ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriber.Id);
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "22")
            {
                message = ContentWith2000Price(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "55")
            {
                message = ContentWith5000Price(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
            }
            else if (message.Content == "")
            {
                message = MessageHandler.InvalidContentWhenSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
            }
        }

        public static MessageObject ContentWith2000Price(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 2000, 0, null);
            message.Content = messagesTemplate.Where(o => o.Title == "ContentWith2000Price").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject ContentWith5000Price(MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            message = MessageHandler.SetImiChargeInfo(message, 5000, 0, null);
            message.Content = messagesTemplate.Where(o => o.Title == "ContentWith5000Price").Select(o => o.Content).FirstOrDefault();
            return message;
        }
    }
}
