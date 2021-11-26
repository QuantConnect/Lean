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
        private bool _useSandbox = Config.GetBool("tradier-use-sandbox");
        private string _accountId = Config.Get("tradier-account-id");
        private string _accessToken = Config.Get("tradier-access-token");

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

                    // invalid canonical symbo, throws "System.ArgumentException : Invalid symbol, cannot use canonical"
                    new TestCaseData(Symbols.SPY_Option_Chain, Resolution.Daily, TimeSpan.FromDays(15), true),

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
                var now = DateTime.UtcNow;
                var request = new HistoryRequest(now.Add(-period),
                    now,
                    typeof(TradeBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard),
                    TimeZones.EasternStandard,
                    Resolution.Minute,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Trade);


                GetHistoryHelper(request, resolution);
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
    
        [TestCase(Resolution.Daily)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Tick)]
        public void GetsOptionHistory(Resolution resolution){
            TestDelegate test = () =>
            { 
                var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                var option = Symbol.CreateOption(spy, Market.USA, OptionStyle.American, OptionRight.Put, 440m, new DateTime(2021, 09, 10));

                var start = new DateTime(2021, 8, 25);
                DateTime end;

                switch(resolution){
                    case Resolution.Daily:
                        end = new DateTime(2021, 9, 3);
                        break;
                    case Resolution.Hour:
                        end = new DateTime(2021, 8, 26);
                        break;
                    case Resolution.Minute:
                    case Resolution.Second:
                    case Resolution.Tick:
                    default:
                        end = new DateTime(2021, 8, 25, 15, 0, 0);
                        break;
                }

                var request = new HistoryRequest(start,
                    end,
                    typeof(TradeBar),
                    option,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard),
                    TimeZones.EasternStandard,
                    Resolution.Minute,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Trade);

                GetHistoryHelper(request, resolution);
            };
            
            Assert.DoesNotThrow(test);
        }

        private void GetHistoryHelper(HistoryRequest request, Resolution resolution){

            var brokerage = new TradierBrokerage(null, null, null, null, _useSandbox, _accountId, _accessToken);
            var requests = new[] { request };
            var history = brokerage.GetHistory(requests, TimeZones.Utc);

            foreach (var slice in history)
            {
                if (resolution == Resolution.Tick)
                {
                    foreach (var tick in slice.Ticks[request.Symbol])
                    {
                        Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol, tick.BidPrice, tick.AskPrice);
                    }
                }
                else
                {
                    var bar = slice.Bars[request.Symbol];

                    Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}, V={6}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                }
            }

            Log.Trace("Data points retrieved: " + brokerage.DataPointCount);
        }   
    }
}