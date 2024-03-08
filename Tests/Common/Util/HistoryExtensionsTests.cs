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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class HistoryExtensionsTests
    {
        [TestCase("GOOGL", "2010/01/01", "2014/02/04", 1, "GOOG", Description = "[GOOG: 2004/08/19 - 2014/04/02][GOOGL: 2014/04/03 - ...)")]
        [TestCase("GOOGL", "2000/01/01", "2002/02/04", 1, "GOOG", Description = "Before founded date [GOOG: 2004/08/19 - ...")]
        [TestCase("GOOGL", "2010/01/01", "2015/02/16", 2, "GOOG,GOOGL")]
        [TestCase("GOOGL", "2020/01/01", "2024/01/01", 1, "GOOGL")]
        [TestCase("GOOG", "2013/04/03", "2023/01/01", 2, "GOOCV,GOOG")]
        [TestCase("SPWR", "2007/11/17", "2023/01/01", 3, "SPWR,SPWRA,SPWR", Description = "[SPWR: 2005/11/17 - 2008/09/29][SPWRA: 2008/09/30 - 2011/11/16][SPWR: 2011/11/17 - ...)")]
        [TestCase("SPWR", "2011/11/17", "2023/01/01", 1, "SPWR")]
        [TestCase("AAPL", "2008/02/01", "2024/03/01", 1, "AAPL")]
        [TestCase("NFLX", "2022/02/01", "2024/03/01", 1, "NFLX", Description = "The Symbol is not presented in map files")]
        public void GetSplitHistoricalRequestWithTheSameSymbolButDifferentTicker(string ticker, DateTime startDateTime, DateTime endDateTime, int expectedAmount, string expectedTickers)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);

            var historyRequest = TestsHelpers.GetHistoryRequest(symbol, startDateTime, endDateTime, Resolution.Daily, TickType.Trade);

            var historyRequests = historyRequest.SplitHistoryRequestWithUpdatedMappedSymbol(TestGlobals.MapFileProvider).ToList();

            Assert.IsNotNull(historyRequests);
            Assert.IsNotEmpty(historyRequests);
            Assert.That(historyRequests.Count, Is.EqualTo(expectedAmount));

            if (expectedAmount >= 2)
            {
                var (firstHistoryRequest, secondHistoryRequest) = (historyRequests[0], historyRequests[1]);

                Assert.IsTrue(firstHistoryRequest.Symbol.Value != secondHistoryRequest.Symbol.Value);

                Assert.That(startDateTime, Is.EqualTo(firstHistoryRequest.StartTimeUtc));
                Assert.That(startDateTime, Is.Not.EqualTo(secondHistoryRequest.StartTimeUtc));

                Assert.That(endDateTime, Is.Not.EqualTo(firstHistoryRequest.EndTimeUtc));
                Assert.That(endDateTime, Is.EqualTo(historyRequests[expectedAmount - 1].EndTimeUtc));

                Assert.That(firstHistoryRequest.StartTimeUtc, Is.Not.EqualTo(secondHistoryRequest.StartTimeUtc));
                Assert.That(firstHistoryRequest.EndTimeUtc, Is.Not.EqualTo(secondHistoryRequest.EndTimeUtc));
                Assert.That(firstHistoryRequest.StartTimeLocal, Is.Not.EqualTo(secondHistoryRequest.StartTimeLocal));
                Assert.That(firstHistoryRequest.EndTimeLocal, Is.Not.EqualTo(secondHistoryRequest.EndTimeLocal));
            }

            if (expectedTickers != null)
            {
                foreach (var (actualTicker, expectedTicker) in historyRequests.Zip(expectedTickers.Split(','), (t, et) => (t.Symbol.Value, et)))
                {
                    Assert.That(actualTicker, Is.EqualTo(expectedTicker));
                }
            }
        }

        [TestCase(Futures.Metals.Gold, 1)]
        [TestCase(Futures.Indices.SP500EMini, 1)]
        public void GetSplitHistoricalRequestFutureSymbol(string ticker, int expectedAmount)
        {
            var futureSymbol = Symbols.CreateFutureSymbol(ticker, new DateTime(2024, 3, 29));

            var historyRequest = TestsHelpers.GetHistoryRequest(futureSymbol, new DateTime(2024, 3, 4), new DateTime(2024, 3, 5), Resolution.Daily, TickType.Trade);

            var historyRequests = historyRequest.SplitHistoryRequestWithUpdatedMappedSymbol(TestGlobals.MapFileProvider).ToList();

            Assert.IsNotNull(historyRequests);
            Assert.IsNotEmpty(historyRequests);
            Assert.That(historyRequests.Count, Is.EqualTo(expectedAmount));
        }

        [TestCase("GOOGL", "2010/01/01", "2014/02/04")]
        [TestCase("GOOGL", "2000/01/01", "2002/02/04")]
        [TestCase("GOOGL", "2010/01/01", "2015/02/16")]
        [TestCase("GOOGL", "2020/01/01", "2024/01/01")]
        [TestCase("GOOG", "2013/04/03", "2023/01/01")]
        [TestCase("SPWR", "2007/11/17", "2023/01/01")]
        [TestCase("SPWR", "2011/11/17", "2023/01/01")]
        [TestCase("AAPL", "2008/02/01", "2024/03/01")]
        [TestCase("NFLX", "2022/02/01", "2024/03/01")]
        public void GetSplitHistoricalRequestWithTheSameSymbolButDifferentTickers(string ticker, DateTime startDateTime, DateTime endDateTime)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);

            foreach (var timeZone in TestsHelpers.GetTimeZones())
            {
                var newEndDateTime = endDateTime.ConvertFromUtc(timeZone);

                var historyRequest = TestsHelpers.GetHistoryRequest(symbol, startDateTime, endDateTime, Resolution.Daily, TickType.Trade, timeZone);

                var historyRequests = historyRequest.SplitHistoryRequestWithUpdatedMappedSymbol(TestGlobals.MapFileProvider).ToList();

                Assert.That(endDateTime, Is.EqualTo(historyRequests.Last().EndTimeUtc));
                Assert.That(newEndDateTime, Is.EqualTo(historyRequests.Last().EndTimeLocal));
            }
        }
    }
}
