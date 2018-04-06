using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Notify
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static bool? OverChargeCheck(dynamic entity, int maxChargeLimit)
        {
            bool? isOverCharged = null;
            try
            {
                var dbName = entity.Database.Connection.Database;
                var overcharged = RawSql.DynamicSqlQuery(entity.Database, @"SELECT MobileNumber, SUM(Price) as Price
  FROM " + dbName + @".[dbo].Singlecharge with (nolock) WHERE CONVERT(date,DateCreated) = CONVERT(date,@TODAY) AND Price > 0 AND IsSucceeded = 1
  GROUP BY MobileNumber
  HAVING SUM(price) > @MAXCHARGE
  ORDER BY SUM(Price) desc", new SqlParameter("@MAXCHARGE", maxChargeLimit), new SqlParameter("@TODAY", DateTime.Now));

                foreach (var item in overcharged)
                {
                    isOverCharged = true;
                    break;
                }
                if (isOverCharged == null)
                    isOverCharged = false;
            }
            catch (Exception e)
            {
                logs.Error("Exception in OverChargeCheck: " + e);
            }
            return isOverCharged;
        }

        public static bool? MoQueueCheck(dynamic entity)
        {
            bool? isOverQueued = null;
            try
            {
                var dbName = entity.Database.Connection.Database;
                var queueSize = RawSql.DynamicSqlQuery(entity.Database, @"SELECT COUNT(*) as c
  FROM " + dbName + @".[dbo].[ReceievedMessages] with (nolock) WHERE IsProcessed = 0 HAVING COUNT(*) > 400");

                foreach (var item in queueSize)
                {
                    isOverQueued = true;
                    break;
                }
                if (isOverQueued == null)
                    isOverQueued = false;
            }
            catch (Exception e)
            {
                logs.Error("Exception in MoQueueCheck: " + e);
            }
            return isOverQueued;
        }

        public static int GetSuccessfulIncomeByDate(dynamic entity, DateTime date)
        {
            int charge = 0;
            try
            {
                var dbName = entity.Database.Connection.Database;
                if (date.Date != DateTime.Now.Date)
                {
                    var chargeFromDb = RawSql.DynamicSqlQuery(entity.Database, @"SELECT SUM(Price) as Price
  FROM " + dbName + @".[dbo].vw_Singlecharge with (nolock) WHERE CONVERT(date,DateCreated) >= CONVERT(date,@date) AND Price > 0 AND IsSucceeded = 1", new SqlParameter("@date", date));

                    foreach (var item in chargeFromDb)
                    {
                        charge = item.Price;
                        break;
                    }
                }
                else
                {

                    var chargeFromDb = RawSql.DynamicSqlQuery(entity.Database, @"SELECT SUM(Price) as Price
  FROM " + dbName + @".[dbo].Singlecharge with (nolock) WHERE CONVERT(date,DateCreated) = CONVERT(date,@TODAY) AND Price > 0 AND IsSucceeded = 1", new SqlParameter("@TODAY", date));

                    foreach (var item in chargeFromDb)
                    {
                        charge = item.Price;
                        break;
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
                var income = new Dictionary<int, int>();
                using (dynamic entity = Activator.CreateInstance(entityType))
                {
                    entity.Database.ExecuteSqlCommand("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");

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
                    entity.Dispose();
                }
                return income;
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetIncomeByHour: ", e);
            }
            return null;
        }
    }
}
