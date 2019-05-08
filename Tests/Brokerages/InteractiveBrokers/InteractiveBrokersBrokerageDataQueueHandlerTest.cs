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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBGateway to be installed.")]
    public class InteractiveBrokersBrokerageDataQueueHandlerTest
    {
        [Test]
        public void GetsTickData()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider()))
            {
                ib.Connect();

                ib.Subscribe(null, new List<Symbol> {Symbols.USDJPY, Symbols.EURGBP});

                Thread.Sleep(2000);

                var gotUsdData = false;
                var gotEurData = false;
                for (int i = 0; i < 20; i++)
                {
                    foreach (var tick in ib.GetNextTicks())
                    {
                        Console.WriteLine("{0}: {1} - {2} @ {3}", tick.Time, tick.Symbol, tick.Price, ((Tick)tick).Quantity);
                        gotUsdData |= tick.Symbol == Symbols.USDJPY;
                        gotEurData |= tick.Symbol == Symbols.EURGBP;
                    }
                }

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }

        [Test]
        public void GetsTickDataAfterDisconnectionConnectionCycle()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider()))
            {
                ib.Connect();
                ib.Subscribe(null, new List<Symbol> {Symbols.USDJPY, Symbols.EURGBP});
                ib.Disconnect();
                Thread.Sleep(2000);

                for (var i = 0; i < 20; i++)
                {
                    foreach (var tick in ib.GetNextTicks()) // we need to make sure we consumer the already sent data, if any
                    {
                        Console.WriteLine("{0}: {1} - {2} @ {3}", tick.Time, tick.Symbol, tick.Price, ((Tick)tick).Quantity);
                    }
                }

                ib.Connect();
                Thread.Sleep(2000);

                var gotUsdData = false;
                var gotEurData = false;
                for (var i = 0; i < 20; i++)
                {
                    foreach (var tick in ib.GetNextTicks())
                    {
                        Console.WriteLine("{0}: {1} - {2} @ {3}", tick.Time, tick.Symbol, tick.Price, ((Tick)tick).Quantity);
                        gotUsdData |= tick.Symbol == Symbols.USDJPY;
                        gotEurData |= tick.Symbol == Symbols.EURGBP;
                    }
                }
                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }
    }
}
