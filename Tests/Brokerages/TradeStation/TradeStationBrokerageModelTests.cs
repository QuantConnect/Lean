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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.TradeStation
{
    [TestFixture]
    public class TradeStationBrokerageModelTests
    {
        private readonly TradeStationBrokerageModel _brokerageModel = new TradeStationBrokerageModel();

        [TestCase(10, -15, -16, false)]
        [TestCase(10, -15, -15, true)]
        [TestCase(0, 1, 2, true)]
        [TestCase(1, 1, 2, true)]
        [TestCase(1, -1, -1, true)]
        [TestCase(1, -2, -2, true)]
        [TestCase(1, -2, -3, false)]
        public void CanUpdateCrossZeroOrder(decimal holdingQuantity, decimal orderQuantity, decimal newOrderQuantity, bool isShouldUpdate)
        {
            var AAPL = Symbols.AAPL;
            var marketOrder = CreateNewOrderByOrderType(OrderType.Market, AAPL, orderQuantity);
            var security = InitializeSecurity((AAPL.Value, 209m, holdingQuantity))[AAPL];
            var updateRequest = new UpdateOrderRequest(new DateTime(default), 1, new UpdateOrderFields() { Quantity = newOrderQuantity });

            var isPossibleUpdate = _brokerageModel.CanUpdateOrder(security, marketOrder, updateRequest, out var message);

            Assert.That(isPossibleUpdate, Is.EqualTo(isShouldUpdate));
        }

        [TestCase(OrderType.ComboMarket, 1, 1, 2, 0, false)]
        [TestCase(OrderType.ComboLimit, 1, 1, 2, 0, false)]
        [TestCase(OrderType.ComboLimit, 1, 1, 1, 20, true)]
        public void CanUpdateComboOrders(OrderType orderType, decimal holdingQuantity, decimal orderQuantity, decimal newOrderQuantity, decimal newLimitPrice, bool isShouldUpdate)
        {
            var AAPL = Symbols.AAPL;
            var groupManager = new GroupOrderManager(1, 2, quantity: 8);

            var order = CreateNewOrderByOrderType(orderType, AAPL, orderQuantity, groupManager);

            var security = InitializeSecurity((AAPL.Value, 209m, holdingQuantity))[AAPL];

            var updateRequest = new UpdateOrderRequest(new DateTime(default), 1, new UpdateOrderFields() { Quantity = newOrderQuantity, LimitPrice = newLimitPrice });

            var isPossibleUpdate = _brokerageModel.CanUpdateOrder(security, order, updateRequest, out var message);

            Assert.That(isPossibleUpdate, Is.EqualTo(isShouldUpdate));
        }

        [TestCase(OrderType.ComboMarket, 10, -15, false)]
        [TestCase(OrderType.ComboMarket, 0, 1, true)]
        [TestCase(OrderType.ComboMarket, 1, 2, true)]
        [TestCase(OrderType.ComboLimit, -1, -2, true)]
        [TestCase(OrderType.ComboLimit, 1, -2, false)]
        public void CanSubmitComboCrossZeroOrder(OrderType orderType, decimal holdingQuantity, decimal orderQuantity, bool isShouldSubmitOrder)
        {
            var AAPL = Symbols.AAPL;

            var groupManager = new GroupOrderManager(1, 2, quantity: 8);

            var order = CreateNewOrderByOrderType(orderType, AAPL, orderQuantity, groupManager);

            var security = InitializeSecurity((AAPL.Value, 209m, holdingQuantity))[AAPL];

            var isPossibleUpdate = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(isPossibleUpdate, Is.EqualTo(isShouldSubmitOrder));
        }

        private static SecurityManager InitializeSecurity(params (string ticker, decimal averagePrice, decimal quantity)[] equityQuantity)
        {
            var algorithm = new AlgorithmStub();
            foreach (var (symbol, averagePrice, quantity) in equityQuantity)
            {
                algorithm.AddEquity(symbol).Holdings.SetHoldings(averagePrice, quantity);
            }

            return algorithm.Securities;
        }

        private static Order CreateNewOrderByOrderType(OrderType orderType, Symbol symbol, decimal orderQuantity, GroupOrderManager groupOrderManager = null) => orderType switch
        {
            OrderType.Market => new MarketOrder(symbol, orderQuantity, new DateTime(default)),
            OrderType.ComboMarket => new ComboMarketOrder(symbol, orderQuantity, new DateTime(default), groupOrderManager),
            OrderType.ComboLimit => new ComboLimitOrder(symbol, orderQuantity, 80m, new DateTime(default), groupOrderManager),
            _ => throw new NotImplementedException()
        };

    }
}
