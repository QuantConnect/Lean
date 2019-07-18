using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Static class contains common utility methods specific to symbols representing the option contracts
    /// </summary>
    public static class OptionSymbol
    {
        /// <summary>
        /// Returns true is the option is a standard contract that expire 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static bool IsStandardContract(Symbol symbol)
        {
            var date = symbol.ID.Date;

            // first we find out the day of week of the first day in the month
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1).DayOfWeek;

            // find out the day of first Friday in this month
            var firstFriday = firstDayOfMonth == DayOfWeek.Saturday ? 7 : 6 - (int)firstDayOfMonth;

            // check if the expiration date is within the week containing 3rd Friday
            // we exclude monday, wednesday, and friday weeklys
            return firstFriday + 7 + 5 /*sat -> wed */ < date.Day && date.Day < firstFriday + 2 * 7 + 2 /* sat, sun*/;
        }

        /// <summary>
        /// Returns lat trading date for the option contract
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static DateTime GetLastDayOfTrading(Symbol symbol)
        {
            // The OCC proposed rule change: starting from 1 Feb 2015 standard monthly contracts
            // expire on 3rd Friday, not Saturday following 3rd Friday as it was before.
            // More details: https://www.sec.gov/rules/sro/occ/2013/34-69480.pdf

            int daysBefore = 0;
            var symbolDateTime = symbol.ID.Date;

            if (IsStandardContract(symbol) &&
                symbolDateTime.DayOfWeek == DayOfWeek.Saturday &&
                symbolDateTime < new DateTime(2015, 2, 1))
            {
                daysBefore--;
            }

            var exchangeHours = MarketHoursDatabase.FromDataFolder()
                                              .GetEntry(symbol.ID.Market, symbol, symbol.SecurityType)
                                              .ExchangeHours;

            while (!exchangeHours.IsDateOpen(symbolDateTime.AddDays(daysBefore)))
            {
                daysBefore--;
            }

            return symbolDateTime.AddDays(daysBefore).Date;
        }
    }
}
