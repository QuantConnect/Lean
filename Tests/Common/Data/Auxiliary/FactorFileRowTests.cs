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

using System;
using NUnit.Framework;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class FactorFileRowTests
    {
        [Test]
        public void ToCsv()
        {
            var row = new FactorFileRow(new DateTime(2000, 01, 01), 1m, 2m, 123m);
            var actual = row.ToCsv("source");
            var expected = "20000101,1,2,123,source";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AppliesDividendWithPreviousTradingDateEqualToRowDate()
        {
            var row = new FactorFileRow(new DateTime(2018, 08, 23), 1m, 2m, 123m);
            var dividend = new Dividend(Symbols.SPY, row.Date.AddDays(1), 1m, 123m);
            var updated = row.Apply(dividend, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            Assert.AreEqual("20180823,0.9918699,2,123", updated.ToCsv());
        }

        [Test]
        public void AppliesSplitWithPreviousTradingDateEqualToRowDate()
        {
            var row = new FactorFileRow(new DateTime(2018, 08, 23), 1m, 2m, 123m);
            var dividend = new Split(Symbols.SPY, row.Date.AddDays(1), 123m, 2m, SplitType.SplitOccurred);
            var updated = row.Apply(dividend, SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            Assert.AreEqual("20180823,1,4,123", updated.ToCsv());
        }
    }
}
