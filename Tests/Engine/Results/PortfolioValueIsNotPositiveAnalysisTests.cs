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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class PortfolioValueIsNotPositiveAnalysisTests
    {
        [Test]
        public void ReturnsNoFindingsWhenStatisticsAreWithheld()
        {
            // The result handler withholds the statistics when they are not meaningful yet,
            // like while the algorithm warms up and no equity has been sampled
            var findings = new PortfolioValueIsNotPositiveAnalysis().Run(new BacktestResult());

            Assert.IsEmpty(findings);
        }

        [TestCase(0)]
        [TestCase(-100000)]
        public void FlagsNonPositiveEndingEquity(int endEquity)
        {
            var findings = new PortfolioValueIsNotPositiveAnalysis().Run(MakeResult(endEquity));

            var finding = findings.Single();
            Assert.AreEqual(nameof(PortfolioValueIsNotPositiveAnalysis), finding.Name);
            Assert.IsNotEmpty(finding.Solutions);
        }

        [Test]
        public void ReturnsNoActionableFindingWhenEndingEquityIsPositive()
        {
            var findings = new PortfolioValueIsNotPositiveAnalysis().Run(MakeResult(100000));

            Assert.IsEmpty(findings.Single().Solutions);
        }

        private static BacktestResult MakeResult(decimal endEquity)
        {
            return new BacktestResult
            {
                TotalPerformance = new AlgorithmPerformance
                {
                    PortfolioStatistics = new PortfolioStatistics { EndEquity = endEquity }
                }
            };
        }
    }
}
