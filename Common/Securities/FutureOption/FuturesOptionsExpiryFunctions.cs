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

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Futures options expiry lookup utility class
    /// </summary>
    public static class FuturesOptionsExpiryFunctions
    {
        private static readonly MarketHoursDatabase _mhdb = MarketHoursDatabase.FromDataFolder();

        private static readonly Symbol _lo = Symbol.CreateOption(Symbol.Create("CL", SecurityType.Future, Market.NYMEX), Market.NYMEX, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _on = Symbol.CreateOption(Symbol.Create("NG", SecurityType.Future, Market.NYMEX), Market.NYMEX, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozb = Symbol.CreateOption(Symbol.Create("ZB", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozc = Symbol.CreateOption(Symbol.Create("ZC", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozn = Symbol.CreateOption(Symbol.Create("ZN", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozs = Symbol.CreateOption(Symbol.Create("ZS", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozt = Symbol.CreateOption(Symbol.Create("ZT", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _ozw = Symbol.CreateOption(Symbol.Create("ZW", SecurityType.Future, Market.CBOT), Market.CBOT, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _hxe = Symbol.CreateOption(Symbol.Create("HG", SecurityType.Future, Market.COMEX), Market.COMEX, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _og = Symbol.CreateOption(Symbol.Create("GC", SecurityType.Future, Market.COMEX), Market.COMEX, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);
        private static readonly Symbol _so = Symbol.CreateOption(Symbol.Create("SI", SecurityType.Future, Market.COMEX), Market.COMEX, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);

        /// <summary>
        /// Futures options expiry functions lookup table, keyed by canonical future option Symbol
        /// </summary>
        private static readonly IReadOnlyDictionary<Symbol, Func<DateTime, DateTime>> _futuresOptionExpiryFunctions = new Dictionary<Symbol, Func<DateTime,DateTime>>
        {
            // Trading terminates 7 business days before the 26th calendar of the month prior to the contract month. https://www.cmegroup.com/trading/energy/crude-oil/light-sweet-crude_contractSpecs_options.html#optionProductId=190
            {_lo, expiryMonth => {
                var twentySixthDayOfPreviousMonthFromContractMonth = expiryMonth.AddMonths(-1).AddDays(-(expiryMonth.Day - 1)).AddDays(25);
                var holidays = _mhdb.GetEntry(_lo.ID.Market, _lo.Underlying, SecurityType.Future)
                    .ExchangeHours
                    .Holidays;

                return FuturesExpiryUtilityFunctions.AddBusinessDays(twentySixthDayOfPreviousMonthFromContractMonth, -7, holidays);
            }},
            // Trading terminates on the 4th last business day of the month prior to the contract month (1 business day prior to the expiration of the underlying futures corresponding contract month).
            // https://www.cmegroup.com/trading/energy/natural-gas/natural-gas_contractSpecs_options.html
            // Although not stated, this follows the same rules as seen in the COMEX markets, but without Fridays. Case: Dec 2020 expiry, Last Trade Date: 24 Nov 2020
            { _on, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_on.Underlying, expiryMonth, 0, 0, noFridays: false) },
            { _ozb, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozb.Underlying, expiryMonth) },
            { _ozc, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozc.Underlying, expiryMonth) },
            { _ozn, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozn.Underlying, expiryMonth) },
            { _ozs, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozs.Underlying, expiryMonth) },
            { _ozt, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozt.Underlying, expiryMonth) },
            { _ozw, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozw.Underlying, expiryMonth) },
            { _hxe, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_hxe.Underlying, expiryMonth, 12, 0) },
            { _og, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_og.Underlying, expiryMonth, 12, 30) },
            { _so, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_so.Underlying, expiryMonth, 12, 25) },
        };

        /// <summary>
        /// Gets the Futures Options' expiry for the given contract month.
        /// </summary>
        /// <param name="canonicalFutureOptionSymbol">Canonical Futures Options Symbol. Will be made canonical if not provided a canonical</param>
        /// <param name="futureContractMonth">Contract month of the underlying Future</param>
        /// <returns>Expiry date/time</returns>
        public static DateTime FuturesOptionExpiry(Symbol canonicalFutureOptionSymbol, DateTime futureContractMonth)
        {
            if (!canonicalFutureOptionSymbol.IsCanonical() || !canonicalFutureOptionSymbol.Underlying.IsCanonical())
            {
                canonicalFutureOptionSymbol = Symbol.CreateOption(
                    Symbol.Create(canonicalFutureOptionSymbol.Underlying.ID.Symbol, SecurityType.Future, canonicalFutureOptionSymbol.Underlying.ID.Market),
                    canonicalFutureOptionSymbol.ID.Market,
                    default(OptionStyle),
                    default(OptionRight),
                    default(decimal),
                    SecurityIdentifier.DefaultDate);
            }

            Func<DateTime, DateTime> expiryFunction;
            if (!_futuresOptionExpiryFunctions.TryGetValue(canonicalFutureOptionSymbol, out expiryFunction))
            {
                // No definition exists for this FOP. Let's default to futures expiry.
                return FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFutureOptionSymbol.Underlying)(futureContractMonth);
            }

            return expiryFunction(futureContractMonth);
        }

        /// <summary>
        /// Gets the Future Option's expiry from the Future Symbol provided
        /// </summary>
        /// <param name="futureSymbol">Future (non-canonical) Symbol</param>
        /// <param name="canonicalFutureOption">The canonical Future Option Symbol</param>
        /// <returns>Future Option Expiry for the Future with the same contract month</returns>
        public static DateTime GetFutureOptionExpiryFromFutureExpiry(Symbol futureSymbol, Symbol canonicalFutureOption = null)
        {
            var futureContractMonthDelta = FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(futureSymbol.ID.Symbol, futureSymbol.ID.Date);
            var futureContractMonth = new DateTime(
                    futureSymbol.ID.Date.Year,
                    futureSymbol.ID.Date.Month,
                    1)
                .AddMonths(futureContractMonthDelta);

            if (canonicalFutureOption == null)
            {
                canonicalFutureOption = Symbol.CreateOption(
                    Symbol.Create(futureSymbol.ID.Symbol, SecurityType.Future, futureSymbol.ID.Market),
                    futureSymbol.ID.Market,
                    default(OptionStyle),
                    default(OptionRight),
                    default(decimal),
                    SecurityIdentifier.DefaultDate);
            }

            return FuturesOptionExpiry(canonicalFutureOption, futureContractMonth);
        }

        /// <summary>
        /// Expiry function for CBOT Futures Options entries.
        /// Returns the Friday before the 2nd last business day of the month preceding the future contract expiry month.
        /// </summary>
        /// <param name="underlyingFuture">Underlying future symbol</param>
        /// <param name="expiryMonth">Expiry month date</param>
        /// <returns>Expiry DateTime of the Future Option</returns>
        private static DateTime FridayBeforeTwoBusinessDaysBeforeEndOfMonth(Symbol underlyingFuture, DateTime expiryMonth)
        {
            var holidays = _mhdb.GetEntry(underlyingFuture.ID.Market, underlyingFuture, SecurityType.Future)
                .ExchangeHours
                .Holidays;

            var expiryMonthPreceding = expiryMonth.AddMonths(-1).AddDays(-(expiryMonth.Day - 1));
            var fridayBeforeSecondLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(
                expiryMonthPreceding,
                2,
                holidayList: holidays).AddDays(-1);

            while (fridayBeforeSecondLastBusinessDay.DayOfWeek != DayOfWeek.Friday)
            {
                fridayBeforeSecondLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(fridayBeforeSecondLastBusinessDay, -1, holidays);
            }

            return fridayBeforeSecondLastBusinessDay;
        }

        /// <summary>
        /// For Trading that terminates on the 4th last business day of the month prior to the contract month.
        /// If the 4th last business day occurs on a Friday or the day before a holiday, trading terminates on the
        /// prior business day. This applies to some NYMEX (with fridays), all COMEX.
        /// </summary>
        /// <param name="underlyingFuture">Underlying Future Symbol</param>
        /// <param name="expiryMonth">Contract expiry month</param>
        /// <param name="hour">Hour the contract expires at</param>
        /// <param name="minutes">Minute the contract expires at</param>
        /// <param name="noFridays">Exclude Friday expiration dates from consideration</param>
        /// <returns>Expiry DateTime of the Future Option</returns>
        private static DateTime FourthLastBusinessDayInPrecedingMonthFromContractMonth(Symbol underlyingFuture, DateTime expiryMonth, int hour, int minutes, bool noFridays = true)
        {
            var holidays = _mhdb.GetEntry(underlyingFuture.ID.Market, underlyingFuture, SecurityType.Future)
                .ExchangeHours
                .Holidays;

            var expiryMonthPreceding = expiryMonth.AddMonths(-1);
            var fourthLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(expiryMonthPreceding, 4, holidayList: holidays);

            if (noFridays)
            {
                while (fourthLastBusinessDay.DayOfWeek == DayOfWeek.Friday || holidays.Contains(fourthLastBusinessDay.AddDays(1)))
                {
                    fourthLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(fourthLastBusinessDay, -1, holidays);
                }
            }

            return fourthLastBusinessDay.AddHours(hour).AddMinutes(minutes);
        }
    }
}
