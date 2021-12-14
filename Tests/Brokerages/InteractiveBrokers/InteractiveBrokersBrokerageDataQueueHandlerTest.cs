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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture, Explicit("These tests require the IBGateway to be installed.")]
    public class InteractiveBrokersBrokerageDataQueueHandlerTest
    {
        [Test]
        public void FutureSubscriptions()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider(), new AggregationManager(), TestGlobals.MapFileProvider))
            {
                ib.Connect();
                var gotEsData = false;
                var gotHsiData = false;

                var cancelationToken = new CancellationTokenSource();

                var es = Symbols.CreateFuturesCanonicalSymbol("ES");
                var firstEs = ib.LookupSymbols(es, includeExpired: false).First();
                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(firstEs, Resolution.Second), (s, e) => { gotEsData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                // non USD quote currency, HDK
                var hsi = Symbols.CreateFuturesCanonicalSymbol("HSI");
                var firstHsi = ib.LookupSymbols(hsi, includeExpired: false).First();
                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(firstHsi, Resolution.Second), (s, e) => { gotHsiData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                Thread.Sleep(2000);
                cancelationToken.Cancel();
                cancelationToken.Dispose();

                Assert.IsTrue(gotEsData);
                Assert.IsTrue(gotHsiData);
            }
        }

        [Test]
        public void GetsTickData()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider(), new AggregationManager(), TestGlobals.MapFileProvider))
            {
                ib.Connect();
                var gotUsdData = false;
                var gotEurData = false;

                var cancelationToken = new CancellationTokenSource();

                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Second), (s, e) => { gotUsdData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.SPY, Resolution.Second), (s, e) => { gotEurData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                Thread.Sleep(2000);
                cancelationToken.Cancel();
                cancelationToken.Dispose();

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }

        [Test]
        public void GetsTickDataAfterDisconnectionConnectionCycle()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider(), new AggregationManager(), TestGlobals.MapFileProvider))
            {
                ib.Connect();
                var cancelationToken = new CancellationTokenSource();
                var gotUsdData = false;
                var gotEurData = false;

                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Second), (s, e) => { gotUsdData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                ProcessFeed(
                    ib.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.SPY, Resolution.Second), (s, e) => { gotEurData = true; }),
                    cancelationToken,
                    (tick) => Log(tick));

                Thread.Sleep(2000);

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);

                ib.Disconnect();
                gotUsdData = false;
                gotEurData = false;

                Thread.Sleep(2000);

                ib.Connect();
                Thread.Sleep(2000);

                cancelationToken.Cancel();
                cancelationToken.Dispose();

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }

        protected SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                entry.DataTimeZone,
                entry.ExchangeHours.TimeZone,
                true,
                true,
                false);
        }

        private void ProcessFeed(IEnumerator<BaseData> enumerator, CancellationTokenSource cancellationToken, Action<BaseData> callback = null)
        {
            Task.Run(() =>
            {
                try
                {
                    while (enumerator.MoveNext() && !cancellationToken.IsCancellationRequested)
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
                    QuantConnect.Logging.Log.Error(err.Message);
                }
            });
        }

        private void Log(BaseData dataPoint)
        {
            if (dataPoint != null)
            {
                QuantConnect.Logging.Log.Trace("{dataPoint}");
            }
        }
    }
}
