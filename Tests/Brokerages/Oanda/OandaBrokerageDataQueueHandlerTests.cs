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
using QuantConnect.Brokerages.Oanda;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Brokerages.Oanda
{
    [TestFixture]
    public partial class OandaBrokerageTests
    {
        [Test]
        public void GetsTickData()
        {
            var cancelationToken = new CancellationTokenSource();
            var brokerage = (OandaBrokerage)Brokerage;

            var configs = new SubscriptionDataConfig[] {
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("EURJPY", SecurityType.Forex, Market.Oanda), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda), Resolution.Second),
                GetSubscriptionDataConfig<QuoteBar>(Symbol.Create("XAUXAG", SecurityType.Cfd, Market.Oanda), Resolution.Second),
            };

            foreach (var config in configs)
            {
                ProcessFeed(
                    brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    (tick) => {
                        if (tick != null)
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time.ToStringInvariant("yyyy-MM-dd HH:mm:ss.fff"), tick.Symbol, (tick as Tick)?.BidPrice, (tick as Tick)?.AskPrice);
                        }
                    });
            }

            Thread.Sleep(20000);

            foreach (var config in configs)
            {
                if (!config.Symbol.Value.Equals("EURUSD", StringComparison.OrdinalIgnoreCase))
                {
                    brokerage.Unsubscribe(config);
                }
            }
            
            Thread.Sleep(20000);

            cancelationToken.Cancel();
        }

        [Test]
        public void GroupsMultipleSubscriptions()
        {
            var symbols = new List<string>
            {
                "AUDJPY", "AUDUSD", "EURCHF", "EURGBP", "EURJPY", "EURUSD", "GBPAUD",
                "GBPJPY", "GBPUSD", "NZDUSD", "USDCAD", "USDCHF", "USDJPY"
            };

            var configs = new List<SubscriptionDataConfig>();
            foreach (var symbol in symbols)
            {
                configs.Add(GetSubscriptionDataConfig<QuoteBar>(Symbol.Create(symbol, SecurityType.Forex, Market.Oanda), Resolution.Second));
            }

            var brokerage = (OandaBrokerage)Brokerage;

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