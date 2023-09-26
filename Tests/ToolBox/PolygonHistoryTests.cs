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
using QuantConnect.Configuration;
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
        private PolygonDataQueueHandler _historyProvider;

        [SetUp]
        public void SetUp()
        {
            Config.Set("polygon-api-key", "");

            Log.LogHandler = new CompositeLogHandler();

            _historyProvider = new PolygonDataQueueHandler(false);
            _historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null, null));

        }

        #region General

        [TestCaseSource(nameof(HistoryTestCases))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool shouldBeEmpty)
        {
            // 3 pm of the previous day
            var now = DateTime.UtcNow.Date.AddHours(-9).RoundDown(resolution.ToTimeSpan());

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

            var history = _historyProvider.GetHistory(requests, TimeZones.NewYork).ToList();

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

            Log.Trace("Data points retrieved: " + _historyProvider.DataPointCount);

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
            new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Kraken), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitstamp), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),
            new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.HitBTC), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), false),

            // invalid security type/tick type combination, no error, empty result
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), true),
            new TestCaseData(Symbols.BTCUSD, Resolution.Second, TickType.Quote, Time.OneMinute, true),

            // invalid period, no error, empty result
            new TestCaseData(Symbols.SPY, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(-5), true),

            // invalid security type, no error, empty result
            new TestCaseData(Symbols.DE30EUR, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(5), true)
        };

        #endregion

        #region Aggregate Bars

        private static TestCaseData[] HistoricalTradeBarsTestCaseDatas => new[]
        {
            // long requests
            new TestCaseData(Symbols.SPY, Resolution.Minute, TickType.Trade, TimeSpan.FromDays(100), true),
            new TestCaseData(Symbols.SPY, Resolution.Minute, TickType.Trade, TimeSpan.FromDays(200), true),
        };

        [TestCaseSource(nameof(HistoricalTradeBarsTestCaseDatas))]
        public void GetHistoricalTradeBarsTest(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool isNonEmptyResult)
        {
            var request = CreateHistoryRequest(symbol, resolution, tickType, period);
            var historyArray = _historyProvider.GetHistory(request).ToArray();

            Log.Trace("Data points retrieved: " + _historyProvider.DataPointCount);

            if (isNonEmptyResult)
            {
                var i = -1;
                foreach (var baseData in historyArray)
                {
                    var bar = (TradeBar)baseData;
                    Log.Trace($"{++i} {bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}");
                }

                // Ordered by time
                Assert.That(historyArray, Is.Ordered.By("Time"));

                // No repeating bars
                var timesArray = historyArray.Select(x => x.Time).ToArray();
                Assert.AreEqual(timesArray.Length, timesArray.Distinct().Count());
            }
        }

        #endregion

        #region Ticks

        private static TestCaseData[] HistoricalTicksTestCaseDatas => new[]
        {
            new TestCaseData(Symbols.AAPL, Resolution.Tick, TickType.Trade, TimeSpan.FromDays(7), true),
            new TestCaseData(Symbols.AAPL, Resolution.Tick, TickType.Quote, TimeSpan.FromDays(7), true),
            new TestCaseData(Symbols.EURUSD, Resolution.Tick, TickType.Quote, TimeSpan.FromDays(7), true),
            new TestCaseData(Symbols.BTCUSD, Resolution.Tick, TickType.Trade, TimeSpan.FromDays(7), true)
        };

        [TestCaseSource(nameof(HistoricalTicksTestCaseDatas))]
        public void GetHistoricalTicksTest(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool isNonEmptyResult)
        {
            var request = CreateHistoryRequest(symbol, resolution, tickType, period);
            var historicalData = _historyProvider.GetHistory(request).ToList();

            Log.Trace("Data points retrieved: " + _historyProvider.DataPointCount);

            // Data goes in chronological order
            Assert.That(historicalData, Is.Ordered.By("Time"));

            if (isNonEmptyResult)
            {
                foreach (var group in historicalData.GroupBy(x => x.Time.Date))
                {
                    Log.Trace($"Downloaded {tickType} ticks count for {group.Key:yyyy-MM-dd} >> {group.Count()}");
                }
            }
        }

        #endregion

        private static HistoryRequest CreateHistoryRequest(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var dataType = LeanData.GetDataType(resolution, tickType);

            return new HistoryRequest(now.Add(-period),
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
                tickType);
        }
    }
}
