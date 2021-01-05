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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
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
            Assert.AreEqual(Currencies.USD, cash.Symbol);
            Assert.AreEqual(0, cash.Amount);
            Assert.AreEqual(1m, cash.ConversionRate);
        }

        [Test]
        public void ComputesValueInAccountCurrency()
        {
            var book = new CashBook();
            book[Currencies.USD].SetAmount(1000);
            book.Add("JPY", 1000, 1/100m);
            book.Add("GBP", 1000, 2m);

            decimal expected = book[Currencies.USD].ValueInAccountCurrency + book["JPY"].ValueInAccountCurrency + book["GBP"].ValueInAccountCurrency;
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
            var actual = book.Convert(1200, book.AccountCurrency, "EUR");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ConvertsToJpyFromAccountCurrencyProperly()
        {
            var book = new CashBook();
            book.Add("JPY", 0, 1/100m);

            var expected = 100000m;
            var actual = book.Convert(1000, book.AccountCurrency, "JPY");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WontAddNullCurrencyCash()
        {
            var book = new CashBook {{Currencies.NullCurrency, 1, 1}};
            Assert.AreEqual(1, book.Count);
            var cash = book.Single().Value;
            Assert.AreEqual(Currencies.USD, cash.Symbol);

            book.Add(Currencies.NullCurrency, 1, 1);
            Assert.AreEqual(1, book.Count);
            cash = book.Single().Value;
            Assert.AreEqual(Currencies.USD, cash.Symbol);

            book.Add(Currencies.NullCurrency,
                new Cash(Currencies.NullCurrency, 1, 1));
            Assert.AreEqual(1, book.Count);
            cash = book.Single().Value;
            Assert.AreEqual(Currencies.USD, cash.Symbol);

            book[Currencies.NullCurrency] =
                new Cash(Currencies.NullCurrency, 1, 1);
            Assert.AreEqual(1, book.Count);
            cash = book.Single().Value;
            Assert.AreEqual(Currencies.USD, cash.Symbol);
        }

        [Test]
        public void WillThrowIfGetNullCurrency()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var symbol = new CashBook()[Currencies.NullCurrency].Symbol;
            });
        }

        [Test]
        public void UpdatedAddedCalledOnlyForNewSymbols()
        {
            var cashBook = new CashBook();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Added)
                {
                    called = true;
                }
            };
            cashBook.Add(cash.Symbol, cash);

            Assert.IsFalse(called);
        }

        [Test]
        public void UpdateEventCalledForCashUpdates()
        {
            var cashBook = new CashBook();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Updated)
                {
                    called = true;
                }
            };
            cash.Update(new Tick { Value = 10 });

            Assert.IsTrue(called);
        }

        [Test]
        public void UpdateEventCalledForAddMethod()
        {
            var cashBook = new CashBook();
            // we remove default USD cash
            cashBook.Clear();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Added)
                {
                    called = true;
                }
            };
            cashBook.Add(cash.Symbol, cash);

            Assert.IsTrue(called);
        }

        [Test]
        public void UpdateEventCalledForAdd()
        {
            var cashBook = new CashBook();
            // we remove default USD cash
            cashBook.Clear();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Added)
                {
                    called = true;
                }
            };

            cashBook[cash.Symbol] = cash;

            Assert.IsTrue(called);
        }

        [Test]
        public void UpdateEventCalledForRemove()
        {
            var cashBook = new CashBook();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Removed)
                {
                    called = true;
                }
            };

            cashBook.Remove(Currencies.USD);

            Assert.IsTrue(called);
        }

        [Test]
        public void UpdateEventNotCalledForCashUpdatesAfterRemoved()
        {
            var cashBook = new CashBook();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Remove(Currencies.USD);
            cashBook.Updated += (sender, args) =>
            {
                called = true;
            };
            cash.Update(new Tick { Value = 10 });

            Assert.IsFalse(called);
        }

        [Test]
        public void UpdateEventNotCalledForCashUpdatesAfterSteppedOn()
        {
            var cashBook = new CashBook();
            var called = false;
            var updatedCalled = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            var cash2 = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Add(cash.Symbol, cash2);

            cashBook.Updated += (sender, updateType) =>
            {
                called = true;
                updatedCalled = updateType == CashBook.UpdateType.Updated;
            };
            cash.Update(new Tick { Value = 10 });
            Assert.IsFalse(called);

            cash2.Update(new Tick { Value = 10 });
            Assert.IsTrue(updatedCalled);
        }

        [Test]
        public void UpdateEventCalledForAddExistingValueCalledOnce()
        {
            var cashBook = new CashBook();
            var called = 0;
            var calledUpdated = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Updated += (sender, updateType) =>
            {
                called++;
                calledUpdated = updateType == CashBook.UpdateType.Updated;
            };

            cashBook.Add(cash.Symbol, cash);

            Assert.AreEqual(1, called);
            Assert.IsTrue(calledUpdated);
        }

        [Test]
        public void UpdateEventCalledForClear()
        {
            var cashBook = new CashBook();
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cashBook.Add(cash.Symbol, cash);
            cashBook.Updated += (sender, updateType) =>
            {
                called = updateType == CashBook.UpdateType.Removed;
            };

            cashBook.Clear();

            Assert.IsTrue(called);
        }
    }
}
