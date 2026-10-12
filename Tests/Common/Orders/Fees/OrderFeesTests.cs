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

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class OrderFeesTests
    {
        [Test]
        public void OrderFeeZeroCanBeConvertedToAccountCurrency()
        {
            var book = new CashBook();

            var result = book.ConvertToAccountCurrency(OrderFee.Zero.Value);

            Assert.AreEqual(0, result.Amount);
            Assert.AreEqual(book.AccountCurrency, result.Currency);

            var result2 = book.ConvertToAccountCurrency(OrderFee.Zero.Value.Amount,
                OrderFee.Zero.Value.Currency);

            Assert.AreEqual(0, result2);

            var result3 = book.Convert(OrderFee.Zero.Value.Amount,
                OrderFee.Zero.Value.Currency,
                book.AccountCurrency);

            Assert.AreEqual(0, result3);
        }

        [Test]
        public void AmountIsASynonymOfValueAmount()
        {
            var orderFee = new OrderFee(new CashAmount(12.5m, Currencies.USD));

            Assert.AreEqual(12.5m, orderFee.Amount);
            Assert.AreEqual(orderFee.Value.Amount, orderFee.Amount);
            // The synonym is not serialized, only Value is
            Assert.AreEqual("{\"Value\":{\"Amount\":12.5,\"Currency\":\"USD\"}}", JsonConvert.SerializeObject(orderFee));
        }

        [Test]
        public void AmountIsAccessibleFromPython()
        {
            var orderFee = new OrderFee(new CashAmount(12.5m, Currencies.USD));

            using (Py.GIL())
            {
                var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def get_amount(order_fee: OrderFee):
    return order_fee.amount
");

                dynamic getAmount = module.GetAttr("get_amount");
                Assert.AreEqual(12.5m, (decimal)getAmount(orderFee));
            }
        }

        [Test]
        public void OrderFeeIsNumericFromPython()
        {
            var fee = new OrderFee(new CashAmount(2m, Currencies.USD));
            var order = new MarketOrder(Symbols.BTCUSD, 0.123m, DateTime.UtcNow);
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, new OrderFee(new CashAmount(3m, Currencies.USD)));

            using (Py.GIL())
            {
                var module = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def check(fee: OrderFee, order_event: OrderEvent):
    fees = [fee, order_event.order_fee]
    return sum(fees), fee + order_event.order_fee, fee > 0, fee < order_event.order_fee, float(order_event.order_fee), max(fees).value.amount
");

                dynamic check = module.GetAttr("check");
                var result = check(fee, orderEvent);

                Assert.AreEqual(5.0, (double)result[0]);
                Assert.AreEqual(5.0, (double)result[1]);
                Assert.IsTrue((bool)result[2]);
                Assert.IsTrue((bool)result[3]);
                Assert.AreEqual(3.0, (double)result[4]);
                Assert.AreEqual(3m, (decimal)result[5]);
            }
        }
    }
}
