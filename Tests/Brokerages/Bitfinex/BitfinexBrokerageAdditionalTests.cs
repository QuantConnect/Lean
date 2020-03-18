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
using System.Net;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Interfaces;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageAdditionalTests
    {
        private BitfinexBrokerage _brokerage;
        private readonly Mock<IWebSocket> _webSocket = new Mock<IWebSocket>();
        private readonly Mock<IRestClient> _restClient = new Mock<IRestClient>();
        private readonly IAlgorithm _algorithm = new QCAlgorithm();

        [SetUp]
        public void Setup()
        {
            var priceProvider = new Mock<IPriceProvider>();
            priceProvider.Setup(x => x.GetLastPrice(It.IsAny<Symbol>())).Returns(1.234m);

            _brokerage = new BitfinexBrokerage(
                "wss://localhost",
                _webSocket.Object,
                _restClient.Object,
                "apikey",
                "apisecret",
                _algorithm,
                priceProvider.Object);

            _algorithm.SetBrokerageModel(new BitfinexBrokerageModel());
        }

        [TearDown]
        public void TearDown()
        {
            _brokerage.Disconnect();
            _brokerage.Dispose();
        }

        [Test]
        public void ReturnsCorrectCashBalancesWithMarginPositions()
        {
            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v1/balances")))
                .Returns(new RestResponse
                {
                    Content = @"
[
  { 'type':'trading', 'currency':'btc', 'amount':'0.0', 'available':'0.0' },
  { 'type':'trading', 'currency':'eth', 'amount':'1.0', 'available':'0.5' },
  { 'type':'trading', 'currency':'usd', 'amount':'0.0', 'available':'0.0' }
]",
                    StatusCode = HttpStatusCode.OK
                });

            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v1/positions")))
                .Returns(new RestResponse
                {
                    Content = "[{'id':142974855,'symbol':'btcusd','status':'ACTIVE','base':'7995.0','amount':'0.05','timestamp':'1583871936.0','swap':'-0.00648185','pl':'-2.48113'}]",
                    StatusCode = HttpStatusCode.OK
                });

            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v1/pubticker/BTCUSD")))
                .Returns(new RestResponse
                {
                    Content = "{'mid':'7964.5','bid':'7963.2','ask':'7965.8','last_price':'7957.4','low':'7776.6','high':'8180.0','volume':'9934.81070662000002805','timestamp':'1583887538.695179'}",
                    StatusCode = HttpStatusCode.OK
                });

            var balances = _brokerage.GetCashBalance();

            Assert.AreEqual(3, balances.Count);

            var eth = balances.Single(a => a.Currency == "ETH");
            var btc = balances.Single(a => a.Currency == "BTC");
            var usd = balances.Single(a => a.Currency == "USD");

            Assert.AreEqual(1.0, eth.Amount);
            Assert.AreEqual(0.05, btc.Amount);
            // 7995 * 0.05
            Assert.AreEqual(-399.75, usd.Amount);
        }
    }
}