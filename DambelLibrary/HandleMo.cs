using SharedLibrary.Models;
using DambelLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace DambelLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static bool ReceivedMessage(MessageObject message, vw_servicesServicesInfo service)
        {
            bool isSucceeded = true;
            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(service.ServiceCode))
            {
                var content = message.Content;
                var messagesTemplate = SharedLibrary.ServiceHandler.GetServiceMessagesTemplate(service);
                List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
                {
                    var verficationMessage = message.Content.Split('-');
                    message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
                    message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                else if (message.Content.ToLower() == "sendservicesubscriptionhelp")
                {
                    message = SharedLibrary.MessageHandler.SendServiceSubscriptionHelp(entity, message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }

                var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
                var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

                if (!message.ReceivedFrom.Contains("Notify") && (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true))
                    return isSucceeded;
                if (message.ReceivedFrom.Contains("Notify") && message.Content.ToLower() == "subscription")
                    isUserSendsSubscriptionKeyword = true;
                else if (message.ReceivedFrom.Contains("Notify") && message.Content.ToLower() == "unsubscription")
                    isUserWantsToUnsubscribe = true;

                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                {
                    if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                    {
                        var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                        if (user != null && user.DeactivationDate == null)
                        {
                            message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return isSucceeded;
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
                        ServiceHandler.CancelUserInstallments(message.MobileNumber);
                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                        return isSucceeded;
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    {
                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                        Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                    }
                    else
                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);

                    message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                if (subscriber == null)
                {
                    //message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    //MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.SubscriberId = subscriber.Id;
                if (subscriber.DeactivationDate != null)
                {
                    //message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    //MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.Content = content;
                ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
            }
            return isSucceeded;
        }
    }
}