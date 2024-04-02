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
using QuantConnect.Orders.Serialization;
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
            var json = JsonConvert.SerializeObject(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero),
                new OrderEventJsonConverter("id"));

            Assert.IsFalse(json.Contains("Message", StringComparison.InvariantCulture));
            Assert.IsFalse(json.Contains("LimitPrice", StringComparison.InvariantCulture));
            Assert.IsFalse(json.Contains("StopPrice", StringComparison.InvariantCulture));
            Assert.IsFalse(json.Contains(value: "IsInTheMoney", StringComparison.InvariantCulture));

            json = JsonConvert.SerializeObject(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "This is a message")
            {
                LimitPrice = 1,
                StopPrice = 2,
                IsInTheMoney = true
            });

            Assert.IsTrue(json.Contains("Message", StringComparison.InvariantCulture));
            Assert.IsTrue(json.Contains("This is a message", StringComparison.InvariantCulture));
            Assert.IsTrue(json.Contains("LimitPrice", StringComparison.InvariantCulture));
            Assert.IsTrue(json.Contains("StopPrice", StringComparison.InvariantCulture));
            Assert.IsTrue(json.Contains(value: "IsInTheMoney", StringComparison.InvariantCulture));
        }

        [Test]
        public void RoundTripSerialization([Values] bool isInTheMoney)
        {
            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
            {
                Message = "Pepe",
                Status = OrderStatus.PartiallyFilled,
                StopPrice = 1,
                LimitPrice = 2,
                FillPrice = 11,
                FillQuantity = 12,
                FillPriceCurrency = "USD",
                Id = 55,
                Quantity = 16,
                IsInTheMoney = isInTheMoney
            };

            var converter = new OrderEventJsonConverter("id");
            var serializeObject = JsonConvert.SerializeObject(orderEvent, converter);

            // OrderFee zero uses null currency and should be ignored when serializing
            Assert.IsFalse(serializeObject.Contains(Currencies.NullCurrency, StringComparison.InvariantCulture));
            Assert.IsFalse(serializeObject.Contains("order-fee-amount", StringComparison.InvariantCulture));
            Assert.IsFalse(serializeObject.Contains("order-fee-currency", StringComparison.InvariantCulture));
            Assert.AreEqual(isInTheMoney, serializeObject.Contains("isInTheMoney", StringComparison.InvariantCulture));

            var deserializeObject = JsonConvert.DeserializeObject<OrderEvent>(serializeObject, converter);

            Assert.AreEqual(orderEvent.Symbol, deserializeObject.Symbol);
            Assert.AreEqual(orderEvent.StopPrice, deserializeObject.StopPrice);
            // there is a small loss of precision because we use double
            Assert.AreEqual(orderEvent.UtcTime.Ticks, deserializeObject.UtcTime.Ticks, 200);
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
            Assert.AreEqual(orderEvent.IsInTheMoney, deserializeObject.IsInTheMoney);
        }

        [Test]
        public void NonNullOrderFee()
        {
            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, new OrderFee(new CashAmount(88, Currencies.USD)));

            var converter = new OrderEventJsonConverter("id");
            var serializeObject = JsonConvert.SerializeObject(orderEvent, converter);
            var deserializeObject = JsonConvert.DeserializeObject<OrderEvent>(serializeObject, converter);

            Assert.IsFalse(serializeObject.Contains("order-fee-amount", StringComparison.InvariantCulture));
            Assert.IsFalse(serializeObject.Contains("order-fee-currency", StringComparison.InvariantCulture));

            Assert.IsTrue(serializeObject.Contains("orderFeeAmount", StringComparison.InvariantCulture));
            Assert.IsTrue(serializeObject.Contains("orderFeeCurrency", StringComparison.InvariantCulture));

            Assert.AreEqual(orderEvent.OrderFee.Value.Amount, deserializeObject.OrderFee.Value.Amount);
            Assert.AreEqual(orderEvent.OrderFee.Value.Currency, deserializeObject.OrderFee.Value.Currency);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ToString_AppendsIsAssignment_ForOptionSymbols(bool isAssignment)
        {
            var fill = new OrderEvent(1, Symbols.SPY_C_192_Feb19_2016, DateTime.Today, OrderStatus.New, OrderDirection.Buy, 1, 2, OrderFee.Zero, "message")
            {
                IsAssignment = isAssignment
            };
            StringAssert.EndsWith($"IsAssignment: {isAssignment}", fill.ToString());
        }

        [Test]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Future)]
        public void ToString_DoesNotIncludeIsAssignment_ForNonOptionsSymbols(SecurityType type)
        {
            var symbol = Symbols.GetBySecurityType(type);
            var fill = new OrderEvent(1, symbol, DateTime.Today, OrderStatus.New, OrderDirection.Buy, 1, 2, OrderFee.Zero, "message");
            StringAssert.DoesNotContain("IsAssignment", fill.ToString());
        }

        [Test]
        public void BackwardsCompatibleDeserialization()
        {
            var serializeObject = "{\"id\":\"id-0-0\",\"algorithm-id\":\"id\",\"order-id\":0,\"order-event-id\":0,\"symbol\":\"BTCUSD 2XR\",\"time\":1711565119.684036,\"status\":\"none\"," +
                "\"order-fee-amount\":88.0,\"order-fee-currency\":\"USD\",\"fill-price\":0.0,\"fill-price-currency\":\"\",\"fill-quantity\":0.0,\"direction\":\"buy\",\"is-assignment\":false," +
                "\"quantity\":0.0}";

            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, new OrderFee(new CashAmount(88, Currencies.USD)));

            var converter = new OrderEventJsonConverter("id");
            var deserializeObject = JsonConvert.DeserializeObject<OrderEvent>(serializeObject, converter);

            // has the old version
            Assert.IsTrue(serializeObject.Contains("order-fee-amount", StringComparison.InvariantCulture));
            Assert.IsTrue(serializeObject.Contains("order-fee-currency", StringComparison.InvariantCulture));
            Assert.IsFalse(serializeObject.Contains("orderFeeAmount", StringComparison.InvariantCulture));
            Assert.IsFalse(serializeObject.Contains("orderFeeCurrency", StringComparison.InvariantCulture));

            Assert.AreEqual(orderEvent.OrderFee.Value.Amount, deserializeObject.OrderFee.Value.Amount);
            Assert.AreEqual(orderEvent.OrderFee.Value.Currency, deserializeObject.OrderFee.Value.Currency);
        }
    }
}
