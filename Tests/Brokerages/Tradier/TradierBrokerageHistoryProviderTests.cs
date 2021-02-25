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
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Tradier
{
    [TestFixture, Explicit("This test requires a configured and active Tradier account")]
    public class TradierBrokerageHistoryProviderTests
    {
        private static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.AAPL, Resolution.Tick, Time.OneMinute, false),
                    new TestCaseData(Symbols.AAPL, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.AAPL, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbols.AAPL, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), false),

                    // invalid period, throws "System.ArgumentException : Invalid date range specified"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(-15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Forex"
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), true)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var useSandbox = Config.GetBool("tradier-use-sandbox");
                var accountId = Config.Get("tradier-account-id");
                var accessToken = Config.Get("tradier-access-token");

                var brokerage = new TradierBrokerage(null, null, null, null, useSandbox, accountId, accessToken);

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                        now,
                        typeof(QuoteBar),
                        symbol,
                        resolution,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard),
                        TimeZones.EasternStandard,
                        Resolution.Minute,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        TickType.Quote)
                };

                var history = brokerage.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol, tick.BidPrice, tick.AskPrice);
                        }
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}, V={6}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                    }
                }

                Log.Trace("Data points retrieved: " + brokerage.DataPointCount);
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