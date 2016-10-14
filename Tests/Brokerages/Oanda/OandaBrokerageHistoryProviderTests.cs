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
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using Environment = QuantConnect.Brokerages.Oanda.Environment;

namespace QuantConnect.Tests.Brokerages.Oanda
{
    [TestFixture, Ignore("This test requires a configured and testable Oanda practice account")]
    public class OandaBrokerageHistoryProviderTests
    {
        public TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.EURUSD, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false),

                    // invalid resolution, throws "System.ArgumentException : Unsupported resolution: Tick"
                    new TestCaseData(Symbols.EURUSD, Resolution.Tick, TimeSpan.FromSeconds(15), true),

                    // invalid period, no error, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(-15), false),

                    // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Forex, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), true),
                };
            }
        }

        [Test, TestCaseSource("TestParameters")]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var environment = Config.Get("oanda-environment").ConvertTo<Environment>();
                var accessToken = Config.Get("oanda-access-token");
                var accountId = Config.Get("oanda-account-id").ConvertTo<int>();

                var brokerage = new OandaBrokerage(null, null, environment, accessToken, accountId);
        
                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest
                    {
                        Symbol = symbol,
                        Resolution = resolution,
                        StartTimeUtc = now.Add(-period),
                        EndTimeUtc = now
                    }
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

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
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