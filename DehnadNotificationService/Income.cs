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
                CalculateTahchinIncomeDifferenceByCycle();
                //CalculateTahchinIncomeDifferenceByHour();
                CalculateMusicYadIncomeDifferenceByHour();
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDiffrenceByHour: ", e);
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
                if (incomePercentageDifference <= -10)
                {
                    logs.Info("MusicYad incomePercentageDifference:" + incomePercentageDifference);
                    var message = "MusicYad income dropped by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.All);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateMusicYadIncomeDifferenceByHour: ", e);
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
                logs.Info("yesterdayPastHourIncome:" + yesterdayPastHourIncome);
                logs.Info("todayPastHourIncome:" + todayPastHourIncome);
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                logs.Info("incomePercentageDifference:" + incomePercentageDifference);
                if (incomePercentageDifference <= -10)
                {
                    logs.Info("Tahchin incomePercentageDifference:" + incomePercentageDifference);
                    var message = "Tahchin income dropped by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.All);
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
                logs.Info("yesterdayPastHourIncome:" + yesterdayPastHourIncome);
                logs.Info("todayPastHourIncome:" + todayPastHourIncome);
                var incomePercentageDifference = CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                logs.Info("incomePercentageDifference:" + incomePercentageDifference);
                if (incomePercentageDifference <= -10 || incomePercentageDifference >= 20)
                {
                    logs.Info("Tahchin incomePercentageDifference:" + incomePercentageDifference);
                    var message = "Tahchin income dropped by %" + incomePercentageDifference;
                    DehnadNotificationService.Service.SaveMessageToSendQueue(message, UserType.All);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in CalculateTahchinIncomeDifferenceByCycle: ", e);
            }
        }

        private static int CaluculatePercentageDifference(int yesterdayPastHourIncome, int todayPastHourIncome)
        {
            var yesterdayPastHourIncomeDivide = yesterdayPastHourIncome == 0 ? 1 : yesterdayPastHourIncome;
            var percent = (((decimal)todayPastHourIncome - (decimal)yesterdayPastHourIncome) / (decimal)yesterdayPastHourIncomeDivide) * (decimal)100;
            return Convert.ToInt32(percent);
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
    }
}
