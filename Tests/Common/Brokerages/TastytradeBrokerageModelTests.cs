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

using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class TastytradeBrokerageModelTests
    {
        private readonly TastytradeBrokerageModel _brokerageModel = new();

        [TestCase(OrderType.ComboLimit, -1, -2, true)]
        [TestCase(OrderType.ComboLimit, 1, -2, false)]
        [TestCase(OrderType.ComboMarket, 1, 1, false, Description = "The API Tastytrade does not support ComboMarket.")]
        public void CanSubmitComboCrossZeroOrder(OrderType orderType, decimal holdingQuantity, decimal orderQuantity, bool isShouldSubmitOrder)
        {
            var AAPL = Symbols.AAPL;

            var groupOrderManager = new GroupOrderManager(1, 2, quantity: 8);

            var order = TestsHelpers.CreateNewOrderByOrderType(orderType, AAPL, orderQuantity, groupOrderManager);

            var security = TestsHelpers.InitializeSecurity(AAPL.SecurityType, (AAPL, 209m, holdingQuantity))[AAPL];

            var isPossibleSubmit = _brokerageModel.CanSubmitOrder(security, order, out _);

            Assert.That(isPossibleSubmit, Is.EqualTo(isShouldSubmitOrder));
        }
    }
}
