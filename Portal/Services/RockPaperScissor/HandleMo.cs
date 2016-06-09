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
        public static void ReceivedMessage(Message message, Service serviceInfo)
        {
            using (var entity = new PortalEntities())
            {
                if (serviceInfo.OnKeywords.Contains(message.Content))
                {
                    var serviceStatusForSubscriberState = HandleSubscription.HandleSubscriptionContent(message, serviceInfo);
                    if (serviceStatusForSubscriberState == HandleSubscription.ServiceStatusForSubscriberState.Activated)
                        RpsSubscribers.AddSubscriberAdditionalInfo(message);
                    return;
                }
                var subscriber =
                    entity.Subscribers.FirstOrDefault(
                        o => o.MobileNumber == message.MobileNumber && o.ServiceId == message.ServiceId);
                if (subscriber == null)
                {
                    MessageHandler.InvalidContentWhenNotSubscribed(message, serviceInfo);
                    return;
                }
            }
        }
    }
}