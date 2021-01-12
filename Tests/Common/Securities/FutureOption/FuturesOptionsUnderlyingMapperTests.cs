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
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;

namespace QuantConnect.Tests.Common.Securities.FutureOption
{
    public class FuturesOptionsUnderlyingMapperTests
    {
        [TestCase("ES", Market.CME, 2021, 3, 19, 2021, 3, false)]
        [TestCase("NQ", Market.CME, 2021, 3, 19, 2021, 3, false)]
        [TestCase("DC", Market.CME, 2021, 2, 2, 2021, 1, false)]
        [TestCase("CL", Market.NYMEX, 2021, 1, 20, 2021, 2, false)]
        [TestCase("RB", Market.NYMEX, 2021, 1, 29, 2021, 1, false)]
        [TestCase("HO", Market.NYMEX, 2021, 1, 29, 2021, 1, false)]
        [TestCase("NG", Market.NYMEX, 2021, 1, 27, 2021, 1, false)]
        [TestCase("HG", Market.COMEX, 2021, 3, 29, 2021, 1, false)]
        [TestCase("HG", Market.COMEX, 2021, 3, 29, 2021, 2, false)]
        [TestCase("HG", Market.COMEX, 2021, 3, 29, 2021, 3, false)]
        [TestCase("SI", Market.COMEX, 2021, 3, 29, 2021, 1, false)]
        [TestCase("SI", Market.COMEX, 2021, 3, 29, 2021, 2, false)]
        [TestCase("SI", Market.COMEX, 2021, 3, 29, 2021, 3, false)]
        [TestCase("GC", Market.COMEX, 2021, 2, 24, 2021, 1, false)]
        [TestCase("GC", Market.COMEX, 2021, 4, 28, 2021, 2, false)]
        [TestCase("GC", Market.COMEX, 2021, 4, 28, 2021, 3, false)]
        [TestCase("ZC", Market.CBOT, 2021, 3, 12, 2021, 2, false)]
        [TestCase("ZC", Market.CBOT, 2021, 3, 12, 2021, 3, false)]
        [TestCase("ZS", Market.CBOT, 2021, 3, 12, 2021, 2, false)]
        [TestCase("ZS", Market.CBOT, 2021, 3, 12, 2021, 3, false)]
        [TestCase("ZW", Market.CBOT, 2021, 3, 12, 2021, 2, false)]
        [TestCase("ZW", Market.CBOT, 2021, 3, 12, 2021, 3, false)]
        [TestCase("ZC", Market.CBOT, 2032, 1, 19, 2032, 1, true)]
        [TestCase("ZS", Market.CBOT, 2035, 1, 19, 2035, 1, true)]
        [TestCase("ZW", Market.CBOT, 2037, 1, 19, 2037, 1, true)]
        public void GetUnderlyingSymbolFromFutureOption(string futureTicker, string market, int year, int month, int day, int fopContractYear, int fopContractMonth, bool nullExpected)
        {
            var optionTicker = FuturesOptionsSymbolMappings.Map(futureTicker);
            var expectedFuture = Symbol.CreateFuture(futureTicker, market, new DateTime(year, month, day));
            var canonicalFutureOption = Symbol.CreateOption(expectedFuture, market, default(OptionStyle), default(OptionRight), default(decimal), SecurityIdentifier.DefaultDate);

            var futureContractMonthDelta = FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(futureTicker, expectedFuture.ID.Date);
            var futureContractMonth = expectedFuture.ID.Date.AddMonths(futureContractMonthDelta);
            var futuresOptionsExpiration = FuturesOptionsExpiryFunctions.FuturesOptionExpiry(canonicalFutureOption, futureContractMonth);

            var actualFuture = FuturesOptionsUnderlyingMapper.GetUnderlyingFutureFromFutureOption(optionTicker, market, futuresOptionsExpiration, new DateTime(2021, 1, 1));

            if (nullExpected)
            {
                // There were no futures that appeared on the or subsequent contract months from the future option.
                Assert.IsNull(actualFuture);
            }
            else
            {
                Assert.AreEqual(expectedFuture, actualFuture);
            }
        }
    }
}
