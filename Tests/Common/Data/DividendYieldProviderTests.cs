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

using System.IO;
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DividendYieldProviderTests
    {
        [TestCase("20230101,0.987,1,130", 0.0, 0.01317)]
        [TestCase("20200101,0.942,0.25,410", 0.0, 0.06157)]
        [TestCase("20200101,0.942,0.25,410", 0.05, 0.01157)]
        public void TryParse(string csvLine, decimal nextPayouts, double expectedDividendYieldRateValue)
        {
            if (!DividendYieldProvider.TryParse(csvLine, nextPayouts, out var _, out var DividendYieldRate))
            {
                Assert.Fail("Could not convert the line into dividend data");
            }

            Assert.AreEqual(expectedDividendYieldRateValue, (double)DividendYieldRate, 0.0001);
        }

        [TestCase("equity/usa/factor_files/aapl.csv", true)]
        [TestCase("non-existing.csv", false)]
        public void FromCsvFile(string dir, bool getResults)
        {
            var filePath = Path.Combine(Globals.DataFolder, dir);
            var result = DividendYieldProvider.FromCsvFile(filePath);

            if (getResults)
            {
                Assert.GreaterOrEqual(result.Count, 5);
            }
            else
            {
                Assert.IsEmpty(result);
            }
        }

        [TestCase("19700306", 0.0)]   // Date in before the first date in file
        [TestCase("20200205", 0.04147)]
        [TestCase("20200206", 0.03355)]
        [TestCase("20200207", 0.03355)]
        [TestCase("20210203", 0.01676)]
        [TestCase("20210204", 0.01239)]
        [TestCase("20210205", 0.01239)]
        [TestCase("20491231", 0.01239)]   // Date in far future, assuming same rate
        public void GetDividendYieldRate(string dateString, double expected)
        {
            var symbol = Symbols.AAPL;
            var provider = new DividendYieldProvider(symbol);
            var dateTime = Parse.DateTimeExact(dateString, "yyyyMMdd");
            var result = provider.GetDividendYield(dateTime);

            Assert.AreEqual(expected, (double)result, 0.0001d);
        }
    }
}
