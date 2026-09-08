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
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class FundamentalTests
    {
        [SetUp]
        public void Setup()
        {
            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(), false);
        }

        [Test]
        public void ComputesMarketCapCorrectly()
        {
            var fine = new QuantConnect.Data.Fundamental.Fundamental(new DateTime(2014, 04, 01), Symbols.AAPL);

            Assert.AreEqual(541.74m, fine.Price);
            Assert.AreEqual(469400291359, fine.MarketCap);
        }

        [Test]
        public void AccessionNumberDefaultsToThreeMonths()
        {
            var fine = new QuantConnect.Data.Fundamental.Fundamental(new DateTime(2014, 04, 01), Symbols.AAPL);

            Assert.IsTrue(fine.EarningReports.AccessionNumber.HasValue);
            Assert.AreEqual("0001193125-13-416534", fine.EarningReports.AccessionNumber.Value);
            Assert.AreEqual("0001193125-13-416534", fine.EarningReports.AccessionNumber.GetPeriodValue(""));
            Assert.IsTrue(fine.FinancialStatements.AccessionNumber.HasValue);
        }

        [Test]
        public void AccessionNumberHasNoValueWhenItIsMissing()
        {
            var fine = new QuantConnect.Data.Fundamental.Fundamental(new DateTime(2014, 04, 01), Symbols.SPY);

            Assert.IsFalse(fine.EarningReports.AccessionNumber.HasValue);
            Assert.IsFalse(fine.FinancialStatements.AccessionNumber.HasValue);
        }

        [Test]
        public void ZeroMarketCapForDefaultObject()
        {
            var fine = new QuantConnect.Data.Fundamental.Fundamental();

            Assert.AreEqual(0, fine.Price);
            Assert.AreEqual(0, fine.MarketCap);
        }
    }
}
