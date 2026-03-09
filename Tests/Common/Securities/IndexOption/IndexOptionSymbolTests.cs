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
using QuantConnect.Securities.IndexOption;

namespace QuantConnect.Tests.Common.Securities.IndexOption
{
    [TestFixture]
    public class IndexOptionSymbolTests
    {
        [TestCase(1, false, "SPXW")]
        [TestCase(20, true, "SPXW")]

        [TestCase(1, false, "NQX")]
        [TestCase(20, true, "NQX")]

        [TestCase(1, true, "VIX")]
        [TestCase(20, true, "VIX")]
        public void IsStandard(int expirationDate, bool isStandard, string optionTicker)
        {
            var symbol = Symbol.Create(IndexOptionSymbol.MapToUnderlying(optionTicker), SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(symbol, optionTicker, Market.USA, OptionStyle.European,
                OptionRight.Call, 3700, new DateTime(2023, 1, expirationDate));

            Assert.AreEqual(isStandard, IndexOptionSymbol.IsStandard(option));
        }
    }
}
