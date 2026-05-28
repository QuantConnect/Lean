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
using QuantConnect.Securities.IndexOption;
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
            yield return new TestCaseData(pmSettledIndexOption, new DateTime(2016, 02, 12, 15, 0, 0));

            var amSettledIndexOption = Symbol.CreateOption(Symbols.SPX, "SPX", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 18));
            yield return new TestCaseData(amSettledIndexOption, new DateTime(2016, 02, 18, 8, 30, 0));

            // 3rd Friday cases: SPX is AM-settled, SPXW is PM-settled even on the same date
            var spxThirdFriday = Symbol.CreateOption(Symbols.SPX, "SPX", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 19));
            yield return new TestCaseData(spxThirdFriday, new DateTime(2016, 02, 19, 8, 30, 0));

            var spxwThirdFriday = Symbol.CreateOption(Symbols.SPX, "SPXW", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 19));
            yield return new TestCaseData(spxwThirdFriday, new DateTime(2016, 02, 19, 15, 0, 0));
        }

        [TestCaseSource(nameof(ExpirationDateTimeTestCases))]
        public void CalculatesSettlementDateTime(Symbol symbol, DateTime expectedSettlementDateTime)
        {
            var settlementDateTime = OptionSymbol.GetSettlementDateTime(symbol);
            Assert.AreEqual(expectedSettlementDateTime, settlementDateTime);
        }

        [TestCase("SPXW")]
        [TestCase("RUTW")]
        [TestCase("VIXW")]
        [TestCase("NDXP")]
        [TestCase("NQX")]
        public void ZeroDTEPMSettledIndexOptionsExpireAt4PM(string ticker)
        {
            var expiry = new DateTime(2024, 1, 5); // regular Friday
            var underlying = Symbol.Create(IndexOptionSymbol.MapToUnderlying(ticker), SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(underlying, ticker, Market.USA, OptionStyle.European, OptionRight.Call, 200m, expiry);

            var settlement = OptionSymbol.GetSettlementDateTime(option);

            Assert.AreEqual(expiry.Date.AddHours(15), settlement);
        }

        // AM-settled: SPX, NDX, RUT, VIX -> settle at market open on expiry day
        // PM-settled: SPXW, RUTW, VIXW, NDXP, NQX -> always settle at market close
        [TestCase("SPX", true)]
        [TestCase("NDX", true)]
        [TestCase("RUT", true)]
        [TestCase("VIX", true)]
        [TestCase("SPXW", false)]
        [TestCase("RUTW", false)]
        [TestCase("VIXW", false)]
        [TestCase("NDXP", false)]
        [TestCase("NQX", false)]
        public void IsAMSettledClassifiesAllIndexOptionTickers(string ticker, bool expectedAMSettled)
        {
            var option = Symbol.CreateOption(Symbols.SPX, ticker, Market.USA, OptionStyle.European, OptionRight.Call, 200m, new DateTime(2016, 02, 19));
            Assert.AreEqual(expectedAMSettled, IndexOptionSymbol.IsAMSettled(option));
        }
    }
}
