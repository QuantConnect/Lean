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
using QuantConnect.Brokerages.Zerodha;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Zerodha
{
    [TestFixture, Ignore("This test requires a configured and active Zerodha account")]
    public class ZerodhaBrokerageHistoryProviderTests
    {
            private static TestCaseData[] TestParameters
            {
                get
                {
                    return new[]
                    {
                    // valid parameters
                    new TestCaseData(Symbols.SBIN, Resolution.Tick, Time.OneMinute, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbols.SBIN, Resolution.Daily, TimeSpan.FromDays(15), false),

                    // invalid period, throws "System.ArgumentException : Invalid date range specified"
                    new TestCaseData(Symbols.SBIN, Resolution.Daily, TimeSpan.FromDays(-15), true),

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
                    var accessToken = Config.Get("zerodha-access-token");
                    var apiKey = Config.Get("zerodha-api-key"); 
                    var tradingSegment = Config.Get("zerodha-trading-segment");
                    var productType = Config.Get("zerodha-product-type");
                    var brokerage = new ZerodhaBrokerage(tradingSegment, productType, apiKey, accessToken, null,null,null);

                    var now = DateTime.UtcNow;

                    var request = new HistoryRequest(now.Add(-period),
                        now,
                        typeof(QuoteBar),
                        symbol,
                        resolution,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.Kolkata),
                        TimeZones.Kolkata,
                        Resolution.Minute,
                        false,
                        false,
                        DataNormalizationMode.Adjusted,
                        TickType.Quote)
                    { };


                    var history = brokerage.GetHistory(request);

                    foreach (var slice in history)
                    {
                        Log.Trace("{0}: {1} - {2} / {3}", slice.Time, slice.Symbol, slice.Price, slice.IsFillForward);
                    }

                    Log.Trace("Base currency: " + brokerage.AccountBaseCurrency);
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
