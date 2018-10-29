using SharedLibrary.Models;
using SharedShortCodeServiceLibrary.SharedModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedShortCodeServiceLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static Task<bool> ReceivedMessage(MessageObject message, Service service)
        {
            string connectionStringeNameInAppConfig = service.ServiceCode + "Entities";
            bool isSucceeded = true;
            var content = message.Content;
            message.ServiceCode = service.ServiceCode;
            message.ServiceId = service.Id;
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate(connectionStringeNameInAppConfig);
            using (var entity = new ShortCodeServiceEntities(connectionStringeNameInAppConfig))
            {
                int isCampaignActive = 0;
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                if (campaign != null)
                    isCampaignActive = Convert.ToInt32(campaign.Value);
                var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
                if (isInBlackList == true)
                    isCampaignActive = (int)CampaignStatus.Deactive;
                List<ImiChargeCode> imiChargeCodes = ServiceHandler.GetImiChargeCodes(connectionStringeNameInAppConfig).ToList();
                //mycomment : List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                #region FromApp
                if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
                {
                    message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    return isSucceeded;
                }
                #endregion
                #region AppVerification
                else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
                {
                    var verficationMessage = message.Content.Split('-');
                    message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
                    message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
                    message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    return isSucceeded;
                }
                #endregion
                #region App Subscription Help
                else if (message.ReceivedFrom.Contains("Verification") && message.Content == "sendservicesubscriptionhelp")
                {
                    message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    message.Content = messagesTemplate.Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    return isSucceeded;
                }
                #endregion
                #region otp Request
                else if (((message.Content.Length == 7 || message.Content.Length == 8 || message.Content.Length == 9 || message.Content == message.ShortCode || message.Content.Length == 2) && message.Content.All(char.IsDigit)) || message.Content.Contains("25000") || message.Content.ToLower().Contains("abc"))
                {
                    if (message.Content.Contains("25000"))
                        message.Content = "25000";
                    var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                    MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
                    if (result.Status == "User already subscribed")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestForAlreadySubsceribed").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    }
                    else if (result.Status == "Otp request already exists for this subscriber")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "OtpRequestExistsForThisSubscriber").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    }
                    else if (result.Status != "SUCCESS-Pending Confirmation")
                    {
                        if (result.Status == "Error" || result.Status == "Exception")
                            isSucceeded = false;
                        else
                        {
                            message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                        }
                    }
                    else
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                        {
                            SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                        }
                    }
                    return isSucceeded;
                }
                #endregion
                #region otp confirm
                else if (message.Content.Length == 4 && message.Content.All(char.IsDigit) && !message.ReceivedFrom.Contains("Register"))
                {
                    var confirmCode = message.Content;
                    var logId = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "confirm", confirmCode);
                    var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
                    MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId, result.Status.ToString());
                    if (result.Status == "Error" || result.Status == "Exception")
                        isSucceeded = false;
                    else if (result.Status.ToString().Contains("NOT FOUND IN LAST 5MINS") || result.Status == "No Otp Request Found")
                    {
                        var logId2 = MessageHandler.OtpLog(connectionStringeNameInAppConfig, message.MobileNumber, "request", message.Content);
                        var result2 = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                        MessageHandler.OtpLogUpdate(connectionStringeNameInAppConfig, logId2, result2.Status.ToString());
                        message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    }
                    else if (result.Status.ToString().Contains("PIN DOES NOT MATCH"))
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    }
                    return isSucceeded;
                }
                #endregion

                var isUserSendsSubscriptionKeyword = SharedLibrary.ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
                var isUserWantsToUnsubscribe = SharedLibrary.ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);

                #region (user wants to unsubscribe or message came from integrated panel) and it did not come from IMI UnsubscribeUserFromTelepromoService
                if ((isUserWantsToUnsubscribe == true || message.IsReceivedFromIntegratedPanel == true) && !message.ReceivedFrom.Contains("IMI"))
                {
                    SharedLibrary.HandleSubscription.UnsubscribeUserFromTelepromoService(service.Id, message.MobileNumber);
                    return isSucceeded;
                }
                #endregion
                #region (user wants to unsub or sub) and message did not come from IMI
                if ((isUserWantsToUnsubscribe == true || isUserSendsSubscriptionKeyword == true) && !message.ReceivedFrom.Contains("IMI"))
                    return isSucceeded;
                #endregion

                if (message.ReceivedFrom.Contains("Register"))
                    isUserSendsSubscriptionKeyword = true;
                else if (message.ReceivedFrom.Contains("Unsubscribe") || message.ReceivedFrom.Contains("Unsubscription"))
                    isUserWantsToUnsubscribe = true;

                #region user wants subscribe or unsubscribe
                if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                {
                    if (isUserSendsSubscriptionKeyword == true)
                    {
                        #region unsub user from old corresponding services
                        var oldServicesStr = service.oldServiceCodes;
                        if (!string.IsNullOrEmpty(oldServicesStr))
                        {
                            int i;
                            string[] oldServicesArr = oldServicesStr.Split(';');
                            for (i = 0; i <= oldServicesArr.Length - 1; i++)
                            {

                                var oldService = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(oldServicesArr[i]);
                                var oldServiceSubscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, oldService.Id);
                                if (oldServiceSubscriber != null && oldServiceSubscriber.DeactivationDate == null)
                                {
                                    await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(oldService.ServiceCode, message.MobileNumber, "-1");
                                }

                            }
                        }
                        #endregion

                        #region Enable2StepSubscription=true if user is verified return true
                        if (service.Enable2StepSubscription == true)
                        {
                            bool isSubscriberdVerified = ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                            if (isSubscriberdVerified == false)
                            {
                                return isSucceeded;
                            }
                        }
                        #endregion
                    }

                    var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);

                    #region received content is sub/unsub/renewal set message.SubUnSubMoMssage and message.SubUnSubType
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
                    #endregion

                    var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                    #region subscriber additionalIndfo/subscriber points/add to single charge
                    if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                    {
                        Subscribers.CreateSubscriberAdditionalInfo(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
                        Subscribers.AddSubscriptionPointIfItsFirstTime(connectionStringeNameInAppConfig, message.MobileNumber, service.Id);
                        message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        ContentManager.DeleteFromSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber);
                        ServiceHandler.CancelUserInstallments(connectionStringeNameInAppConfig, message.MobileNumber);
                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                        message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    {
                        message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                        Subscribers.SetIsSubscriberSendedOffReason(connectionStringeNameInAppConfig, subscriberId.Value, false);
                        ContentManager.AddSubscriberToSinglechargeQueue(connectionStringeNameInAppConfig, message.MobileNumber, content);
                    }
                    else
                        message = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
                    #endregion

                    #region campaignActive and user is active or renewal
                    if (isCampaignActive == (int)CampaignStatus.Active && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
                    {
                        //create a special uniqueId for the mobilenumber in serviceid and save to DB
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
                        if (!string.IsNullOrEmpty(service.referralUrl))
                        {
                            //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha)); 
                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
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
                                        newMessage = MessageHandler.SetImiChargeInfo(connectionStringeNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                                        newMessage.Content = messagesTemplate.Where(o => o.Title == "CampaignNotifyParentForNewReferral").Select(o => o.Content).FirstOrDefault();
                                        if (newMessage.Content.Contains("{REFERRALCODE}"))
                                        {
                                            newMessage.Content = message.Content.Replace("{REFERRALCODE}", parentSubscriber.SpecialUniqueId);
                                        }
                                        MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, newMessage);
                                        message.MobileNumber = oldMobileNumber;
                                        message.SubscriberId = oldSubId;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region campaignActive and user is deactivated
                    else if ((isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        var subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                        {
                            subId = sub.SpecialUniqueId;
                            var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);

                            //var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));

                            if (!string.IsNullOrEmpty(service.referralUrl))
                            {
                                var result = await SharedLibrary.UsefulWebApis.DanoopReferral(service.referralUrl + (service.referralUrl.EndsWith("/") ? "" : "/") + "unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                            }
                        }
                    }
                    #endregion


                    message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
                    if (message.Content.Contains("{REFERRALCODE}"))
                    {
                        var subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                            subId = sub.SpecialUniqueId;
                        message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                    }

                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);

                    return isSucceeded;
                }
                #endregion

                var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                #region there is no such subscriber check campaign status
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
                            message = MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
                    }
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    return isSucceeded;
                }
                #endregion

                message.SubscriberId = subscriber.Id;

                #region subscriber exists but deactivated check campaign status
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
                            message = MessageHandler.InvalidContentWhenNotSubscribed(connectionStringeNameInAppConfig, message, messagesTemplate);
                    }
                    MessageHandler.InsertMessageToQueue(connectionStringeNameInAppConfig, message);
                    return isSucceeded;
                }
                #endregion 
                message.Content = content;
                ContentManager.HandleContent(connectionStringeNameInAppConfig, message, service, subscriber, messagesTemplate, imiChargeCodes);
            }
            return isSucceeded;
        }
    }
    public enum CampaignStatus
    {
        Deactive = 0,
        Active = 1,
        Suspend = 2
    }
}

