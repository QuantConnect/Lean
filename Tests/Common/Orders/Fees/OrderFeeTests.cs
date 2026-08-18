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

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class OrderFeeTests
    {
        [Test]
        public void FlatAmountAndCurrencyShortcuts()
        {
            var fee = new OrderFee(new CashAmount(12.34m, Currencies.EUR));

            Assert.AreEqual(12.34m, fee.Amount);
            Assert.AreEqual(Currencies.EUR, fee.Currency);
        }

        [Test]
        public void ArithmeticOperatorsDelegateToTheFeeAmount()
        {
            var fee = new OrderFee(new CashAmount(2m, Currencies.USD));
            var otherFee = new OrderFee(new CashAmount(3m, Currencies.USD));

            Assert.AreEqual(5m, fee + otherFee);
            Assert.AreEqual(3m, fee + 1m);
            Assert.AreEqual(3m, 1m + fee);

            Assert.AreEqual(-1m, fee - otherFee);
            Assert.AreEqual(1m, fee - 1m);
            Assert.AreEqual(8m, 10m - fee);

            Assert.AreEqual(6m, fee * otherFee);
            Assert.AreEqual(4m, fee * 2m);
            Assert.AreEqual(4m, 2m * fee);

            Assert.AreEqual(1.5m, otherFee / fee);
            Assert.AreEqual(1m, fee / 2m);
            Assert.AreEqual(5m, 10m / fee);
        }

        [Test]
        public void ComparisonOperatorsDelegateToTheFeeAmount()
        {
            var fee = new OrderFee(new CashAmount(2m, Currencies.USD));
            var otherFee = new OrderFee(new CashAmount(3m, Currencies.USD));

            Assert.IsTrue(fee < otherFee);
            Assert.IsFalse(fee > otherFee);
            Assert.IsTrue(fee <= otherFee);
            Assert.IsFalse(fee >= otherFee);

            Assert.IsTrue(fee > 0m);
            Assert.IsFalse(fee < 2m);
            Assert.IsTrue(fee <= 2m);
            Assert.IsTrue(fee >= 2m);
        }

        [Test]
        public void SerializationShapeIsUnchangedByTheShortcutProperties()
        {
            var fee = new OrderFee(new CashAmount(12.34m, Currencies.EUR));

            var json = JsonConvert.SerializeObject(fee);
            var jObject = JObject.Parse(json);

            // the flat Amount/Currency shortcuts are json-ignored, only 'Value' is serialized
            CollectionAssert.AreEqual(new[] { "Value" }, jObject.Properties().Select(property => property.Name));

            var deserialized = JsonConvert.DeserializeObject<OrderFee>(json);
            Assert.AreEqual(fee.Value.Amount, deserialized.Value.Amount);
            Assert.AreEqual(fee.Value.Currency, deserialized.Value.Currency);
        }
    }
}
