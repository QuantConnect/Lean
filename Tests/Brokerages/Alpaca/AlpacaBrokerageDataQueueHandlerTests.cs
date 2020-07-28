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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages.Alpaca;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.Alpaca
{
    [TestFixture]
    public partial class AlpacaBrokerageTests
    { 
        [Test]
        public void GetsTickData()
        {
            var brokerage = (AlpacaBrokerage)Brokerage;
            var cancelationToken = new CancellationTokenSource();

            var configs = new SubscriptionDataConfig[] {
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("FB", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("TSLA", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("MSFT", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("GOOGL", SecurityType.Equity, Market.USA), Resolution.Second),
            };

            foreach (var config in configs)
            {
                ProcessFeed(
                    brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    (tick) => {
                        if (tick != null)
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol.Value, (tick as Tick)?.BidPrice, (tick as Tick)?.AskPrice);
                        }
                    });
            }

            Thread.Sleep(20000);

            foreach (var config in configs)
            {
                brokerage.Unsubscribe(config);
            }

            Thread.Sleep(20000);

            cancelationToken.Cancel();
        }

        [Test]
        public void SubscribesAndUnsubscribesMultipleSymbols()
        {
            var symbols = new List<string>
            {
                "AAPL", "FB", "MSFT", "GOOGL"
            };

            var brokerage = (AlpacaBrokerage)Brokerage;

            var configs = new List<SubscriptionDataConfig>();
            foreach (var symbol in symbols)
            {
                configs.Add(GetSubscriptionDataConfig<QuoteBar>(Symbol.Create(symbol, SecurityType.Forex, Market.Oanda), Resolution.Second));
            }

            var stopwatch = Stopwatch.StartNew();
            foreach (var config in configs)
            {
                brokerage.Subscribe(config, (s, e) => { });
            }
            stopwatch.Stop();
            Console.WriteLine("Subscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(10000);

            stopwatch.Restart();
            foreach (var config in configs)
            {
                brokerage.Unsubscribe(config);
            }
            Console.WriteLine("Unsubscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(5000);
        }

    }
}