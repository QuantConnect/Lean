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
using QuantConnect.Data;
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
            var aggregator = new Mock<IDataAggregator>();

            _brokerage = new BitfinexBrokerage(
                _webSocket.Object,
                _restClient.Object,
                "apikey",
                "apisecret",
                _algorithm,
                priceProvider.Object,
                aggregator.Object);

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
            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v2/auth/r/wallets")))
                .Returns(new RestResponse
                {
                    Content = @"
[
    [""margin"", ""BTC"", 0, 0, null, null, null],
    [""margin"", ""ETH"", 1, 0, null, null, null],
    [""margin"", ""USD"", 0, 0, null, null, null]
]",
                    StatusCode = HttpStatusCode.OK
                });

            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v2/auth/r/positions")))
                .Returns(new RestResponse
                {
                    Content = @"[[""tBTCUSD"",""ACTIVE"",0.004,9782.71142321,0,0,-2.400852892839999,-5.947343206195589,5114.619993419812,1.0413992183285932,null,143625635,1591796461000,1591796461000,null,0,null,0,0,{""reason"":""TRADE"",""order_id"":46295906640,""liq_stage"":null,""trade_price"":""9782.71142321"",""trade_amount"":""0.004"",""order_id_oppo"":46295895839}]]",
                    StatusCode = HttpStatusCode.OK
                });

            _restClient.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/v2/ticker/tBTCUSD")))
                .Returns(new RestResponse
                {
                    Content = "[9236,21.86775367,9236.1,17.27753753,-76,-0.0082,9236.1,2480.56819599,9339.03025493,9107]",
                    StatusCode = HttpStatusCode.OK
                });

            var balances = _brokerage.GetCashBalance();

            Assert.AreEqual(3, balances.Count);

            var eth = balances.Single(a => a.Currency == "ETH");
            var btc = balances.Single(a => a.Currency == "BTC");
            var usd = balances.Single(a => a.Currency == "USD");

            Assert.AreEqual(1.0, eth.Amount);
            Assert.AreEqual(0.004, btc.Amount);
            // 9236.1 * 0.004
            Assert.AreEqual(-39.13084569284m, usd.Amount);
        }
    }
}