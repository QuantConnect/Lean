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
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages.GDAX;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using Moq;
using QuantConnect.Brokerages;
using QuantConnect.Logging;
using QuantConnect.Tests.Common.Securities;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture, Ignore("This test requires a configured and active account")]
    public class GDAXBrokerageIntegrationTests : BrokerageTests
    {
        #region Properties
        protected override Symbol Symbol => Symbol.Create("ETHBTC", SecurityType, Market.GDAX);

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Crypto;

        /// <summary>
        ///     Gets a high price for the specified symbol so a limit sell won't fill
        /// </summary>
        protected override decimal HighPrice => 1m;

        /// <summary>
        /// Gets a low price for the specified symbol so a limit buy won't fill
        /// </summary>
        protected override decimal LowPrice => 0.0001m;

        protected override decimal GetDefaultQuantity()
        {
            return 0.01m;
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var securities =
                new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
                {
                    { Symbol, CreateSecurity(Symbol) }
                };
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var restClient = new RestClient("https://api.pro.coinbase.com");
            var webSocketClient = new WebSocketWrapper();

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new GDAXBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));
            algorithm.Setup(a => a.Securities).Returns(securities);

            var priceProvider = new ApiPriceProvider(Config.GetInt("job-user-id"), Config.Get("api-access-token"));

            return new GDAXBrokerage(Config.Get("gdax-url", "wss://ws-feed.pro.coinbase.com"), webSocketClient, restClient,
                Config.Get("gdax-api-key"), Config.Get("gdax-api-secret"), Config.Get("gdax-passphrase"), algorithm.Object,
                priceProvider);
        }

        [Test]
        public override void GetCashBalanceContainsUsd()
        {
            Log.Trace("");
            Log.Trace("GET CASH BALANCE");
            Log.Trace("");
            var balance = Brokerage.GetCashBalance();

            // Non US users cannot have USD balance so we check any fiat currency
            Assert.AreEqual(1, balance.Count(x => x.Currency == Currencies.USD || x.Currency == "EUR" || x.Currency == "GBP"));
        }

        [Test]
        public override void GetAccountHoldings()
        {
            // For GDAX we use GetCashBalance instead of GetAccountHoldings
            // since GetAccountHoldings always returns an empty list
            Log.Trace("");
            Log.Trace("GET ACCOUNT HOLDINGS");
            Log.Trace("");

            var before = Brokerage.GetCashBalance();

            PlaceOrderWaitForStatus(new MarketOrder(Symbol, GetDefaultQuantity(), DateTime.UtcNow));

            Thread.Sleep(3000);

            var after = Brokerage.GetCashBalance();

            var beforeHoldings = before.FirstOrDefault(x => x.Currency == "ETH");
            var afterHoldings = after.FirstOrDefault(x => x.Currency == "ETH");

            var beforeQuantity = beforeHoldings.Amount;
            var afterQuantity = afterHoldings.Amount;

            Assert.AreEqual(GetDefaultQuantity(), afterQuantity - beforeQuantity);
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

        // no stop market order support
        public override TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)).SetName("MarketOrder"),
            new TestCaseData(new NonUpdateableLimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("LimitOrder"),
            new TestCaseData(new NonUpdateableStopLimitOrderTestParameters(Symbol, HighPrice, LowPrice)).SetName("StopLimitOrder")
        };
    }
}
