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
using QuantConnect.Algorithm;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Tests.Common.Orders.TimeInForces
{
    [TestFixture]
    public class TimeInForceHandlerTests
    {
        [Test]
        public void GtcTimeInForceOrderDoesNotExpire()
        {
            var handler = new GoodTilCancelledTimeInForceHandler();

            var order = new LimitOrder(Symbols.SPY, 10, 100, DateTime.UtcNow);

            Assert.IsFalse(handler.HasOrderExpired(order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(order, fill2));
        }

        [Test]
        public void DayTimeInForceOrderExpiresAtMarketClose()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var algorithm = new QCAlgorithm
            {
                DefaultOrderProperties = { TimeInForce = TimeInForce.Day }
            };
            var handler = new DayTimeInForceHandler(algorithm);

            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", algorithm.DefaultOrderProperties);

            algorithm.SetDateTime(utcTime);

            Assert.IsFalse(handler.HasOrderExpired(order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(order, fill2));

            algorithm.SetDateTime(utcTime.AddHours(6));
            Assert.IsTrue(handler.HasOrderExpired(order));

            Assert.IsTrue(handler.IsFillValid(order, fill1));
            Assert.IsTrue(handler.IsFillValid(order, fill2));
        }


    }
}
