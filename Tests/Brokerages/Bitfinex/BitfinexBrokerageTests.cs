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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using NUnit.Framework;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Configuration;
using Moq;
using QuantConnect.Brokerages;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    [Ignore("This test requires a configured Bitfinex account")]
    public partial class BitfinexBrokerageTests : BrokerageTests
    {
        /// <summary>
        /// Creates the brokerage under test and connects it
        /// </summary>
        /// <param name="orderProvider"></param>
        /// <param name="securityProvider"></param>
        /// <returns></returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                {Symbol, CreateSecurity(Symbol)}
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new BitfinexBrokerageModel(AccountType.Margin));
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));

            var priceProvider = new Mock<IPriceProvider>();
            priceProvider.Setup(a => a.GetLastPrice(It.IsAny<Symbol>())).Returns(1.234m);

            return new BitfinexBrokerage(
                    Config.Get("bitfinex-url", "wss://api.bitfinex.com/ws"),
                    Config.Get("bitfinex-rest", "https://api.bitfinex.com"),
                    Config.Get("bitfinex-api-key"),
                    Config.Get("bitfinex-api-secret"),
                    algorithm.Object,
                    priceProvider.Object
                );
        }

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Crypto;

        //no stop limit support in v1
        public override TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
            new TestCaseData(new StopMarketOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopMarketOrder"),
        };

        /// <summary>
        /// Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice => 1000m;

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice => 100m;

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var tick = ((BitfinexBrokerage)this.Brokerage).GetTick(symbol);
            return tick.AskPrice;
        }

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync() => false;

        /// <summary>
        /// Gets the default order quantity
        /// </summary>
        protected override decimal GetDefaultQuantity() => 0.04m;
    }
}
