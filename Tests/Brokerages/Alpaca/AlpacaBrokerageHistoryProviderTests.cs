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
using QuantConnect.Brokerages.Alpaca;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Alpaca
{
    [TestFixture, Ignore("This test requires a configured and testable Alpaca practice account. Since it uses the Polygon API, the account needs to be funded.")]
    public class AlpacaBrokerageHistoryProviderTests
    {
        private static TestCaseData[] TestParameters
        {
            get
            {
                var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

                return new[]
                {
                    // valid parameters
                    // Setting TimeSpan to 18 hours ensures that the market is open when running
                    // the test during weekdays
                    new TestCaseData(aapl, Resolution.Tick, TimeSpan.FromHours(18), false),
                    new TestCaseData(aapl, Resolution.Second, TimeSpan.FromHours(18), false),
                    new TestCaseData(aapl, Resolution.Minute, Time.OneDay, false),
                    new TestCaseData(aapl, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(aapl, Resolution.Daily, TimeSpan.FromDays(30), false),

                    // invalid period, no error, empty result
                    new TestCaseData(aapl, Resolution.Daily, TimeSpan.FromDays(-15), true),

                    // invalid symbol, no error, empty result
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Forex, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, no error, empty result
                    new TestCaseData(Symbols.ETHBTC, Resolution.Daily, TimeSpan.FromDays(15), true)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool shouldBeEmpty)
        {
            Log.LogHandler = new ConsoleLogHandler();

            var keyId = Config.Get("alpaca-key-id");
            var secretKey = Config.Get("alpaca-secret-key");
            var tradingMode = Config.Get("alpaca-trading-mode");

            using (var brokerage = new AlpacaBrokerage(null, null, keyId, secretKey, tradingMode, true))
            {
                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(
                        now.Add(-period),
                        now,
                        typeof(TradeBar),
                        symbol,
                        resolution,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        TimeZones.NewYork,
                        null,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        TickType.Trade)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.NewYork).ToList();

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Console.WriteLine($"{tick.Time}: {tick.Symbol} - P={tick.Price}, Q={tick.Quantity}");
                        }
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Console.WriteLine($"{bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}, V={bar.Volume}");
                    }
                }

                if (shouldBeEmpty)
                {
                    Assert.IsTrue(history.Count == 0);
                }
                else
                {
                    Assert.IsTrue(history.Count > 0);
                }

                brokerage.Disconnect();

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
            }
        }
    }
}