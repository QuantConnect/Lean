/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
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
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.Polygon;
using QuantConnect.Util;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    [Explicit("Tests require a Polygon.io api key.")]
    public class PolygonHistoryTests
    {
        [TestCaseSource(nameof(HistoryTestCases))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool shouldBeEmpty)
        {
            Log.LogHandler = new ConsoleLogHandler();

            var historyProvider = new PolygonDataQueueHandler(false);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null));

            var now = new DateTime(2020, 5, 20, 15, 0, 0).RoundDown(resolution.ToTimeSpan());

            var dataType = LeanData.GetDataType(resolution, tickType);

            var requests = new[]
            {
                new HistoryRequest(now.Add(-period),
                    now,
                    dataType,
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    TimeZones.NewYork,
                    null,
                    true,
                    false,
                    DataNormalizationMode.Adjusted,
                    tickType)
            };

            var history = historyProvider.GetHistory(requests, TimeZones.NewYork).ToList();

            if (dataType == typeof(TradeBar))
            {
                foreach (var slice in history)
                {
                    var bar = slice.Bars[symbol];
                    Log.Trace($"{bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}");
                }
            }
            else if (dataType == typeof(QuoteBar))
            {
                foreach (var slice in history)
                {
                    var bar = slice.QuoteBars[symbol];
                    Log.Trace($"{bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}");
                }
            }
            else if (dataType == typeof(Tick))
            {
                foreach (var slice in history)
                {
                    var ticks = slice.Ticks[symbol];
                    foreach (var tick in ticks)
                    {
                        Log.Trace($"{tick.Time}: {tick.Symbol} - B={tick.BidPrice}, A={tick.AskPrice}, P={tick.LastPrice}, Q={tick.Quantity}");
                    }
                }
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);

            if (shouldBeEmpty)
            {
                Assert.IsTrue(history.Count == 0);
            }
            else
            {
                Assert.IsTrue(history.Count > 0);
            }
        }

        private static TestCaseData[] HistoryTestCases => new[]
        {
            // equity (trades)
            new TestCaseData(Symbols.SPY, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), false),
            new TestCaseData(Symbols.SPY, Resolution.Second, TickType.Trade, Time.OneMinute, false),
            new TestCaseData(Symbols.SPY, Resolution.Minute, TickType.Trade, Time.OneHour, false),
            new TestCaseData(Symbols.SPY, Resolution.Hour, TickType.Trade, TimeSpan.FromHours(6), false),
            new TestCaseData(Symbols.SPY, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),

            // equity (quotes)
            new TestCaseData(Symbols.SPY, Resolution.Tick, TickType.Quote, TimeSpan.FromSeconds(15), false),
            new TestCaseData(Symbols.SPY, Resolution.Second, TickType.Quote, Time.OneMinute, false),
            new TestCaseData(Symbols.SPY, Resolution.Minute, TickType.Quote, Time.OneHour, false),
            new TestCaseData(Symbols.SPY, Resolution.Hour, TickType.Quote, TimeSpan.FromHours(6), false),
            new TestCaseData(Symbols.SPY, Resolution.Daily, TickType.Quote, TimeSpan.FromDays(1), false),

            // forex (quotes)
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, TickType.Quote, TimeSpan.FromSeconds(15), false),
            new TestCaseData(Symbols.EURUSD, Resolution.Second, TickType.Quote, Time.OneMinute, false),
            new TestCaseData(Symbols.EURUSD, Resolution.Minute, TickType.Quote, Time.OneHour, false),
            new TestCaseData(Symbols.EURUSD, Resolution.Hour, TickType.Quote, TimeSpan.FromHours(6), false),
            new TestCaseData(Symbols.EURUSD, Resolution.Daily, TickType.Quote, TimeSpan.FromDays(1), false),

            // crypto (trades)
            new TestCaseData(Symbols.BTCUSD, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Second, TickType.Trade, Time.OneMinute, false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Minute, TickType.Trade, Time.OneHour, false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Hour, TickType.Trade, TimeSpan.FromHours(6), false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),

            // invalid security type/tick type combination, no error, empty result
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), true),
            new TestCaseData(Symbols.BTCUSD, Resolution.Second, TickType.Quote, Time.OneMinute, true),

            // invalid period, no error, empty result
            new TestCaseData(Symbols.SPY, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(-5), true),

            // invalid security type, no error, empty result
            new TestCaseData(Symbols.DE30EUR, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), true)
        };
    }
}
