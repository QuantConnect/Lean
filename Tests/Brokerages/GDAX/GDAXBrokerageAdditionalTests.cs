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

using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.GDAX;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture, Explicit("These tests requires a configured and active GDAX account.")]
    public class GDAXBrokerageAdditionalTests
    {
        [Test]
        public void PublicEndpointCallsAreRateLimited()
        {
            using (var brokerage = GetBrokerage())
            {
                brokerage.Connect();
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
                brokerage.Connect();
                Assert.IsTrue(brokerage.IsConnected);

                for (var i = 0; i < 50; i++)
                {
                    Assert.DoesNotThrow(() => brokerage.GetOpenOrders());
                }
            }
        }

        [Test]
        public void ClientConnects()
        {
            using (var brokerage = GetBrokerage())
            {
                var hasError = false;

                brokerage.Message += (s, e) => { hasError = true; };

                Log.Trace("Connect #1");
                brokerage.Connect();
                Assert.IsTrue(brokerage.IsConnected);

                Assert.IsFalse(hasError);

                Log.Trace("Disconnect #1");
                brokerage.Disconnect();
                Assert.IsFalse(brokerage.IsConnected);

                Log.Trace("Connect #2");
                brokerage.Connect();
                Assert.IsTrue(brokerage.IsConnected);

                Log.Trace("Disconnect #2");
                brokerage.Disconnect();
                Assert.IsFalse(brokerage.IsConnected);
            }
        }

        [Test]
        public void DataQueueHandlerConnectsAndSubscribes()
        {
            var symbols = new[]
            {
                "LTCUSD", "LTCEUR", "LTCBTC",
                "BTCUSD", "BTCEUR", "BTCGBP",
                "ETHBTC", "ETHUSD", "ETHEUR",
                "BCHBTC", "BCHUSD", "BCHEUR",
                "XRPUSD", "XRPEUR", "XRPBTC",
                "EOSUSD", "EOSEUR", "EOSBTC",
                "XLMUSD", "XLMEUR", "XLMBTC",
                "ETCUSD", "ETCEUR", "ETCBTC",
                "ZRXUSD", "ZRXEUR", "ZRXBTC"
            }
            .Select(ticker => Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX))
            .ToList();

            using (var dqh = GetDataQueueHandler())
            {
                dqh.Connect();
                Assert.IsTrue(dqh.IsConnected);

                dqh.Subscribe(symbols);

                Thread.Sleep(5000);

                dqh.Unsubscribe(symbols);

                dqh.Disconnect();
                Assert.IsFalse(dqh.IsConnected);
            }
        }

        private static GDAXDataQueueHandler GetDataQueueHandler()
        {
            var wssUrl = Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com");
            var webSocketClient = new WebSocketClientWrapper();
            var restClient = new RestClient("https://api.pro.coinbase.com");
            var apiKey = Config.Get("gdax-api-key");
            var apiSecret = Config.Get("gdax-api-secret");
            var passPhrase = Config.Get("gdax-passphrase");
            var algorithm = new QCAlgorithm();
            var userId = Config.GetInt("job-user-id");
            var userToken = Config.Get("api-access-token");
            var priceProvider = new ApiPriceProvider(userId, userToken);
            var aggregator = new AggregationManager();

            return new GDAXDataQueueHandler(wssUrl, webSocketClient, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider, aggregator);
        }

        private static GDAXBrokerage GetBrokerage()
        {
            var wssUrl = Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com");
            var webSocketClient = new WebSocketClientWrapper();
            var restClient = new RestClient("https://api.pro.coinbase.com");
            var apiKey = Config.Get("gdax-api-key");
            var apiSecret = Config.Get("gdax-api-secret");
            var passPhrase = Config.Get("gdax-passphrase");
            var algorithm = new QCAlgorithm();
            var userId = Config.GetInt("job-user-id");
            var userToken = Config.Get("api-access-token");
            var priceProvider = new ApiPriceProvider(userId, userToken);
            var aggregator = new AggregationManager();

            return new GDAXBrokerage(wssUrl, webSocketClient, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider, aggregator);
        }
    }
}
