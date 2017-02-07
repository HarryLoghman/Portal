using SharedLibrary.Models;
using BimeIranLibrary.Models;
using System;
using System.Text.RegularExpressions;

namespace BimeIranLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void ReceivedMessage(MessageObject message, Service service)
        {
            //System.Diagnostics.Debugger.Launch();
            var content = message.Content;
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                {
                    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    if (user != null && user.DeactivationDate == null)
                    {
                        message.Content = content;
                        ContentManager.HandleContent(message, service, user, messagesTemplate);
                        return;
                    }
                }
                var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    if (message.IsReceivedFromIntegratedPanel)
                    {
                        message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
                        message.SubUnSubType = 2;
                    }
                    else
                    {
                        message.SubUnSubMoMssage = message.Content;
                        message.SubUnSubType = 1;
                    }
                }
                var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                {
                    Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                    Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    ContentManager.DeleteFromSinglechargeQueue(message.MobileNumber);
                    //var userLevel = ContentManager.GetUserLevel(subsciber.Id);
                    //if (userLevel < 3)
                    //    ContentManager.ChangeUserLevel(subsciber.Id, 1);
                    ServiceHandler.CancelUserInstallments(message.MobileNumber);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                    message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                    MessageHandler.InsertMessageToQueue(message);
                    return;
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                    ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                }
                
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    message.Content = content;
                    ContentManager.HandleContent(message, service, subsciber, messagesTemplate);
                }
                return;
            }
            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }
            message.Content = content;
            ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
        }
    }
}