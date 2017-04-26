using System;
using System.Collections.Generic;

namespace QuantConnect.Securities.Future
{
    public class FuturesExpiryFunctions
    {
        public static Func<DateTime, DateTime> FuturesExpiryFunction(Symbol symbol)
        {
            if (FuturesExpiryDictionary.ContainsKey(symbol.ID.Symbol))
            {
                return FuturesExpiryDictionary[symbol.ID.Symbol];
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
                    var daysAfterThirdToLastBusinessDay = 0;
                    var i = 0;
                    while (daysAfterThirdToLastBusinessDay < 3)
                    {
                        var previousDay = lastDayOfMonth.AddDays(-i);
                        if (previousDay.IsCommonBusinessDay() && !holidays.Contains(previousDay))
                        {
                            daysAfterThirdToLastBusinessDay++;
                        }

                        if (daysAfterThirdToLastBusinessDay < 3) i++;
                    }

                    return lastDayOfMonth.AddDays(-i);
                })
            }
        };
    }
}
