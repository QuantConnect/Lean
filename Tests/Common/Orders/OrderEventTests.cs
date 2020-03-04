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
    }
}
