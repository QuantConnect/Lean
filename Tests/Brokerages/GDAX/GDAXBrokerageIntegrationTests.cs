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

using QuantConnect.Brokerages.GDAX;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using Moq;
using QuantConnect.Brokerages;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class GDAXBrokerageIntegrationTests : BrokerageTests
    {
        #region Properties
        protected override Symbol Symbol
        {
            get { return Symbol.Create("ETHBTC", SecurityType, Market.GDAX); }
        }

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType
        {
            get { return SecurityType.Crypto; }
        }

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice
        {
            get { return 1m; }
        }

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice
        {
            get { return 0.0001m; }
        }

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var restClient = new RestClient("https://api.pro.coinbase.com");
            var webSocketClient = new WebSocketWrapper();

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.BrokerageModel).Returns(new GDAXBrokerageModel(AccountType.Cash));

            var priceProvider = new ApiPriceProvider(Config.GetInt("job-user-id"), Config.Get("api-access-token"));

            return new GDAXBrokerage(Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com"), webSocketClient, restClient,
                Config.Get("gdax-api-key"), Config.Get("gdax-api-secret"), Config.Get("gdax-passphrase"), algorithm.Object,
                priceProvider);
        }

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            var tick = ((GDAXBrokerage)this.Brokerage).GetTick(symbol);
            return tick.AskPrice;
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported");
        }

        //no stop limit support
        public override TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
            new TestCaseData(new StopMarketOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopMarketOrder"),
        };
    }
}
