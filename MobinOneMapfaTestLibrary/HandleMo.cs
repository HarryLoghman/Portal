﻿using SharedLibrary.Models;
using MobinOneMapfaTestLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace MobinOneMapfaTestLibrary
{
    public class HandleMo:SharedShortCodeServiceLibrary.HandleMo
    {
        public HandleMo():base("MobinOneMapfaTest")
        {

        }
    //    static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    //    public async static void ReceivedMessage(MessageObject message, vw_servicesServicesInfo service)
    //    {
    //        try
    //        {
    //            logs.Error("mobilenumber:" + message.MobileNumber + " - " + "1");
    //            var content = message.Content;
    //            message.ServiceCode = service.ServiceCode;
    //            message.ServiceId = service.Id;
    //            var messagesTemplate = SharedLibrary.ServiceHandler.GetServiceMessagesTemplate(service);
    //            logs.Error("mobilenumber:" + message.MobileNumber + " - " + "2");
    //            using (var entity = new SharedLibrary.Models.ServiceModel.SharedServiceEntities(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service)))
    //            {
    //                int isCampaignActive = 0;
    //                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
    //                if (campaign != null)
    //                    isCampaignActive = Convert.ToInt32(campaign.Value);
    //                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "3");
    //                var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
    //                if (isInBlackList == true)
    //                    isCampaignActive = (int)CampaignStatus.Deactive;
    //                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "4");
    //                Type entityType = typeof(MobinOneMapfaTestEntities);
    //                Type ondemandType = typeof(OnDemandMessagesBuffer);
    //                List<SharedLibrary.Models.ServiceModel.ImiChargeCode> imiChargeCodes = SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity).ToList();
    //                message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    
    //                if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
    //                {
    //                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    return;
    //                }
    //                else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
    //                {
    //                    var verficationMessage = message.Content.Split('-');
    //                    message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
    //                    message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
    //                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    return;
    //                }
    //                else if (message.Content.ToLower() == "sendservicesubscriptionhelp")
    //                {
    //                    message = SharedShortCodeServiceLibrary.MessageHandler.SendServiceSubscriptionHelp(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service), message, messagesTemplate);
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    return;
    //                }
    //                else if (((message.Content.Length == 8 || message.Content == message.ShortCode || message.Content.Length == 2) && message.Content.All(char.IsDigit)) || message.Content.Contains("25000") || message.Content.ToLower().Contains("abc"))
    //                {
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "5");
    //                    if (message.Content.Contains("25000"))
    //                        message.Content = "25000";
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "6");
    //                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "7");
    //                    if (result.Status != "SUCCESS-Pending Confirmation")
    //                    {
    //                        message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
    //                        SharedLibrary.MessageHandler.InsertMessageToQueue(entityType, message, null, null, ondemandType);
    //                    }
    //                    else
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "8");
    //                        if (isCampaignActive == (int)CampaignStatus.Active)
    //                        {
    //                            SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
    //                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
    //                            SharedLibrary.MessageHandler.InsertMessageToQueue(entityType, message, null, null, ondemandType);
    //                        }
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "9");
    //                    }
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "10");
    //                    return;
    //                }
    //                //else if (message.Content.ToLower().Contains("abc")) //Otp Help
    //                //{
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "11");
    //                //    var mobile = message.MobileNumber;
    //                //    var singleCharge = new Singlecharge();
    //                //    var imiChargeCode = new ImiChargeCode();
    //                //    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, message);
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "12");
    //                //    if (singleCharge != null && singleCharge.DateCreated.AddMinutes(5) > DateTime.Now)
    //                //    {
    //                //        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "13");
    //                //        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
    //                //        message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
    //                //        MessageHandler.InsertMessageToQueue(message);
    //                //        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "14");
    //                //        return;
    //                //    }
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "15");
    //                //    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, "MobinOneMapfa");
    //                //    message = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "16");
    //                //    message.Price = 0;
    //                //    message.MobileNumber = mobile;
    //                //    singleCharge = new Singlecharge();
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "17");
    //                //    await SharedLibrary.MessageSender.MapfaOTPRequest(typeof(MobinOneMapfaTestEntities), singleCharge, message, serviceAdditionalInfo);
    //                //    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "18");
    //                //    return;
    //                //}
    //                else if (message.Content.Length == 4 && message.Content.All(char.IsDigit))
    //                {
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "19");
    //                    var confirmCode = message.Content;
    //                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "20");
    //                    return;
    //                }
    //                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "21");
    //                var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
    //                var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
    //                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "22");
    //                //UNCOMMENT BELOW LINE!!!!
    //                //if ((isUserWantsToUnsubscribe == true || message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
    //                //{
    //                //    SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
    //                //    return;
    //                //}

    //                if (!message.ReceivedFrom.Contains("IMI") && (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true))
    //                    return;

    //                if (message.ReceivedFrom.Contains("Register"))
    //                    isUserSendsSubscriptionKeyword = true;
    //                else if (message.ReceivedFrom.Contains("Unsubscribe") || message.ReceivedFrom.Contains("Unsubscription"))
    //                    isUserWantsToUnsubscribe = true;
    //                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "23");
    //                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
    //                {
    //                    //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
    //                    //{
    //                    //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //                    //    if (user != null && user.DeactivationDate == null)
    //                    //    {
    //                    //        message = MessageHandler.SendServiceHelp(message, messagesTemplate);
    //                    //        MessageHandler.InsertMessageToQueue(message);
    //                    //        return;
    //                    //    }
    //                    //}
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "24");
    //                    if (service.Enable2StepSubscription == true && isUserSendsSubscriptionKeyword == true)
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "25");
    //                        bool isSubscriberdVerified = SharedLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
    //                        if (isSubscriberdVerified == false)
    //                        {
    //                            message = SharedShortCodeServiceLibrary.MessageHandler.InvalidContentWhenNotSubscribed(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service), message, messagesTemplate);
    //                            message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
    //                            MessageHandler.InsertMessageToQueue(message);
    //                            return;
    //                        }
    //                    }

    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "26");
    //                    var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "27");
    //                    if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
    //                    {
    //                        if (message.IsReceivedFromIntegratedPanel)
    //                        {
    //                            message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
    //                            message.SubUnSubType = 2;
    //                        }
    //                        else
    //                        {
    //                            message.SubUnSubMoMssage = message.Content;
    //                            message.SubUnSubType = 1;
    //                        }
    //                    }
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "28");
    //                    var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "29");
    //                    if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "30");
    //                        Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
    //                        Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
    //                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
    //                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "31");
    //                    }
    //                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "32");
    //                        ContentManager.DeleteFromSinglechargeQueue(message.MobileNumber);
    //                        ServiceHandler.CancelUserInstallments(message.MobileNumber);
    //                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
    //                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "33");
    //                    }
    //                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "34");
    //                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
    //                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
    //                        Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
    //                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "35");
    //                    }
    //                    else
    //                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);

    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "36");
    //                    if (isCampaignActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
    //                    {
    //                        SharedLibrary.HandleSubscription.CampaignUniqueId(message.MobileNumber, service.Id);
    //                        subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //                        string parentId = "1";
    //                        var subscriberInviterCode = SharedLibrary.HandleSubscription.IsSubscriberInvited(message.MobileNumber, service.Id);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "37");
    //                        if (subscriberInviterCode != "")
    //                        {
    //                            parentId = subscriberInviterCode;
    //                            SharedLibrary.HandleSubscription.AddReferral(subscriberInviterCode, subsciber.SpecialUniqueId);
    //                        }
    //                        var subId = "1";
    //                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "38");
    //                        if (sub != null)
    //                            subId = sub.SpecialUniqueId;
    //                        var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "39");
    //                        var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/MobinOneMapfaTest/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "40");
    //                        if (result.description == "success")
    //                        {
    //                            logs.Error("mobilenumber:" + message.MobileNumber + " - " + "41");
    //                            if (parentId != "1")
    //                            {
    //                                var parentSubscriber = SharedLibrary.HandleSubscription.GetSubscriberBySpecialUniqueId(parentId);
    //                                logs.Error("mobilenumber:" + message.MobileNumber + " - " + "42");
    //                                if (parentSubscriber != null)
    //                                {
    //                                    var oldMobileNumber = message.MobileNumber;
    //                                    var oldSubId = message.SubscriberId;
    //                                    var newMessage = message;
    //                                    newMessage.MobileNumber = parentSubscriber.MobileNumber;
    //                                    newMessage.SubscriberId = parentSubscriber.Id;
    //                                    newMessage = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
    //                                    newMessage.Content = messagesTemplate.Where(o => o.Title == "CampaignNotifyParentForNewReferral").Select(o => o.Content).FirstOrDefault();
    //                                    if (newMessage.Content.Contains("{REFERRALCODE}"))
    //                                    {
    //                                        newMessage.Content = message.Content.Replace("{REFERRALCODE}", parentSubscriber.SpecialUniqueId);
    //                                    }
    //                                    MessageHandler.InsertMessageToQueue(newMessage);
    //                                    message.MobileNumber = oldMobileNumber;
    //                                    message.SubscriberId = oldSubId;
    //                                }
    //                            }
    //                        }
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "43");
    //                    }
    //                    else if ((isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
    //                    {
    //                        logs.Error("mobilenumber:" + message.MobileNumber + " - " + "44");
    //                        var subId = "1";
    //                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
    //                        if (sub != null && sub.SpecialUniqueId != null)
    //                        {
    //                            subId = sub.SpecialUniqueId;
    //                            var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
    //                            logs.Error("mobilenumber:" + message.MobileNumber + " - " + "45");
    //                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/MobinOneMapfaTest/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
    //                            logs.Error("mobilenumber:" + message.MobileNumber + " - " + "46");
    //                        }
    //                    }
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "47");
    //                    message.Content = SharedShortCodeServiceLibrary.MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "48");
    //                    if (message.Content.Contains("{REFERRALCODE}"))
    //                    {
    //                        var subId = "1";
    //                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
    //                        if (sub != null && sub.SpecialUniqueId != null)
    //                            subId = sub.SpecialUniqueId;
    //                        message.Content = message.Content.Replace("{REFERRALCODE}", subId);
    //                    }
    //                    logs.Error("mobilenumber:" + message.MobileNumber + " - " + "49");
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    //if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
    //                    //{
    //                    //    message.Content = content;
    //                    //    ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
    //                    //}
    //                    return;
    //                }
    //                var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

    //                if (subscriber == null)
    //                {
    //                    if (isCampaignActive == (int)CampaignStatus.Active)
    //                    {
    //                        if (message.Content == null || message.Content == "" || message.Content == " ")
    //                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
    //                        else
    //                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
    //                    }
    //                    else
    //                    {
    //                        if (message.Content == null || message.Content == "" || message.Content == " ")
    //                            message = SharedShortCodeServiceLibrary.MessageHandler.EmptyContentWhenNotSubscribed(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service), message, messagesTemplate);
    //                        else
    //                            message = SharedShortCodeServiceLibrary.MessageHandler.InvalidContentWhenNotSubscribed(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service),message, messagesTemplate);
    //                    }
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    return;
    //                }
    //                message.SubscriberId = subscriber.Id;
    //                if (subscriber.DeactivationDate != null)
    //                {
    //                    if (isCampaignActive == (int)CampaignStatus.Active)
    //                    {
    //                        if (message.Content == null || message.Content == "" || message.Content == " ")
    //                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
    //                        else
    //                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
    //                    }
    //                    else
    //                    {
    //                        if (message.Content == null || message.Content == "" || message.Content == " ")
    //                            message = SharedShortCodeServiceLibrary.MessageHandler.EmptyContentWhenNotSubscribed(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service), message, messagesTemplate);
    //                        else
    //                            message = SharedShortCodeServiceLibrary.MessageHandler.InvalidContentWhenNotSubscribed(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service),message, messagesTemplate);
    //                    }
    //                    MessageHandler.InsertMessageToQueue(message);
    //                    return;
    //                }
    //                message.Content = content;
    //                SharedShortCodeServiceLibrary. ContentManager.HandleContent(SharedLibrary.HelpfulFunctions.fnc_getConnectionStringInAppConfig(service),message, service, subscriber, messagesTemplate,imiChargeCodes);
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            logs.Error("Exception in ReceivedMessage: ", e);
    //        }
    //    }

    //    //public static Singlecharge ReceivedMessageForSingleCharge(MessageObject message, Service service)
    //    //{
    //    //    //System.Diagnostics.Debugger.Launch();
    //    //    var content = message.Content;
    //    //    var singlecharge = new Singlecharge();
    //    //    if (message.Content.All(char.IsDigit))
    //    //    {
    //    //        var price = Convert.ToInt32(message.Content);
    //    //        var imiObject = MessageHandler.GetImiChargeObjectFromPrice(price, null);
    //    //        message.Content = imiObject.ChargeCode.ToString();
    //    //    }
    //    //    var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
    //    //    var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
    //    //    var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
    //    //    var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //    //    if (subscriber == null)
    //    //        isUserSendsSubscriptionKeyword = true;
    //    //    if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
    //    //    {
    //    //        //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
    //    //        //{
    //    //        //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //    //        //    if (user != null && user.DeactivationDate == null)
    //    //        //    {
    //    //        //        message.Content = content;
    //    //        //        singlecharge = ContentManager.HandleSinglechargeContent(message, service, user, messagesTemplate);
    //    //        //        return singlecharge;
    //    //        //    }
    //    //        //}
    //    //        var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
    //    //        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
    //    //        {
    //    //            if (message.IsReceivedFromIntegratedPanel)
    //    //            {
    //    //                message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
    //    //                message.SubUnSubType = 2;
    //    //            }
    //    //            else
    //    //            {
    //    //                message.SubUnSubMoMssage = message.Content;
    //    //                message.SubUnSubType = 1;
    //    //            }
    //    //        }
    //    //        var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
    //    //        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
    //    //        {
    //    //            Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
    //    //            Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
    //    //            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
    //    //        }
    //    //        else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
    //    //        {
    //    //            var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
    //    //            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
    //    //        }
    //    //        else
    //    //        {
    //    //            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
    //    //            var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
    //    //            //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
    //    //        }
    //    //        //message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
    //    //        //MessageHandler.InsertMessageToQueue(message);
    //    //        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
    //    //        {
    //    //            message.Content = content;
    //    //            singlecharge = ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
    //    //        }
    //    //        return singlecharge;
    //    //    }
    //    //    subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

    //    //    if (subscriber == null)
    //    //    {
    //    //        if (message.Content == null || message.Content == "" || message.Content == " ")
    //    //            message = MessageHandler.EmptyContentWhenNotSubscribed(message, messagesTemplate);
    //    //        else
    //    //            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
    //    //        MessageHandler.InsertMessageToQueue(message);
    //    //        return null;
    //    //    }
    //    //    message.SubscriberId = subscriber.Id;
    //    //    if (subscriber.DeactivationDate != null)
    //    //    {
    //    //        if (message.Content == null || message.Content == "" || message.Content == " ")
    //    //            message = MessageHandler.EmptyContentWhenNotSubscribed(message, messagesTemplate);
    //    //        else
    //    //            message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
    //    //        MessageHandler.InsertMessageToQueue(message);
    //    //        return null;
    //    //    }
    //    //    message.Content = content;
    //    //    singlecharge = ContentManager.HandleSinglechargeContent(message, service, subscriber, messagesTemplate);
    //    //    return singlecharge;
    //    //}
    }
    //public enum CampaignStatus
    //{
    //    Deactive = 0,
    //    Active = 1,
    //    Suspend = 2
    //}
}