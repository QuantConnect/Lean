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
using NUnit.Framework;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;

namespace QuantConnect.Tests.Common.Securities.FutureOption
{
    public class FuturesOptionsUnderlyingMapperTests
    {
        [TestCase("ES", Market.CME, 2021, 3, 19, 2021, 3, 19, false)]
        [TestCase("NQ", Market.CME, 2021, 3, 19, 2021, 3, 19, false)]
        [TestCase("CL", Market.NYMEX, 2021, 1, 14, 2021, 1, 20, false)]
        [TestCase("RB", Market.NYMEX, 2021, 1, 29, 2021, 1, 29, false)]
        [TestCase("HO", Market.NYMEX, 2021, 1, 29, 2021, 1, 29, false)]
        [TestCase("NG", Market.NYMEX, 2021, 1, 26, 2021, 1, 27, false)]
        [TestCase("HG", Market.COMEX, 2021, 2, 23, 2021, 3, 29, false)]
        [TestCase("SI", Market.COMEX, 2021, 2, 23, 2021, 3, 29, false)]
        [TestCase("GC", Market.COMEX, 2021, 1, 26, 2021, 2, 24, false)]
        [TestCase("ZC", Market.CBOT, 2021, 2, 19, 2021, 3, 12, false)]
        [TestCase("ZN", Market.CBOT, 2021, 2, 19, 2021, 3, 22, false)]
        [TestCase("ZS", Market.CBOT, 2021, 2, 19, 2021, 3, 12, false)]
        [TestCase("ZW", Market.CBOT, 2021, 2, 19, 2021, 3, 12, false)]
        [TestCase("6A", Market.CME, 2025, 09, 05, 2025, 09, 15, false)]
        [TestCase("6A", Market.CME, 2025, 12, 05, 2025, 12, 15, false)]
        [TestCase("6B", Market.CME, 2025, 09, 05, 2025, 09, 15, false)]
        [TestCase("6B", Market.CME, 2025, 12, 05, 2025, 12, 15, false)]
        [TestCase("6C", Market.CME, 2025, 09, 05, 2025, 09, 16, false)]
        [TestCase("6C", Market.CME, 2025, 12, 05, 2025, 12, 16, false)]
        [TestCase("6E", Market.CME, 2025, 09, 05, 2025, 09, 15, false)]
        [TestCase("6E", Market.CME, 2025, 12, 05, 2025, 12, 15, false)]
        [TestCase("6J", Market.CME, 2025, 09, 05, 2025, 09, 15, false)]
        [TestCase("6J", Market.CME, 2025, 12, 05, 2025, 12, 15, false)]
        [TestCase("6S", Market.CME, 2025, 09, 05, 2025, 09, 15, false)]
        [TestCase("6S", Market.CME, 2025, 12, 05, 2025, 12, 15, false)]
        [TestCase("ZC", Market.CBOT, 2031, 12, 26, 2031, 12, 26, true)]
        [TestCase("ZS", Market.CBOT, 2034, 12, 22, 2034, 12, 22, true)]
        [TestCase("ZW", Market.CBOT, 2036, 12, 26, 2036, 12, 26, true)]
        public void GetUnderlyingSymbolFromFutureOption(string futureTicker, string market,
            int fopContractYear, int fopContractMonth, int fopContractDay,
            int expectedFutureYear, int expectedFutureMonth, int expectedFutureDay,
            bool nullExpected)
        {
            var optionTicker = FuturesOptionsSymbolMappings.Map(futureTicker);
            var expectedFuture = Symbol.CreateFuture(futureTicker, market, new DateTime(expectedFutureYear, expectedFutureMonth, expectedFutureDay));
            var futuresOptionsExpiration = new DateTime(fopContractYear, fopContractMonth, fopContractDay);

            var actualFuture = FuturesOptionsUnderlyingMapper.GetUnderlyingFutureFromFutureOption(optionTicker, market, futuresOptionsExpiration, new DateTime(2021, 1, 1));

            if (nullExpected)
            {
                // There were no futures that appeared on the or subsequent contract months from the future option.
                Assert.IsNull(actualFuture);
            }
            else
            {
                Assert.AreEqual(expectedFuture, actualFuture, $"Expected {expectedFuture.ID.Date} but got {actualFuture.ID.Date}");
            }
        }
    }
}
