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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders.Fees;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Moq;
using NodaTime;
using QuantConnect.Data;
using System.Reflection;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture()]
    public class BitfinexFeeModelTests
    {

        BitfinexFeeModel unit;
        Symbol symbol;
        SubscriptionDataConfig sub;
        decimal price = 100.00m;
        Mock<Security> security;

        [SetUp]
        public void Setup()
        {
            unit = new BitfinexFeeModel();
            symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);

            //hack: no parameterless constructors
            sub = (SubscriptionDataConfig)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SubscriptionDataConfig));
            security = new Mock<Security>(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc), sub, new Cash("USD", 1, 1), new SymbolProperties("", "USD", 1, 1, 1));
            security.Setup(s => s.Price).Returns(price);
            security.Setup(s => s.AskPrice).Returns(price + 2);
            security.Setup(s => s.BidPrice).Returns(price - 2);
        }

        [Test()]
        public void GetTakerOrderFeeTest()
        {
            var order = new Mock<MarketOrder>();
            order.Setup(o => o.Type).Returns(OrderType.Market);
            order.Object.Quantity = 10;

            decimal expected = 2m;
            var actual = unit.GetOrderFee(security.Object, order.Object);

            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void GetMakerOrderFeeTest()
        {
            var order = new Mock<LimitOrder>();
            order.Setup(o => o.Type).Returns(OrderType.Limit);
            order.Object.Quantity = 10;
            order.Object.LimitPrice = price + 1;

            decimal expected = 1m;
            var actual = unit.GetOrderFee(security.Object, order.Object);

            Assert.AreEqual(expected, actual);

            order.Object.LimitPrice = price - 1;
            order.Object.Quantity = -10;

            expected = 1m;
            actual = unit.GetOrderFee(security.Object, order.Object);

            Assert.AreEqual(expected, actual);

        }

    }
}
