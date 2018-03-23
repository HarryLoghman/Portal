using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DehnadNotificationService
{
    public class Income
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int defaultIncomePercentageDecrease = 10;

        public static void IncomeDiffrenceByHour()
        {
            try
            {
                //CalculateTahchinIncomeDifferenceByCycle();
                CalculateTahchinIncomeDifferenceByHour();
                CalculateMusicYadIncomeDifferenceByHour();
                CalculateDambelIncomeDifferenceByHour();
                CalculatePhantomIncomeDifferenceByHour();
                CalculateMedioIncomeDifferenceByHour();
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDiffrenceByHour: ", e);
            }
        }

        public static void OverChargeChecker()
        {
            try
            {
                MusicYadOverChargeCheck();
                DambelOverChargeCheck();
                TahchinOverChargeCheck();
                PhantomOverChargeCheck();
                MedioOverChargeCheck();
            }
            catch (Exception e)
            {
                logs.Error("Exception in OverChargeChecker: " + e);
            }

        }

        public static void MusicYadOverChargeCheck()
        {
            try
            {
                var maxChargeLimit = 300;
                var today = DateTime.Now;
                using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
                {
                    var overcharged = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                    {
                        var message = "Overcharge in MusicYad Service";
                        if (!Service.CheckMessagesAlreadySent(message))
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in MusicYadOverChargeCheck: " + e);
                var message = "Exception in MusicYad Service for Overcharge Checking";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }

        }

        public static void CalculateMusicYadIncomeDifferenceByHour()
        {
            try
            {
                var musicYadYesterdayIncome = MusicYadGetIncomeByHour(DateTime.Now.AddDays(-1));
                var musicYadtoadyIncome = MusicYadGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (musicYadYesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = musicYadYesterdayIncome[pastHour];
                if (musicYadtoadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = musicYadtoadyIncome[pastHour];

                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("MusicYad incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "MusicYad income dropped by %" + incomePercentageDifference;
                    else
                        message = "MusicYad income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateMusicYadIncomeDifferenceByHour: ", e);
            }
        }

        public static void TahchinOverChargeCheck()
        {
            try
            {
                var maxChargeLimit = 300;
                var today = DateTime.Now;
                using (var entity = new TahChinLibrary.Models.TahChinEntities())
                {
                    var overcharged = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                    {
                        var message = "Overcharge in TahChin Service";
                        if (!Service.CheckMessagesAlreadySent(message))
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in TahChinOverChargeCheck: " + e);
                var message = "Exception in Tahchin Service for Overcharge Checking";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }

        }

        public static void CalculateTahchinIncomeDifferenceByHour()
        {
            try
            {
                var tahchinYesterdayIncome = TahChinGetIncomeByHour(DateTime.Now.AddDays(-1));
                var tahchinToadyIncome = TahChinGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (tahchinYesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = tahchinYesterdayIncome[pastHour];
                if (tahchinToadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = tahchinToadyIncome[pastHour];
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("TahChin incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "TaChin income dropped by %" + incomePercentageDifference;
                    else
                        message = "TahChin income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateTahchinIncomeDifferenceByHour: ", e);
            }
        }

        public static void CalculateTahchinIncomeDifferenceByCycle()
        {
            try
            {
                var tahchinYesterdayIncome = TahChinGetIncomeByHour(DateTime.Now.AddDays(-1));
                var tahchinToadyIncome = TahChinGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (tahchinYesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = tahchinYesterdayIncome[pastHour];
                if (tahchinToadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = tahchinToadyIncome[pastHour];
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("TahChin incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "TahChin income dropped by %" + incomePercentageDifference;
                    else
                        message = "TahChin income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateTahchinIncomeDifferenceByCycle: ", e);
            }
        }

        public static void DambelOverChargeCheck()
        {
            try
            {
                var maxChargeLimit = 300;
                var today = DateTime.Now;
                using (var entity = new DambelLibrary.Models.DambelEntities())
                {
                    var overcharged = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                    {
                        var message = "Overcharge in Dambel Service";
                        if (!Service.CheckMessagesAlreadySent(message))
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in DambelOverChargeCheck: " + e);
                var message = "Exception in Dambel Service for Overcharge Checking";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }

        }

        public static void CalculateDambelIncomeDifferenceByHour()
        {
            try
            {
                var yesterdayIncome = DambelGetIncomeByHour(DateTime.Now.AddDays(-1));
                var toadyIncome = DambelGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (yesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = yesterdayIncome[pastHour];
                if (toadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = toadyIncome[pastHour];
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("Dambel incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "Dambel income dropped by %" + incomePercentageDifference;
                    else
                        message = "Dambel income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateDambelIncomeDifferenceByHour: ", e);
            }
        }


        public static Dictionary<int, int> TahChinGetIncomeByHour(DateTime date)
        {
            try
            {
                using (var entity = new TahChinLibrary.Models.TahChinEntities())
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = entity.SinglechargeArchives.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                            .GroupBy(o => o.DateCreated.Hour)
                            .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                            .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = entity.Singlecharges.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                              .GroupBy(o => o.DateCreated.Hour)
                              .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                              .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in TahChinGetIncomeByHour: ", e);
            }
            return null;
        }

        public static Dictionary<int, int> MusicYadGetIncomeByHour(DateTime date)
        {
            try
            {
                using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = entity.SinglechargeArchives.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                        .GroupBy(o => o.DateCreated.Hour)
                        .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                        .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = entity.Singlecharges.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                        .GroupBy(o => o.DateCreated.Hour)
                        .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                        .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in MusicYadGetIncomeByHour: ", e);
            }
            return null;
        }

        public static Dictionary<int, int> DambelGetIncomeByHour(DateTime date)
        {
            try
            {
                using (var entity = new DambelLibrary.Models.DambelEntities())
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = entity.SinglechargeArchives.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                            .GroupBy(o => o.DateCreated.Hour)
                            .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                            .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = entity.Singlecharges.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                              .GroupBy(o => o.DateCreated.Hour)
                              .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                              .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in DambelGetIncomeByHour: ", e);
            }
            return null;
        }

        public static void MedioOverChargeCheck()
        {
            try
            {
                var maxChargeLimit = 400;
                var today = DateTime.Now;
                using (var entity = new MedioLibrary.Models.MedioEntities())
                {
                    var overcharged = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                    {
                        var message = "Overcharge in Medio Service";
                        if (!Service.CheckMessagesAlreadySent(message))
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in MedioOverChargeCheck: " + e);
                var message = "Exception in Medio Service for Overcharge Checking";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }

        }

        public static void CalculateMedioIncomeDifferenceByHour()
        {
            try
            {
                var yesterdayIncome = MedioGetIncomeByHour(DateTime.Now.AddDays(-1));
                var toadyIncome = MedioGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (yesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = yesterdayIncome[pastHour];
                if (toadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = toadyIncome[pastHour];
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("Medio incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "Medio income dropped by %" + incomePercentageDifference;
                    else
                        message = "Medio income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateMedioIncomeDifferenceByHour: ", e);
            }
        }


        public static Dictionary<int, int> MedioGetIncomeByHour(DateTime date)
        {
            try
            {
                using (var entity = new MedioLibrary.Models.MedioEntities())
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = entity.SinglechargeArchives.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                            .GroupBy(o => o.DateCreated.Hour)
                            .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                            .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = entity.Singlecharges.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                              .GroupBy(o => o.DateCreated.Hour)
                              .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                              .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in MedioGetIncomeByHour: ", e);
            }
            return null;
        }

        public static void PhantomOverChargeCheck()
        {
            try
            {
                var maxChargeLimit = 500;
                var today = DateTime.Now;
                using (var entity = new PhantomLibrary.Models.PhantomEntities())
                {
                    var overcharged = entity.Singlecharges.AsNoTracking()
                            .Where(o => DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(today) && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                    {
                        var message = "Overcharge in Phantom Service";
                        if (!Service.CheckMessagesAlreadySent(message))
                            DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                    }
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in PhantomOverChargeCheck: " + e);
                var message = "Exception in Phantom Service for Overcharge Checking";
                DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
            }

        }

        public static void CalculatePhantomIncomeDifferenceByHour()
        {
            try
            {
                var yesterdayIncome = PhantomGetIncomeByHour(DateTime.Now.AddDays(-1));
                var toadyIncome = PhantomGetIncomeByHour(DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (yesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = yesterdayIncome[pastHour];
                if (toadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = toadyIncome[pastHour];
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("Phantom incomePercentageDifference:" + incomePercentageDifference);
                    var message = "";
                    if (incomePercentageDifference < 0)
                        message = "Phantom income dropped by %" + incomePercentageDifference;
                    else
                        message = "Phantom income increased by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.AdminOnly);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculatePhantomIncomeDifferenceByHour: ", e);
            }
        }


        public static Dictionary<int, int> PhantomGetIncomeByHour(DateTime date)
        {
            try
            {
                using (var entity = new PhantomLibrary.Models.PhantomEntities())
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = entity.SinglechargeArchives.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                            .GroupBy(o => o.DateCreated.Hour)
                            .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                            .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = entity.Singlecharges.Where(o => o.IsSucceeded == true && DbFunctions.TruncateTime(o.DateCreated) == DbFunctions.TruncateTime(date))
                              .GroupBy(o => o.DateCreated.Hour)
                              .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                              .AsNoTracking().ToDictionary(o => o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in PhantomGetIncomeByHour: ", e);
            }
            return null;
        }

        private static int CaluculatePercentageDifference(int yesterdayPastHourIncome, int todayPastHourIncome)
        {
            var yesterdayPastHourIncomeDivide = yesterdayPastHourIncome == 0 ? 1 : yesterdayPastHourIncome;
            var percent = (((decimal)todayPastHourIncome - (decimal)yesterdayPastHourIncome) / (decimal)yesterdayPastHourIncomeDivide) * (decimal)100;
            return Convert.ToInt32(percent);
        }
    }
}
