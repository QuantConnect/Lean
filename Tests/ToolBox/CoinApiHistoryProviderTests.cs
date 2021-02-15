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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.ToolBox.CoinApi;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    [Explicit("Requires CoinApi key")]
    public class CoinApiHistoryProviderTests
    {
        private static readonly Symbol _CoinbaseBtcUsdSymbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
        private static readonly Symbol _BitfinexBtcUsdSymbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);
        private readonly CoinApiDataQueueHandler _coinApiDataQueueHandler = new CoinApiDataQueueHandler(); 

        // -- DATA TO TEST --
        private static TestCaseData[] TestData => new[]
        {
            // No data - period is too short
            new TestCaseData(_BitfinexBtcUsdSymbol, Resolution.Minute, TimeSpan.FromMinutes(1), false),
            new TestCaseData(_CoinbaseBtcUsdSymbol, Resolution.Minute, TimeSpan.FromMinutes(1), false),
            // Has data
            new TestCaseData(_BitfinexBtcUsdSymbol, Resolution.Minute, TimeSpan.FromMinutes(10), true),
            new TestCaseData(_CoinbaseBtcUsdSymbol, Resolution.Minute, TimeSpan.FromMinutes(10), true),
            new TestCaseData(_CoinbaseBtcUsdSymbol, Resolution.Hour, TimeSpan.FromHours(99), true),
            new TestCaseData(_CoinbaseBtcUsdSymbol, Resolution.Daily, TimeSpan.FromDays(99), true)
        };

        [Test]
        [TestCaseSource(nameof(TestData))]
        public void CanGetHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool isNonEmptyResult)
        {
            var now = DateTime.UtcNow;
            var historyRequests = new[]
            {
                new HistoryRequest(now.Add(-period), now, typeof(TradeBar), symbol, resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), TimeZones.Utc,
                    resolution, true, false, DataNormalizationMode.Raw, TickType.Trade)
            };

            var slices = _coinApiDataQueueHandler.GetHistory(historyRequests, TimeZones.Utc).ToArray();
            
            if (isNonEmptyResult)
            {
                // Slices are not empty
                Assert.IsNotEmpty(slices);
                // And are ordered by time
                Assert.That(slices, Is.Ordered.By("Time"));
            }
            else
            {
                // Empty
                Assert.IsEmpty(slices);
            }
        }
    }
}
