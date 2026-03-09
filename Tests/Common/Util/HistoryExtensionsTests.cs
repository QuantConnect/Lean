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
using System.Globalization;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class HistoryExtensionsTests
    {
        [TestCase("GOOGL", "2010/01/01", "2014/02/04", 1, "GOOG", Description = "[GOOG: 2004/08/19 - 2014/04/02][GOOGL: 2014/04/03 - ...)")]
        [TestCase("GOOGL", "2004/08/19", "2014/04/02", 1, "GOOG", Description = "[GOOG: 2004/08/19 - 2014/04/02][GOOGL: 2014/04/03 - ...)")]
        [TestCase("GOOGL", "2000/01/01", "2002/02/04", 1, "GOOG", Description = "Before founded date [GOOG: 2004/08/19 - ...")]
        [TestCase("GOOGL", "2010/01/01", "2015/02/16", 2, "GOOG,GOOGL")]
        [TestCase("GOOGL", "2020/01/01", "2024/01/01", 1, "GOOGL")]
        [TestCase("GOOG", "2013/04/03", "2023/01/01", 2, "GOOCV,GOOG")]
        [TestCase("SPWR", "2007/11/17", "2023/01/01", 3, "SPWR,SPWRA,SPWR", Description = "[SPWR: 2005/11/17 - 2008/09/29][SPWRA: 2008/09/30 - 2011/11/16][SPWR: 2011/11/17 - ...)")]
        [TestCase("SPWR", "2005/11/17", "2011/11/16", 2, "SPWR,SPWRA")]
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

                Assert.That(firstHistoryRequest.EndTimeLocal, Is.EqualTo(secondHistoryRequest.StartTimeLocal));
                Assert.That(firstHistoryRequest.EndTimeUtc, Is.EqualTo(secondHistoryRequest.StartTimeUtc));

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
        [TestCase("SPWR", "2005/11/17", "2011/11/16", Description = "[SPWR: 2005/11/17 - 2008/09/29][SPWRA: 2008/09/30 - 2011/11/16][SPWR: 2011/11/17 - ...)")]
        [TestCase("SPWR", "2005/11/17", "2008/09/29")]
        [TestCase("SPWR", "2008/09/30", "2011/11/16")]
        [TestCase("SPWR", "2008/09/29", "2011/11/16")]
        [TestCase("SPWR", "2008/09/28", "2011/11/18")]
        [TestCase("SPWR", "2008/09/29", "2011/11/17")]
        [TestCase("SPWR", "2011/11/17", "2022/11/16")]
        public void GetSplitHistoricalRequestAndValidateEndDateInDifferentTimeZones(
            string ticker,
            DateTime userRequestedStartDateTime,
            DateTime userRequestedEndDateTime
            )
        {
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);

            foreach (var timeZone in TestsHelpers.GetTimeZones())
            {
                var historyRequest = TestsHelpers.GetHistoryRequest(symbol, userRequestedStartDateTime, userRequestedEndDateTime, Resolution.Daily, TickType.Trade, timeZone);

                var historyRequests = historyRequest.SplitHistoryRequestWithUpdatedMappedSymbol(TestGlobals.MapFileProvider).ToList();

                // Ensure that the user-requested end date time matches the end time of the last history request.
                Assert.That(userRequestedEndDateTime, Is.EqualTo(historyRequests.Last().EndTimeUtc));
                Assert.That(userRequestedEndDateTime.ConvertFromUtc(timeZone), Is.EqualTo(historyRequests.Last().EndTimeLocal));

                Assert.That(userRequestedStartDateTime, Is.EqualTo(historyRequests.First().StartTimeUtc));
                Assert.That(userRequestedStartDateTime.ConvertFromUtc(timeZone), Is.EqualTo(historyRequests.First().StartTimeLocal));

                // Ensure that the end time of the previous history request matches the start time of the next one.
                for (int i = 0; i < historyRequests.Count - 1; i++)
                {
                    Assert.That(historyRequests[i].EndTimeLocal, Is.EqualTo(historyRequests[i + 1].StartTimeLocal));
                    Assert.That(historyRequests[i].EndTimeUtc, Is.EqualTo(historyRequests[i + 1].StartTimeUtc));
                }
            }
        }

        [TestCase("GOOGL", "2010/01/01", "2015/02/16", "2010/01/01-2014/04/03,2014/04/03-2015/02/16", Description = "[GOOG:2004/08/19 - 2014/04/02][GOOGL: 2014/04/03 - ...)")]
        [TestCase("GOOG", "2013/04/03", "2023/01/01", "2013/04/03-2014/04/03,2014/04/03-2023/01/01")]
        [TestCase("SPWR", "2007/11/17", "2023/01/01", "2007/11/17-2008/09/30,2008/09/30-2011/11/17,2011/11/17-2023/01/01", Description = "[SPWR: 2005/11/17 - 2008/09/29][SPWRA: 2008/09/30 - 2011/11/16][SPWR: 2011/11/17 - ...)")]
        [TestCase("SPWR", "2005/11/17", "2011/11/16", "2005/11/17-2008/09/30,2008/09/30-2011/11/16")]
        public void GetSplitHistoricalRequestReturnExpectedDateTimeRanges(
            string ticker, DateTime startDateTime, DateTime endDateTime, string expectedDateTimeRanges)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);

            var dateRanges = expectedDateTimeRanges?.Split(',').Select(x => x.Split('-'))
                .ToList(x => (startDateTime: DateTime.Parse(x[0],CultureInfo.InvariantCulture), endDateTime: DateTime.Parse(x[1],CultureInfo.InvariantCulture)));

            foreach (var timeZone in TestsHelpers.GetTimeZones())
            {
                var historyRequest = TestsHelpers.GetHistoryRequest(symbol, startDateTime, endDateTime, Resolution.Daily, TickType.Trade, timeZone);

                var historyRequests = historyRequest.SplitHistoryRequestWithUpdatedMappedSymbol(TestGlobals.MapFileProvider).ToList();

                if (historyRequests.Count == 2)
                {
                    Assert.That(historyRequests[0].StartTimeUtc, Is.EqualTo(dateRanges[0].startDateTime));
                    Assert.That(historyRequests[0].EndTimeUtc, Is.EqualTo(dateRanges[0].endDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[1].StartTimeUtc, Is.EqualTo(dateRanges[1].startDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[1].EndTimeUtc, Is.EqualTo(dateRanges[1].endDateTime)); 
                }

                if (historyRequests.Count == 3)
                {
                    Assert.That(historyRequests[0].StartTimeUtc, Is.EqualTo(dateRanges[0].startDateTime));
                    Assert.That(historyRequests[0].EndTimeUtc, Is.EqualTo(dateRanges[0].endDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[1].StartTimeUtc, Is.EqualTo(dateRanges[1].startDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[1].EndTimeUtc, Is.EqualTo(dateRanges[1].endDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[2].StartTimeUtc, Is.EqualTo(dateRanges[2].startDateTime.ConvertToUtc(timeZone)));
                    Assert.That(historyRequests[2].EndTimeUtc, Is.EqualTo(dateRanges[2].endDateTime));
                }
            }
        }
    }
}
