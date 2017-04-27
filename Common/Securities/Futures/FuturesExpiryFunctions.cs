using System;
using System.Collections.Generic;

namespace QuantConnect.Securities.Future
{
    public class FuturesExpiryFunctions
    {
        public static Func<DateTime, DateTime> FuturesExpiryFunction(string symbol)
        {
            if (FuturesExpiryDictionary.ContainsKey(symbol.ToUpper()))
            {
                return FuturesExpiryDictionary[symbol.ToUpper()];
            }

            // If func for expiry cannot be found pass the date through
            return (date) => date;
        }

        public static Dictionary<string, Func<DateTime, DateTime>> FuturesExpiryDictionary = new Dictionary<string, Func<DateTime, DateTime>>()
        {
            {Futures.Metals.Gold, (time =>
                {
                    // Trading terminates on the third last business day of the delivery month.
                    var daysInMonth = DateTime.DaysInMonth(time.Year, time.Month);
                    var lastDayOfMonth = new DateTime(time.Year, time.Month, daysInMonth);
                    var holidays = Time.CommonAmericanHolidays(time.Year);

                    // Count the number of days in the month after the third to last business day
                    var businessDays = 3;
                    var totalDays = 0;
                    do
                    {
                        var previousDay = lastDayOfMonth.AddDays(-totalDays);
                        if (previousDay.IsCommonBusinessDay() && !holidays.Contains(previousDay))
                        {
                            businessDays--;
                        }
                        if (businessDays > 0) totalDays++;
                    } while (businessDays > 0);

                    return lastDayOfMonth.AddDays(-totalDays);
                })
            }
        };
    }
}
