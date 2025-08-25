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
using QuantConnect.Securities;
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
