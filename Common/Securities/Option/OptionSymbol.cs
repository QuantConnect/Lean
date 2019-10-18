/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Static class contains common utility methods specific to symbols representing the option contracts
    /// </summary>
    public static class OptionSymbol
    {
        /// <summary>
        /// Returns true if the option is a standard contract that expires 3rd Friday of the month
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
        /// Returns the last trading date for the option contract
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

        /// <summary>
        /// Returns true if the option contract is expired at the specified time
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <returns>True if the option contract is expired at the specified time, false otherwise</returns>
        public static bool IsOptionContractExpired(Symbol symbol, DateTime currentTimeUtc)
        {
            if (symbol.SecurityType != SecurityType.Option)
            {
                return false;
            }

            var exchangeHours = MarketHoursDatabase.FromDataFolder()
                .GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var currentTime = currentTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);
            var expiryTime = exchangeHours.GetNextMarketClose(symbol.ID.Date, false);

            return currentTime >= expiryTime;
        }

    }
}
