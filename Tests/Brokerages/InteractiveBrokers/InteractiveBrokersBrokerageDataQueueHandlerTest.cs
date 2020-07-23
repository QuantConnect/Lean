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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    [Ignore("These tests require the IBGateway to be installed.")]
    public class InteractiveBrokersBrokerageDataQueueHandlerTest
    {
        [Test]
        public void GetsTickData()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider(), new AggregationManager()))
            {
                ib.Connect();
                var gotUsdData = false;
                var gotEurData = false;

                ib.Subscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.USDJPY, Resolution.Minute), (s, e) => { gotUsdData = true; });
                ib.Subscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.EURGBP, Resolution.Minute), (s, e) => { gotEurData = true; });

                Thread.Sleep(2000);

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }

        [Test]
        public void GetsTickDataAfterDisconnectionConnectionCycle()
        {
            using (var ib = new InteractiveBrokersBrokerage(new QCAlgorithm(), new OrderProvider(), new SecurityProvider(), new AggregationManager()))
            {
                var gotUsdData = false;
                var gotEurData = false;
                ib.Connect();
                ib.Subscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.USDJPY, Resolution.Minute), (s, e) => { gotUsdData = true; });
                ib.Subscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.EURGBP, Resolution.Minute), (s, e) => { gotEurData = true; });
                Thread.Sleep(2000);

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);

                ib.Disconnect();
                gotUsdData = false;
                gotEurData = false;

                Thread.Sleep(2000);

                ib.Connect();
                Thread.Sleep(2000);

                Assert.IsTrue(gotUsdData);
                Assert.IsTrue(gotEurData);
            }
        }

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
    }
}
