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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class TerminalLinkBrokerageModelTests
    {
        private readonly TerminalLinkBrokerageModel _brokerageModel = new();

        [TestCase("SPY", SecurityType.Equity)]
        [TestCase("SPY", SecurityType.Option)]
        [TestCase("ES", SecurityType.Future)]
        public void CanSubmitOrder_ForSupportedSecurityTypes(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2024, 1, 2));

            Assert.IsTrue(_brokerageModel.CanSubmitOrder(security, order, out var message), message?.Message);
            Assert.IsNull(message);
        }

        // Index is data-only on TerminalLink; Forex/Crypto/Cfd and option chains other than
        // equity options (IndexOption/FutureOption) are not supported for trading.
        [TestCase("EURUSD", SecurityType.Forex)]
        [TestCase("BTCUSD", SecurityType.Crypto)]
        [TestCase("DE10YBEUR", SecurityType.Cfd)]
        [TestCase("SPX", SecurityType.Index)]
        public void CannotSubmitOrder_ForUnsupportedSecurityTypes(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2024, 1, 2));

            Assert.IsFalse(_brokerageModel.CanSubmitOrder(security, order, out var message));
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
        }

        [TestCase(OrderType.Market)]
        [TestCase(OrderType.MarketOnOpen)]
        [TestCase(OrderType.Limit)]
        [TestCase(OrderType.StopMarket)]
        [TestCase(OrderType.StopLimit)]
        public void CanSubmitOrder_ForSupportedOrderTypes(OrderType orderType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(SecurityType.Equity, "SPY");
            var order = CreateOrder(orderType, security.Symbol, 1);

            Assert.IsTrue(_brokerageModel.CanSubmitOrder(security, order, out var message), message?.Message);
            Assert.IsNull(message);
        }

        [TestCase(OrderType.MarketOnClose)]
        [TestCase(OrderType.LimitIfTouched)]
        [TestCase(OrderType.TrailingStop)]
        public void CannotSubmitOrder_ForUnsupportedOrderTypes(OrderType orderType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(SecurityType.Equity, "SPY");
            var order = CreateOrder(orderType, security.Symbol, 1);

            Assert.IsFalse(_brokerageModel.CanSubmitOrder(security, order, out var message));
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
        }

        [Test]
        public void CannotUpdateOrder()
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(SecurityType.Equity, "SPY");
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2024, 1, 2));
            var request = new UpdateOrderRequest(new DateTime(2024, 1, 2), order.Id, new UpdateOrderFields());

            Assert.IsFalse(_brokerageModel.CanUpdateOrder(security, order, request, out var message));
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
        }

        private static Order CreateOrder(OrderType orderType, Symbol symbol, decimal quantity)
        {
            var time = new DateTime(2024, 1, 2);
            return orderType switch
            {
                OrderType.Market => new MarketOrder(symbol, quantity, time),
                OrderType.MarketOnOpen => new MarketOnOpenOrder(symbol, quantity, time),
                OrderType.MarketOnClose => new MarketOnCloseOrder(symbol, quantity, time),
                OrderType.Limit => new LimitOrder(symbol, quantity, 100m, time),
                OrderType.StopMarket => new StopMarketOrder(symbol, quantity, 100m, time),
                OrderType.StopLimit => new StopLimitOrder(symbol, quantity, 100m, 100m, time),
                OrderType.LimitIfTouched => new LimitIfTouchedOrder(symbol, quantity, 100m, 100m, time),
                OrderType.TrailingStop => new TrailingStopOrder(symbol, quantity, 100m, 0.1m, true, time),
                _ => throw new NotImplementedException($"Unhandled order type: {orderType}")
            };
        }
    }
}
