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
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.IndexOption;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Static class contains common utility methods specific to symbols representing the option contracts
    /// </summary>
    public static class OptionSymbol
    {
        private static readonly Dictionary<string, byte> _optionExpirationErrorLog = new();

        /// <summary>
        /// Returns true if the option is a standard contract that expires 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static bool IsStandardContract(Symbol symbol)
        {
            return IsStandard(symbol);
        }

        /// <summary>
        /// Returns true if the option is a standard contract that expires 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static bool IsStandard(Symbol symbol)
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
        /// Returns true if the option is a weekly contract that expires on Friday , except 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static bool IsWeekly(Symbol symbol)
        {
            return !IsStandard(symbol) && symbol.ID.Date.DayOfWeek == DayOfWeek.Friday;
        }

        /// <summary>
        /// Maps the option ticker to it's underlying
        /// </summary>
        /// <param name="optionTicker">The option ticker to map</param>
        /// <param name="securityType">The security type of the option or underlying</param>
        /// <returns>The underlying ticker</returns>
        public static string MapToUnderlying(string optionTicker, SecurityType securityType)
        {
            if(securityType == SecurityType.FutureOption || securityType == SecurityType.Future)
            {
                return FuturesOptionsSymbolMappings.MapFromOption(optionTicker);
            }
            else if (securityType == SecurityType.IndexOption || securityType == SecurityType.Index)
            {
                return IndexOptionSymbol.MapToUnderlying(optionTicker);
            }

            return optionTicker;
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

            if (IsStandard(symbol) &&
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
        /// Returns the actual expiration date time, adjusted to market close of the expiration day.
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <returns>The expiration date time, adjusted to market close of the expiration day</returns>
        public static DateTime GetExpirationDateTime(Symbol symbol)
        {
            if (!TryGetExpirationDateTime(symbol, out var expiryTime, out _))
            {
                throw new ArgumentException("The symbol must be an option type");
            }

            return expiryTime;
        }

        /// <summary>
        /// Returns true if the option contract is expired at the specified time
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <returns>True if the option contract is expired at the specified time, false otherwise</returns>
        public static bool IsOptionContractExpired(Symbol symbol, DateTime currentTimeUtc)
        {
            if (TryGetExpirationDateTime(symbol, out var expiryTime, out var exchangeHours))
            {
                var currentTime = currentTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);
                return currentTime >= expiryTime;
            }

            return false;
        }

        private static bool TryGetExpirationDateTime(Symbol symbol, out DateTime expiryTime, out SecurityExchangeHours exchangeHours)
        {
            if (!symbol.SecurityType.IsOption())
            {
                expiryTime = default;
                exchangeHours = null;
                return false;
            }

            exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            // Ideally we can calculate expiry on the date of the symbol ID, but if that exchange is not open on that day we
            // will consider expired on the last trading day close before this; Example in AddOptionContractExpiresRegressionAlgorithm
            var lastTradingDay = exchangeHours.IsDateOpen(symbol.ID.Date)
                ? symbol.ID.Date
                : exchangeHours.GetPreviousTradingDay(symbol.ID.Date);

            expiryTime = exchangeHours.GetNextMarketClose(lastTradingDay, false);

            // Once bug 6189 was solved in ´GetNextMarketClose()´ there was found possible bugs on some futures symbol.ID.Date or delisting/liquidation handle event.
            // Specifically see 'DelistingFutureOptionRegressionAlgorithm' where Symbol.ID.Date: 4/1/2012 00:00 ExpiryTime: 4/2/2012 16:00 for Milk 3 futures options.
            // See 'bug-milk-class-3-future-options-expiration' branch. So let's limit the expiry time to up to end of day of expiration
            if (expiryTime >= symbol.ID.Date.AddDays(1).Date)
            {
                lock (_optionExpirationErrorLog)
                {
                    if (symbol.ID.Underlying != null
                        // let's log this once per underlying and expiration date: avoiding the same log for multiple option contracts with different strikes/rights
                        && _optionExpirationErrorLog.TryAdd($"{symbol.ID.Underlying}-{symbol.ID.Date}", 1))
                    {
                        Logging.Log.Error($"OptionSymbol.IsOptionContractExpired(): limiting unexpected option expiration time for symbol {symbol.ID}. Symbol.ID.Date {symbol.ID.Date}. ExpiryTime: {expiryTime}");
                    }
                }
                expiryTime = symbol.ID.Date.AddDays(1).Date;
            }

            // Standard index options are AM-settled, which means they settle the morning after the last trading day
            if (symbol.SecurityType == SecurityType.IndexOption && IsStandard(symbol))
            {
                expiryTime = exchangeHours.GetNextMarketOpen(expiryTime, false);
            }

            return true;
        }
    }
}
