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
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Tradier
{
    [TestFixture, Ignore("This test requires a configured and active Tradier account")]
    public class TradierBrokerageTests : BrokerageTests
    {
        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static TestCaseData[] OrderParameters()
        {
            return new[]
            {
                new TestCaseData(new MarketOrderTestParameters(Symbols.AAPL)).SetName("MarketOrder"),
                new TestCaseData(new LimitOrderTestParameters(Symbols.AAPL, 1000m, 0.01m)).SetName("LimitOrder"),
                new TestCaseData(new StopMarketOrderTestParameters(Symbols.AAPL, 1000m, 0.01m)).SetName("StopMarketOrder"),
                new TestCaseData(new StopLimitOrderTestParameters(Symbols.AAPL, 1000m, 0.01m)).SetName("StopLimitOrder")
            };
        }

        /// <summary>
        /// Creates the brokerage under test
        /// </summary>
        /// <returns>A connected brokerage instance</returns>
        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var accountID = TradierBrokerageFactory.Configuration.AccountID;
            var tradier = new TradierBrokerage(orderProvider, securityProvider, new AggregationManager(), accountID);

            var qcUserID = TradierBrokerageFactory.Configuration.QuantConnectUserID;
            var tokens = TradierBrokerageFactory.GetTokens();
            tradier.SetTokens(qcUserID, tokens.AccessToken, tokens.RefreshToken, tokens.IssuedAt, TimeSpan.FromSeconds(tokens.ExpiresIn));

            // keep the tokens up to date in the event of a refresh
            tradier.SessionRefreshed += (sender, args) =>
            {
                File.WriteAllText(TradierBrokerageFactory.TokensFile, JsonConvert.SerializeObject(args, Formatting.Indented));
            };

            return tradier;
        }

        /// <summary>
        /// Gets the symbol to be traded, must be shortable
        /// </summary>
        protected override Symbol Symbol => Symbols.AAPL;

        /// <summary>
        /// Gets the security type associated with the <see cref="BrokerageTests.Symbol"/>
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Equity;

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        /// <summary>
        /// Gets the current market price of the specified security
        /// </summary>
        protected override decimal GetAskPrice(Symbol symbol)
        {
            var tradier = (TradierBrokerage) Brokerage;
            var quotes = tradier.GetQuotes(new List<string> {symbol.Value});
            return quotes.Single().Ask;
        }

        [Test, TestCaseSource("OrderParameters")]
        public void AllowsOneActiveOrderPerSymbol(OrderTestParameters parameters)
        {
            // tradier's api gets special with zero holdings crossing in that they need to fill the order
            // before the next can be submitted, so we just limit this impl to only having on active order
            // by symbol at a time, new orders will issue cancel commands for the existing order

            bool orderFilledOrCanceled = false;
            var order = parameters.CreateLongOrder(1);
            EventHandler<OrderEvent> brokerageOnOrderStatusChanged = (sender, args) =>
            {
                // we expect all orders to be cancelled except for market orders, they may fill before the next order is submitted
                if (args.OrderId == order.Id && args.Status == OrderStatus.Canceled || (order is MarketOrder && args.Status == OrderStatus.Filled))
                {
                    orderFilledOrCanceled = true;
                }
            };

            Brokerage.OrderStatusChanged += brokerageOnOrderStatusChanged;

            // starting from zero initiate two long orders and see that the first is canceled
            PlaceOrderWaitForStatus(order, OrderStatus.Submitted);
            PlaceOrderWaitForStatus(parameters.CreateLongMarketOrder(1));

            Brokerage.OrderStatusChanged -= brokerageOnOrderStatusChanged;

            Assert.IsTrue(orderFilledOrCanceled);
        }

        [Test, Ignore("This test exists to manually verify how rejected orders are handled when we don't receive an order ID back from Tradier.")]
        public void ShortZnga()
        {
            PlaceOrderWaitForStatus(new MarketOrder(Symbols.ZNGA, -1, DateTime.Now), OrderStatus.Invalid, allowFailedSubmission: true);

            // wait for output to be generated
            Thread.Sleep(20*1000);
        }

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
