using System;

namespace TitanCalc.Modules
{
    public struct AgeBreakdown
    {
        public int Years;
        public int Months;
        public int Days;
        public int Hours;
        public int Minutes;
        public int Seconds;
        public double TotalDays;

        public override string ToString()
        {
            return string.Format("{0} Years, {1} Months, {2} Days, {3}h {4}m {5}s",
                Years, Months, Days, Hours, Minutes, Seconds);
        }
    }

    /// <summary>
    /// Chronological age calculator and date arithmetic engine.
    /// </summary>
    public static class DateChronology
    {
        public static AgeBreakdown CalculateAge(DateTime birthDate, DateTime targetDate)
        {
            if (targetDate < birthDate)
            {
                DateTime tmp = birthDate;
                birthDate = targetDate;
                targetDate = tmp;
            }

            int years = targetDate.Year - birthDate.Year;
            int months = targetDate.Month - birthDate.Month;
            int days = targetDate.Day - birthDate.Day;
            int hours = targetDate.Hour - birthDate.Hour;
            int minutes = targetDate.Minute - birthDate.Minute;
            int seconds = targetDate.Second - birthDate.Second;

            if (seconds < 0)
            {
                seconds += 60;
                minutes--;
            }

            if (minutes < 0)
            {
                minutes += 60;
                hours--;
            }

            if (hours < 0)
            {
                hours += 24;
                days--;
            }

            if (days < 0)
            {
                // Borrow days from previous month
                DateTime prevMonth = targetDate.AddMonths(-1);
                int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
                days += daysInPrevMonth;
                months--;
            }

            if (months < 0)
            {
                months += 12;
                years--;
            }

            AgeBreakdown ab = new AgeBreakdown();
            ab.Years = years;
            ab.Months = months;
            ab.Days = days;
            ab.Hours = hours;
            ab.Minutes = minutes;
            ab.Seconds = seconds;
            ab.TotalDays = (targetDate - birthDate).TotalDays;
            return ab;
        }

        public static int CountBusinessDays(DateTime start, DateTime end)
        {
            if (start > end)
            {
                DateTime tmp = start;
                start = end;
                end = tmp;
            }

            int count = 0;
            DateTime current = start.Date.AddDays(1);
            DateTime target = end.Date;

            while (current <= target)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    count++;
                }
                current = current.AddDays(1);
            }

            return count;
        }

        public static DateTime AddBusinessDays(DateTime start, int businessDays)
        {
            int step = businessDays < 0 ? -1 : 1;
            int remaining = Math.Abs(businessDays);
            DateTime current = start;

            while (remaining > 0)
            {
                current = current.AddDays(step);
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    remaining--;
                }
            }

            return current;
        }
    }
}
