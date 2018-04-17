using SharedLibrary.Models;
using DefendIranLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DefendIranLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static void ReceivedMessage(MessageObject message, Service service)
        {
            try
            {
                using (var entity = new DefendIranEntities())
                {
                    bool isCampaignActive = false;
                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        isCampaignActive = campaign.Value == "0" ? false : true;
                    var content = message.Content;
                    Type entityType = typeof(DefendIranEntities);
                    Type ondemandType = typeof(OnDemandMessagesBuffer);
                    message.ServiceCode = service.ServiceCode;
                    message.ServiceId = service.Id;
                    var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                    List<ImiChargeCode> imiChargeCodes = ((IEnumerable)SharedLibrary.ServiceHandler.GetServiceImiChargeCodes(entity)).OfType<ImiChargeCode>().ToList();
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);

                    var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
                    var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);

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
                    else if (((message.Content.Length == 8 || message.Content == message.ShortCode) && message.Content.All(char.IsDigit)) || message.Content.ToLower().Contains("abc"))
                    {
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(message.ServiceCode, "Hub");
                        var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                        if (result.Status != "SUCCESS-Pending Confirmation")
                        {
                            message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                            SharedLibrary.MessageHandler.InsertMessageToQueue(entityType, message, null, null, ondemandType);
                        }
                        else
                        {
                            SharedLibrary.HandleSubscription.AddToTempReferral(message.MobileNumber, service.Id, message.Content);
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOtpFromUniqueId").Select(o => o.Content).FirstOrDefault();
                            SharedLibrary.MessageHandler.InsertMessageToQueue(entityType, message, null, null, ondemandType);
                        }
                        return;
                    }
                    else if (message.Content.Length == 4 && message.Content.All(char.IsDigit))
                    {
                        var confirmCode = message.Content;
                        var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
                        return;
                    }

                    if (message.ReceivedFrom.Contains("Notify-Register"))
                        isUserSendsSubscriptionKeyword = true;
                    else if (message.ReceivedFrom.Contains("Notify-Unsubscription") || message.IsReceivedFromIntegratedPanel == true)
                        isUserWantsToUnsubscribe = true;

                    //if (isUserWantsToUnsubscribe == true)
                    //    SharedLibrary.HandleSubscription.UnsubscribeUserFromHubService(service.Id, message.MobileNumber);

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

                        if (isCampaignActive == true && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
                        {
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
                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/sub.php", string.Format("code={0}&number={1}&parent_code={2}&kc={3}", subId, message.MobileNumber, parentId, sha));
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
                        else if (isCampaignActive == true && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                        {
                            var subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null)
                                subId = sub.SpecialUniqueId;
                            var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/unsub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                        }

                        message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState, isCampaignActive);
                        if (message.Content.Contains("{REFERRALCODE}"))
                        {
                            var subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null)
                                subId = sub.SpecialUniqueId;
                            message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                        }
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
                        if (isCampaignActive == true)
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
                        return;
                    }
                    message.SubscriberId = subscriber.Id;
                    if (subscriber.DeactivationDate != null)
                    {
                        if (isCampaignActive == true)
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
                        return;
                    }
                    message.Content = content;
                    ContentManager.HandleContent(message, service, subscriber, messagesTemplate, imiChargeCodes);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in DefendIran ReceivedMessage:", e);
            }
        }
    }
}