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
using NUnit.Framework;
using QuantConnect.Brokerages.Oanda;
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
            var brokerage = (OandaBrokerage)Brokerage;

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("EURJPY", SecurityType.Forex, Market.Oanda),
                Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda),
            });

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda),
            });

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("XAUXAG", SecurityType.Cfd, Market.Oanda),
            });

            Thread.Sleep(20000);

            foreach (var tick in brokerage.GetNextTicks())
            {
                Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol.Value, ((Tick)tick).BidPrice, ((Tick)tick).AskPrice);
            }

            brokerage.Unsubscribe(null, new List<Symbol>
            {
                Symbol.Create("EURJPY", SecurityType.Forex, Market.Oanda),
                Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda),
                Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda),
                Symbol.Create("XAUXAG", SecurityType.Cfd, Market.Oanda),
            });

            Thread.Sleep(20000);

            foreach (var tick in brokerage.GetNextTicks())
            {
                Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol.Value, ((Tick)tick).BidPrice, ((Tick)tick).AskPrice);
            }

            Thread.Sleep(5000);
        }

        [Test]
        public void GroupsMultipleSubscriptions()
        {
            var symbols = new List<string>
            {
                "AUDJPY", "AUDUSD", "EURCHF", "EURGBP", "EURJPY", "EURUSD", "GBPAUD", 
                "GBPJPY", "GBPUSD", "NZDUSD", "USDCAD", "USDCHF", "USDJPY"
            };

            var brokerage = (OandaBrokerage)Brokerage;

            var stopwatch = Stopwatch.StartNew();
            foreach (var symbol in symbols)
            {
                brokerage.Subscribe(null, new List<Symbol>
                {
                    Symbol.Create(symbol, SecurityType.Forex, Market.Oanda),
                });
                //Thread.Sleep(50);
            }
            stopwatch.Stop();
            Console.WriteLine("Subscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(10000);

            stopwatch.Restart();
            foreach (var symbol in symbols)
            {
                brokerage.Unsubscribe(null, new List<Symbol>
                {
                    Symbol.Create(symbol, SecurityType.Forex, Market.Oanda),
                });
            }
            Console.WriteLine("Unsubscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(5000);
        }

    }
}