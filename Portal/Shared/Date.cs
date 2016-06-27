using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Shared
{
    public class Date
    {
        public static string GetPersianDate(DateTime? dt = null)
        {
            if (dt == null)
                dt = DateTime.Now;
            var calendar = new System.Globalization.PersianCalendar();

            int Year = calendar.GetYear(dt.Value);
            int Month = calendar.GetMonth(dt.Value);
            int Day = calendar.GetDayOfMonth(dt.Value);

            return Year.ToString() + "-" + Month.ToString().PadLeft(2, '0') + "-" + Day.ToString().PadLeft(2, '0');
        }

        public static string GetPersianDateTime(DateTime? dt = null)
        {
            if (dt == null)
                dt = DateTime.Now;

            var dateTime = GetPersianDate(dt) + " " + dt.Value.ToString("HH:mm");
            return dateTime;
        }

        public static DateTime GetGregorianDate(string persianDate)
        {
            var persianDateArray = persianDate.Split('-');
            var persianCal = new System.Globalization.PersianCalendar();
            DateTime gregorianDate = persianCal.ToDateTime(Convert.ToInt32(persianDateArray[0]), Convert.ToInt32(persianDateArray[1]), Convert.ToInt32(persianDateArray[2]), 0, 0, 0, 0);
            return gregorianDate;
        }
    }
}