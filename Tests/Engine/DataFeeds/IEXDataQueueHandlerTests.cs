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
 *
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IEX;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    [Explicit("Tests are dependent on network and are long")]
    public class IEXDataQueueHandlerTests
    {
        private void ProcessFeed(IEXDataQueueHandler iex, Action<BaseData> callback = null)
        {
            Task.Run(() =>
            {
                foreach (var tick in iex.GetNextTicks())
                {
                    try
                    {
                        if (callback != null)
                        {
                            callback.Invoke(tick);
                        }
                    }
                    catch (AssertionException)
                    {
                        throw;
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            });
        }

        [Test]
        public void IEXCouldConnect()
        {
            var iex = new IEXDataQueueHandler();
            Thread.Sleep(5000);
            Assert.IsTrue(iex.IsConnected);
            iex = null;
            GC.Collect(2, GCCollectionMode.Forced, true);
            Thread.Sleep(1000);
            // finalizer should print disconnected message
        }

        /// <summary>
        /// Firehose is a special symbol that subscribes to all IEX symbols
        /// </summary>
        [Test]
        public void IEXCouldSubscribeToAll()
        {
            var iex = new IEXDataQueueHandler();

            ProcessFeed(iex, tick => Console.WriteLine(tick.ToString()));

            iex.Subscribe(null, new[]
            {
                Symbol.Create("firehose", SecurityType.Equity, Market.USA)
            });

            Thread.Sleep(30000);
            iex.Dispose();
        }

        /// <summary>
        /// Subscribe to multiple symbols in a single call
        /// </summary>
        [Test]
        public void IEXCouldSubscribe()
        {
            var iex = new IEXDataQueueHandler();

            ProcessFeed(iex, tick => Console.WriteLine(tick.ToString()));

            iex.Subscribe(null, new[]
            {
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("XIV", SecurityType.Equity, Market.USA),
                Symbol.Create("PTN", SecurityType.Equity, Market.USA),
                Symbol.Create("USO", SecurityType.Equity, Market.USA),
            });

            Thread.Sleep(10000);
            iex.Dispose();
        }

        /// <summary>
        /// Subscribe to multiple symbols in a series of calls
        /// </summary>
        [Test]
        public void IEXCouldSubscribeManyTimes()
        {
            var iex = new IEXDataQueueHandler();

            ProcessFeed(iex, tick => Console.WriteLine(tick.ToString()));

            iex.Subscribe(null, new[]
            {
                Symbol.Create("MBLY", SecurityType.Equity, Market.USA),
            });

            iex.Subscribe(null, new[]
            {
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
            });

            iex.Subscribe(null, new[]
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            });

            iex.Subscribe(null, new[]
            {
                Symbol.Create("USO", SecurityType.Equity, Market.USA),
            });

            Thread.Sleep(10000);

            Console.WriteLine("Unsubscribing from all except MBLY");

            iex.Unsubscribe(null, new[]
            {
                Symbol.Create("FB", SecurityType.Equity, Market.USA),
            });

            iex.Unsubscribe(null, new[]
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            });

            iex.Unsubscribe(null, new[]
            {
                Symbol.Create("USO", SecurityType.Equity, Market.USA),
            });

            Thread.Sleep(10000);

            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeAndUnsubscribe()
        {
            // MBLY is the most liquid IEX instrument
            var iex = new IEXDataQueueHandler();
            var unsubscribed = false;
            ProcessFeed(iex, tick =>
            {
                Console.WriteLine(tick.ToString());
                if (unsubscribed && tick.Symbol.Value == "MBLY")
                {
                    Assert.Fail("Should not receive data for unsubscribed symbol");
                }
            });

            iex.Subscribe(null, new[] {
                Symbol.Create("MBLY", SecurityType.Equity, Market.USA),
                Symbol.Create("USO", SecurityType.Equity, Market.USA)
            });

            Thread.Sleep(20000);

            iex.Unsubscribe(null, new[]
            {
                Symbol.Create("MBLY", SecurityType.Equity, Market.USA)
            });
            Console.WriteLine("Unsubscribing");
            Thread.Sleep(2000);
            // some messages could be inflight, but after a pause all MBLY messages must have beed consumed by ProcessFeed
            unsubscribed = true;

            Thread.Sleep(20000);
            iex.Dispose();
        }

        [Test]
        public void IEXCouldReconnect()
        {
            var iex = new IEXDataQueueHandler();
            var realEndpoint = iex.Endpoint;
            Thread.Sleep(1000);
            iex.Dispose();
            iex.Endpoint = "https://badd.address";
            iex.Reconnect();
            Thread.Sleep(1000);
            iex.Dispose();
            iex.Endpoint = realEndpoint;
            iex.Reconnect();
            Thread.Sleep(1000);
            Assert.IsTrue(iex.IsConnected);
            iex.Dispose();
        }

        #region History provider tests

        public TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(15), true),
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(3), true),

                    // invalid resolution == empty result.
                    new TestCaseData(Symbols.SPY, Resolution.Tick, TimeSpan.FromSeconds(15), false),
                    new TestCaseData(Symbols.SPY, Resolution.Second, Time.OneMinute, false),
                    new TestCaseData(Symbols.SPY, Resolution.Hour, Time.OneDay, false),

                    // invalid period == empty result
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(45), false), // beyond 30 days
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(-15), false), // date in future
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(365*5.5), false), // beyond 5 years

                    // invalid symbol: XYZ
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Equity, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), false)
                        .Throws("System.Net.WebException"),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Forex"
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false)
                        .Throws("System.Net.WebException")
                };
            }
        }

        [Test, TestCaseSource("TestParameters")]
        public void IEXCouldGetHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool received)
        {
            var historyProvider = new IEXDataQueueHandler();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null));

            var now = DateTime.UtcNow;

            var requests = new[]
            {
                new HistoryRequest(now.Add(-period),
                                   now,
                                   typeof(QuoteBar),
                                   symbol,
                                   resolution,
                                   SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                   DateTimeZone.Utc,
                                   Resolution.Minute,
                                   false,
                                   false,
                                   DataNormalizationMode.Adjusted,
                                   TickType.Quote)
            };

            var history = historyProvider.GetHistory(requests, TimeZones.Utc);

            foreach (var slice in history)
            {
                if (resolution == Resolution.Tick || resolution == Resolution.Second || resolution == Resolution.Hour)
                {
                    Assert.IsNull(slice);
                }
                else if (resolution == Resolution.Daily || resolution == Resolution.Minute)
                {
                    Assert.IsNotNull(slice);

                    var bar = slice.Bars[symbol];

                    Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}: {6}, {7}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, resolution, period);
                }
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
            if (received)
            {
                Assert.IsTrue(historyProvider.DataPointCount > 0);
            }
            else
            {
                Assert.IsTrue(historyProvider.DataPointCount == 0);
            }
        }

        #endregion
    }
}