/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.DataDownloader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Engine.DataFeeds.DataDownloader
{
    [TestFixture]
    public class CanonicalDataDownloaderDecoratorTests
    {
        [TestCase("2026/03/20", "2025/03/01", "2025/12/31", "2025/03/01", "2025/12/31")]
        [TestCase("2026/03/20", "2025/03/01", "2026/03/25", "2025/03/01", "2026/03/21")]
        [TestCase("2026/03/20", "2020/01/01", "2026/03/25", "2024/03/20", "2026/03/21")]
        [TestCase("2022/02/28", "2018/01/01", "2020/12/31", "2020/02/28", "2020/12/31")]
        [TestCase("2022/02/28", "2018/01/01", "2019/01/01", default, default)]
        [TestCase("2022/02/28", "2022/01/01", "2019/01/01", default, default)]
        [TestCase("2022/02/28", "2022/03/01", "2022/03/02", default, default)]
        [TestCase("2022/02/28", "2022/02/01 9:30", "2022/02/10 16:00", "2022/02/01 9:30", "2022/02/10 16:00")]
        [TestCase("2022/02/28", "2022/02/01 9:30", "2022/02/01 9:30", "2022/02/01 9:30", "2022/02/01 9:30")]
        [TestCase("2022/02/28", "2018/02/01 9:30", "2022/03/01 9:30", "2020/02/28", "2022/03/01")]
        [TestCase("2022/02/28", "2018/02/01 9:30", "2018/03/01 9:30", default, default)]
        [TestCase("2022/02/28", "1997/12/31", "2026/02/20", "2020/02/28", "2022/03/01")]
        public void ShouldReceiveAdjustedDateFutureContract(DateTime expiry, DateTime startUtc, DateTime endUtc, DateTime expectedStart, DateTime expectedEnd)
        {
            var future = Symbol.CreateFuture(Securities.Futures.Indices.SP500EMini, Market.CME, expiry);

            CanonicalDataDownloaderDecorator.TryAdjustDateRangeForContract(future, startUtc, endUtc, out var start, out var end);

            Assert.AreEqual(expectedStart, start);
            Assert.AreEqual(expectedEnd, end);
        }

        [TestCase("2026/02/20", "2025/03/01", "2025/12/31", "2025/03/01", "2025/12/31")]
        [TestCase("2026/02/20", "2025/02/18", "2026/03/25", "2025/02/20", "2026/02/21")]
        [TestCase("2026/02/20", "2020/01/01", "2026/03/25", "2025/02/20", "2026/02/21")]
        [TestCase("2026/02/20", "1997/12/31", "2026/02/20", "2025/02/20", "2026/02/21")]
        [TestCase("2020/02/20", "2020/01/01", "2021/03/25", "2020/01/01", "2020/02/21")]
        [TestCase("2020/02/20", "2019/01/01", "2021/03/25", "2019/02/20", "2020/02/21")]
        [TestCase("2020/02/20", "2018/01/01", "2020/02/25", "2019/02/20", "2020/02/21")]
        [TestCase("2020/02/20", "2019/08/10", "2020/02/20", "2019/08/10", "2020/02/21")]
        [TestCase("2020/02/20", "2018/01/01", "2018/02/01", default, default)]
        [TestCase("2020/02/20", "2021/01/01", "2022/02/01", default, default)]
        [TestCase("2020/02/20", "2020/01/01 9:30", "2020/02/20 16:00", "2020/01/01 9:30", "2020/02/21")]
        [TestCase("2020/02/20", "2019/01/01 9:30", "2020/02/20 16:00", "2019/02/20", "2020/02/21")]
        [TestCase("2020/02/20", "2019/01/01 9:30", "2019/02/01 9:30", default, default)]
        [TestCase("2020/02/20", "2019/02/20 9:30", "2020/02/20", "2019/02/20 9:30", "2020/02/21")]
        [TestCase("2020/02/20", "2020/02/20 9:30", "2020/02/21", default, default)]
        public void ShouldReceiveAdjustedDateOptionContract(DateTime expiry, DateTime startUtc, DateTime endUtc, DateTime expectedStart, DateTime expectedEnd)
        {
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var option = Symbol.CreateOption(aapl, aapl.ID.Market, aapl.SecurityType.DefaultOptionStyle(), OptionRight.Call, 260m, expiry);

            CanonicalDataDownloaderDecorator.TryAdjustDateRangeForContract(option, startUtc, endUtc, out var start, out var end);

            Assert.AreEqual(expectedStart, start);
            Assert.AreEqual(expectedEnd, end);
        }

        [Test]
        public void ShouldNotAdjustNonOptionOrFutureContract()
        {
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var res = CanonicalDataDownloaderDecorator.TryAdjustDateRangeForContract(aapl, new DateTime(2025, 03, 01), new DateTime(2025, 12, 31), out var start, out var end);
            Assert.IsFalse(res);
            Assert.AreEqual(default(DateTime), start);
            Assert.AreEqual(default(DateTime), end);
        }

        [Test]
        public void ReturnsEveryBarOfEveryContractForCanonicalFuture()
        {
            var startUtc = new DateTime(2025, 12, 08);
            var endUtc = new DateTime(2025, 12, 09);
            var canonical = Symbol.Create(Securities.Futures.Indices.MicroNASDAQ100EMini, SecurityType.Future, Market.CME);
            var first = Symbol.CreateFuture(Securities.Futures.Indices.MicroNASDAQ100EMini, Market.CME, new DateTime(2025, 12, 19));
            var second = Symbol.CreateFuture(Securities.Futures.Indices.MicroNASDAQ100EMini, Market.CME, new DateTime(2026, 06, 19));

            var downloader = new TestDataDownloader(parameters => GetBars(parameters.Symbol, startUtc, 3));
            var decorator = new CanonicalDataDownloaderDecorator(downloader, new TestFutureChainProvider(first, second), null);

            var data = decorator.Get(new DataDownloaderGetParameters(canonical, Resolution.Minute, startUtc, endUtc, TickType.Trade));

            Assert.IsNotNull(data);
            var expected = new[] { first, second }
                .SelectMany(symbol => Enumerable.Range(0, 3).Select(minute => $"{symbol.ID} {startUtc.AddMinutes(minute):O}"));
            CollectionAssert.AreEquivalent(expected, data.Select(bar => $"{bar.Symbol.ID} {bar.Time:O}").ToList());
        }

        [Test]
        public void ReturnsTheOnlyBarForCanonicalFuture()
        {
            var startUtc = new DateTime(2025, 12, 08);
            var canonical = Symbol.Create(Securities.Futures.Indices.MicroNASDAQ100EMini, SecurityType.Future, Market.CME);
            var contract = Symbol.CreateFuture(Securities.Futures.Indices.MicroNASDAQ100EMini, Market.CME, new DateTime(2025, 12, 19));
            var downloader = new TestDataDownloader(parameters => GetBars(contract, startUtc, 1));
            var decorator = new CanonicalDataDownloaderDecorator(downloader, new TestFutureChainProvider(contract), null);

            var data = decorator.Get(new DataDownloaderGetParameters(canonical, Resolution.Minute, startUtc, startUtc.AddDays(1), TickType.Trade));

            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Count());
        }

        [Test]
        public void ReturnsNullForCanonicalFutureWhenNoContractHasData()
        {
            var startUtc = new DateTime(2025, 12, 08);
            var canonical = Symbol.Create(Securities.Futures.Indices.MicroNASDAQ100EMini, SecurityType.Future, Market.CME);
            var contract = Symbol.CreateFuture(Securities.Futures.Indices.MicroNASDAQ100EMini, Market.CME, new DateTime(2025, 12, 19));
            var downloader = new TestDataDownloader(parameters => Enumerable.Empty<BaseData>());
            var decorator = new CanonicalDataDownloaderDecorator(downloader, new TestFutureChainProvider(contract), null);

            var data = decorator.Get(new DataDownloaderGetParameters(canonical, Resolution.Minute, startUtc, startUtc.AddDays(1), TickType.Trade));

            Assert.IsNull(data);
        }

        [Test]
        public void ReturnsNullForCanonicalFutureWithNoContract()
        {
            var startUtc = new DateTime(2025, 12, 08);
            var canonical = Symbol.Create(Securities.Futures.Indices.MicroNASDAQ100EMini, SecurityType.Future, Market.CME);
            var downloader = new TestDataDownloader(parameters => Enumerable.Empty<BaseData>());
            var decorator = new CanonicalDataDownloaderDecorator(downloader, new TestFutureChainProvider(), null);

            var data = decorator.Get(new DataDownloaderGetParameters(canonical, Resolution.Minute, startUtc, startUtc.AddDays(1), TickType.Trade));

            Assert.IsNull(data);
        }

        private static IEnumerable<BaseData> GetBars(Symbol symbol, DateTime start, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new TradeBar(start.AddMinutes(i), symbol, 1m, 1m, 1m, 1m, 1m, Time.OneMinute);
            }
        }

        private class TestDataDownloader : IDataDownloader
        {
            private readonly Func<DataDownloaderGetParameters, IEnumerable<BaseData>> _contractData;

            public TestDataDownloader(Func<DataDownloaderGetParameters, IEnumerable<BaseData>> contractData)
            {
                _contractData = contractData;
            }

            public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
            {
                // like a downloader with no support for canonical symbols
                return dataDownloaderGetParameters.Symbol.IsCanonical() ? null : _contractData(dataDownloaderGetParameters);
            }
        }

        private class TestFutureChainProvider : IFutureChainProvider
        {
            private readonly Symbol[] _contracts;

            public TestFutureChainProvider(params Symbol[] contracts)
            {
                _contracts = contracts;
            }

            public IEnumerable<Symbol> GetFutureContractList(Symbol symbol, DateTime date)
            {
                return _contracts;
            }
        }
    }
}
