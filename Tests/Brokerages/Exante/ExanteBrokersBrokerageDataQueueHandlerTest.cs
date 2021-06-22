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
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages.Exante;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Brokerages.Exante
{
    // Test DataQueueHandler
    [TestFixture]
    public partial class ExanteBrokerageTests
    {
        [Test]
        public void GetsTickData()
        {
            var brokerage = (ExanteBrokerage) Brokerage;
            brokerage.Connect();
            var gotUsdData = false;

            var cancelationToken = new CancellationTokenSource();

            var market = "ARCA";
            Market.Add(market, 998);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, market);

            ProcessFeed(
                brokerage.Subscribe(GetSubscriptionDataConfig<TradeBar>(symbol, Resolution.Second), (s, e) =>
                {
                    gotUsdData = true;
                }),
                cancelationToken,
                (tick) => Log(tick));

            Thread.Sleep(2000);
            cancelationToken.Cancel();
            cancelationToken.Dispose();

            Assert.IsTrue(gotUsdData);
        }

        private new static void ProcessFeed(
            IEnumerator<BaseData> enumerator,
            CancellationTokenSource cancellationToken,
            Action<BaseData> callback = null
            )
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

        private new static SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
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

        private static void Log(BaseData tick)
        {
            if (tick != null)
            {
                QuantConnect.Logging.Log.Trace("{0}: {1} - {2} @ {3}", tick.Time, tick.Symbol, tick.Price,
                    ((Tick) tick).Quantity);
            }
        }
    }
}
