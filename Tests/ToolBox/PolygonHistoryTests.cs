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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.Polygon;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    [Explicit("Tests require a Polygon.io api key.")]
    public class PolygonHistoryTests
    {
        [TestCaseSource(nameof(HistoryTestCases))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool shouldBeEmpty)
        {
            Log.LogHandler = new ConsoleLogHandler();

            var historyProvider = new PolygonDataQueueHandler(false);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, null));

            var now = new DateTime(2020, 5, 20).RoundDown(resolution.ToTimeSpan());

            var requests = new[]
            {
                new HistoryRequest(now.Add(-period),
                    now,
                    typeof(TradeBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    DateTimeZone.Utc,
                    Resolution.Minute,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Trade)
            };

            var history = historyProvider.GetHistory(requests, TimeZones.Utc).ToList();

            foreach (var slice in history)
            {
                var bar = slice.Bars[symbol];
                Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
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
            // equity
            new TestCaseData(Symbols.SPY, Resolution.Minute, Time.OneDay, false),
            new TestCaseData(Symbols.SPY, Resolution.Hour, Time.OneDay, false),
            new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(15), false),

            // forex
            new TestCaseData(Symbols.EURUSD, Resolution.Minute, Time.OneDay, false),
            new TestCaseData(Symbols.EURUSD, Resolution.Hour, Time.OneDay, false),
            new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false),

            // crypto
            new TestCaseData(Symbols.BTCUSD, Resolution.Minute, Time.OneDay, false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Hour, Time.OneDay, false),
            new TestCaseData(Symbols.BTCUSD, Resolution.Daily, TimeSpan.FromDays(15), false),

            // invalid resolution, no error, empty result
            new TestCaseData(Symbols.SPY, Resolution.Tick, TimeSpan.FromSeconds(15), true),
            new TestCaseData(Symbols.SPY, Resolution.Second, Time.OneMinute, true),

            // invalid period, no error, empty result
            new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(-15), true),

            // invalid security type, no error, empty result
            new TestCaseData(Symbols.DE30EUR, Resolution.Daily, TimeSpan.FromDays(15), true)
        };

    }
}
