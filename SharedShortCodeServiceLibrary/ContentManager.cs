using SharedLibrary.Models;
using SharedShortCodeServiceLibrary.SharedModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Data.Entity;
using static SharedShortCodeServiceLibrary.HandleMo;

namespace SharedShortCodeServiceLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static Singlecharge HandleSinglechargeContent(string connectionStringNameInAppConfig, MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            Singlecharge singlecharge = new Singlecharge();
            message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            try
            {
                var content = Convert.ToInt32(message.Content);
                bool chargecodeFound = false;
                var imiChargeCodes = ServiceHandler.GetImiChargeCodes(connectionStringNameInAppConfig);
                foreach (var imiChargecode in imiChargeCodes)
                {
                    if (imiChargecode.Price == content)
                    {
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(service.ServiceCode, "Telepromo");
                        message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig, message, imiChargecode.Price, 0, null);
                        chargecodeFound = true;
                        singlecharge = MessageHandler.SendSinglechargeMesssageToTelepromo(connectionStringNameInAppConfig, message, serviceAdditionalInfo).Result;
                        break;
                    }
                }
                if (chargecodeFound == false)
                {
                    message = MessageHandler.SendServiceHelp(connectionStringNameInAppConfig, message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                }
                if (singlecharge.IsSucceeded == true)
                {
                    message.Content = "خرید شما به مبلغ " + message.Price * 10 + " ریال با موفقیت انجام شد.";
                    message = MessageHandler.SetImiChargeInfo( connectionStringNameInAppConfig, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in ContentManager: ", e);
            }
            return singlecharge;
        }

        public static bool DeleteFromSinglechargeQueue(string connectionStringNameInAppConfig, string mobileNumber)
        {
            bool succeed = false;
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
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
                    succeed = DeleteFromSinglechargeQueue(connectionStringNameInAppConfig, mobileNumber);
                }
            }
            return succeed;
        }

        public static bool AddSubscriberToSinglechargeQueue(string connectionStringNameInAppConfig, string mobileNumber, string content)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    //var chargeCode = Convert.ToInt32(content);
                    //var imichargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.ChargeCode == chargeCode);
                    //if (imichargeCode == null)
                    //    return false;
                    var singlechargeQueueItem = new SinglechargeWaiting();
                    singlechargeQueueItem.MobileNumber = mobileNumber;
                    singlechargeQueueItem.Price = 10000;
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

        public static async void HandleContent(string connectionStringNameInAppConfig, MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate, List<ImiChargeCode> imiChargeCodes)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    int isCampaignActive = 0;
                    var campaign = entity.Settings.FirstOrDefault(o => o.Name == "campaign");
                    if (campaign != null)
                        isCampaignActive = Convert.ToInt32(campaign.Value);
                    var isInBlackList = SharedLibrary.MessageHandler.IsInBlackList(message.MobileNumber, service.Id);
                    if (isInBlackList == true)
                        isCampaignActive = (int)CampaignStatus.Deactive;
                    message = MessageHandler.SetImiChargeInfo(connectionStringNameInAppConfig ,message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    if (message.Content == null || message.Content == "" || message.Content == " ")
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignEmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }
                    else if (message.Content.ToLower() == "h")
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignHContent").Select(o => o.Content).FirstOrDefault();
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "HContent").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }
                    else if (message.Content.ToLower() == "s")
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend)
                        {
                            string subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null && sub.SpecialUniqueId != null)
                            {
                                subId = sub.SpecialUniqueId;
                                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                                dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/status.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
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
                                MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                            }
                            MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberStatus").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        }
                        return;
                    }
                    else if (message.Content.ToLower() == "g")
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active || isCampaignActive == (int)CampaignStatus.Suspend)
                        {
                            string subId = "1";
                            var sub = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, service.Id);
                            if (sub != null && sub.SpecialUniqueId != null)
                            {
                                subId = sub.SpecialUniqueId;
                                var sha = SharedLibrary.Security.GetSha256Hash(subId + message.MobileNumber);
                                dynamic result = await SharedLibrary.UsefulWebApis.DanoopReferral("http://79.175.164.52/ashpazkhoone/getCharge.php", string.Format("code={0}&number={1}&kc={2}", subId, message.MobileNumber, sha));
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
                                MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                            }
                            MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignOffSubscriberCharges").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        }
                        return;
                    }
                    else if (message.Content == "77" || message.Content.ToLower() == "m")
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "Content77Response").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }
                    if (!service.OnKeywords.Contains(message.Content))
                    {
                        if (isCampaignActive == (int)CampaignStatus.Active)
                            message.Content = messagesTemplate.Where(o => o.Title == "CampaignInvalidContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
                        else
                            message = MessageHandler.InvalidContentWhenSubscribed(connectionStringNameInAppConfig, message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }
                    var isUserAlreadyInSinglechargeQueue = IsUserAlreadyInSinglechargeQueue(connectionStringNameInAppConfig, message.MobileNumber);
                    if (isUserAlreadyInSinglechargeQueue == true)
                    {
                        message = MessageHandler.UserHasActiveSinglecharge(connectionStringNameInAppConfig, message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }
                    var isUserAlreadyChargedThisMonth = IsUserAlreadyChargedThisMonth(connectionStringNameInAppConfig, message.MobileNumber);
                    if (isUserAlreadyChargedThisMonth == true)
                    {
                        message = MessageHandler.UserHasActiveSinglecharge(connectionStringNameInAppConfig, message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                        return;
                    }

                    var isSuccessful = AddSubscriberToSinglechargeQueue(connectionStringNameInAppConfig, message.MobileNumber, message.Content);
                    if (isSuccessful == true)
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "WelcomeMessage").Select(o => o.Content).FirstOrDefault();
                        MessageHandler.InsertMessageToQueue(connectionStringNameInAppConfig, message);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in HandleContent: ", e);
            }
        }

        private static bool IsUserAlreadyChargedThisMonth(string connectionStringNameInAppConfig, string mobileNumber)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
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

        private static bool IsUserAlreadyInSinglechargeQueue(string connectionStringNameInAppConfig, string mobileNumber)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
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