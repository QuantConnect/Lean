/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Securities.IndexOption;

namespace QuantConnect.Tests.Common.Securities.IndexOption
{
    [TestFixture]
    public class IndexOptionSymbolPropertiesTests
    {
        [TestCase("XSP", 2.99, 0.01)]
        [TestCase("XSP", 3, 0.01)]
        [TestCase("XSP", 3.01, 0.01)]
        [TestCase("SPX", 2.99, 0.05)]
        [TestCase("SPX", 3, 0.10)]
        [TestCase("SPX", 3.01, 0.10)]
        public void MinimumPriceVariationForPrice(string ticker, decimal referencePrice, decimal expected)
        {
            var underlying = Symbol.Create(ticker, SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(underlying, ticker, Market.USA, OptionStyle.European,
                OptionRight.Call, 100m, new DateTime(2026, 1, 16));

            Assert.AreEqual(ticker, option.ID.Symbol);
            Assert.AreEqual(ticker, option.Underlying.ID.Symbol);
            Assert.AreEqual(expected, IndexOptionSymbolProperties.MinimumPriceVariationForPrice(option, referencePrice));
        }

        [Test]
        public void XspMinimumPriceVariationWithoutReferencePrice()
        {
            var underlying = Symbol.Create("XSP", SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(underlying, "XSP", Market.USA, OptionStyle.European,
                OptionRight.Call, 100m, new DateTime(2026, 1, 16));

            Assert.AreEqual(0.01m, IndexOptionSymbolProperties.MinimumPriceVariationForPrice(option, null));
        }
    }
}
