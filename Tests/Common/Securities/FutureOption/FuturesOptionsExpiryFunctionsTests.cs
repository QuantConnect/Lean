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
 *
*/

using System;
using System.Linq;
using NUnit.Framework;
using System.Globalization;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;

namespace QuantConnect.Tests.Common.Securities.FutureOption
{
    [TestFixture]
    public class FuturesOptionsExpiryFunctionsTests
    {
        [TestCase("ES", Market.CME, 0)]
        [TestCase("ZB", Market.CBOT, 1)]
        [TestCase("ZN", Market.CBOT, 1)]
        [TestCase("CL", Market.NYMEX, 1)]
        [TestCase("GC", Market.COMEX, 1)] // No mapping is done for this Symbol as expected, although rules exist.
        public void FutureContractMonthDelta(string futureTicker, string market, int expectedDelta)
        {
            var contractMonth = new DateTime(2020, 12, 1);

            var future = Symbol.Create(futureTicker, SecurityType.Future, market);
            var option = Symbol.CreateOption(
                future,
                market,
                default(OptionStyle),
                default(OptionRight),
                default(decimal),
                SecurityIdentifier.DefaultDate);

            var futureOptionExpiry = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(option, contractMonth);
            Assert.AreEqual(expectedDelta, contractMonth.Month - futureOptionExpiry.Month);
        }

        [TestCaseSource(nameof(ExpiryTestCases))]
        public void ExpiryFunctionsReturnExpectedResults(string futureTicker, string market, DateTime expected)
        {
            var future = Symbol.Create(futureTicker, SecurityType.Future, market);
            var futureOption = Symbol.CreateCanonicalOption(future);

            var december = new DateTime(2020, 12, 1);
            var actual = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(futureOption, december);

            Assert.AreEqual(expected, actual);
        }

        [TestCase("ZM", Market.CBOT, "202601", "20251226", "20260114")]
        [TestCase("ZM", Market.CBOT, "202512", "20251121", "20251212")]
        [TestCase("ZM", Market.CBOT, "202511", "20251024", "20251212")]
        [TestCase("ZL", Market.CBOT, "202601", "20251226", "20260114")]
        [TestCase("ZL", Market.CBOT, "202512", "20251121", "20251212")]
        [TestCase("ZL", Market.CBOT, "202511", "20251024", "20251212")]
        [TestCase("TN", Market.CBOT, "202601", "20251226", "20260320")]
        [TestCase("TN", Market.CBOT, "202512", "20251121", "20251219")]
        [TestCase("TN", Market.CBOT, "202511", "20251024", "20251219")]
        [TestCase("UB", Market.CBOT, "202601", "20251226", "20260320")]
        [TestCase("UB", Market.CBOT, "202512", "20251121", "20251219")]
        [TestCase("UB", Market.CBOT, "202511", "20251024", "20251219")]
        [TestCase("ZO", Market.CBOT, "202603", "20260220", "20260313")]
        [TestCase("ZO", Market.CBOT, "202512", "20251121", "20251212")]
        [TestCase("ZO", Market.CBOT, "202511", "20251024", "20251212")]
        [TestCase("KE", Market.CBOT, "202512", "20251121", "20251212")]
        [TestCase("KE", Market.CBOT, "202511", "20251024", "20251212")]
        [TestCase("KE", Market.CBOT, "202601", "20251226", "20260313")]
        [TestCase("ZF", Market.CBOT, "202512", "20251121", "20251231")]
        [TestCase("ZF", Market.CBOT, "202511", "20251024", "20251231")]
        [TestCase("ZF", Market.CBOT, "202601", "20251226", "20260331")]
        [TestCase("LE", Market.CME, "202612", "20261204", "20261231")]
        [TestCase("LE", Market.CME, "202702", "20270205", "20270226")]
        [TestCase("LE", Market.CME, "202510", "20251003", "20251031")]
        [TestCase("LE", Market.CME, "202511", "20251107", "20251231")]
        [TestCase("HE", Market.CME, "202512", "20251212", "20251212")]
        [TestCase("HE", Market.CME, "202511", "20251114", "20251212")]
        [TestCase("HE", Market.CME, "202510", "20251014", "20251014")]
        [TestCase("LBR", Market.CME, "202510", "20250930", "20251114")]
        [TestCase("LBR", Market.CME, "202511", "20251031", "20251114")]
        [TestCase("LBR", Market.CME, "202603", "20260227", "20260313")]
        [TestCase("LBS", Market.CME, "202510", "20250930", "20251114")]
        [TestCase("LBS", Market.CME, "202511", "20251031", "20251114")]
        [TestCase("LBS", Market.CME, "202603", "20260227", "20260313")]
        [TestCase("NQ", Market.CME, "202512", "20251219", "20251219")]
        [TestCase("NQ", Market.CME, "202603", "20260320", "20260320")]
        [TestCase("EMD", Market.CME, "202512", "20251219", "20251219")]
        [TestCase("EMD", Market.CME, "202603", "20260320", "20260320")]
        [TestCase("ES", Market.CME, "202512", "20251219", "20251219")]
        [TestCase("ES", Market.CME, "202603", "20260320", "20260320")]
        [TestCase("ES", Market.CME, "201601", "20160115", "20160318")]
        [TestCase("YM", Market.CBOT, "202512", "20251219", "20251219")]
        [TestCase("YM", Market.CBOT, "202603", "20260320", "20260320")]
        [TestCase("6N", Market.CME, "202511", "20251107", "20251215")]
        [TestCase("6N", Market.CME, "202512", "20251205", "20251215")]
        [TestCase("6N", Market.CME, "202601", "20260109", "20260316")]
        public void FutureAndOptionMapping(string futureTicker, string market, string fopContractMonthYear, string expectedFop, string expectedFuture)
        {
            var contractMonth = DateTime.ParseExact(fopContractMonthYear, DateFormat.YearMonth, CultureInfo.InvariantCulture);

            var fopExpiry = Time.ParseDate(expectedFop);
            var referenceDate = new DateTime(fopExpiry.Year, 9, 1);
            var canonicalFuture = Symbol.Create(futureTicker, SecurityType.Future, market);
            var canonicalFutureOption = Symbol.CreateOption(
                canonicalFuture,
                market,
                default,
                default,
                default,
                SecurityIdentifier.DefaultDate);
            var futureOptionExpiry = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(canonicalFutureOption, contractMonth);
            Assert.AreEqual(fopExpiry, futureOptionExpiry.Date);

            var underlyingFuture = FuturesOptionsUnderlyingMapper.GetUnderlyingFutureFromFutureOption(canonicalFutureOption.ID.Symbol, market, futureOptionExpiry, referenceDate);

            Assert.AreEqual(Time.ParseDate(expectedFuture), underlyingFuture.ID.Date.Date);
        }

        [Test]
        public void ExpiryFunctionsReturnExpectedResultWhenExpiryIsAHoliday()
        {
            var mhdb = MarketHoursDatabase.FromDataFolder();
            var entry = mhdb.GetEntry(Market.CME, "6A", SecurityType.Future);
            var holidays = entry.ExchangeHours.Holidays;
            holidays.Add(new DateTime(2025, 07, 04));
            var exchangeHours = new SecurityExchangeHours(entry.ExchangeHours.TimeZone,
                holidays,
                entry.ExchangeHours.MarketHours.ToDictionary(),
                entry.ExchangeHours.EarlyCloses,
                entry.ExchangeHours.LateOpens);
            mhdb.SetEntry(Market.CME, "6A", SecurityType.Future, exchangeHours, entry.DataTimeZone);

            var future = Symbol.Create("6A", SecurityType.Future, Market.CME);
            var futureOption = Symbol.CreateCanonicalOption(future);

            var july = new DateTime(2025, 07, 1);
            var actual = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(futureOption, july);

            // The second Friday before the third Wednesday of July is the 4th of July, which is a holiday
            var expected = new DateTime(2025, 07, 03, 9, 0, 0);
            Assert.AreEqual(expected, actual);
        }

        private static object[] ExpiryTestCases =
        {
            new TestCaseData("CL", Market.NYMEX, new DateTime(2020, 11, 17)),
            new TestCaseData("ZB", Market.CBOT, new DateTime(2020, 11, 20)),
            new TestCaseData("ZN", Market.CBOT, new DateTime(2020, 11, 20)),
            new TestCaseData("GC", Market.COMEX, new DateTime(2020, 11, 24, 12, 30, 0)),
            new TestCaseData("6A", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
            new TestCaseData("6B", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
            new TestCaseData("6C", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
            new TestCaseData("6E", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
            new TestCaseData("6J", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
            new TestCaseData("6S", Market.CME, new DateTime(2020, 12, 04, 09, 0, 0)),
        };
    }
}
