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
using QuantConnect.Tests.Brokerages;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    class KrakenBrokerageModelTests
    {
        private readonly KrakenBrokerageModel _krakenBrokerageModel = new KrakenBrokerageModel();

        [TestCase(0.01, true)]
        [TestCase(0.00009, false)]
        public void CanSubmitOrder_WhenQuantityIsLargeEnough(decimal orderQuantity, bool isValidOrderQuantity)
        {
            BrokerageMessageEvent message;
            var order = new Mock<Order>();
            order.Setup(x => x.Quantity).Returns(orderQuantity);

            Assert.AreEqual(isValidOrderQuantity, _krakenBrokerageModel.CanSubmitOrder(TestsHelpers.GetSecurity(market: Market.Kraken), order.Object, out message));
        }
    }
}
