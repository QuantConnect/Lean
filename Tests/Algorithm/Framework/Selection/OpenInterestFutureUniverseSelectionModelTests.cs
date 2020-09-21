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
using System.Linq;
using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class OpenInterestFutureUniverseSelectionModelTests
    {
        private static readonly Symbol Jan = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 01, 01));
        private static readonly Symbol Feb = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 02, 01));
        private static readonly Symbol March = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 03, 01));
        private static readonly Symbol April = Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 04, 01));
        private static readonly DateTime TestDate = new DateTime(2020, 05, 11, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ExpectedPreviousDate = new DateTime(2020, 05, 09, 20, 0, 0, DateTimeKind.Utc);
        private static readonly IReadOnlyDictionary<Symbol, decimal> OpenInterestData = new Dictionary<Symbol, decimal>
        {
            [Jan] = 3,
            [Feb] = 6,
            [March] = 3, // Same as Jan.
            [April] = 1
        };
        private static readonly MarketHoursDatabase.Entry MarketHours = MarketHoursDatabase.FromDataFolder().GetEntry(Jan.ID.Market, Jan, Jan.SecurityType);
        private Mock<IHistoryProvider> _mockHistoryProvider;
        private OpenInterestFutureUniverseSelectionModel _underTest;

        [Test]
        public void No_Open_Interest_Returns_Empty()
        {
            SetupSubject(OpenInterestData.Count, OpenInterestData.Count);
            _mockHistoryProvider.Setup(x => x.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns<IEnumerable<HistoryRequest>, DateTimeZone>((r, tz) => new Slice[0])
                .Verifiable();

            var data = OpenInterestData.Keys.ToDictionary(x => x, x => MarketHours);
            var results = _underTest.FilterByOpenInterest(data).ToList();
            _mockHistoryProvider.Verify();
            Assert.IsEmpty(results);
        }

        [Test]
        public void Can_Sort_By_Open_Interest()
        {
            SetupSubject(OpenInterestData.Count, OpenInterestData.Count);
            _mockHistoryProvider.Setup(x => x.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns<IEnumerable<HistoryRequest>, DateTimeZone>(
                    (r, tz) =>
                    {
                        var requests = r.ToList();
                        Assert.AreEqual(4, requests.Count);
                        var slices = new List<Slice>(requests.Count);
                        foreach (var request in requests)
                        {
                            Assert.NotNull(request.Symbol);
                            Assert.AreEqual(typeof(Tick), request.DataType);
                            Assert.AreEqual(DataNormalizationMode.Raw, request.DataNormalizationMode);
                            Assert.AreEqual(ExpectedPreviousDate, request.StartTimeUtc);
                            Assert.AreEqual(TestDate, request.EndTimeUtc);
                            Assert.AreEqual(Resolution.Tick, request.Resolution);
                            Assert.AreEqual(TickType.OpenInterest, request.TickType);
                            Assert.AreEqual(tz, MarketHours.ExchangeHours.TimeZone);
                            slices.Add(CreateReplySlice(request.Symbol, OpenInterestData[request.Symbol]));
                        }

                        return slices;
                    }
                )
                .Verifiable();

            var data = OpenInterestData.Keys.ToDictionary(x => x, x => MarketHours);
            var results = _underTest.FilterByOpenInterest(data).ToList();

            // Results should be sorted by open interest (descending), and then by the date.
            _mockHistoryProvider.Verify();
            Assert.AreEqual(4, results.Count);
            Assert.AreEqual(Feb, results[0]);
            Assert.AreEqual(Jan, results[1]);
            Assert.AreEqual(March, results[2]);
            Assert.AreEqual(April, results[3]);
        }

        [Test]
        public void Can_Limit_Number_Of_Contracts()
        {
            SetupSubject(6, 4);
            var expected = Enumerable.Range(1, 4).Select(d => Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 01, d))).ToList();

            // Create 7 requests.  Reverse the list so the order isn't correct, but remains consistent for tests.
            var data = expected.Concat(Enumerable.Range(5, 3).Select(d => Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, new DateTime(2020, 01, d))))
                .Reverse()
                .ToDictionary(x => x, _ => MarketHours);

            // 7 input requests, but the look-up should be limited to only 6.
            _mockHistoryProvider.Setup(x => x.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns<IEnumerable<HistoryRequest>, DateTimeZone>((rq, tz) => rq.Select(r => CreateReplySlice(r.Symbol, 1)).ToArray());

            // Run the test.
            var results = _underTest.FilterByOpenInterest(data).ToList();

            // Verify the chain limit was applied.
            _mockHistoryProvider.Verify(x => x.GetHistory(It.Is<IEnumerable<HistoryRequest>>(r => r.Count() == 6), MarketHours.ExchangeHours.TimeZone), Times.Once);

            // Verify the results.
            CollectionAssert.AreEqual(expected, results);
        }

        [Test]
        public void Limits_Do_Not_Need_To_Be_Provided()
        {
            SetupSubject(null, null);
            var startDate = new DateTime(2020, 01, 01);
            var items = Enumerable.Range(0, 100).ToDictionary(d => Symbol.CreateFuture(Futures.Metals.Gold, Market.COMEX, startDate.AddDays(d)), _ => MarketHours);
            _mockHistoryProvider.Setup(x => x.GetHistory(It.IsAny<IEnumerable<HistoryRequest>>(), It.IsAny<DateTimeZone>()))
                .Returns<IEnumerable<HistoryRequest>, DateTimeZone>((rq, tz) => rq.Select(r => CreateReplySlice(r.Symbol, 1)).ToArray());
            var results = _underTest.FilterByOpenInterest(items).ToList();
            _mockHistoryProvider.Verify(x => x.GetHistory(It.Is<IEnumerable<HistoryRequest>>(r => r.Count() == 100), MarketHours.ExchangeHours.TimeZone), Times.Once);
            Assert.AreEqual(items.Keys, results);
        }

        private static Slice CreateReplySlice(Symbol symbol, decimal openInterest)
        {
            var ticks = new Ticks {{symbol, new List<Tick> {new OpenInterest(TestDate, symbol, openInterest)}}};
            return new Slice(TestDate, null, null, null, ticks, null, null, null, null, null, null, true);
        }

        private void SetupSubject(int? testChainContractLookupLimit, int? testResultsLimit)
        {
            _mockHistoryProvider = new Mock<IHistoryProvider>();

            var mockAlgorithm = new Mock<IAlgorithm>();
            mockAlgorithm.SetupGet(x => x.HistoryProvider).Returns(_mockHistoryProvider.Object);
            mockAlgorithm.SetupGet(x => x.UtcTime).Returns(TestDate);
            _underTest = new OpenInterestFutureUniverseSelectionModel(mockAlgorithm.Object, _ => OpenInterestData.Keys, testChainContractLookupLimit, testResultsLimit);
        }
    }
}