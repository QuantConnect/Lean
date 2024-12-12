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
using System.Collections.Generic;
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

        [Test]
        public void IsOptionContractExpiredReturnsFalseIfTimeOfDayDiffer()
        {
            var symbol = Symbol.CreateOption(
                "BHP",
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                55m,
                new DateTime(2022, 03, 11));

            Assert.IsFalse(OptionSymbol.IsOptionContractExpired(symbol, new DateTime(2022, 03, 11)));
        }

        private static IEnumerable<TestCaseData> ExpirationDateTimeTestCases()
        {
            var equityOption = Symbols.SPY_C_192_Feb19_2016;
            yield return new TestCaseData(equityOption, new DateTime(2016, 02, 19, 16, 0, 0));

            // Expires on a Saturday, so the expiration date time should be the Friday before
            equityOption = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2016, 02, 20));
            yield return new TestCaseData(equityOption, new DateTime(2016, 02, 19, 16, 0, 0));

            var pmSettledIndexOption = Symbol.CreateOption(Symbols.SPX, "SPXW", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 12));
            yield return new TestCaseData(pmSettledIndexOption, new DateTime(2016, 02, 12, 15, 15, 0));

            var amSettledIndexOption = Symbol.CreateOption(Symbols.SPX, "SPX", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 18));
            yield return new TestCaseData(amSettledIndexOption, new DateTime(2016, 02, 19, 8, 30, 0));
        }

        [TestCaseSource(nameof(ExpirationDateTimeTestCases))]
        public void CalculatesExpirationDateTime(Symbol symbol, DateTime expectedExpirationDateTime)
        {
            var expirationDateTime = OptionSymbol.GetExpirationDateTime(symbol);
            Assert.AreEqual(expectedExpirationDateTime, expirationDateTime);
        }
    }
}
