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
using QuantConnect.Brokerages.Alpaca;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Brokerages.Alpaca
{
    [TestFixture]
    public partial class AlpacaBrokerageTests
    {
        [Test]
        public void GetsTickData()
        {
            var brokerage = (AlpacaBrokerage)Brokerage;

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
            });

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("TSLA", SecurityType.Equity, Market.USA),
                Symbol.Create("MSFT", SecurityType.Equity, Market.USA),
            });

            brokerage.Subscribe(null, new List<Symbol>
            {
                Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
            });

            Thread.Sleep(20000);

            foreach (var tick in brokerage.GetNextTicks())
            {
                Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol.Value, ((Tick)tick).BidPrice, ((Tick)tick).AskPrice);
            }

            brokerage.Unsubscribe(null, new List<Symbol>
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
                Symbol.Create("TSLA", SecurityType.Equity, Market.USA),
                Symbol.Create("MSFT", SecurityType.Equity, Market.USA),
                Symbol.Create("GOOGL", SecurityType.Equity, Market.USA),
            });

            Thread.Sleep(20000);

            foreach (var tick in brokerage.GetNextTicks())
            {
                Log.Trace("{0}: {1} - {2} / {3}", tick.Time, tick.Symbol.Value, ((Tick)tick).BidPrice, ((Tick)tick).AskPrice);
            }

            Thread.Sleep(5000);
        }

        [Test]
        public void SubscribesAndUnsubscribesMultipleSymbols()
        {
            var symbols = new List<string>
            {
                "AAPL", "FB", "MSFT", "GOOGL"
            };

            var brokerage = (AlpacaBrokerage)Brokerage;

            var stopwatch = Stopwatch.StartNew();
            foreach (var symbol in symbols)
            {
                brokerage.Subscribe(null, new List<Symbol>
                {
                    Symbol.Create(symbol, SecurityType.Equity, Market.USA),
                });
            }
            stopwatch.Stop();
            Console.WriteLine("Subscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(10000);

            stopwatch.Restart();
            foreach (var symbol in symbols)
            {
                brokerage.Unsubscribe(null, new List<Symbol>
                {
                    Symbol.Create(symbol, SecurityType.Equity, Market.USA),
                });
            }
            Console.WriteLine("Unsubscribe: Elapsed time: " + stopwatch.Elapsed);

            Thread.Sleep(5000);
        }

    }
}