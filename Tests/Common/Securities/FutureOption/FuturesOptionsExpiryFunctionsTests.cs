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
using NUnit.Framework;
using QuantConnect.Securities.FutureOption;

namespace QuantConnect.Tests.Common.Securities.FutureOption
{
    [TestFixture]
    public class FuturesOptionsExpiryFunctionsTests
    {
        [TestCase("ES", Market.CME, 12, 0)]
        [TestCase("ZB", Market.CBOT, 11, 1)]
        [TestCase("CL", Market.NYMEX, 11, 1)]
        [TestCase("GC", Market.COMEX, 11, 1)] // No mapping is done for this Symbol as expected, although rules exist.
        public void FutureContractMonthDelta(string futureTicker, string market, int expiryMonth, int expectedDelta)
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
        public void ExpiryFunctionsReturnExpectedResults(string futureTicker, string market, DateTime month, DateTime expected)
        {
            var future = Symbol.Create(futureTicker, SecurityType.Future, market);
            var futureOption = Symbol.CreateOption(future, market, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);

            var actual = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(futureOption, month);

            Assert.AreEqual(expected, actual);
        }

        private static object[] ExpiryTestCases =
        {
            new TestCaseData("DC", Market.CME, new DateTime(2012, 3, 1), new DateTime(2012, 3, 30, 17, 10 ,0)),
            new TestCaseData("DC", Market.CME, new DateTime(2012, 4, 1), new DateTime(2012, 5, 1, 17, 10 ,0)),
            new TestCaseData("CL", Market.NYMEX, new DateTime(2020, 12, 1), new DateTime(2020, 11, 17)),
            new TestCaseData("ZB", Market.CBOT, new DateTime(2020, 12, 1), new DateTime(2020, 11, 20)),
            new TestCaseData("GC", Market.COMEX, new DateTime(2020, 12, 1), new DateTime(2020, 11, 24, 12, 30, 0))
        };
    }
}
