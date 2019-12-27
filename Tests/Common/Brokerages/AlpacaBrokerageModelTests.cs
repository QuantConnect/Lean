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
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class AlpacaBrokerageModelTests
    {
        [TestCaseSource(nameof(GetOrderTestData))]
        public void ValidatesOrders(OrderType orderType, Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, decimal existingPosition, decimal existingOrderQuantity, bool isValid)
        {
            var algorithm = new QCAlgorithm();
            var mock = new Mock<ITransactionHandler>();
            var request = new SubmitOrderRequest(orderType, symbol.SecurityType, symbol, quantity, stopPrice, limitPrice, DateTime.UtcNow, "");
            mock.Setup(m => m.Process(It.IsAny<OrderRequest>())).Returns(new OrderTicket(null, request));

            var existingOrders = new List<Order>();
            if (existingOrderQuantity != 0)
            {
                existingOrders.Add(new LimitOrder(symbol, existingOrderQuantity, 1, DateTime.UtcNow));
            }

            mock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(existingOrders);
            algorithm.Transactions.SetOrderProcessor(mock.Object);

            var security = CreateSecurity(symbol);
            security.SetMarketPrice(new Tick { Value = 320m });

            if (existingPosition != 0)
            {
                security.Holdings.SetHoldings(1, existingPosition);
            }

            var order = Order.CreateOrder(request);

            var model = new AlpacaBrokerageModel(algorithm);

            BrokerageMessageEvent messageEvent;
            Assert.AreEqual(isValid, model.CanSubmitOrder(security, order, out messageEvent));
        }

        public TestCaseData[] GetOrderTestData()
        {
            return new[]
            {
                // valid security type
                new TestCaseData(OrderType.Market, Symbols.SPY, 1m, 0m, 0m, 0m, 0m, true),

                // invalid security type
                new TestCaseData(OrderType.Market, Symbols.EURUSD, 1m, 0m, 0m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.DE30EUR, 1m, 0m, 0m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.BTCUSD, 1m, 0m, 0m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.Fut_SPY_Feb19_2016, 1m, 0m, 0m, 0m, 0m, false),
                new TestCaseData(OrderType.Market, Symbols.SPY_C_192_Feb19_2016, 1m, 0m, 0m, 0m, 0m, false),

                // invalid order type
                new TestCaseData(OrderType.MarketOnClose, Symbols.SPY, 1m, 0m, 0m, 0m, 0m, false),

                // invalid reverse from long to short (with no open orders)
                new TestCaseData(OrderType.Market, Symbols.SPY, -2m, 0m, 0m, 1m, 0m, false),

                // invalid reverse from short to long (with no open orders)
                new TestCaseData(OrderType.Market, Symbols.SPY, 2m, 0m, 0m, -1m, 0m, false),

                // cannot open a short sell while a long buy order is open
                new TestCaseData(OrderType.Market, Symbols.SPY, -1m, 0m, 0m, 0m, 1m, false),

                // cannot open a long buy while a short sell order is open
                new TestCaseData(OrderType.Market, Symbols.SPY, 1m, 0m, 0m, 0m, -1m, false),

                // cannot submit sell order with long position and open sell order
                new TestCaseData(OrderType.Market, Symbols.SPY, -1m, 0m, 0m, 1m, -1m, false),

                // cannot submit buy order with short position and open buy order
                new TestCaseData(OrderType.Market, Symbols.SPY, 1m, 0m, 0m, -1m, 1m, false)
            };
        }

        private Security CreateSecurity(Symbol symbol)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
