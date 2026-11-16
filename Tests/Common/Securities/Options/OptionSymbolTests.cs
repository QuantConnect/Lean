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
using QuantConnect.Securities;
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

            // VIX monthly options are AM-settled and expire on a Wednesday
            var vixWednesday = Symbol.CreateOption(Symbol.Create("VIX", SecurityType.Index, Market.USA), "VIX", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2016, 02, 17));
            yield return new TestCaseData(vixWednesday, new DateTime(2016, 02, 17, 8, 30, 0));

            // VIX weekly options are AM-settled too
            var vixwWednesday = Symbol.CreateOption(Symbol.Create("VIX", SecurityType.Index, Market.USA), "VIXW", Market.USA, OptionStyle.European,
                OptionRight.Call, 20m, new DateTime(2016, 02, 10));
            yield return new TestCaseData(vixwWednesday, new DateTime(2016, 02, 10, 8, 30, 0));

            // Before Feb 2015 SPX monthly options expired on the Saturday after the 3rd Friday, settling on the Friday open
            var spxLegacySaturday = Symbol.CreateOption(Symbols.SPX, "SPX", Market.USA, OptionStyle.European,
                OptionRight.Call, 200m, new DateTime(2014, 06, 21));
            yield return new TestCaseData(spxLegacySaturday, new DateTime(2014, 06, 20, 8, 30, 0));

            // Tickers not known to be AM-settled, such as XSP, keep the previous result: market open only on the 3rd Friday.
            // XSP is actually PM-settled; supporting it is #7769
            var xsp = Symbol.Create("XSP", SecurityType.Index, Market.USA);
            var xspWednesday = Symbol.CreateOption(xsp, "XSP", Market.USA, OptionStyle.European,
                OptionRight.Call, 500m, new DateTime(2024, 02, 14));
            yield return new TestCaseData(xspWednesday, new DateTime(2024, 02, 14, 15, 20, 0));

            var xspThirdFriday = Symbol.CreateOption(xsp, "XSP", Market.USA, OptionStyle.European,
                OptionRight.Call, 500m, new DateTime(2024, 02, 16));
            yield return new TestCaseData(xspThirdFriday, new DateTime(2024, 02, 16, 8, 30, 0));
        }

        [TestCaseSource(nameof(ExpirationDateTimeTestCases))]
        public void CalculatesSettlementDateTime(Symbol symbol, DateTime expectedSettlementDateTime)
        {
            var settlementDateTime = OptionSymbol.GetSettlementDateTime(symbol);
            Assert.AreEqual(expectedSettlementDateTime, settlementDateTime);
        }

        [TestCase("SPXW", 2024, 1, 5, 15)]
        [TestCase("RUTW", 2024, 1, 5, 15)]
        [TestCase("NDXP", 2024, 1, 5, 16)]
        [TestCase("NQX", 2024, 1, 5, 16)]
        [TestCase("SPXW", 2025, 7, 7, 15)]
        [TestCase("NDXP", 2025, 7, 7, 16)]
        [TestCase("NQX", 2025, 7, 7, 16)]
        public void ZeroDTEPMSettledIndexOptionsExpireAt4PMEastern(string ticker, int year, int month, int day, int exchangeLocalHour)
        {
            var expiry = new DateTime(year, month, day);
            var underlying = Symbol.Create(IndexOptionSymbol.MapToUnderlying(ticker), SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(underlying, ticker, Market.USA, OptionStyle.European, OptionRight.Call, 200m, expiry);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, option, SecurityType.IndexOption);

            var settlement = OptionSymbol.GetSettlementDateTime(option);

            Assert.AreEqual(expiry.Date.AddHours(exchangeLocalHour), settlement);
            Assert.AreEqual(expiry.Date.AddHours(16), settlement.ConvertTo(exchangeHours.TimeZone, TimeZones.NewYork));
        }

        [TestCase("SPXW", 2025, 7, 3, 12)]
        [TestCase("SPXW", 2025, 11, 28, 12)]
        [TestCase("SPXW", 2025, 12, 24, 12)]
        [TestCase("RUTW", 2025, 7, 3, 12)]
        [TestCase("NDXP", 2025, 11, 28, 13)]
        public void ZeroDTEPMSettledIndexOptionsExpireAtEarlyClose(string ticker, int year, int month, int day, int earlyCloseHour)
        {
            var expiry = new DateTime(year, month, day);
            var underlying = Symbol.Create(IndexOptionSymbol.MapToUnderlying(ticker), SecurityType.Index, Market.USA);
            var option = Symbol.CreateOption(underlying, ticker, Market.USA, OptionStyle.European, OptionRight.Call, 200m, expiry);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, option, SecurityType.IndexOption);

            var settlement = OptionSymbol.GetSettlementDateTime(option);

            Assert.AreEqual(expiry.Date.AddHours(earlyCloseHour), settlement);
            Assert.AreEqual(exchangeHours.GetLastDailyMarketClose(expiry, false), settlement);
        }

        // AM-settled: SPX, NDX, RUT, VIX, VIXW -> settle at market open on expiry day
        // PM-settled: SPXW, RUTW, NDXP, NQX -> always settle at market close
        [TestCase("SPX", true)]
        [TestCase("NDX", true)]
        [TestCase("RUT", true)]
        [TestCase("VIX", true)]
        [TestCase("SPXW", false)]
        [TestCase("RUTW", false)]
        [TestCase("VIXW", true)]
        [TestCase("NDXP", false)]
        [TestCase("NQX", false)]
        public void IsAMSettledClassifiesAllIndexOptionTickers(string ticker, bool expectedAMSettled)
        {
            var option = Symbol.CreateOption(Symbols.SPX, ticker, Market.USA, OptionStyle.European, OptionRight.Call, 200m, new DateTime(2016, 02, 19));
            Assert.AreEqual(expectedAMSettled, IndexOptionSymbol.IsAMSettled(option));
        }
    }
}
