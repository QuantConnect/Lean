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
using NodaTime;
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
    [TestFixture, Ignore("This test requires a configured and testable Alpaca practice account")]
    public class AlpacaBrokerageHistoryProviderTests
    {
        public TestCaseData[] TestParameters
        {
            get
            {
                var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

                return new[]
                {
                    // valid parameters
                    new TestCaseData(aapl, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(aapl, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(aapl, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(aapl, Resolution.Daily, TimeSpan.FromDays(5), false),

                    // invalid resolution, no error, empty result
                    new TestCaseData(aapl, Resolution.Tick, TimeSpan.FromSeconds(15), false),

                    // invalid period, no error, empty result
                    new TestCaseData(aapl, Resolution.Daily, TimeSpan.FromDays(-15), false),

                    // invalid symbol, no error, empty result
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Forex, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, no error, empty result
                    new TestCaseData(Symbols.ETHBTC, Resolution.Daily, TimeSpan.FromDays(15), true),
                };
            }
        }

        [Test, TestCaseSource("TestParameters")]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var accountKeyId = Config.Get("alpaca-access-token");
                var secretKey = Config.Get("alpaca-account-id");
                var baseUrl = Config.Get("alpaca-base-url");
                var brokerage = new AlpacaBrokerage(null, null, accountKeyId, secretKey, baseUrl);

                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(null, null, null, null, null, null);

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                        now,
                        typeof(TradeBar),
                        symbol,
                        resolution,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        DateTimeZone.Utc,
                        Resolution.Daily,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        TickType.Trade)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Console.WriteLine("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol, tick.BidPrice, tick.AskPrice);
                        }
                    }
                    else if (resolution == Resolution.Second)
                    {
                        var bar = slice.QuoteBars[symbol];

                        Console.WriteLine("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Console.WriteLine("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                    }
                }

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
            };

            if (throwsException)
            {
                Assert.Throws<ArgumentException>(test);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }
    }
}