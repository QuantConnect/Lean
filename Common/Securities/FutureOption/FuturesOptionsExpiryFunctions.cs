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
        private static readonly Symbol _lo = Symbol.CreateCanonicalOption(Symbol.Create("CL", SecurityType.Future, Market.NYMEX));
        private static readonly Symbol _on = Symbol.CreateCanonicalOption(Symbol.Create("NG", SecurityType.Future, Market.NYMEX));
        private static readonly Symbol _ozm = Symbol.CreateCanonicalOption(Symbol.Create("ZM", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozb = Symbol.CreateCanonicalOption(Symbol.Create("ZB", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozc = Symbol.CreateCanonicalOption(Symbol.Create("ZC", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozn = Symbol.CreateCanonicalOption(Symbol.Create("ZN", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _otn = Symbol.CreateCanonicalOption(Symbol.Create("TN", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _oub = Symbol.CreateCanonicalOption(Symbol.Create("UB", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozo = Symbol.CreateCanonicalOption(Symbol.Create("ZO", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _oke = Symbol.CreateCanonicalOption(Symbol.Create("KE", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozf = Symbol.CreateCanonicalOption(Symbol.Create("ZF", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozs = Symbol.CreateCanonicalOption(Symbol.Create("ZS", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozt = Symbol.CreateCanonicalOption(Symbol.Create("ZT", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozl = Symbol.CreateCanonicalOption(Symbol.Create("ZL", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _ozw = Symbol.CreateCanonicalOption(Symbol.Create("ZW", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _oym = Symbol.CreateCanonicalOption(Symbol.Create("YM", SecurityType.Future, Market.CBOT));
        private static readonly Symbol _hxe = Symbol.CreateCanonicalOption(Symbol.Create("HG", SecurityType.Future, Market.COMEX));
        private static readonly Symbol _og = Symbol.CreateCanonicalOption(Symbol.Create("GC", SecurityType.Future, Market.COMEX));
        private static readonly Symbol _so = Symbol.CreateCanonicalOption(Symbol.Create("SI", SecurityType.Future, Market.COMEX));
        private static readonly Symbol _aud = Symbol.CreateCanonicalOption(Symbol.Create("6A", SecurityType.Future, Market.CME));
        private static readonly Symbol _gbu = Symbol.CreateCanonicalOption(Symbol.Create("6B", SecurityType.Future, Market.CME));
        private static readonly Symbol _cau = Symbol.CreateCanonicalOption(Symbol.Create("6C", SecurityType.Future, Market.CME));
        private static readonly Symbol _euu = Symbol.CreateCanonicalOption(Symbol.Create("6E", SecurityType.Future, Market.CME));
        private static readonly Symbol _jpu = Symbol.CreateCanonicalOption(Symbol.Create("6J", SecurityType.Future, Market.CME));
        private static readonly Symbol _chu = Symbol.CreateCanonicalOption(Symbol.Create("6S", SecurityType.Future, Market.CME));
        private static readonly Symbol _nzd = Symbol.CreateCanonicalOption(Symbol.Create("6N", SecurityType.Future, Market.CME));
        private static readonly Symbol _le = Symbol.CreateCanonicalOption(Symbol.Create("LE", SecurityType.Future, Market.CME));
        private static readonly Symbol _he = Symbol.CreateCanonicalOption(Symbol.Create("HE", SecurityType.Future, Market.CME));
        private static readonly Symbol _lbr = Symbol.CreateCanonicalOption(Symbol.Create("LBR", SecurityType.Future, Market.CME));
        private static readonly Symbol _lbs = Symbol.CreateCanonicalOption(Symbol.Create("LBS", SecurityType.Future, Market.CME));
        private static readonly Symbol _es = Symbol.CreateCanonicalOption(Symbol.Create("ES", SecurityType.Future, Market.CME));
        private static readonly Symbol _emd = Symbol.CreateCanonicalOption(Symbol.Create("EMD", SecurityType.Future, Market.CME));
        private static readonly Symbol _nq = Symbol.CreateCanonicalOption(Symbol.Create("NQ", SecurityType.Future, Market.CME));

        /// <summary>
        /// Futures options expiry functions lookup table, keyed by canonical future option Symbol
        /// </summary>
        private static readonly IReadOnlyDictionary<Symbol, Func<DateTime, DateTime>> _futuresOptionExpiryFunctions = new Dictionary<Symbol, Func<DateTime,DateTime>>
        {
            // Trading terminates 7 business days before the 26th calendar of the month prior to the contract month. https://www.cmegroup.com/trading/energy/crude-oil/light-sweet-crude_contractSpecs_options.html#optionProductId=190
            {_lo, expiryMonth => {
                var twentySixthDayOfPreviousMonthFromContractMonth = expiryMonth.AddMonths(-1).AddDays(-(expiryMonth.Day - 1)).AddDays(25);
                var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(_lo.ID.Market, _lo.Underlying.ID.Symbol);

                return FuturesExpiryUtilityFunctions.AddBusinessDays(twentySixthDayOfPreviousMonthFromContractMonth, -7, holidays);
            }},
            // Trading terminates on the 4th last business day of the month prior to the contract month (1 business day prior to the expiration of the underlying futures corresponding contract month).
            // https://www.cmegroup.com/trading/energy/natural-gas/natural-gas_contractSpecs_options.html
            // Although not stated, this follows the same rules as seen in the COMEX markets, but without Fridays. Case: Dec 2020 expiry, Last Trade Date: 24 Nov 2020
            { _on, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_on.Underlying, expiryMonth, 0, 0, noFridays: false) },
            { _ozb, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozb.Underlying, expiryMonth) },
            { _ozc, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozc.Underlying, expiryMonth) },
            { _ozn, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozn.Underlying, expiryMonth) },
            { _otn, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_otn.Underlying, expiryMonth) },
            { _oub, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_oub.Underlying, expiryMonth) },
            { _ozo, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozo.Underlying, expiryMonth) },
            { _oke, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_oke.Underlying, expiryMonth) },
            { _ozf, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozf.Underlying, expiryMonth) },
            { _ozs, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozs.Underlying, expiryMonth) },
            { _ozt, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozt.Underlying, expiryMonth) },
            { _ozw, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozw.Underlying, expiryMonth) },
            { _ozl, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozl.Underlying, expiryMonth) },
            { _ozm, expiryMonth => FridayBeforeTwoBusinessDaysBeforeEndOfMonth(_ozm.Underlying, expiryMonth) },
            { _hxe, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_hxe.Underlying, expiryMonth, 12, 0) },
            { _og, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_og.Underlying, expiryMonth, 12, 30) },
            { _so, expiryMonth => FourthLastBusinessDayInPrecedingMonthFromContractMonth(_so.Underlying, expiryMonth, 12, 25) },
            { _aud, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_aud.Underlying, expiryMonth) },
            { _gbu, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_gbu.Underlying, expiryMonth) },
            { _cau, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_cau.Underlying, expiryMonth) },
            { _euu, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_euu.Underlying, expiryMonth) },
            { _jpu, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_jpu.Underlying, expiryMonth) },
            { _chu, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_chu.Underlying, expiryMonth) },
            { _nzd, expiryMonth => SecondFridayBeforeThirdWednesdayOfContractMonth(_nzd.Underlying, expiryMonth) },
            { _le, expiryMonth => FirstFridayOfContractMonth(_le.Underlying, expiryMonth) },
            { _he, expiryMonth => TenthBusinessDayOfContractMonth(_he.Underlying, expiryMonth) },
            { _lbr, expiryMonth => LastBusinessDayInPrecedingMonthFromContractMonth(_lbr.Underlying, expiryMonth) },
            { _lbs, expiryMonth => LastBusinessDayInPrecedingMonthFromContractMonth(_lbs.Underlying, expiryMonth) },
            // even though these FOPs are currently quarterly (as underlying), they had until some serial months. Expiration is the same rule for all
            { _es, expiryMonth => FuturesExpiryUtilityFunctions.ThirdFriday(expiryMonth, _es) },
            { _emd, expiryMonth => FuturesExpiryUtilityFunctions.ThirdFriday(expiryMonth, _emd) },
            { _oym, expiryMonth => FuturesExpiryUtilityFunctions.ThirdFriday(expiryMonth, _oym) },
            { _nq, expiryMonth => FuturesExpiryUtilityFunctions.ThirdFriday(expiryMonth, _nq) },
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
                canonicalFutureOptionSymbol = Symbol.CreateCanonicalOption(
                    Symbol.Create(canonicalFutureOptionSymbol.Underlying.ID.Symbol,
                        SecurityType.Future,
                        canonicalFutureOptionSymbol.Underlying.ID.Market));
            }

            if (!_futuresOptionExpiryFunctions.TryGetValue(canonicalFutureOptionSymbol, out var expiryFunction))
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
            var futureContractMonth = FuturesExpiryUtilityFunctions.GetFutureContractMonth(futureSymbol);

            if (canonicalFutureOption == null)
            {
                canonicalFutureOption = Symbol.CreateCanonicalOption(
                    Symbol.Create(futureSymbol.ID.Symbol, SecurityType.Future, futureSymbol.ID.Market));
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
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlyingFuture.ID.Market, underlyingFuture.ID.Symbol);

            var expiryMonthPreceding = expiryMonth.AddMonths(-1).AddDays(-(expiryMonth.Day - 1));
            var fridayBeforeSecondLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(
                expiryMonthPreceding,
                2,
                holidays).AddDays(-1);

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
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlyingFuture.ID.Market, underlyingFuture.ID.Symbol);

            var expiryMonthPreceding = expiryMonth.AddMonths(-1);
            var fourthLastBusinessDay = FuturesExpiryUtilityFunctions.NthLastBusinessDay(expiryMonthPreceding, 4, holidays);

            if (noFridays)
            {
                while (fourthLastBusinessDay.DayOfWeek == DayOfWeek.Friday || holidays.Contains(fourthLastBusinessDay.AddDays(1)))
                {
                    fourthLastBusinessDay = FuturesExpiryUtilityFunctions.AddBusinessDays(fourthLastBusinessDay, -1, holidays);
                }
            }

            return fourthLastBusinessDay.AddHours(hour).AddMinutes(minutes);
        }

        /// <summary>
        /// Expiry function for AUD Future Options expiry.
        /// Returns the second Friday before the 3rd Wednesday of contract expiry month, 9am.
        /// </summary>
        /// <param name="underlyingFuture">Underlying future symbol</param>
        /// <param name="expiryMonth">Expiry month date</param>
        /// <returns>Expiry DateTime of the Future Option</returns>
        private static DateTime SecondFridayBeforeThirdWednesdayOfContractMonth(Symbol underlyingFuture, DateTime expiryMonth)
        {
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlyingFuture.ID.Market, underlyingFuture.ID.Symbol);
            var thirdWednesday = FuturesExpiryUtilityFunctions.ThirdWednesday(expiryMonth);
            var secondFridayBeforeThirdWednesday = thirdWednesday.AddDays(-12);

            if (holidays.Contains(secondFridayBeforeThirdWednesday))
            {
                secondFridayBeforeThirdWednesday = FuturesExpiryUtilityFunctions.AddBusinessDays(secondFridayBeforeThirdWednesday, -1, holidays);
            }

            return secondFridayBeforeThirdWednesday.AddHours(9);
        }

        /// <summary>
        /// First friday of the contract month
        /// </summary>
        public static DateTime FirstFridayOfContractMonth(Symbol underlyingFuture, DateTime expiryMonth)
        {
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlyingFuture.ID.Market, underlyingFuture.ID.Symbol);
            var firstFriday = FuturesExpiryUtilityFunctions.NthFriday(expiryMonth, 1);
            if (holidays.Contains(firstFriday))
            {
                firstFriday = FuturesExpiryUtilityFunctions.AddBusinessDays(firstFriday, -1, holidays);
            }
            return firstFriday.AddHours(13);
        }

        /// <summary>
        /// Tenth business day of the month
        /// </summary>
        public static DateTime TenthBusinessDayOfContractMonth(Symbol underlyingFuture, DateTime expiryMonth)
        {
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlyingFuture.ID.Market, underlyingFuture.ID.Symbol);
            return FuturesExpiryUtilityFunctions.NthBusinessDay(expiryMonth, 10, holidays);
        }

        /// <summary>
        /// Last business day of the month preceding the contract month
        /// </summary>
        private static DateTime LastBusinessDayInPrecedingMonthFromContractMonth(Symbol underlying, DateTime expiryMonth)
        {
            var holidays = FuturesExpiryUtilityFunctions.GetExpirationHolidays(underlying.ID.Market, underlying.ID.Symbol);
            return FuturesExpiryUtilityFunctions.NthLastBusinessDay(expiryMonth.AddMonths(-1), 1, holidays);
        }
    }
}
