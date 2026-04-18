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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class CapacityEstimateTests
    {
        [Test]
        public void UpdateMarketCapacitySkipsSymbolsWithoutMarketCapacityData()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetStartDate(2020, 1, 1);
            algorithm.SetEndDate(2020, 1, 10);

            var security = algorithm.AddEquity("SPY", Resolution.Minute);
            security.BuyingPowerModel = new ThrowingBuyingPowerModel();

            var utcTime = new DateTime(2020, 1, 2, 15, 0, 0, DateTimeKind.Utc);
            algorithm.SetDateTime(utcTime);
            algorithm.SetCurrentSlice(new Slice(utcTime, Enumerable.Empty<BaseData>(), utcTime));

            var capacityEstimate = new CapacityEstimate(algorithm);
            capacityEstimate.OnOrderEvent(new OrderEvent(
                orderId: 1,
                symbol: security.Symbol,
                utcTime: utcTime,
                status: OrderStatus.Filled,
                direction: OrderDirection.Buy,
                fillPrice: 100m,
                fillQuantity: 1m,
                orderFee: OrderFee.Zero));

            Assert.DoesNotThrow(() => capacityEstimate.UpdateMarketCapacity(forceProcess: true));
        }

        private class ThrowingBuyingPowerModel : SecurityMarginModel
        {
            public ThrowingBuyingPowerModel() : base(1m)
            {
            }

            public override ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters)
            {
                throw new InvalidOperationException("Reserved buying power should not be queried for symbols without market capacity data.");
            }
        }
    }
}
