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
        public void ShouldReceiveAdjustedDateFutureContract(DateTime expiry, DateTime startUtc, DateTime endUtc, DateTime expectedStart, DateTime expectedEnd)
        {
            var future = Symbol.CreateFuture(Securities.Futures.Indices.SP500EMini, Market.CME, expiry);

            var (start, end) = CanonicalDataDownloaderDecorator.AdjustDateRangeForContract(future, startUtc, endUtc);

            Assert.AreEqual(expectedStart, start);
            Assert.AreEqual(expectedEnd, end);
        }

        [TestCase("2026/02/20", "2025/03/01", "2025/12/31", "2025/03/01", "2025/12/31")]
        [TestCase("2026/02/20", "2025/02/18", "2026/03/25", "2025/02/20", "2026/02/21")]
        [TestCase("2026/02/20", "2020/01/01", "2026/03/25", "2025/02/20", "2026/02/21")]
        public void ShouldReceiveAdjustedDateOptionContract(DateTime expiry, DateTime startUtc, DateTime endUtc, DateTime expectedStart, DateTime expectedEnd)
        {
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var option = Symbol.CreateOption(aapl, aapl.ID.Market, aapl.SecurityType.DefaultOptionStyle(), OptionRight.Call, 260m, expiry);

            var (start, end) = CanonicalDataDownloaderDecorator.AdjustDateRangeForContract(option, startUtc, endUtc);

            Assert.AreEqual(expectedStart, start);
            Assert.AreEqual(expectedEnd, end);
        }
    }
}
