using SharedLibrary.Models;
using TamlyLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TamlyLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static Task<bool> ReceivedMessage(MessageObject message, Service service)
        {
            bool isSucceeded = true;
            var content = message.Content;
            message.ServiceCode = service.ServiceCode;
            message.ServiceId = service.Id;
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            using (var entity = new TamlyEntities())
            {
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
                else if (message.Content == "00" || message.Content.Length == 7 || message.Content.Length == 8 || message.Content.Length == 9 || message.Content.ToLower().Contains("abc"))
                {
                    var logId = MessageHandler.OtpLog(message.MobileNumber, "request", message.Content);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                    MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                    if (result.Status != "SUCCESS-Pending Confirmation")
                    {
                        message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                        MessageHandler.InsertMessageToQueue(message);
                    }
                    return isSucceeded;
                }
                else if (message.Content.Length == 4 && message.Content.All(char.IsDigit) && !message.ReceivedFrom.Contains("Register"))
                {
                    var confirmCode = message.Content;
                    var logId = MessageHandler.OtpLog(message.MobileNumber, "confirm", confirmCode);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
                    MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                    return isSucceeded;
                }

                var isUserSendsSubscriptionKeyword = SharedLibrary.ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
                var isUserWantsToUnsubscribe = SharedLibrary.ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

                //if ((isUserWantsToUnsubscribe == true || message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
                //{
                //    SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
                //    return;
                //}

                if (!message.ReceivedFrom.Contains("IMI") && (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true))
                    return isSucceeded;
                if (message.ReceivedFrom.Contains("Register"))
                    isUserSendsSubscriptionKeyword = true;
                else if (message.ReceivedFrom.Contains("Unsubscribe"))
                    isUserWantsToUnsubscribe = true;

                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                {
                    //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                    //{
                    //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                    //    if (user != null && user.DeactivationDate == null)
                    //    {
                    //        message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                    //        MessageHandler.InsertMessageToQueue(message);
                    //        return;
                    //    }
                    //}
                    if (service.Enable2StepSubscription == true && isUserSendsSubscriptionKeyword == true)
                    {
                        bool isSubscriberdVerified = TamlyLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                        if (isSubscriberdVerified == false)
                        {
                            //message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                            //message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
                            //MessageHandler.InsertMessageToQueue(message);
                            return isSucceeded;
                        }
                    }
                    var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                    if (isUserSendsSubscriptionKeyword == true)
                    {
                        var oldService = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Tamly500");
                        var oldServiceSubscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, oldService.Id);
                        if (oldServiceSubscriber != null)
                        {
                            if (oldServiceSubscriber.DeactivationDate == null)
                            {
                                await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "-1");
                                return isSucceeded;
                            }
                        }
                    }
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
                    if (isUserWantsToUnsubscribe != true)
                        MessageHandler.InsertMessageToQueue(message);
                    //if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    //{
                    //    message.Content = content;
                    //    ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
                    //}
                    return isSucceeded;
                }
                var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                if (subscriber == null)
                {
                    var ser = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Tamly500");
                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, ser.Id);
                    if (sub != null)
                    {
                        if (sub.DeactivationDate == null)
                        {
                            if (message.Content == null || message.Content == "" || message.Content == " ")
                                message = SharedLibrary.MessageHandler.EmptyContentWhenSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                            else
                                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return isSucceeded;
                        }
                    }
                    if (message.Content == null || message.Content == "" || message.Content == " ")
                        message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                    else
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.SubscriberId = subscriber.Id;
                if (subscriber.DeactivationDate != null)
                {
                    var ser = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Tamly500");
                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, ser.Id);
                    if (sub != null)
                    {
                        if (sub.DeactivationDate == null)
                        {
                            if (message.Content == null || message.Content == "" || message.Content == " ")
                                message = SharedLibrary.MessageHandler.EmptyContentWhenSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                            else
                                message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                            MessageHandler.InsertMessageToQueue(message);
                            return isSucceeded;
                        }
                    }
                    if (message.Content == null || message.Content == "" || message.Content == " ")
                        message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                    else
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.Content = content;
                ContentManager.HandleContent(message, service, subscriber, messagesTemplate, imiChargeCodes);
            }
            return isSucceeded;
        }

        public static Singlecharge ReceivedMessageForSingleCharge(MessageObject message, Service service)
        {
            message.Content = message.Price.ToString();
            var content = message.Content;
            var singlecharge = new Singlecharge();
            if (message.Content.All(char.IsDigit))
            {
                var price = Convert.ToInt32(message.Content);
                var imiObject = MessageHandler.GetImiChargeObjectFromPrice(price, null);
                message.Content = imiObject.ChargeCode.ToString();
            }
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
                isUserSendsSubscriptionKeyword = true;
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                //{
                //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                //    if (user != null && user.DeactivationDate == null)
                //    {
                //        message.Content = content;
                //        singlecharge = ContentManager.HandleSinglechargeContent(message, service, user, messagesTemplate);
                //        return singlecharge;
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
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                }
                //message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                //MessageHandler.InsertMessageToQueue(message);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    message.Content = content;
                    singlecharge = ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
                }
                return singlecharge;
            }
            subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.Content = content;
            singlecharge = ContentManager.HandleSinglechargeContent(message, service, subscriber, messagesTemplate);
            return singlecharge;
        }
    }
}