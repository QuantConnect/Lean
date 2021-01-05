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

namespace QuantConnect
{
    /// <summary>
    /// Enum lists available trading events
    /// </summary>
    public enum TradingDayType
    {
        /// <summary>
        /// Business day
        /// </summary>
        BusinessDay,

        /// <summary>
        /// Public Holiday
        /// </summary>
        PublicHoliday,

        /// <summary>
        /// Weekend
        /// </summary>
        Weekend,

        /// <summary>
        /// Option Expiration Date
        /// </summary>
        OptionExpiration,

        /// <summary>
        /// Futures Expiration Date
        /// </summary>
        FutureExpiration,

        /// <summary>
        /// Futures Roll Date
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        FutureRoll,

        /// <summary>
        /// Symbol Delisting Date
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        SymbolDelisting,

        /// <summary>
        /// Equity Ex-dividend Date
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        EquityDividends,

        /// <summary>
        /// FX Economic Event
        /// </summary>
        /// <remarks>FX Economic Event e.g. from DailyFx (DailyFx.cs). Not used yet. For future use.</remarks>
        EconomicEvent
    }

    /// <summary>
    /// Class contains trading events associated with particular day in <see cref="TradingCalendar"/>
    /// </summary>
    public class TradingDay
    {
        /// <summary>
        /// The date that this instance is associated with
        /// </summary>
        public DateTime Date { get; internal set; }

        /// <summary>
        /// Property returns true, if the day is a business day
        /// </summary>
        public bool BusinessDay { get; internal set; }

        /// <summary>
        /// Property returns true, if the day is a public holiday
        /// </summary>
        public bool PublicHoliday { get; internal set; }

        /// <summary>
        /// Property returns true, if the day is a weekend
        /// </summary>
        public bool Weekend { get; internal set; }

        /// <summary>
        /// Property returns the list of options (among currently traded) that expire on this day
        /// </summary>
        public IEnumerable<Symbol> OptionExpirations { get; internal set; }

        /// <summary>
        /// Property returns the list of futures (among currently traded) that expire on this day
        /// </summary>
        public IEnumerable<Symbol> FutureExpirations { get; internal set; }

        /// <summary>
        /// Property returns the list of futures (among currently traded) that roll forward on this day
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        public IEnumerable<Symbol> FutureRolls { get; internal set; }

        /// <summary>
        /// Property returns the list of symbols (among currently traded) that are delisted on this day
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        public IEnumerable<Symbol> SymbolDelistings { get; internal set; }

        /// <summary>
        /// Property returns the list of symbols (among currently traded) that have ex-dividend date on this day
        /// </summary>
        /// <remarks>Not used yet. For future use.</remarks>
        public IEnumerable<Symbol> EquityDividends { get; internal set; }
    }
}
