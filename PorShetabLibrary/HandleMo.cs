using SharedLibrary.Models;
using PorShetabLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PorShetabLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task<bool> ReceivedMessage(MessageObject message, Service service)
        {
            bool isSucceeded = true;
            using (var entity = new PorShetabEntities())
            {
                var content = message.Content;
                int isCampaignActive = 0;
                int isMatchActive = 0;
                var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                var match = entity.Settings.FirstOrDefault(o => o.Name == "match");
                if (campaign != null)
                    isCampaignActive = Convert.ToInt32(campaign.Value);
                if (match != null)
                    isMatchActive = Convert.ToInt32(match.Value);
                var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
                if (isInBlackList == true)
                    isCampaignActive = (int)CampaignStatus.MatchAndReferalDeactive;
                var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
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
                    message = SharedLibrary.MessageHandler.SendServiceSubscriptionHelp(entity, imiChargeCodes, message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                    return isSucceeded;
                }
                else if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive && message.Content.Length == 8 && message.Content.All(char.IsDigit))
                {
                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                    var sha = SharedLibrary.Security.GetSha256Hash("parent" + message.MobileNumber);
                    dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/parent.php", string.Format("code={0}&parent_code={1}&number={2}&kc={3}", sub.SpecialUniqueId, message.Content, message.MobileNumber, sha));
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                    message.Content = "";
                    if (result.status.ToString() == "ok")
                        message.Content = messagesTemplate.Where(o => o.Title == "ParentReferralCodeExists").Select(o => o.Content).FirstOrDefault();
                    else
                        message.Content = messagesTemplate.Where(o => o.Title == "ParentReferralCodeNotExists").Select(o => o.Content).FirstOrDefault();
                    if (message.Content != "")
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
                    }
                    else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                    {
                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                        ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                    }
                    else
                        message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);

                    if ((isCampaignActive == (int)CampaignStatus.MatchActiveAndReferalDeactive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive) && (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal))
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive)
                        {
                            var specialUniqueId = SharedLibrary.HandleSubscription.CampaignUniqueId(message.MobileNumber, service.Id);
                            var subId = specialUniqueId;
                            var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);

                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/sub.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                            if (result.description == "success")
                            {
                            }
                        }
                        else if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend || isCampaignActive == (int)CampaignStatus.MatchActiveAndReferalDeactive)
                        {
                            var sha = SharedLibrary.Security.GetSha256Hash("match" + message.MobileNumber);
                            var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/sub.php", string.Format("number={0}&kc={1}", message.MobileNumber, sha));
                            if (result.description == "success")
                            {
                            }
                        }
                    }
                    else if ((isCampaignActive == (int)CampaignStatus.MatchActiveAndReferalDeactive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend) && serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                    {
                        var sha = SharedLibrary.Security.GetSha256Hash("match" + message.MobileNumber);
                        var result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/unsub.php", string.Format("number={0}&kc={1}", message.MobileNumber, sha));
                        if (result.description == "success")
                        {
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
                    if (serviceStatusForSubscriberState != SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
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
    public enum CampaignStatus
    {
        MatchAndReferalDeactive = 0,
        MatchActiveAndReferalDeactive = 1,
        MatchActiveReferralActive = 2,
        MatchActiveReferralSuspend = 3
    }
}