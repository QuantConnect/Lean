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
using QuantConnect.Brokerages.GDAX;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using Moq;
using QuantConnect.Brokerages;
using QuantConnect.Tests.Common.Securities;
using RestSharp;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture, Explicit("This test requires a configured and active account")]
    public class GDAXBrokerageIntegrationTests : BrokerageTests
    {
        #region Properties
        protected override Symbol Symbol => Symbol.Create("ETHBTC", SecurityType.Crypto, Market.GDAX);

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Crypto;

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var restClient = new RestClient("https://api.pro.coinbase.com");
            var webSocketClient = new WebSocketClientWrapper();

            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                {Symbol, CreateSecurity(Symbol)}
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new GDAXBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));
            algorithm.Setup(a => a.Securities).Returns(securities);

            var priceProvider = new Mock<IPriceProvider>();
            priceProvider.Setup(a => a.GetLastPrice(It.IsAny<Symbol>())).Returns(1.234m);

            var aggregator = new AggregationManager();
            return new GDAXBrokerage(Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com"), webSocketClient, restClient,
                Config.Get("gdax-api-key"), Config.Get("gdax-api-secret"), Config.Get("gdax-passphrase"), algorithm.Object,
                priceProvider.Object, aggregator);
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
            var tick = ((GDAXBrokerage)Brokerage).GetTick(symbol);
            return tick.AskPrice;
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported");
        }

        [Test]
        public override void GetAccountHoldings()
        {
            // GDAX GetAccountHoldings() always returns an empty list
            Assert.That(Brokerage.GetAccountHoldings().Count == 0);
        }

        // stop market orders no longer supported (since 3/23/2019)
        // no stop limit support
        private static TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.GDAX))).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(Symbol.Create("ETHBTC", SecurityType.Crypto, Market.GDAX), 1m, 0.0001m)).SetName("LimitOrder"),
        };

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }
    }
}
