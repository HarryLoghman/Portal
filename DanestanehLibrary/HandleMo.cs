using SharedLibrary.Models;
using DanestanehLibrary.Models;
using System;
using System.Text.RegularExpressions;

namespace DanestanehLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void ReceivedMessage(MessageObject message, Service service)
        {
            //System.Diagnostics.Debugger.Launch();
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                //{
                //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                //    if (user != null && user.DeactivationDate == null)
                //    {
                //        message = MessageHandler.SendContentWhenUserIsSubscribedAndWantsToSubscribeAgain(message, messagesTemplate);
                //        MessageHandler.InsertMessageToQueue(message);
                //        return;
                //    }
                //}
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
                    var messageWithContent = ContentManager.LeveledAutochargeMessage(subsciber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend);
                    if (messageWithContent != null && messageWithContent.Content != "" && messageWithContent.Content != null)
                        MessageHandler.InsertMessageToTimedTempQueue(messageWithContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                    ContentManager.RemoveLeveledAutochargeContentDailyListForSubscriber(subsciber.Id);
                }
                else
                {
                    var messageWithContent = ContentManager.LeveledAutochargeMessage(subsciber, service.Id, message.ShortCode, null, message.AggregatorId, SharedLibrary.MessageHandler.MessageType.OnDemand, SharedLibrary.MessageHandler.ProcessStatus.TryingToSend);
                    if (messageWithContent != null && messageWithContent.Content != "" && messageWithContent.Content != null)
                        MessageHandler.InsertMessageToTimedTempQueue(messageWithContent, SharedLibrary.MessageHandler.MessageType.OnDemand);
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