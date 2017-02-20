using SharedLibrary.Models;
using Tabriz2018Library.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Tabriz2018Library
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
                        message = MessageHandler.SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                }
                if (service.Enable2StepSubscription == true && isUserSendsSubscriptionKeyword == true)
                {
                    bool isSubscriberdVerified = SharedLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                    if (isSubscriberdVerified == false)
                    {
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                        message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
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
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    ServiceHandler.RemoveSubscriberMobiles(subscriberId);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                }
                message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }
            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                if (message.Content == "101")
                    message.Content = messagesTemplate.Where(o => o.Title == "101Content").Select(o => o.Content).FirstOrDefault();
                else if (message.Content == "102")
                    message.Content = messagesTemplate.Where(o => o.Title == "102Content").Select(o => o.Content).FirstOrDefault();
                else if (message.Content == "103")
                    message.Content = messagesTemplate.Where(o => o.Title == "103Content").Select(o => o.Content).FirstOrDefault();
                else
                    message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                var isSubscriberSendedOffReason = Subscribers.GetIsSubscriberSendedOffReason(subscriber.Id);
                if (Regex.IsMatch(message.Content, @"^[a-dA-D]+$") && !isSubscriberSendedOffReason)
                {
                    Subscribers.AddSubscriptionOffReasonPoint(subscriber, service.Id);
                    Subscribers.SetIsSubscriberSendedOffReason(subscriber.Id, true);
                    MessageHandler.SetOffReason(subscriber, message, messagesTemplate);
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    if (message.Content == "101")
                        message.Content = messagesTemplate.Where(o => o.Title == "101Content").Select(o => o.Content).FirstOrDefault();
                    else if (message.Content == "102")
                        message.Content = messagesTemplate.Where(o => o.Title == "102Content").Select(o => o.Content).FirstOrDefault();
                    else if (message.Content == "103")
                        message.Content = messagesTemplate.Where(o => o.Title == "103Content").Select(o => o.Content).FirstOrDefault();
                    else
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                }
                return;
            }
            ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
        }
    }
}