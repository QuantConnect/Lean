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

using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Kraken;
using QuantConnect.Configuration;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Kraken
{
    [TestFixture, Ignore("These tests requires a configured and active Kraken account.")]
    public class KrakenBrokerageAdditionalTests
    {
        [Test]
        public void PublicEndpointCallsAreRateLimited()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                for (var i = 0; i < 50; i++)
                {
                    Assert.DoesNotThrow(() => brokerage.GetTick(Symbols.BTCEUR));
                }
            }
        }

        [Test]
        public void PrivateEndpointCallsAreRateLimited()
        {
            using (var brokerage = GetBrokerage())
            {
                Assert.IsTrue(brokerage.IsConnected);

                for (var i = 0; i < 50; i++)
                {
                    Assert.DoesNotThrow(() => brokerage.GetOpenOrders());
                }
            }
        }

        private static KrakenBrokerage GetBrokerage()
        {
            var apiKey    = Config.Get("kraken-api-key");
            var apiSecret = Config.Get("kraken-api-secret");
            
            var brokerage = new KrakenBrokerage(apiKey, apiSecret);
            
            // NOP
            brokerage.Connect();

            return brokerage;
        }
    }
}
