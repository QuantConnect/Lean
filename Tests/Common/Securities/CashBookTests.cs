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
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashBookTests
    {
        [Test]
        public void InitializesWithAccountCurrencyAdded()
        {
            var book = new CashBook();
            Assert.AreEqual(1, book.Count);
            var cash = book.Single().Value;
            Assert.AreEqual(CashBook.AccountCurrency, cash.Symbol);
            Assert.AreEqual(0, cash.Amount);
            Assert.AreEqual(1m, cash.ConversionRate);
        }

        [Test]
        public void ComputesValueInAccountCurrency()
        {
            var book = new CashBook();
            book["USD"].SetAmount(1000);
            book.Add("JPY", 1000, 1/100m);
            book.Add("GBP", 1000, 2m);

            decimal expected = book["USD"].ValueInAccountCurrency + book["JPY"].ValueInAccountCurrency + book["GBP"].ValueInAccountCurrency;
            Assert.AreEqual(expected, book.TotalValueInAccountCurrency);
        }

        [Test]
        public void ConvertsProperly()
        {
            var book = new CashBook();
            book.Add("EUR", 0, 1.10m);
            book.Add("GBP", 0, 0.71m);

            var expected = 1549.2957746478873239436619718m;
            var actual = book.Convert(1000, "EUR", "GBP");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ConvertsToAccountCurrencyProperly()
        {
            var book = new CashBook();
            book.Add("EUR", 0, 1.10m);

            var expected = 1100m;
            var actual = book.ConvertToAccountCurrency(1000, "EUR");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ConvertsToEurFromAccountCurrencyProperly()
        {
            var book = new CashBook();
            book.Add("EUR", 0, 1.20m);

            var expected = 1000m;
            var actual = book.Convert(1200, CashBook.AccountCurrency, "EUR");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ConvertsToJpyFromAccountCurrencyProperly()
        {
            var book = new CashBook();
            book.Add("JPY", 0, 1/100m);

            var expected = 100000m;
            var actual = book.Convert(1000, CashBook.AccountCurrency, "JPY");
            Assert.AreEqual(expected, actual);
        }
    }
}
