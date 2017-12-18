using SharedLibrary.Models;
using SoratyLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SoratyLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static async void ReceivedMessage(MessageObject message, Service service)
        {
            try
            {
                using (var entity = new SoratyEntities())
                {
                    string aggregatorName = "Hub";
                    message.ServiceCode = service.ServiceCode;
                    var content = message.Content;
                    var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                    List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();

                    var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
                    var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
                    if ((content == "9" || isUserWantsToUnsubscribe == true || isUserSendsSubscriptionKeyword == true) && message.IsReceivedFromIntegratedPanel != true && !message.ReceivedFrom.Contains("Portal"))
                        return;

                    if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
                    {
                        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
                    {
                        var verficationMessage = message.Content.Split('-');
                        message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
                        message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
                        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.Content.ToLower() == "sendservicesubscriptionhelp")
                    {
                        message = SharedLibrary.MessageHandler.SendServiceSubscriptionHelp(entity, imiChargeCodes, message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.Content == "22") //Otp Help
                    {
                        var mobile = message.MobileNumber;
                        var singleCharge = new SoratyLibrary.Models.Singlecharge();
                        var imiChargeCode = new ImiChargeCode();
                        singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, message);
                        if (singleCharge != null && singleCharge.DateCreated.AddMinutes(5) > DateTime.Now)
                        {
                            message = SharedLibrary.MessageHandler.SendServiceOTPRequestExists(entity, imiChargeCodes, message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return;
                        }

                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, aggregatorName);
                        message = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        message = SharedLibrary.MessageHandler.SendServiceOTPHelp(entity, imiChargeCodes, message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        message.Price = 5; //Hub Subscription is 5
                        message.MobileNumber = mobile;
                        await MessageHandler.HubOtpChargeRequest(message, serviceAdditionalInfo);
                        return;
                    }
                    else if (message.Content.ToLower().Contains("abc")) //Otp Help
                    {
                        var mobile = message.MobileNumber;
                        var singleCharge = new Singlecharge();
                        var imiChargeCode = new ImiChargeCode();
                        singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, message);
                        if (singleCharge != null && singleCharge.DateCreated.AddMinutes(5) > DateTime.Now)
                        {
                            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                            message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                            //message = SharedLibrary.MessageHandler.SendServiceOTPRequestExists(entity, imiChargeCodes, message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return;
                        }
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, "Hub");
                        message = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        //message = SharedLibrary.MessageHandler.SendServiceOTPHelp(entity, imiChargeCodes, message, messagesTemplate);
                        //MessageHandler.InsertMessageToQueue(message);
                        message.Price = 5; //Hub Subscription is 5
                        message.MobileNumber = mobile;
                        singleCharge = new Singlecharge();
                        await SharedLibrary.MessageSender.HubOtpChargeRequest(entity, singleCharge, message, serviceAdditionalInfo);
                        return;
                    }
                    else if (message.Content.Length == 4 && message.Content.All(char.IsDigit))
                    {
                        var singleCharge = new SoratyLibrary.Models.Singlecharge();
                        singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, message);
                        if (singleCharge != null)
                        {
                            var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, aggregatorName);
                            singleCharge = await SharedLibrary.MessageSender.HubOTPConfirm(entity, singleCharge, message, serviceAdditionalInfo, message.Content);
                        }
                        return;
                    }

                    if (content == "Subscription".ToLower())
                        isUserSendsSubscriptionKeyword = true;
                    else if (content == "Unsubscription".ToLower() || message.IsReceivedFromIntegratedPanel == true)
                        isUserWantsToUnsubscribe = true;

                    if (isUserWantsToUnsubscribe == true)
                        SharedLibrary.HandleSubscription.UnsubscribeUserFromHubService(service.Id, message.MobileNumber);

                    if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                    {
                        if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                        {
                            var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                            if (user != null && user.DeactivationDate == null)
                            {
                                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                                MessageHandler.InsertMessageToQueue(message);
                                return;
                            }
                        }
                        if (service.Enable2StepSubscription == true && isUserSendsSubscriptionKeyword == true)
                        {
                            string subscriberdUsedKeyword = SoratyLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                            if (subscriberdUsedKeyword == "")
                            {
                                //message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                                //message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
                                //MessageHandler.InsertMessageToQueue(message);
                                return;
                            }
                            else
                                content = message.Content = subscriberdUsedKeyword;
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
                        }
                        else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        {
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                            var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                            //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                            ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                        }
                        else
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);

                        message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                        MessageHandler.InsertMessageToQueue(message);
                        //if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        //{
                        //    message.Content = content;
                        //    ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
                        //}
                        return;
                    }
                    var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                    if (subscriber == null)
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                        else
                            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    message.SubscriberId = subscriber.Id;
                    if (subscriber.DeactivationDate != null)
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                        else
                            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    message.Content = content;
                    ContentManager.HandleContent(message, service, subscriber, messagesTemplate, imiChargeCodes);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Soraty ReceivedMessage:", e);
            }
        }
    }
}