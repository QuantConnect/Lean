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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages.Fxcm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Brokerages.Fxcm
{
    [TestFixture]
    public partial class FxcmBrokerageTests
    {
        [Test]
        public void GetsTickData()
        {
            var brokerage = (FxcmBrokerage)Brokerage;
            var cancelationToken = new CancellationTokenSource();

            var configs = new SubscriptionDataConfig[] {
                GetSubscriptionDataConfig<TradeBar>(Symbols.USDJPY, Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbols.EURGBP, Resolution.Second)
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

            Thread.Sleep(5000);

            foreach (var config in configs)
            {
                brokerage.Unsubscribe(config);
            }

            Thread.Sleep(20000);

            cancelationToken.Cancel();
        }
    }
}
