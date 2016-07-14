using Portal.Models;
using Portal.Shared;

namespace Portal.Services.Danestan
{
    public class HandleMo
    {
        public static void ReceivedMessage(MessageObject message, Service service)
        {
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            if (service.OnKeywords.Contains(message.Content) || isUserWantsToUnsubscribe == true)
            {
                var serviceStatusForSubscriberState = HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                if (serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    message.SubUnSubMoMssage = message.Content;
                    message.SubUnSubType = 1;
                }
                message.Content = Shared.MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                message = MessageHandler.SetImiChargeInfo(message, 0, 21);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }
            var subscriber = Shared.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                return;
            }
            message.SubscriberId = subscriber.Id;
            ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
        }
    }
}