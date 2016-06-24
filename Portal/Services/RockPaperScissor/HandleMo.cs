using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Portal.Models;
using Portal.Shared;

namespace Portal.Services.RockPaperScissor
{
    public class HandleMo
    {
        public static void ReceivedMessage(Message message, Service service)
        {
            using (var entity = new DanestanEntities())
            {
                var messagesTemplate = Services.GetServiceMessagesTemplate();
            }
            using (var entity = new PortalEntities())
            {
                if (service.OnKeywords.Contains(message.Content))
                {
                    var serviceStatusForSubscriberState = HandleSubscription.HandleSubscriptionContent(message, service);
                    if (serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Activated)
                        RpsSubscribers.AddSubscriberAdditionalInfo(message);
                    return;
                }
                var subscriber = RpsSubscribers.GetSubscriber(message.MobileNumber, message.ServiceId, entity);

                if (subscriber == null)
                {
                    MessageHandler.InvalidContentWhenNotSubscribed(message, service);
                    return;
                }
                ContentManager.HandleContent(message, service, subscriber);
            }
        }
    }
}