using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Notify
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static bool? OverChargeCheck(Type entityType, int maxChargeLimit)
        {
            bool? isOverCharged = null;
            try
            {
                var today = DateTime.Now;
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    var overcharged = ((IEnumerable)entity.Singlecharges).Cast<dynamic>()
                            .Where(o => o.DateCreated.Date == today.Date && o.IsSucceeded == true && o.Price > 0)
                            .GroupBy(o => o.MobileNumber).Where(o => o.Sum(x => x.Price) > maxChargeLimit).Select(o => o.Key).ToList();
                    if (overcharged.Count > 0)
                        isOverCharged = true;
                    else
                        isOverCharged = false;
                }

            }
            catch (Exception e)
            {
                logs.Error("Exception in OverChargeCheck: " + e);
            }
            return isOverCharged;
        }

        public static int GetSuccessfulIncomeByDate(Type entityType, DateTime date)
        {
            int charge = 0;
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    if (date.Date != DateTime.Now.Date)
                    {
                        charge = ((IEnumerable)entity.Singlecharges).Cast<dynamic>()
                            .Where(o => o.DateCreated.Date >= date.Date && o.IsSucceeded == true && o.Price > 0).ToList().Sum(o => o.Price);
                    }
                    else
                    {
                        charge = ((IEnumerable)entity.Singlecharges).Cast<dynamic>()
                            .Where(o => o.DateCreated.Date == date.Date && o.IsSucceeded == true && o.Price > 0).ToList().Sum(o => o.Price);
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetSuccessfulIncomeByDate: ", e);
            }
            return charge;
        }

        public static int? GetIncomeDifferenceByHourPercent(Type entityType)
        {
            try
            {
                var yesterdayIncome = GetIncomeByHour(entityType, DateTime.Now.AddDays(-1));
                var toadyIncome = GetIncomeByHour(entityType, DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (yesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = yesterdayIncome[pastHour];
                if (toadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = toadyIncome[pastHour];

                var incomePercentageDifference = SharedLibrary.HelpfulFunctions.CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                return incomePercentageDifference;
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDifferenceByHourPercent: ", e);
            }
            return null;
        }


        public static int? GetIncomeDifferenceByCyclePercent(Type entityType)
        {
            try
            {
                var yesterdayIncome = GetIncomeByHour(entityType, DateTime.Now.AddDays(-1));
                var toadyIncome = GetIncomeByHour(entityType, DateTime.Now);
                var pastHour = DateTime.Now.AddHours(-1).Hour;
                var yesterdayPastHourIncome = 0;
                var todayPastHourIncome = 0;
                if (yesterdayIncome.ContainsKey(pastHour))
                    yesterdayPastHourIncome = yesterdayIncome[pastHour];
                if (toadyIncome.ContainsKey(pastHour))
                    todayPastHourIncome = toadyIncome[pastHour];
                var incomePercentageDifference = SharedLibrary.HelpfulFunctions.CaluculatePercentageDifference(yesterdayPastHourIncome, todayPastHourIncome);
                return incomePercentageDifference;
            }
            catch (Exception e)
            {
                logs.Error("Exception in IncomeDifferenceByCyclePercent: ", e);
            }
            return null;
        }

        private static Dictionary<int, int> GetIncomeByHour(Type entityType, DateTime date)
        {
            try
            {
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    var income = new Dictionary<int, int>();
                    if (date.Date != DateTime.Now.Date)
                    {
                        income = ((IEnumerable)entity.SinglechargeArchives).Cast<dynamic>().Where(o => o.IsSucceeded == true && o.DateCreated.Date == date.Date)
                        .GroupBy(o => o.DateCreated.Hour)
                        .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                        .ToDictionary(o => (int)o.Hour, o => o.Amount);
                    }
                    else
                    {
                        income = ((IEnumerable)entity.Singlecharges).Cast<dynamic>().Where(o => o.IsSucceeded == true && o.DateCreated.Date == date.Date)
                        .GroupBy(o => o.DateCreated.Hour)
                        .Select(o => new { Hour = o.Key, Amount = o.Sum(x => x.Price) })
                        .ToDictionary(o => (int)o.Hour, o => o.Amount);
                    }
                    return income;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetIncomeByHour: ", e);
            }
            return null;
        }
    }
}
