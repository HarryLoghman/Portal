using SharedLibrary.Models;
using BimeKarbalaLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Data.Entity;
using System.Text;

namespace BimeKarbalaLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    var userLevel = GetUserLevel(subscriber.Id);
                    if (userLevel == 0)
                        return;
                    var userInput = message.Content;
                    if (message.Content == "4")
                    {
                        var purchasesContent = GetUserPurchases(subscriber.MobileNumber);
                        if (purchasesContent == "")
                        {
                            message.Content = "شما هیچ خریدی انجام نداده اید";
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "SendPurchasesContent").Select(o => o.Content).FirstOrDefault();
                            if (message.Content.Contains("{PURCHASES}"))
                            {
                                message.Content = message.Content.Replace("{PURCHASES}", purchasesContent);
                            }
                        }
                    }
                    else if (message.Content == "" || message.Content == "راهنما" || message.Content == "H")
                    {
                        message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                    }
                    else if (message.Content == "10")
                    {
                        var InsuranceCodesContent = GetInsuranceCodesContent(subscriber.MobileNumber);
                        if (InsuranceCodesContent == "")
                        {
                            message.Content = "کد بیمه ای برای شما موجود نمی باشد.";
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox8Content").Select(o => o.Content).FirstOrDefault();
                            if (message.Content.Contains("{INSURANCE}"))
                            {
                                message.Content = message.Content.Replace("{INSURANCE}", InsuranceCodesContent);
                            }
                        }
                    }
                    else if (userLevel == 1)
                    {
                        if (message.Content == "1")
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox1Content").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                            ChangeUserLevel(subscriber.Id, 2);
                            Singlecharge singlecharge = new Singlecharge();
                            int price = 24800;
                            message = MessageHandler.SetImiChargeInfo(message, price, 0, null);
                            singlecharge = MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                            if (singlecharge.IsSucceeded == true)
                            {
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox3Content").Select(o => o.Content).FirstOrDefault();
                                ChangeUserLevel(subscriber.Id, 3);
                            }
                            else
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox2Content").Select(o => o.Content).FirstOrDefault();
                            
                            MessageHandler.InsertMessageToTimedTempQueue(message, SharedLibrary.MessageHandler.MessageType.OnDemand);
                            return;
                        }
                        else
                            message.Content = GetCorrectLevelMessage(userLevel, messagesTemplate, userInput);
                    }
                    else if (userLevel == 2)
                    {
                        if (message.Content == "2")
                        {
                            Singlecharge singlecharge = new Singlecharge();
                            int price = 24800;
                            message = MessageHandler.SetImiChargeInfo(message, price, 0, null);
                            singlecharge = MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                            if (singlecharge.IsSucceeded == true)
                            {
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox3Content").Select(o => o.Content).FirstOrDefault();
                                ChangeUserLevel(subscriber.Id, 3);
                            }
                            else
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox2Content").Select(o => o.Content).FirstOrDefault();
                        }
                        else
                            message.Content = GetCorrectLevelMessage(userLevel, messagesTemplate, userInput);
                    }
                    else if (userLevel == 3)
                    {
                        var passportNo = message.Content;
                        //if (passportNo.Length == 9)
                        //    passportNo.Remove(0, 1);
                        //if (passportNo.Length != 8)
                        if (passportNo.Length != 10)
                            message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox6Content").Select(o => o.Content).FirstOrDefault();
                        else
                        {
                            if (Regex.IsMatch(passportNo, @"^\d+$") == false)
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox6Content").Select(o => o.Content).FirstOrDefault();
                            else
                            {
                                CreateInsuranceInfo(message.MobileNumber, passportNo);
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox7Content").Select(o => o.Content).FirstOrDefault();
                                if (message.Content.Contains("{PASSPORTNO}"))
                                {
                                    message.Content = message.Content.Replace("{PASSPORTNO}", passportNo);
                                }
                                ChangeUserLevel(subscriber.Id, 4);
                            }
                        }
                    }
                    else if (userLevel == 4)
                    {
                        if (message.Content == "3")
                        {
                            var confirmation = CreateConfirmationCode(subscriber.MobileNumber);
                            message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox5Content").Select(o => o.Content).FirstOrDefault();
                            if (message.Content.Contains("{CONFRIMCODE}"))
                            {
                                message.Content = message.Content.Replace("{CONFRIMCODE}", confirmation["ConfirmationCode"]);
                            }
                            if (message.Content.Contains("{PASSPORTNO}"))
                            {
                                message.Content = message.Content.Replace("{PASSPORTNO}", confirmation["PassportNo"]);
                            }
                            DeleteUnApprovedPassportNumbers(subscriber.MobileNumber);
                            ChangeUserLevel(subscriber.Id, 1);
                        }
                        else if (message.Content.Length == 10)
                        {
                            var passportNo = message.Content;

                            if (Regex.IsMatch(passportNo, @"^\d+$") == false)
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox6Content").Select(o => o.Content).FirstOrDefault();
                            else
                            {
                                CreateInsuranceInfo(message.MobileNumber, passportNo);
                                message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox7Content").Select(o => o.Content).FirstOrDefault();
                                if (message.Content.Contains("{PASSPORTNO}"))
                                {
                                    message.Content = message.Content.Replace("{PASSPORTNO}", passportNo);
                                }
                            }
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "SendMessageBox6Content").Select(o => o.Content).FirstOrDefault();
                            ChangeUserLevel(subscriber.Id, 3);
                        }
                    }
                    MessageHandler.InsertMessageToQueue(message);
                    return;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in HandleContent: ", e);
            }
        }

        private static string GetInsuranceCodesContent(string mobileNumber)
        {
            var insuranceContent = "";
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var passportNumbers = entity.InsuranceInfoes.Where(o => o.MobileNumber == mobileNumber && o.InsuranceNo != null);
                    if (passportNumbers != null)
                    {
                        int counter = 1;
                        foreach (var passportNumber in passportNumbers)
                        {
                            insuranceContent += counter + ". " + "برای کد ملی " + passportNumber.PassportNo + " شماره بیمه عبارت است از " + passportNumber.InsuranceNo + Environment.NewLine;
                            counter++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetInsuranceCodesContent: ", e);
            }
            return insuranceContent;
        }

        private static string GetUserPurchases(string mobileNumber)
        {
            var purchases = "";
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var passportNumbers = entity.InsuranceInfoes.Where(o => o.MobileNumber == mobileNumber && o.ConfirmationCode != null);
                    if (passportNumbers != null)
                    {
                        int counter = 1;
                        foreach (var passportNumber in passportNumbers)
                        {
                            purchases += counter + ". " + "برای کد ملی " + passportNumber.PassportNo + " کد رهگیری عبارت است از " + passportNumber.ConfirmationCode + Environment.NewLine;
                            counter++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetUserPurchases: ", e);
            }
            return purchases;
        }

        private static void DeleteUnApprovedPassportNumbers(string mobileNumber)
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var passportNumbers = entity.InsuranceInfoes.Where(o => o.MobileNumber == mobileNumber && o.ConfirmationCode == null);
                    if (passportNumbers != null)
                    {
                        entity.InsuranceInfoes.RemoveRange(passportNumbers);
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in DeleteUnApprovedPassportNumbers: ", e);
            }
        }

        private static Dictionary<string, string> CreateConfirmationCode(string mobileNumber)
        {
            var confirmation = new Dictionary<string, string>();
            confirmation["PassportNo"] = "خطا";
            confirmation["ConfirmationCode"] = "خطا";
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var insuranceInfo = entity.InsuranceInfoes.Where(o => o.MobileNumber == mobileNumber && o.ConfirmationCode == null).OrderByDescending(o => o.Id).FirstOrDefault();
                    var code = GenerateRandomString(11);
                    insuranceInfo.ConfirmationCode = code;
                    insuranceInfo.DateApproved = DateTime.Now;
                    insuranceInfo.PersianDateApproved = SharedLibrary.Date.GetPersianDateTime();
                    entity.Entry(insuranceInfo).State = EntityState.Modified;
                    entity.SaveChanges();
                    confirmation["PassportNo"] = insuranceInfo.PassportNo;
                    confirmation["ConfirmationCode"] = code;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CreateConfirmationCode: ", e);
            }
            return confirmation;
        }

        public static string GenerateRandomString(int size)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            string number = random.Next(1, 9).ToString();
            for (int i = 1; i < size; i++)
            {
                number += random.Next(0, 9).ToString();
            }

            return number;
        }

        private static void CreateInsuranceInfo(string mobileNumber, string passportNo)
        {
            try
            {
                var insuranceInfo = new InsuranceInfo();
                insuranceInfo.MobileNumber = mobileNumber;
                insuranceInfo.PassportNo = passportNo;
                insuranceInfo.IsApproved = false;
                insuranceInfo.DateCreated = DateTime.Now;
                insuranceInfo.PersianDateCreated = SharedLibrary.Date.GetPersianDateTime();
                using (var entity = new BimeKarbalaEntities())
                {
                    entity.InsuranceInfoes.Add(insuranceInfo);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CreateInsuranceInfo: ", e);
            }
        }

        public static void ChangeUserLevel(long subscriberId, int level)
        {
            using (var entity = new BimeKarbalaEntities())
            {
                var userInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                userInfo.SubscriberLevel = level;
                entity.Entry(userInfo).State = EntityState.Modified;
                entity.SaveChanges();
            }
        }

        private static string GetCorrectLevelMessage(int userLevel, List<MessagesTemplate> messagesTemplate, string userInput)
        {
            var content = "";
            if (userLevel == 1)
                content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            else if (userLevel == 2 && userInput == "3")
                content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            else if (userLevel == 2)
                content = messagesTemplate.Where(o => o.Title == "SendMessageBox2Content").Select(o => o.Content).FirstOrDefault();
            else if (userLevel == 3 && userInput == "3")
                content = messagesTemplate.Where(o => o.Title == "ServiceHelp").Select(o => o.Content).FirstOrDefault();
            return content;
        }

        public static int GetUserLevel(long subscriberId)
        {
            var level = 0;
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    level = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId).SubscriberLevel;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in GetUserLevel: ", e);
            }
            return level;
        }

        public static Singlecharge HandleSinglechargeContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            Singlecharge singlecharge = new Singlecharge();
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            try
            {
                var content = Convert.ToInt32(message.Content);
                bool chargecodeFound = false;
                var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
                foreach (var imiChargecode in imiChargeCodes)
                {
                    if (imiChargecode.ChargeCode == content)
                    {
                        message = MessageHandler.SetImiChargeInfo(message, imiChargecode.Price, 0, null);
                        chargecodeFound = true;
                        singlecharge = MessageHandler.SendSinglechargeMesssageToPardisImi(message);
                        break;
                    }
                }
                if (chargecodeFound == false)
                {
                    message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                    MessageHandler.InsertMessageToQueue(message);
                }
                if (singlecharge.IsSucceeded == true)
                {
                    message.Content = "خرید شما به مبلغ " + message.Price * 10 + " ریال با موفقیت انجام شد.";
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    MessageHandler.InsertMessageToQueue(message);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in ContentManager: ", e);
            }
            return singlecharge;
        }

        public static void DeleteFromSinglechargeQueue(string mobileNumber)
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var singlechargeQueue = entity.SinglechargeWaitings.Where(o => o.MobileNumber == mobileNumber);
                    entity.SinglechargeWaitings.RemoveRange(singlechargeQueue);
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in DeleteFromSinglechargeQueue: ", e);
            }
        }

        public static bool AddSubscriberToSinglechargeQueue(string mobileNumber, string content)
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
                {
                    var chargeCode = Convert.ToInt32(content);
                    var imichargeCode = entity.ImiChargeCodes.FirstOrDefault(o => o.ChargeCode == chargeCode);
                    if (imichargeCode == null)
                        return false;
                    var singlechargeQueueItem = new SinglechargeWaiting();
                    singlechargeQueueItem.MobileNumber = mobileNumber;
                    singlechargeQueueItem.Price = imichargeCode.Price;
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


        private static bool IsUserAlreadyChargedThisMonth(string mobileNumber)
        {
            try
            {
                using (var entity = new BimeKarbalaEntities())
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
                using (var entity = new BimeKarbalaEntities())
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