using SharedLibrary.Models;
using System;
using System.Text.RegularExpressions;

namespace SepidRoodLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void ReceivedMessage(MessageObject message, Service service)
        {
            //System.Diagnostics.Debugger.Launch();
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

            if (message.Content == "9" || (isUserSendsSubscriptionKeyword == true && !message.ReceivedFrom.Contains("Notify")))
                return;
            if (!message.ReceivedFrom.Contains("Notify") && message.IsReceivedFromIntegratedPanel != true && isUserWantsToUnsubscribe == true)
                return;
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                {
                    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    if (user != null && user.DeactivationDate == null)
                    {
                        if (message.Content == "5" || message.Content == "22" || message.Content == "55" || message.Content == "9")
                            message.Content = "2";

                        if (message.Content == "22" || message.Content == "55")
                        {
                            ContentManager.HandleContent(message, service, user, messagesTemplate);
                            return;
                        }
                        else if (message.Content == "2" || message.Content == "5")
                        {
                            bool isSubscribed = false;
                            isSubscribed = ContentManager.IsUserAlreadySubscribedToKeyword("2", user.Id);
                            if (isSubscribed == false)
                                isSubscribed = ContentManager.IsUserAlreadySubscribedToKeyword("5", user.Id);

                            if (isSubscribed == true)
                            {
                                message = MessageHandler.SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(message, messagesTemplate);
                                MessageHandler.InsertMessageToQueue(message);
                                return;
                            }
                            else
                            {
                                ContentManager.HandleContent(message, service, user, messagesTemplate);
                                return;
                            }
                        }
                        else
                        {
                            message = MessageHandler.SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return;
                        }
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
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                {
                    Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                    Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                    var subscriberData = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                    if (message.Content == "5" || message.Content == "22" || message.Content == "55" || message.Content == "9")
                        message.Content = "2";
                    if (message.Content == "2" || message.Content == "5")
                    {
                        ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriberData.Id);
                        MessageObject lastEventbaseContent;
                        lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriberData, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, message.Content);
                        if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                            MessageHandler.InsertMessageToTimedTempQueue(lastEventbaseContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
                    }
                    else if (message.Content == "22" || message.Content == "55")
                    {
                        ContentManager.HandleContent(message, service, subscriberData, messagesTemplate);
                        return;
                    }
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                    ServiceHandler.RemoveSubscriberSubscriptionKeywords(subscriberId);
                }
                else
                {
                    var subscriberData = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                    Subscribers.SetIsSubscriberSendedOffReason(subscriberData.Id, false);
                    if (message.Content == "5" || message.Content == "22" || message.Content == "55" || message.Content == "9")
                        message.Content = "2";
                    if (message.Content == "2" || message.Content == "5")
                    {
                        ServiceHandler.AddSubscriberToSubscriptionKeywords(message, subscriberData.Id);
                        MessageObject lastEventbaseContent;
                        lastEventbaseContent = ContentManager.GetLastEventbaseContent(subscriberData, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend, message.Content);
                        if (lastEventbaseContent != null && lastEventbaseContent.Content != "" && lastEventbaseContent.Content != null)
                            MessageHandler.InsertMessageToTimedTempQueue(lastEventbaseContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
                    }
                    else if (message.Content == "22" || message.Content == "55")
                    {
                        ContentManager.HandleContent(message, service, subscriberData, messagesTemplate);
                        return;
                    }
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, message);
                MessageHandler.InsertMessageToQueue(message);
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
                var isSubscriberSendedOffReason = Subscribers.GetIsSubscriberSendedOffReason(subscriber.Id);
                if (Regex.IsMatch(message.Content, @"^[a-dA-D]+$") && !isSubscriberSendedOffReason)
                {
                    Subscribers.AddSubscriptionOffReasonPoint(subscriber, service.Id);
                    Subscribers.SetIsSubscriberSendedOffReason(subscriber.Id, true);
                    MessageHandler.SetOffReason(subscriber, message, messagesTemplate);
                }
                else
                {
                    message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                }
                return;
            }
            ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
        }
    }
}