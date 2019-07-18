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
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashAmountTests
    {
        [Test]
        public void DoesNotInitializeWithInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, null));
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, ""));
        }

        [Test]
        public void InitializesProperlyUsingAccountCurrency()
        {
            var cashAmount = new CashAmount(1000m, Currencies.USD);

            Assert.AreEqual(1000m, cashAmount.Amount);
            Assert.AreEqual(Currencies.USD, cashAmount.Currency);
        }

        [Test]
        public void InitializesProperlyUsingNonAccountCurrency()
        {
            var cashAmount = new CashAmount(1000m, "EUR");

            Assert.AreEqual(1000m, cashAmount.Amount);
            Assert.AreEqual("EUR", cashAmount.Currency);
        }

        [Test]
        public void EqualityOperator()
        {
            var cashAmount = new CashAmount(1000m, "EUR");
            var cashAmount2 = new CashAmount(1000m, "EUR");
            var uglyDuckling = new CashAmount(10m, "EUR");
            var uglyDuckling2 = new CashAmount(10m, "USD");
            var zeroCashAmount = new CashAmount(0, "USD");

            Assert.IsTrue(cashAmount2 == cashAmount);
            Assert.IsFalse(uglyDuckling == cashAmount);
            Assert.IsFalse(uglyDuckling == cashAmount2);
            Assert.IsFalse(uglyDuckling2 == cashAmount2);
            Assert.IsFalse(uglyDuckling2 == cashAmount);
            Assert.IsFalse(uglyDuckling2 == uglyDuckling);
            Assert.IsFalse(uglyDuckling2 == default(CashAmount));
            Assert.IsFalse(zeroCashAmount == default(CashAmount));
        }

        [Test]
        public void NotEqualOperator()
        {
            var cashAmount = new CashAmount(1000m, "EUR");
            var cashAmount2 = new CashAmount(1000m, "EUR");
            var uglyDuckling = new CashAmount(10m, "EUR");
            var uglyDuckling2 = new CashAmount(10m, "USD");
            var zeroCashAmount = new CashAmount(0, "USD");

            Assert.IsFalse(cashAmount2 != cashAmount);
            Assert.IsTrue(uglyDuckling != cashAmount);
            Assert.IsTrue(uglyDuckling != cashAmount2);
            Assert.IsTrue(uglyDuckling2 != cashAmount2);
            Assert.IsTrue(uglyDuckling2 != cashAmount);
            Assert.IsTrue(uglyDuckling2 != uglyDuckling);
            Assert.IsTrue(uglyDuckling2 != default(CashAmount));
            Assert.IsTrue(zeroCashAmount != default(CashAmount));
        }

        [Test]
        public void EqualsOperator()
        {
            var cashAmount = new CashAmount(1000m, "EUR");
            var cashAmount2 = new CashAmount(1000m, "EUR");
            var uglyDuckling = new CashAmount(10m, "EUR");
            var uglyDuckling2 = new CashAmount(10m, "USD");
            var zeroCashAmount = new CashAmount(0, "USD");

            Assert.IsTrue(cashAmount2.Equals(cashAmount));
            Assert.IsFalse(uglyDuckling.Equals(cashAmount));
            Assert.IsFalse(uglyDuckling.Equals(cashAmount2));
            Assert.IsFalse(uglyDuckling2.Equals(cashAmount2));
            Assert.IsFalse(uglyDuckling2.Equals(cashAmount));
            Assert.IsFalse(uglyDuckling2.Equals(uglyDuckling));
            Assert.IsFalse(uglyDuckling2.Equals(default(CashAmount)));
            Assert.IsFalse(zeroCashAmount.Equals(default(CashAmount)));
        }

        [Test]
        public void DefaultCashAmount()
        {
            var cashAmount = default(CashAmount);

            Assert.IsNotNull(cashAmount);
            Assert.AreEqual(0, cashAmount.Amount);
            Assert.AreEqual(null, cashAmount.Currency);
        }
    }
}
