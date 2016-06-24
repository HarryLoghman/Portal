using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Shared;

namespace Portal.Services.Danestan
{
    public class HandleMo
    {
        public static void ReceivedMessage(Message message, Service service)
        {
            var messagesTemplate = Services.GetServiceMessagesTemplate();
            var isUserWantsToUnsubscribe = Services.CheckIsUserWantsToUnsubscribe(message.Content);
            if (service.OnKeywords.Contains(message.Content) || isUserWantsToUnsubscribe == true)
            {
                var serviceStatusForSubscriberState = HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                ServiceMessage.InsertMessageToQueue(message);
                return;
            }
            var subscriber = Subscribers.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                ServiceMessage.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                return;
            }
            ContentManager.HandleContent(message, service, subscriber);

        }
    }
}