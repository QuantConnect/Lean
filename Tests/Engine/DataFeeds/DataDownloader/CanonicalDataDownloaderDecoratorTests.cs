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
using QuantConnect.Lean.Engine.DataFeeds.DataDownloader;
using System;

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
    }
}
