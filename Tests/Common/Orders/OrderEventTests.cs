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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderEventTests
    {
        [Test]
        public void JsonIgnores()
        {
            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var json = JsonConvert.SerializeObject(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero));

            Assert.IsFalse(json.Contains("Message"));
            Assert.IsFalse(json.Contains("Message"));
            Assert.IsFalse(json.Contains("LimitPrice"));
            Assert.IsFalse(json.Contains("StopPrice"));

            json = JsonConvert.SerializeObject(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "This is a message")
            {
                LimitPrice = 1,
                StopPrice = 2
            });

            Assert.IsTrue(json.Contains("Message"));
            Assert.IsTrue(json.Contains("This is a message"));
            Assert.IsTrue(json.Contains("LimitPrice"));
            Assert.IsTrue(json.Contains("StopPrice"));
        }

        [Test]
        public void RoundTripSerialization()
        {
            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
            {
                OrderFee = new OrderFee(new CashAmount(99, "EUR")),
                Message = "Pepe",
                Status = OrderStatus.PartiallyFilled,
                StopPrice = 1,
                LimitPrice = 2,
                FillPrice = 11,
                FillQuantity = 12,
                FillPriceCurrency = "USD"
            };

            var serializeObject = JsonConvert.SerializeObject(orderEvent);
            var deserializeObject = JsonConvert.DeserializeObject<OrderEvent>(serializeObject);

            Assert.AreEqual(orderEvent.Symbol, deserializeObject.Symbol);
            Assert.AreEqual(orderEvent.StopPrice, deserializeObject.StopPrice);
            Assert.AreEqual(orderEvent.UtcTime, deserializeObject.UtcTime);
            Assert.AreEqual(orderEvent.OrderId, deserializeObject.OrderId);
            Assert.AreEqual(orderEvent.AbsoluteFillQuantity, deserializeObject.AbsoluteFillQuantity);
            Assert.AreEqual(orderEvent.Direction, deserializeObject.Direction);
            Assert.AreEqual(orderEvent.FillPrice, deserializeObject.FillPrice);
            Assert.AreEqual(orderEvent.FillPriceCurrency, deserializeObject.FillPriceCurrency);
            Assert.AreEqual(orderEvent.FillQuantity, deserializeObject.FillQuantity);
            Assert.AreEqual(orderEvent.Id, deserializeObject.Id);
            Assert.AreEqual(orderEvent.IsAssignment, deserializeObject.IsAssignment);
            Assert.AreEqual(orderEvent.LimitPrice, deserializeObject.LimitPrice);
            Assert.AreEqual(orderEvent.Message, deserializeObject.Message);
            Assert.AreEqual(orderEvent.Quantity, deserializeObject.Quantity);
            Assert.AreEqual(orderEvent.Status, deserializeObject.Status);
            Assert.AreEqual(orderEvent.OrderFee.Value.Amount, deserializeObject.OrderFee.Value.Amount);
            Assert.AreEqual(orderEvent.OrderFee.Value.Currency, deserializeObject.OrderFee.Value.Currency);
        }
    }
}
