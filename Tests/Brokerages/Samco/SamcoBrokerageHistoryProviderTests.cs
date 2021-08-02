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
using QuantConnect.Brokerages.Samco;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Samco
{
    [TestFixture, Ignore("This test requires a configured and active Samco account")]
    public class SamcoBrokerageHistoryProviderTests
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
                    new TestCaseData(Symbols.SBIN, Resolution.Daily, TimeSpan.FromDays(-15), false),
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false)
                };
                }
            }

            [Test, TestCaseSource(nameof(TestParameters))]
            public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
            {
                TestDelegate test = () =>
                {
                    var apiSecret = Config.Get("samco.api-secret");
                    var apiKey = Config.Get("samco.api-key");
                    var yob = Config.Get("samco.year-of-birth");
                    var tradingSegment = Config.Get("samco.trading-segment","Equity");
                    var productType = Config.Get("samco.product-type","MIS");
                    var brokerage = new SamcoBrokerage(tradingSegment, productType, apiKey, apiSecret, yob, null,null);

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
