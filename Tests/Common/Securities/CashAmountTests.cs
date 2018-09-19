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
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, null, new CashBook()));
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, "", new CashBook()));
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, "USD", null));
            Assert.Throws<ArgumentNullException>(() => new CashAmount(100m, null, null));
        }

        [Test]
        public void InitializesProperlyUsingAccountCurrency()
        {
            var cashAmount = new CashAmount(1000m, CashBook.AccountCurrency, new CashBook());

            Assert.AreEqual(1000m, cashAmount.Amount);
            Assert.AreEqual(CashBook.AccountCurrency, cashAmount.Currency);

            Assert.AreEqual(1000m, cashAmount.ValueInAccountCurrency.Amount);
            Assert.AreEqual(CashBook.AccountCurrency, cashAmount.ValueInAccountCurrency.Currency);
        }

        [Test]
        public void InitializesProperlyUsingNonAccountCurrency()
        {
            var cashBook = new CashBook { { "EUR", new Cash("EUR", 0, 1.17m) } };
            var cashAmount = new CashAmount(1000m, "EUR", cashBook);

            Assert.AreEqual(1000m, cashAmount.Amount);
            Assert.AreEqual("EUR", cashAmount.Currency);

            Assert.AreEqual(1170m, cashAmount.ValueInAccountCurrency.Amount);
            Assert.AreEqual(CashBook.AccountCurrency, cashAmount.ValueInAccountCurrency.Currency);
        }
    }
}
