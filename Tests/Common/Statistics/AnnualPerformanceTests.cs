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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class AnnualPerformanceTests
    {
        [Test]
        public void ZeroTradingDays()
        {
            var performance = new List<double> { 0.1, 0.2, 0.1, 0.2 };

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, 0);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void FullYearPerformance()
        {
            // Ensure mean is 1
            var performance = Enumerable.Repeat(0.5, 176).ToList();
            performance.AddRange(Enumerable.Repeat(1.5, 176).ToList());

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance, 4);

            Assert.AreEqual(15.0, result);
        }

        [Test]
        public void AllZeros()
        {
            var performance = Enumerable.Repeat(0.0, 252).ToList();

            var result = QuantConnect.Statistics.Statistics.AnnualPerformance(performance);

            Assert.AreEqual(0.0, result);
        }
    }
}
