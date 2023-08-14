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
using System.IO;
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class InterestRateProviderTests
    {
        [Test]
        public void Create()
        {
            const string csvLine = "2020-01-01,2.5";
            const decimal expectedInterestRateValue = 0.025m;
            var expectedInterestRateDate = new DateTime(2020, 1, 1);

            if (!InterestRateProvider.TryParse(csvLine, out var date, out var interestRate))
            {
                Assert.Fail("Could not convert the line into interest rate data");
            }

            Assert.AreEqual(expectedInterestRateDate, date);
            Assert.AreEqual(expectedInterestRateValue, interestRate);
        }

        [TestCase("alternative/interest-rate/usa/interest-rate.csv", true)]
        [TestCase("non-existing.csv", false)]
        public void FromCsvFile(string dir, bool getResults)
        {
            var filePath = Path.Combine(Globals.DataFolder, dir);
            var result = InterestRateProvider.FromCsvFile(filePath, out _);

            if (getResults)
            {
                Assert.GreaterOrEqual(result.Count, 30);
            }
            else
            {
                Assert.IsEmpty(result);
            }
        }

        [TestCase("19700306", 0.0225)]   // Date in before the first date in file
        [TestCase("20200306", 0.0175)]
        [TestCase("20200307", 0.0175)]
        [TestCase("20200308", 0.0175)]
        [TestCase("20200310", 0.0175)]
        [TestCase("20501231", 0.055)]   // Date in far future
        public void GetInterestRate(string dateString, decimal expected)
        {
            var provider = new InterestRateProvider();
            var dateTime = Parse.DateTimeExact(dateString, "yyyyMMdd");
            var result = provider.GetInterestRate(dateTime);

            Assert.AreEqual(expected, result);
        }
    }
}
