using SharedLibrary.Models;
using PorShetabLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Data.Entity;

namespace PorShetabLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool DeleteFromSinglechargeQueue(string mobileNumber)
        {
            bool succeed = false;
            try
            {
                using (var entity = new PorShetabEntities())
                {
                    var singlechargeQueue = entity.SinglechargeWaitings.Where(o => o.MobileNumber == mobileNumber).ToList();
                    foreach (var item in singlechargeQueue)
                    {
                        entity.Entry(item).State = EntityState.Deleted;
                    }
                    entity.SaveChanges();
                    succeed = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in DeleteFromSinglechargeQueue: ", e);
                while (succeed == false)
                {
                    succeed = DeleteFromSinglechargeQueue(mobileNumber);
                }
            }
            return succeed;
        }

        public static bool AddSubscriberToSinglechargeQueue(string mobileNumber, string content)
        {
            try
            {
                using (var entity = new PorShetabEntities())
                {
                    //var chargeCode = Convert.ToInt32(content);
                    //var imichargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.ChargeCode == chargeCode);
                    //if (imichargeCode == null)
                    //    return false;
                    var singlechargeQueueItem = new SinglechargeWaiting();
                    singlechargeQueueItem.MobileNumber = mobileNumber;
                    singlechargeQueueItem.Price = 9000;
                    singlechargeQueueItem.DateAdded = DateTime.Now;
                    singlechargeQueueItem.PersianDateAdded = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    singlechargeQueueItem.IsLastDayWarningSent = false;
                    entity.SinglechargeWaitings.Add(singlechargeQueueItem);
                    entity.SaveChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddSubscriberToSinglechargeQueue: ", e);
                return false;
            }
        }

        public static async void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            try
            {
                using (var entity = new PorShetabEntities())
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
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
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    if (message.Content == null || message.Content.Trim() == "")
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive)
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                        string subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                            subId = sub.SpecialUniqueId;
                        if (message.Content.Contains("{REFERRALCODE}"))
                        {
                            message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                        }
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.Content.ToLower() == "h")
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive)
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignHContent").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "HContent").Select(o => o.Content).FirstOrDefault();

                        string subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null && sub.SpecialUniqueId != null)
                            subId = sub.SpecialUniqueId;
                        if (message.Content.Contains("{REFERRALCODE}"))
                        {
                            message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                        }
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.Content.ToLower() == "r")
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend)
                        {
                            string subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null && sub.SpecialUniqueId != null)
                            {
                                subId = sub.SpecialUniqueId;
                                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                                dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/status.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                                string n = result.n.ToString();
                                string m = result.m.ToString();
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignSubscriberStatus").Select(o => o.Content).FirstOrDefault();
                                message.Content = message.Content.Replace("{m}", m);
                                message.Content = message.Content.Replace("{n}", n);
                                if (message.Content.Contains("{REFERRALCODE}"))
                                {
                                    message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                                }
                            }
                            else
                            {
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberStatus").Select(o => o.Content).FirstOrDefault();
                                MessageHandler.InsertMessageToQueue(message);
                            }
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberStatus").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        return;
                    }
                    else if (message.Content.ToLower() == "s")
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveAndReferalDeactive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend)
                        {
                            string subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null)
                            {
                                var sha = SharedLibrary.Security.GetSha256Hash("score" + message.MobileNumber);
                                dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/score.php", string.Format("number={1}&kc={2}", subId, message.MobileNumber, sha));
                                if (result.status.ToString() == "ok")
                                {
                                    message.Content = messagesTemplate.Where(o => o.Title == "SubscriberScore").Select(o => o.Content).FirstOrDefault();
                                    message.Content = message.Content.Replace("{SCORE}", result.description.ToString());
                                    if (message.Content.Contains("{REFERRALCODE}"))
                                    {
                                        message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                                    }
                                    MessageHandler.InsertMessageToQueue(message);
                                }
                            }
                        }
                        return;
                    }
                    else if (message.Content.ToLower() == "q")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "QContent").Select(o => o.Content).FirstOrDefault();
                        string subId = "1";
                        var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                        if (sub != null)
                        {
                            var sha = SharedLibrary.Security.GetSha256Hash("score" + message.MobileNumber);
                            dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/score.php", string.Format("number={1}&kc={2}", subId, message.MobileNumber, sha));
                            if (result.status.ToString() == "ok")
                            {
                                message.Content = message.Content.Replace("{SCORE}", result.description.ToString());
                                if (message.Content.Contains("{REFERRALCODE}"))
                                {
                                    message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                                }
                            }
                        }
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    else if (message.Content.ToLower() == "g")
                    {
                        if (isCampaignActive == (int)CampaignStatus.MatchActiveReferralActive || isCampaignActive == (int)CampaignStatus.MatchActiveReferralSuspend)
                        {
                            string subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null && sub.SpecialUniqueId != null)
                            {
                                subId = sub.SpecialUniqueId;
                                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                                dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/porshetab/getCharge.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
                                var chargesList = new List<string>();
                                foreach (var item in result.charges)
                                {
                                    chargesList.Add(item.ToString());
                                }
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignSubscriberCharges").Select(o => o.Content).FirstOrDefault();
                                message.Content = message.Content.Replace("{count}", chargesList.Count.ToString());
                                if (chargesList.Count > 0)
                                {
                                    var text = "";
                                    foreach (var item in chargesList)
                                    {
                                        text = text + item + Environment.NewLine;
                                    }
                                    message.Content = message.Content.Replace("{charges}", text);
                                }
                                else
                                    message.Content = message.Content.Replace("{charges}", "");

                                if (message.Content.Contains("{REFERRALCODE}"))
                                {
                                    message.Content = message.Content.Replace("{REFERRALCODE}", subId);
                                }
                            }
                            else
                            {
                                message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberStatus").Select(o => o.Content).FirstOrDefault();
                                MessageHandler.InsertMessageToQueue(message);
                            }
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberCharges").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        return;
                    }
                    else if (message.Content == "77" || message.Content.ToLower() == "m")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "Content77Response").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    if (message.Content == "77" || message.Content.ToLower() == "m")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "Content77Response").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    if (!service.OnKeywords.Contains(message.Content))
                    {
                        //if (subscriber.ActivationDate.Value.AddMinutes(1) < DateTime.Now)
                        //{
                        //    message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                        //    MessageHandler.InsertMessageToQueue(message);
                        //}
                        return;
                    }

                    var isUserAlreadyInSinglechargeQueue = IsUserAlreadyInSinglechargeQueue(message.MobileNumber);
                    if (isUserAlreadyInSinglechargeQueue == true)
                    {
                        message = MessageHandler.UserHasActiveSinglecharge(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }
                    var isUserAlreadyChargedThisMonth = IsUserAlreadyChargedThisMonth(message.MobileNumber);
                    if (isUserAlreadyChargedThisMonth == true)
                    {
                        message = MessageHandler.UserHasActiveSinglecharge(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return;
                    }

                    var isSuccessful = AddSubscriberToSinglechargeQueue(message.MobileNumber, message.Content);
                    if (isSuccessful == true)
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(message);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in HandleContent: ", e);
            }
        }

        private static bool IsUserAlreadyChargedThisMonth(string mobileNumber)
        {
            try
            {
                using (var entity = new PorShetabEntities())
                {
                    var lastMonth = DateTime.Today.AddDays(-30);
                    var isUserAlreadychargedThisMonth = entity.Singlecharges.FirstOrDefault(o => o.MobileNumber == mobileNumber && (DbFunctions.TruncateTime(o.DateCreated) <= DateTime.Now.Date && DbFunctions.TruncateTime(o.DateCreated) >= lastMonth));
                    if (isUserAlreadychargedThisMonth == null)
                        return false;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in IsUserAlreadyChargedThisMonth: ", e);
            }
            return true;
        }

        private static bool IsUserAlreadyInSinglechargeQueue(string mobileNumber)
        {
            try
            {
                using (var entity = new PorShetabEntities())
                {
                    var isUserAlreadyInSinglechargeQueue = entity.SinglechargeWaitings.Where(o => o.MobileNumber == mobileNumber);
                    if (isUserAlreadyInSinglechargeQueue == null)
                        return false;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in IsUserAlreadyInSinglechargeQueue: ", e);
            }
            return true;
        }
    }
}