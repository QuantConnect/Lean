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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Tests.Brokerages;
using System;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class BinanceBrokerageModelTests
    {
        private readonly BinanceBrokerageModel _binanceBrokerageModel = new();
        private readonly Symbol _btceur = Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance);

        [TestCase(0.01, true)]
        [TestCase(0.000009, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();

            order.Object.Quantity = orderQuantity;

            var security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");
            security.Cache.AddData(new Tick
            {
                AskPrice = 50001,
                BidPrice = 49999,
                Time = DateTime.UtcNow,
                Symbol = _btceur,
                TickType = TickType.Quote,
                AskSize = 1,
                BidSize = 1
            });

            Assert.AreEqual(isValidOrderQuantity, _binanceBrokerageModel.CanSubmitOrder(security, order.Object, out message));
            Assert.AreEqual(isValidOrderQuantity, message == null);
        }

        [Test]
        public void CannotSubmitOrder_IfPriceNotInitialized()
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();

            order.Object.Quantity = 1;

            var security = TestsHelpers.GetSecurity(symbol: _btceur.Value, market: _btceur.ID.Market, quoteCurrency: "EUR");

            Assert.AreEqual(false, _binanceBrokerageModel.CanSubmitOrder(security, order.Object, out message));
            Assert.NotNull(message);
        }
    }
}
