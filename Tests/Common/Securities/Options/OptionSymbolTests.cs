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
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionSymbolTests
    {
        [Test]
        public void IsOptionContractExpiredReturnsFalseForNonOptionSymbol()
        {
            Assert.IsFalse(OptionSymbol.IsOptionContractExpired(Symbols.SPY, DateTime.UtcNow));
        }

        [Test]
        public void IsOptionContractExpiredReturnsTrueIfExpiredContract()
        {
            var symbol = Symbol.CreateOption(
                "BHP",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                55m,
                new DateTime(2019, 9, 20));

            Assert.IsTrue(OptionSymbol.IsOptionContractExpired(symbol, DateTime.UtcNow));
        }

        [Test]
        public void IsOptionContractExpiredReturnsFalseIfActiveContract()
        {
            var symbol = Symbol.CreateOption(
                "BHP",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                55m,
                new DateTime(2019, 9, 20));

            Assert.IsFalse(OptionSymbol.IsOptionContractExpired(symbol, new DateTime(2019, 1, 1)));
        }
    }
}
