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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Brokerages.Bitfinex;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public partial class BitfinexBrokerageTests
    {
        // the last two bools in params order are:
        // 1) whether or not 'GetHistory' is expected to return an empty result
        // 2) whether or not an ArgumentException is expected to be thrown during 'GetHistory' execution
        public TestCaseData[] History => new[]
        {
            // valid
            new TestCaseData(Symbol, Resolution.Minute, Time.OneMinute, false, false),
            new TestCaseData(Symbol, Resolution.Hour, Time.OneDay, false, false),
            new TestCaseData(Symbol, Resolution.Daily, TimeSpan.FromDays(15), false, false),

            // invalid resolution, no error, empty result
            new TestCaseData(Symbol, Resolution.Tick, TimeSpan.FromSeconds(15), true, false),
            new TestCaseData(Symbol, Resolution.Second, Time.OneMinute, true, false),

            // invalid period, no error, empty result
            new TestCaseData(Symbol, Resolution.Daily, TimeSpan.FromDays(-15), true, false),

            // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
            new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.Bitfinex), 
                Resolution.Daily, TimeSpan.FromDays(15), true, true),

            // invalid security type, no error, empty result
            new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), true, false)
        };

        [Test]
        [TestCaseSource(nameof(History))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool shouldBeEmpty, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var brokerage = (BitfinexBrokerage)Brokerage;

                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false));

                var now = DateTime.UtcNow.RoundDown(resolution.ToTimeSpan());

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

                // 'GetBrokerageSymbol' method called inside 'GetHistory' may throw an ArgumentException for invalid symbol supplied
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
            };

            // assert for ArgumentException
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
