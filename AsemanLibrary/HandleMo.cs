using SharedLibrary.Models;
using AsemanLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

namespace AsemanLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static Task<bool> ReceivedMessage(MessageObject message, Service service)
        {
            bool isSucceeded = true;
            //System.Diagnostics.Debugger.Launch();
            using (var entity = new AsemanEntities())
            {
                var content = message.Content;
                message.ServiceCode = service.ServiceCode;
                message.ServiceId = service.Id;
                var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                int isCampaignActive = 0;
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    isCampaignActive = Convert.ToInt32(campaign.Value);
                var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
                if (isInBlackList == true)
                    isCampaignActive = (int)CampaignStatus.Deactive;
                List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);

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
                    message = SharedLibrary.MessageHandler.SendServiceSubscriptionHelp(entity, imiChargeCodes, message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                else if (((message.Content.Length == 7 || message.Content.Length == 8 || message.Content.Length == 9 || message.Content == message.ShortCode || message.Content.Length == 2) && message.Content.All(char.IsDigit)) || message.Content.Contains("25000") || message.Content.ToLower().Contains("abc"))
                {
                    if (message.Content.Contains("25000"))
                        message.Content = "25000";
                    var logId = MessageHandler.OtpLog(message.MobileNumber, "request", message.Content);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                    MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                    if (result.Status != "SUCCESS-Pending Confirmation")
                    {
                        if (result.Status == "Error" || result.Status == "Exception")
                            isSucceeded = false;
                        message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                        MessageHandler.InsertMessageToQueue(message);
                    }
                    else
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                        {
                            SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                        }
                    }
                    return isSucceeded;
                }
                //else if (message.Content.ToLower().Contains("abc")) //Otp Help
                //{
                //    var mobile = message.MobileNumber;
                //    var singleCharge = new Singlecharge();
                //    var imiChargeCode = new ImiChargeCode();
                    //singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, message);
                //    if (singleCharge != null && singleCharge.DateCreated.AddMinutes(5) > DateTime.Now)
                //    {
                //        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                //        message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                //        //message = SharedLibrary.MessageHandler.SendServiceOTPRequestExists(entity, imiChargeCodes, message, messagesTemplate);
                //        MessageHandler.InsertMessageToQueue(message);
                //        return;
                //    }
                //    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, "Telepromo");
                //    message = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                //    //message = SharedLibrary.MessageHandler.SendServiceOTPHelp(entity, imiChargeCodes, message, messagesTemplate);
                //    //MessageHandler.InsertMessageToQueue(message);
                //    message.Price = 0;
                //    message.MobileNumber = mobile;
                //    singleCharge = new Singlecharge();
                //    await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, message, serviceAdditionalInfo);
                //    return;
                //}
                else if (message.Content.Length == 4 && message.Content.All(char.IsDigit) && !message.ReceivedFrom.Contains("Register"))
                {
                    var confirmCode = message.Content;
                    var logId = MessageHandler.OtpLog(message.MobileNumber, "confirm", confirmCode);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
                    MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                    if (result.Status == "Error" || result.Status == "Exception")
                        isSucceeded = false;
                    else if (result.Status.ToString().Contains("NOT FOUND IN LAST 5MINS") || result.Status == "No Otp Request Found")
                    {
                        var logId2 = MessageHandler.OtpLog(message.MobileNumber, "request", message.Content);
                        var result2 = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                        MessageHandler.OtpLogUpdate(logId2, result2.Status.ToString());
                        message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
                    }
                    else if (result.Status.ToString().Contains("PIN DOES NOT MATCH"))
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
                    }
                    return isSucceeded;
                }

                var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
                var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

                if ((isUserWantsToUnsubscribe == true || message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
                {
                    SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
                    return isSucceeded;
                }

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
                        bool isSubscriberdVerified = SharedLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                        if (isSubscriberdVerified == false)
                        {
                            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                            message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
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

                    if (isCampaignActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
                    {
                        SharedLibrary.HandleSubscription.CampaignUniqueId(message.MobileNumber, service.Id);
                        subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                        string parentId = "1";
                        var subscriberInviterCode = SharedLibrary.HandleSubscription.IsSubscriberInvited(message.MobileNumber, service.Id);
                        if (subscriberInviterCode != "")
                        {
                            parentId = subscriberInviterCode;
                            SharedLibrary.HandleSubscription.AddReferral(subscriberInviterCode, subsciber.SpecialUniqueId);
                        }
                        var subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null)
                            subId = sub.SpecialUniqueId;
                        var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                        var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/aseman/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
                        if (result.description == "success")
                        {
                            if (parentId != "1")
                            {
                                var parentSubscriber = SharedLibrary.HandleSubscription.GetSubscriberBySpecialUniqueId(parentId);
                                if (parentSubscriber != null)
                                {
                                    var oldMobileNumber = message.MobileNumber;
                                    var oldSubId = message.SubscriberId;
                                    var newMessage = message;
                                    newMessage.MobileNumber = parentSubscriber.MobileNumber;
                                    newMessage.SubscriberId = parentSubscriber.Id;
                                    newMessage = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                                    newMessage.Content = messagesTemplate.Where(o => o.Title == "CampaignNotifyParentForNewReferral").Select(o => o.Content).FirstOrDefault();
                                    if (newMessage.Content.Contains("{REFERRALCODE}"))
                                    {
                                        newMessage.Content = message.Content.Replace("{REFERRALCODE}", parentSubscriber.SpecialUniqueId);
                                    }
                                    MessageHandler.InsertMessageToQueue(newMessage);
                                    message.MobileNumber = oldMobileNumber;
                                    message.SubscriberId = oldSubId;
                                }
                            }
                        }
                    }
                    else if ((isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        var subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                        {
                            subId = sub.SpecialUniqueId;
                            var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/aseman/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                        }
                    }

                    message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
                    if (message.Content.Contains("{REFERRALCODE}"))
                    {
                        var subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                            subId = sub.SpecialUniqueId;
                        message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                    }
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
                    if (isCampaignActive == (int)CampaignStatus.Active)
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    }
                    else
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                        else
                            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    }
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.SubscriberId = subscriber.Id;
                if (subscriber.DeactivationDate != null)
                {
                    if (isCampaignActive == (int)CampaignStatus.Active)
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
                    }
                    else
                    {
                        if (message.Content == null || message.Content == "" || message.Content == " ")
                            message = SharedLibrary.MessageHandler.EmptyContentWhenNotSubscribed(entity, imiChargeCodes, message, messagesTemplate);
                        else
                            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                    }
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                message.Content = content;
                ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
            }
            return isSucceeded;
        }

        public static Singlecharge ReceivedMessageForSingleCharge(MessageObject message, Service service)
        {
            //System.Diagnostics.Debugger.Launch();
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
                if (message.Content == null || message.Content == "" || message.Content == " ")
                    message = MessageHandler.EmptyContentWhenNotSubscribed(message, messagesTemplate);
                else
                    message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                if (message.Content == null || message.Content == "" || message.Content == " ")
                    message = MessageHandler.EmptyContentWhenNotSubscribed(message, messagesTemplate);
                else
                    message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.Content = content;
            singlecharge = ContentManager.HandleSinglechargeContent(message, service, subscriber, messagesTemplate);
            return singlecharge;
        }
    }
    public enum CampaignStatus
    {
        Deactive = 0,
        Active = 1,
        Suspend = 2
    }
}