﻿using SharedLibrary.Models;
using BimeIranLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Data.Entity;
using System.Text;

namespace BimeIranLibrary
{
    public class ContentManager
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void HandleContent(MessageObject message, Service service, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
                    var userInput = message.Content;
                    var userLevel = GetUserLevel(subscriber.Id);
                    if (message.Content == "off")
                    {
                        bool isCanceled = CancelUserInsuranceIfExists(subscriber);
                        if(isCanceled)
                            ChangeUserLevel(subscriber.Id, 1);
                    }
                    else if (userLevel == 0)
                    {
                        var extractedNumber = SharedLibrary.HelpfulFunctions.GetAllTheNumbersFromComplexString(userInput);
                        if (extractedNumber.Count() > 0)
                        {
                            var packageNumber = extractedNumber.Substring(0, 1);
                            var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
                            bool isChargeCodeFound = false;
                            foreach (var item in imiChargeCodes)
                            {
                                if (item.ChargeCode.ToString() == packageNumber)
                                {
                                    isChargeCodeFound = true;
                                    break;
                                }
                            }
                            if (isChargeCodeFound)
                            {
                                SharedLibrary.HandleSubscription.ChangeSubscriptionKeyword(subscriber, packageNumber);
                                ChangeUserLevel(subscriber.Id, 3);
                                message.Content = messagesTemplate.Where(o => o.Title == "InformationSuccessfulyEntredContent").Select(o => o.Content).FirstOrDefault();
                            }
                            else
                                message.Content = messagesTemplate.Where(o => o.Title == "EnterInsurancePackageNumber").Select(o => o.Content).FirstOrDefault();
                        }
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "EnterInsurancePackageNumber").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (message.Content == "110")
                    {
                        message = CreateDamageReportIfConfirmed(subscriber, message, messagesTemplate);
                        if (message.Content == "")
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "NotHaveActiveInsurance").Select(o => o.Content).FirstOrDefault();
                        }
                    }
                    else if (userLevel == 1)
                    {
                        var isUserHaveActiveInsurance = GetUserActiveInsurance(subscriber);
                        if (isUserHaveActiveInsurance == null)
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "FillInformationContent").Select(o => o.Content).FirstOrDefault();
                            ChangeUserLevel(subscriber.Id, 2);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "AlreadyHaveActiveInsurance").Select(o => o.Content).FirstOrDefault();
                        }
                    }
                    else if (userLevel == 2)
                    {
                        var extractedNumbers = SharedLibrary.HelpfulFunctions.GetAllTheNumbersFromComplexString(userInput);
                        if (extractedNumbers.Length != 20)
                            message.Content = messagesTemplate.Where(o => o.Title == "IncorrectInformationContent").Select(o => o.Content).FirstOrDefault();
                        else
                        {
                            bool isSocialNumberFound = false;
                            string socailNumber = "";
                            string zipCode = "";
                            var firstNumber = extractedNumbers.Substring(0, 10);
                            var secondNumber = extractedNumbers.Substring(10);
                            isSocialNumberFound = SocialCodeValidator(firstNumber);
                            if (isSocialNumberFound == true)
                            {
                                isSocialNumberFound = true;
                                socailNumber = firstNumber;
                            }
                            else
                            {
                                isSocialNumberFound = SocialCodeValidator(secondNumber);
                                {
                                    isSocialNumberFound = true;
                                    socailNumber = secondNumber;
                                }
                            }
                            if (isSocialNumberFound == false)
                                message.Content = messagesTemplate.Where(o => o.Title == "IncorrectInformationContent").Select(o => o.Content).FirstOrDefault();
                            else
                            {
                                if (socailNumber == firstNumber)
                                    zipCode = secondNumber;
                                else
                                    zipCode = firstNumber;
                            }
                            var insuranceType = subscriber.OnKeyword;
                            var isCreated = CheckAndCreateInsuranceInfo(subscriber.MobileNumber, null, socailNumber, zipCode, insuranceType);
                            if (isCreated == false)
                            {
                                message.Content = messagesTemplate.Where(o => o.Title == "InformationExistsContent").Select(o => o.Content).FirstOrDefault();
                            }
                            else
                            {
                                ChangeUserLevel(subscriber.Id, 3);
                                ResetWarningsCounter(subscriber.Id);
                                message.Content = messagesTemplate.Where(o => o.Title == "InformationSuccessfulyEntredContent").Select(o => o.Content).FirstOrDefault();
                            }
                        }
                    }
                    else if (userLevel == 3 && userInput == "100")
                    {
                        message = TryToChargeUser(message, subscriber, messagesTemplate);
                        if (GetUserLevel(subscriber.Id) == 4)
                        {
                            AddUserForRegisteringInsurance(subscriber);
                            ResetWarningsCounter(subscriber.Id);
                        }
                    }
                    else if (userLevel == 3 && userInput == "101")
                    {
                        ChangeUserLevel(subscriber.Id, 2);
                        RemoveInsurance(subscriber);
                        message.Content = messagesTemplate.Where(o => o.Title == "FillInformationContent").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userLevel == 3 && userInput == "102")
                    {
                        ChangeUserLevel(subscriber.Id, 0);
                        RemoveInsurance(subscriber);
                        message.Content = messagesTemplate.Where(o => o.Title == "EnterInsurancePackageNumber").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userLevel == 3)
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "InformationSuccessfulyEntredContent").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userLevel == 5)
                    {
                        message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeSuccessful").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userLevel == 6)
                    {
                        var messageContent = messagesTemplate.Where(o => o.Title == "InformInsuranceNumber").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userLevel == 7 && userInput == "100")
                    {
                        Singlecharge singlecharge = new Singlecharge();
                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("BimeIran", "Telepromo");
                        var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
                        var insuranceInfo = GetUserActiveInsurance(subscriber);
                        var imiChargeCode = SelectChargeCode(imiChargeCodes, insuranceInfo.InsuranceType, insuranceInfo.NextRenewalDate.Value);
                        message = MessageHandler.SetImiChargeInfo(message, imiChargeCode.Price, 0, null);
                        singlecharge = MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo).Result;

                        if (singlecharge.IsSucceeded == true)
                        {
                            var nextRenewalDate = BimeIranLibrary.ContentManager.GetNextRenewalDate(insuranceInfo.NextRenewalDate.Value);
                            entity.InsuranceInfoes.Attach(insuranceInfo);
                            insuranceInfo.NextRenewalDate = nextRenewalDate;
                            insuranceInfo.PersianNextRenewalDate = SharedLibrary.Date.GetPersianDateTime(nextRenewalDate);
                            entity.Entry(insuranceInfo).State = EntityState.Modified;
                            entity.SaveChanges();
                            message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeSuccessful").Select(o => o.Content).FirstOrDefault();
                            ChangeUserLevel(subscriber.Id, 6);
                        }
                        else
                            message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeNotSuccessful").Select(o => o.Content).FirstOrDefault();
                    }
                    else if (userInput == message.Content)
                    {
                        message.Content = "";
                    }
                    if (message.Content != "")
                        MessageHandler.InsertMessageToQueue(message);
                    return;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in HandleContent: ", e);
            }
        }

        public static ImiChargeCode SelectChargeCode(List<ImiChargeCode> imiChargeCodes, string insuranceType, DateTime renewalDate)
        {
            var nextRenewalDate = BimeIranLibrary.ContentManager.GetNextRenewalDate(renewalDate);
            var daysDifference = nextRenewalDate.Subtract(renewalDate).TotalDays;
            int chargeKey;
            if (insuranceType == "A")
                chargeKey = 1;
            else if (insuranceType == "B")
                chargeKey = 2;
            else if (insuranceType == "C")
                chargeKey = 3;
            else
                chargeKey = 4;

            var imi = imiChargeCodes.FirstOrDefault(o => o.ChargeCode == chargeKey && o.Days == daysDifference);
            return imi;
        }

        public static void ResetWarningsCounter(long subscriberId)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var subscriberAdditionalInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                    if (subscriberAdditionalInfo != null)
                    {
                        subscriberAdditionalInfo.DateWarningSent = null;
                        subscriberAdditionalInfo.NumberOfWarningsSent = 0;
                        entity.Entry(subscriberAdditionalInfo).State = EntityState.Modified;
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in ResetWarningsCounter: " + e);
            }
        }

        public static InsuranceInfo GetUserActiveInsurance(Subscriber subscriber)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var insuranceInfo = entity.InsuranceInfoes.Where(o => o.MobileNumber == subscriber.MobileNumber && o.IsUserRequestedInsuranceCancelation != true).OrderByDescending(o => o.DateInsuranceRequested).FirstOrDefault();
                    if (insuranceInfo == null)
                        return null;
                    else
                        return insuranceInfo;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CheckIsUserHaveActiveInsurance: ", e);
            }
            return null;
        }

        public static MessageObject CreateDamageReportIfConfirmed(Subscriber subscriber, MessageObject message, List<MessagesTemplate> messagesTemplate)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var insuranceInfo = entity.InsuranceInfoes.Where(o => o.MobileNumber == subscriber.MobileNumber && o.IsUserRequestedInsuranceCancelation != true).OrderByDescending(o => o.DateInsuranceRequested).FirstOrDefault();
                    if (insuranceInfo == null)
                        message.Content = "";
                    else
                    {
                        var damage = entity.DamageReports.Where(o => o.InsuranceId == insuranceInfo.Id && o.IsSendedToInsuranceCompany == null && DbFunctions.AddDays(o.DateDamageReported, 1) < DateTime.Now).OrderByDescending(o => o.DateDamageReported).FirstOrDefault();
                        if (damage == null)
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "DamageReportConfirmation").Select(o => o.Content).FirstOrDefault();
                            var damageReport = new DamageReport();
                            damageReport.InsuranceId = insuranceInfo.Id;
                            damageReport.DateDamageReported = DateTime.Now;
                            damageReport.PersianDateDamageReported = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                            entity.DamageReports.Add(damageReport);
                        }
                        else
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "DamageReported").Select(o => o.Content).FirstOrDefault();
                            damage.IsSendedToInsuranceCompany = false;
                            entity.Entry(insuranceInfo).State = EntityState.Modified;
                        }
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CreateDamageReportIfConfirmed: ", e);
            }
            return message;
        }

        public static bool CancelUserInsuranceIfExists(Subscriber subscriber)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var insuranceInfo = entity.InsuranceInfoes.Where(o => o.MobileNumber == subscriber.MobileNumber && o.IsUserRequestedInsuranceCancelation != true).OrderByDescending(o => o.DateInsuranceRequested).FirstOrDefault();
                    if (insuranceInfo == null)
                        return false;
                    insuranceInfo.IsUserRequestedInsuranceCancelation = true;
                    insuranceInfo.IsCancelationSendedToInsuranceCompany = false;
                    insuranceInfo.DateCancelationRequested = DateTime.Now;
                    insuranceInfo.PersianDateCancelationRequested = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
                    entity.Entry(insuranceInfo).State = EntityState.Modified;
                    entity.SaveChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CancelUserInsuranceIfExists: ", e);
            }
            return false;
        }

        public static void AddUserForRegisteringInsurance(Subscriber subscriber)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var insuranceInfo = entity.InsuranceInfoes.Where(o => o.MobileNumber == subscriber.MobileNumber).OrderByDescending(o => o.DateInsuranceRequested).FirstOrDefault();
                    insuranceInfo.IsSendedToInsuranceCompany = false;
                    entity.Entry(insuranceInfo).State = EntityState.Modified;
                    entity.SaveChanges();
                    ChangeUserLevel(subscriber.Id, 5);
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in AddUserForRegisteringInsurance: ", e);
            }
        }

        private static MessageObject TryToChargeUser(MessageObject message, Subscriber subscriber, List<MessagesTemplate> messagesTemplate)
        {
            var imiChargeCodes = ServiceHandler.GetImiChargeCodes();
            var onKeyword = Convert.ToInt32(SharedLibrary.ServiceHandler.GetSubscriberOnKeyword(subscriber.Id));
            var now = DateTime.Now;
            var nextRenewalDate = GetNextRenewalDate(now);
            var daysDifference = nextRenewalDate.Subtract(now).TotalDays;
            
            Singlecharge singlecharge = new Singlecharge();
            foreach (var imiChargecode in imiChargeCodes)
            {
                if (imiChargecode.ChargeCode == onKeyword && imiChargecode.Days == daysDifference)
                {
                    var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage("BimeIran", "Telepromo");
                    message = MessageHandler.SetImiChargeInfo(message, imiChargecode.Price, 0, null);
                    singlecharge = MessageHandler.SendSinglechargeMesssageToTelepromo(message, serviceAdditionalInfo).Result;
                    break;
                }
            }
            message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            if (singlecharge.IsSucceeded == true)
            {
                ChangeUserLevel(subscriber.Id, 4);
                message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeSuccessful").Select(o => o.Content).FirstOrDefault();
            }
            else
                message.Content = messagesTemplate.Where(o => o.Title == "SingleChargeNotSuccessful").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static bool SocialCodeValidator(string socialCode)
        {
            if (String.IsNullOrEmpty(socialCode))
                return false;

            if (socialCode.Length != 10)
                return false;

            var regex = new Regex(@"\d{10}");
            if (!regex.IsMatch(socialCode))
                return false;

            var allDigitEqual = new[] { "0000000000", "1111111111", "2222222222", "3333333333", "4444444444", "5555555555", "6666666666", "7777777777", "8888888888", "9999999999" };
            if (allDigitEqual.Contains(socialCode))
                return false;

            var chArray = socialCode.ToCharArray();
            var num0 = Convert.ToInt32(chArray[0].ToString()) * 10;
            var num2 = Convert.ToInt32(chArray[1].ToString()) * 9;
            var num3 = Convert.ToInt32(chArray[2].ToString()) * 8;
            var num4 = Convert.ToInt32(chArray[3].ToString()) * 7;
            var num5 = Convert.ToInt32(chArray[4].ToString()) * 6;
            var num6 = Convert.ToInt32(chArray[5].ToString()) * 5;
            var num7 = Convert.ToInt32(chArray[6].ToString()) * 4;
            var num8 = Convert.ToInt32(chArray[7].ToString()) * 3;
            var num9 = Convert.ToInt32(chArray[8].ToString()) * 2;
            var a = Convert.ToInt32(chArray[9].ToString());

            var b = (((((((num0 + num2) + num3) + num4) + num5) + num6) + num7) + num8) + num9;
            var c = b % 11;

            return (((c < 2) && (a == c)) || ((c >= 2) && ((11 - c) == a)));
        }

        private static string GetInsuranceCodesContent(string mobileNumber)
        {
            var insuranceContent = "";
            try
            {
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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

        public static DateTime GetNextRenewalDate(DateTime date)
        {
            var splittedPersianDate = SharedLibrary.Date.GetPersianDate(date).Split('-');
            if (splittedPersianDate[1] == "01" || splittedPersianDate[1] == "02" || splittedPersianDate[1] == "03" || splittedPersianDate[1] == "04" || splittedPersianDate[1] == "05" || splittedPersianDate[1] == "06")
                return date.AddDays(31);
            else if (splittedPersianDate[1] == "12")
                return date.AddDays(29);
            else
                return date.AddDays(30);
        }
        private static bool CheckAndCreateInsuranceInfo(string mobileNumber, string passportNo, string socialCode, string zipCode, string insuranceType)
        {
            try
            {
                var insuranceInfo = new InsuranceInfo();
                insuranceInfo.MobileNumber = mobileNumber;
                insuranceInfo.PassportNo = (passportNo == null || passportNo == "") ? null : passportNo;
                insuranceInfo.IsApproved = false;
                insuranceInfo.SocialNumber = socialCode;
                insuranceInfo.ZipCode = zipCode;
                if (insuranceType == "1")
                    insuranceInfo.InsuranceType = "A";
                else if (insuranceType == "2")
                    insuranceInfo.InsuranceType = "B";
                else if (insuranceType == "1")
                    insuranceInfo.InsuranceType = "C";
                else if (insuranceType == "1")
                    insuranceInfo.InsuranceType = "D";
                else
                    insuranceInfo.InsuranceType = "";
                insuranceInfo.DateInsuranceRequested = DateTime.Now;
                insuranceInfo.PersianDateInsuranceRequested = SharedLibrary.Date.GetPersianDateTime();
                using (var entity = new BimeIranEntities())
                {
                    var exists = entity.InsuranceInfoes.FirstOrDefault(o => o.ZipCode == zipCode);
                    if (exists != null)
                        return false;
                    entity.InsuranceInfoes.Add(insuranceInfo);
                    entity.SaveChanges();
                    return true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Error in CreateInsuranceInfo: ", e);
            }
            return false;
        }

        public static void RemoveInsurance(Subscriber subscriber)
        {
            try
            {
                using (var entity = new BimeIranEntities())
                {
                    var userInsurance = entity.InsuranceInfoes.Where(o => o.MobileNumber == subscriber.MobileNumber).OrderByDescending(o => o.Id).FirstOrDefault();
                    if (userInsurance != null)
                    {
                        entity.InsuranceInfoes.Remove(userInsurance);
                        entity.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in RemoveInsurance: " + e);
            }
        }

        public static void ChangeUserLevel(long subscriberId, int level)
        {
            using (var entity = new BimeIranEntities())
            {
                var userInfo = entity.SubscribersAdditionalInfoes.FirstOrDefault(o => o.SubscriberId == subscriberId);
                userInfo.SubscriberLevel = level;
                entity.Entry(userInfo).State = EntityState.Modified;
                entity.SaveChanges();
            }
        }

        public static int GetUserLevel(long subscriberId)
        {
            var level = 0;
            try
            {
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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
                using (var entity = new BimeIranEntities())
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