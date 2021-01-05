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
using System.Linq;
using QuantConnect.Securities;
using QLNet;

namespace QuantConnect
{
    /// <summary>
    /// Class represents trading calendar, populated with variety of events relevant to currently trading instruments
    /// </summary>
    public class TradingCalendar
    {
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly SecurityManager _securityManager;

        public TradingCalendar(SecurityManager securityManager, MarketHoursDatabase marketHoursDatabase)
        {
            _securityManager = securityManager;
            _marketHoursDatabase = marketHoursDatabase;
        }
        /// <summary>
        /// Method returns <see cref="TradingDay"/> that contains trading events associated with today's date
        /// </summary>
        /// <returns>Populated instance of <see cref="TradingDay"/></returns>
        public TradingDay GetTradingDay()
        {
            var today = _securityManager.UtcTime.Date;

            return GetTradingDay(today);
        }

        /// <summary>
        /// Method returns <see cref="TradingDay"/> that contains trading events associated with the given date
        /// </summary>
        /// <returns>Populated instance of <see cref="TradingDay"/></returns>
        public TradingDay GetTradingDay(DateTime day)
        {
            return GetTradingDays(day, day).First();
        }

        /// <summary>
        /// Method returns <see cref="TradingDay"/> that contains trading events associated with the range of dates
        /// </summary>
        /// <param name="start">Start date of the range (inclusive)</param>
        /// <param name="end">End date of the range (inclusive)</param>
        /// <returns>>Populated list of <see cref="TradingDay"/></returns>
        public IEnumerable<TradingDay> GetTradingDays(DateTime start, DateTime end)
        {
            return PopulateTradingDays(start, end);
        }

        /// <summary>
        /// Method returns <see cref="TradingDay"/> of the specified type (<see cref="TradingDayType"/>) that contains trading events associated with the range of dates
        /// </summary>
        /// <param name="type">Type of the events</param>
        /// <param name="start">Start date of the range (inclusive)</param>
        /// <param name="end">End date of the range (inclusive)</param>
        /// <returns>>Populated list of <see cref="TradingDay"/></returns>
        public IEnumerable<TradingDay> GetDaysByType(TradingDayType type, DateTime start, DateTime end)
        {
            Func<TradingDay, bool> typeFilter = day =>
                {
                    switch (type)
                    {
                        case TradingDayType.BusinessDay:
                            return day.BusinessDay;
                        case TradingDayType.PublicHoliday:
                            return day.PublicHoliday;
                        case TradingDayType.Weekend:
                            return day.Weekend;
                        case TradingDayType.OptionExpiration:
                            return day.OptionExpirations.Any();
                        case TradingDayType.FutureExpiration:
                            return day.FutureExpirations.Any();
                        case TradingDayType.FutureRoll:
                            return day.FutureRolls.Any();
                        case TradingDayType.SymbolDelisting:
                            return day.SymbolDelistings.Any();
                        case TradingDayType.EquityDividends:
                            return day.EquityDividends.Any();
                    };
                    return false;
                };
            return GetTradingDays(start, end).Where(typeFilter);
        }


        private IEnumerable<TradingDay> PopulateTradingDays(DateTime start, DateTime end)
        {
            var symbols = _securityManager.Keys;

            var holidays = new HashSet<DateTime>();
            foreach (var symbol in symbols)
            {
                var entry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);

                foreach (var holiday in entry.ExchangeHours.Holidays)
                {
                    holidays.Add(holiday.Date);
                }
            }

            var qlCalendar = new UnitedStates();
            var options = symbols.Where(x => x.ID.SecurityType == SecurityType.Option || x.ID.SecurityType == SecurityType.FutureOption).ToList();
            var futures = symbols.Where(x => x.ID.SecurityType == SecurityType.Future).ToList();

            foreach (var dayIdx in Enumerable.Range(0, (int)(end.Date.AddDays(1.0) - start.Date).TotalDays))
            {
                var currentDate = start.Date.AddDays(dayIdx);

                var publicHoliday = holidays.Contains(currentDate) || !qlCalendar.isBusinessDay(currentDate);
                var weekend = currentDate.DayOfWeek == DayOfWeek.Sunday ||
                                currentDate.DayOfWeek == DayOfWeek.Saturday;
                var businessDay = !publicHoliday && !weekend;

                yield return
                    new TradingDay
                    {
                        Date = currentDate,
                        PublicHoliday = publicHoliday,
                        Weekend = weekend,
                        BusinessDay = businessDay,
                        OptionExpirations = options.Where(x => x.ID.Date.Date == currentDate),
                        FutureExpirations = futures.Where(x => x.ID.Date.Date == currentDate)
                    };
            }
        }
    }
}
