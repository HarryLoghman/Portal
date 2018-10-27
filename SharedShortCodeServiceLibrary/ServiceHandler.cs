﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SharedLibrary.Models;
using SharedShortCodeServiceLibrary.SharedModel;

namespace SharedShortCodeServiceLibrary
{
    public class ServiceHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static AutochargeContent SelectAutochargeContentByOrder(string connectionStringNameInAppConfig, ShortCodeServiceEntities entity, long subscriberId)
        {
            AutochargeContent autochargeContent = null;
            var lastContentSubscriberReceived = entity.AutochargeContentsSendedToUsers.Where(o => o.SubscriberId == subscriberId).OrderByDescending(o => o.Id).FirstOrDefault();
            if (lastContentSubscriberReceived == null)
            {
                AddToAutochargeContentSendedToUserTable(connectionStringNameInAppConfig, subscriberId, autochargeContent.Id);
                autochargeContent = entity.AutochargeContents.FirstOrDefault();
            }
            else
            {
                var nextAutochargeContent = entity.AutochargeContents.FirstOrDefault(o => o.Id > lastContentSubscriberReceived.AutochargeContentId);
                if (nextAutochargeContent != null)
                {
                    AddToAutochargeContentSendedToUserTable(connectionStringNameInAppConfig, subscriberId, nextAutochargeContent.Id);
                    autochargeContent = nextAutochargeContent;
                }
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static Singlecharge GetOTPRequestId(ShortCodeServiceEntities entity, MessageObject message)
        {
            try
            {
                var singlecharge = entity.Singlecharges.Where(o => o.MobileNumber == message.MobileNumber && o.Price == 0 && o.Description == "SUCCESS-Pending Confirmation").OrderByDescending(o => o.DateCreated).FirstOrDefault();
                if (singlecharge != null)
                    return singlecharge;
                else
                    return null;
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetOTPRequestId: ", e);
            }
            return null;
        }

        public static void AddToAutochargeContentSendedToUserTable(string connectionStringNameInAppConfig, long subscriberId, long autochargeId)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig + "Entities"))
                {
                    var lastContent = new AutochargeContentsSendedToUser();
                    lastContent.SubscriberId = subscriberId;
                    lastContent.AutochargeContentId = autochargeId;
                    lastContent.DateAdded = DateTime.Now;
                    entity.AutochargeContentsSendedToUsers.Add(lastContent);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddToAutochargeContentSendedToUserTable: " + e);
            }
        }

        public static void AddToAutochargeContentSendedToUserTable(string connectionStringNameInAppConfig, long subscriberId, long autochargeId, long mobileId)
        {
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig + "Entities"))
                {
                    var lastContent = new AutochargeContentsSendedToUser();
                    lastContent.SubscriberId = subscriberId;
                    lastContent.AutochargeContentId = autochargeId;
                    lastContent.DateAdded = DateTime.Now;
                    entity.AutochargeContentsSendedToUsers.Add(lastContent);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddToAutochargeContentSendedToUserTable: " + e);
            }
        }

        public static bool IsUserVerifedTheSubscription(string mobileNumber, long serviceId, string keyword)
        {
            var result = false;
            using (var entity = new PortalEntities())
            {
                try
                {
                    var user = entity.VerifySubscribers.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                    if (user == null)
                    {
                        var verify = new VerifySubscriber();
                        verify.MobileNumber = mobileNumber;
                        verify.ServiceId = serviceId;
                        verify.UsedKeyword = keyword;
                        entity.VerifySubscribers.Add(verify);
                    }
                    else if (keyword != "9")
                        result = false;
                    else
                    {
                        entity.VerifySubscribers.Remove(user);
                        result = true;
                    }
                    entity.SaveChanges();
                }
                catch (System.Exception e)
                {
                    logs.Error("Error in IsUserVerifedTheSubscription: " + e);
                }
                return result;
            }
        }

        public static AutochargeContent SelectAutochargeContentByDate(string connectionStringNameInAppConfig, ShortCodeServiceEntities entity, long subscriberId)
        {
            var today = DateTime.Now.Date;
            var autochargesAlreadyInQueue = entity.AutochargeMessagesBuffers.Where(o => DbFunctions.TruncateTime(o.DateAddedToQueue) == today).Select(o => o.Id).ToList();

            var autochargeContent = entity.AutochargeContents.FirstOrDefault(o => DbFunctions.TruncateTime(o.SendDate) == today && !autochargesAlreadyInQueue.Contains(o.Id));
            if (autochargeContent != null)
            {
                AddToAutochargeContentSendedToUserTable(connectionStringNameInAppConfig, subscriberId, autochargeContent.Id);
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static AutochargeContent SelectAutochargeContent(string connectionStringNameInAppConfig, ShortCodeServiceEntities entity, long subscriberId)
        {
            var today = DateTime.Now.Date;
            var autochargesAlreadyInQueue = entity.AutochargeMessagesBuffers.Where(o => DbFunctions.TruncateTime(o.DateAddedToQueue) == today).Select(o => o.Id).ToList();

            var autochargeContent = entity.AutochargeContents.FirstOrDefault(o => DbFunctions.TruncateTime(o.SendDate) == today && !autochargesAlreadyInQueue.Contains(o.Id));
            if (autochargeContent != null)
            {
                AddToAutochargeContentSendedToUserTable(connectionStringNameInAppConfig, subscriberId, autochargeContent.Id);
            }
            entity.SaveChanges();
            return autochargeContent;
        }

        public static bool CancelUserInstallments(string connectionStringNameInAppConfig, string mobileNumber)
        {
            bool succeed = false;
            try
            {
                using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
                {
                    var userinstallments = entity.SinglechargeInstallments.Where(o => o.MobileNumber == mobileNumber && o.IsUserCanceledTheInstallment == false).ToList();
                    foreach (var installment in userinstallments)
                    {
                        installment.IsUserCanceledTheInstallment = true;
                        installment.CancelationDate = DateTime.Now;
                        installment.PersianCancelationDate = SharedLibrary.Date.GetPersianDateTime();
                        entity.Entry(installment).State = EntityState.Modified;
                    }
                    entity.SaveChanges();
                    succeed = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CancelUserInstallments:" + e);
                while (succeed == false)
                {
                    succeed = CancelUserInstallments(connectionStringNameInAppConfig, mobileNumber);
                }
            }
            return succeed;
        }

        public static List<MessagesTemplate> GetServiceMessagesTemplate(string connectionStringNameInAppConfig)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                return entity.MessagesTemplates.ToList();
            }
        }

        public static bool CheckIfUserWantsToUnsubscribe(string content)
        {
            foreach (var offKeyword in ServiceOffKeywords())
            {
                if (content.Contains(offKeyword))
                    return true;
            }
            return false;
        }

        public static List<ImiChargeCode> GetImiChargeCodes(string connectionStringNameInAppConfig)
        {
            using (var entity = new ShortCodeServiceEntities(connectionStringNameInAppConfig))
            {
                return entity.ImiChargeCodes.ToList();
            }
        }

        public static bool CheckIfUserSendsSubscriptionKeyword(string content, Service service)
        {
            var serviceKeywords = service.OnKeywords.Split(',');
            foreach (var keyword in serviceKeywords)
            {
                if (content == keyword || content == keyword)
                    return true;
            }
            return false;
        }

        public static string[] ServiceOffKeywords()
        {
            var offContents = new string[]
            {
                "off",
                "of",
                "cancel",
                "stop",
                "laghv",
                "lagv",
                "khamoosh",
                "خاموش",
                "لغو",
                "کنسل",
                "توقف",
                "پایان"
            };
            return offContents;
        }
    }
}