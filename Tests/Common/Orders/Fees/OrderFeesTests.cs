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

using NUnit.Framework;
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
    }
}
