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

using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class OrderCrossingBrokerageTests
    {
        [TestCase(0, 1, false)]
        [TestCase(-1, -1, false)]
        [TestCase(-1, 1, false)]
        [TestCase(-1, 2, true)]
        [TestCase(1, -2, true)]
        public void ShouldOrderCrossesZero(decimal holdingQuantity, decimal orderQuantity, bool expectedCrossResult)
        {
            var isOrderCrosses = BrokerageHelpers.OrderCrossesZero(holdingQuantity, orderQuantity);
            Assert.That(isOrderCrosses, Is.EqualTo(expectedCrossResult));
        }

        [TestCase(-1, 2, 1, 1, Description = "short to long")]
        [TestCase(1, -2, -1, -1, Description = "long to short")]
        [TestCase(-10, 20, 10, 10, Description = "long to short")]
        [TestCase(10, -20, -10, -10, Description = "long to short")]
        public void GetQuantityOnCrossPosition(decimal holdingQuantity, decimal orderQuantity, decimal expectedFirstOrderQuantity, decimal expectedSecondOrderQuantity)
        {
            if (BrokerageHelpers.OrderCrossesZero(holdingQuantity, orderQuantity))
            {
                var (firstOrderQuantity, secondOrderQuantity) = BrokerageHelpers.GetQuantityOnCrossPosition(holdingQuantity, orderQuantity);
                Assert.That(expectedFirstOrderQuantity, Is.EqualTo(firstOrderQuantity));
                Assert.That(expectedSecondOrderQuantity, Is.EqualTo(secondOrderQuantity));
            }
            else
            {
                Assert.Fail($"Order does not cross zero.Holding quantity: {holdingQuantity}, Order quantity: {orderQuantity}");
            }
        }


    }
}
