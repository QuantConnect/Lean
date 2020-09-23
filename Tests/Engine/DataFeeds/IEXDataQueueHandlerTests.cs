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
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IEX;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    [Explicit("Tests are dependent on network and are long")]
    public class IEXDataQueueHandlerTests
    {
        private void ProcessFeed(IEnumerator<BaseData> enumerator, Action<BaseData> callback = null)
        {
            Task.Run(() =>
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        BaseData tick = enumerator.Current;
                        if (callback != null)
                        {
                            callback.Invoke(tick);
                        }
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

            ProcessFeed(
                iex.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbol.Create("firehose", SecurityType.Equity, Market.USA), Resolution.Second), (s, e) => { }),
                tick =>
                {
                    if (tick != null)
                    {
                        Console.WriteLine(tick.ToString());
                    }
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

            Array.ForEach(new[] { "FB", "AAPL", "XIV", "PTN", "USO" }, (ticker) =>
            {
                ProcessFeed(
                    iex.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbol.Create(ticker, SecurityType.Equity, Market.USA), Resolution.Second), (sender, e) => { }),
                    tick =>
                    {
                        if (tick != null)
                        {
                            Console.WriteLine(tick.ToString());
                        }
                    });
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

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MBLY", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("FB", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("USO", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    tick =>
                    {
                        if (tick != null)
                        {
                            Console.WriteLine(tick.ToString());
                        }
                    });
            });

            Thread.Sleep(10000);

            Console.WriteLine("Unsubscribing from all except MBLY");

            Array.ForEach(configs, (c) =>
            {
                if (!string.Equals(c.Symbol.Value, "MBLY"))
                {
                    iex.Unsubscribe(c);
                }
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
            Action<BaseData> callback = (tick) =>
            {
                if (tick == null)
                    return;

                Console.WriteLine(tick.ToString());
                if (unsubscribed && tick.Symbol.Value == "MBLY")
                {
                    Assert.Fail("Should not receive data for unsubscribed symbol");
                }
            };

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MBLY", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("USO", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    callback);
            });

            Thread.Sleep(20000);

            iex.Unsubscribe(configs.First(c => string.Equals(c.Symbol.Value, "MBLY")));

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

        public static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(15), true, false),
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(3), true, false),

                    // invalid resolution == empty result.
                    new TestCaseData(Symbols.SPY, Resolution.Tick, TimeSpan.FromSeconds(15), false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Second, Time.OneMinute, false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Hour, Time.OneDay, false, false),

                    // invalid period == empty result
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(45), false, false), // beyond 30 days
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(-15), false, false), // date in future
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(365*5.5), false, false), // beyond 5 years

                    // invalid symbol: XYZ -> not found WebException
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Equity, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), false, true),

                    // invalid security type, no exception, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false, false)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void IEXCouldGetHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool received, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new IEXDataQueueHandler();
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager()));

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
            };

            if (throwsException)
            {
                Assert.Throws<WebException>(test);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }

        #endregion

        #region helper

        protected SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        #endregion

    }
}