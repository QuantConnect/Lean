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

        [TestCase("AAPL", 10, -15, -16, false)]
        [TestCase("AAPL", 10, -15, -15, true)]
        [TestCase("AAPL", 0, 1, 2, true)]
        [TestCase("AAPL", 1, 1, 2, true)]
        [TestCase("AAPL", 1, -1, -1, true)]
        [TestCase("AAPL", 1, -2, -2, true)]
        [TestCase("AAPL", 1, -2, -3, false)]
        public void CanUpdateCrossZeroOrder(string ticker, decimal holdingQuantity, decimal orderQuantity, decimal newOrderQuantity, bool isShouldUpdate)
        {
            var AAPL = Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            var marketOrder = new MarketOrder(AAPL, orderQuantity, new DateTime(default));
            var security = InitializeSecurity((AAPL.Value, 209m, holdingQuantity))[AAPL];
            var updateRequest = new UpdateOrderRequest(new DateTime(default), 1, new UpdateOrderFields() { Quantity = newOrderQuantity });

            var isPossibleUpdate = _brokerageModel.CanUpdateOrder(security, marketOrder, updateRequest, out var message);

            Assert.That(isPossibleUpdate, Is.EqualTo(isShouldUpdate));
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
    }
}
