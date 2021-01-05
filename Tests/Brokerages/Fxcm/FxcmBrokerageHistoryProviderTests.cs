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
using System.Diagnostics;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Fxcm
{
    [TestFixture]
    public partial class FxcmBrokerageTests
    {
        private static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.EURUSD, Resolution.Tick, TimeSpan.FromSeconds(15), false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false),

                    // invalid period, no error, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(-15), false),

                    // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Forex, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), true),
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var brokerage = (FxcmBrokerage) Brokerage;

                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       typeof(QuoteBar),
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                       DateTimeZone.Utc,
                                       Resolution.Minute,
                                       false,
                                       false,
                                       DataNormalizationMode.Adjusted,
                                       TickType.Quote)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time.ToStringInvariant("yyyy-MM-dd HH:mm:ss.fff"), tick.Symbol, tick.BidPrice, tick.AskPrice);
                        }
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
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

        [Test]
        public void GetsFullDayTickHistory()
        {
            var symbol = Symbols.EURUSD;
            const Resolution resolution = Resolution.Tick;

            var startDate = new DateTime(2016, 11, 1);
            var endDate = startDate.AddDays(1);

            var brokerage = (FxcmBrokerage)Brokerage;

            var historyProvider = new BrokerageHistoryProvider();
            historyProvider.SetBrokerage(brokerage);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null));

            var stopwatch = Stopwatch.StartNew();

            var requests = new[]
            {
                new HistoryRequest(startDate,
                    endDate,
                    typeof(QuoteBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    DateTimeZone.Utc,
                    Resolution.Minute,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Quote)
            };

            var history = historyProvider.GetHistory(requests, TimeZones.Utc);
            var tickCount = history.SelectMany(slice => slice.Ticks[symbol]).Count();

            stopwatch.Stop();
            Log.Trace("Download time: " + stopwatch.Elapsed);

            Log.Trace("History ticks returned: " + tickCount);
            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);

            Assert.AreEqual(tickCount, historyProvider.DataPointCount);
            Assert.AreEqual(241233, tickCount);
        }
    }
}
