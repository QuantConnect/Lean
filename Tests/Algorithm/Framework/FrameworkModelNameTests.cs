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

using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class FrameworkModelNameTests
    {
        private static IEnumerable<TestCaseData> FrameworkComponents
        {
            get
            {
                yield return new TestCaseData(new ExecutionModel(), "ExecutionModel");
                yield return new TestCaseData(new PortfolioConstructionModel(), "PortfolioConstructionModel");
                yield return new TestCaseData(new RiskManagementModel(), "RiskManagementModel");
                yield return new TestCaseData(new UniverseSelectionModel(), "UniverseSelectionModel");
                yield return new TestCaseData(new AlphaStreamsPortfolioConstructionModel(), "AlphaStreamsPortfolioConstructionModel");
            }
        }

        private static IEnumerable<TestCaseData> PortfolioOptimizers
        {
            get
            {
                yield return new TestCaseData(
                    new MaximumSharpeRatioPortfolioOptimizer(-2.5, 3.5, 0.125),
                    "MaximumSharpeRatioPortfolioOptimizer(-2.5,3.5,0.125)");
                yield return new TestCaseData(
                    new MinimumVariancePortfolioOptimizer(-2.5, 3.5, 0.125),
                    "MinimumVariancePortfolioOptimizer(-2.5,3.5,0.125)");
                yield return new TestCaseData(
                    new RiskParityPortfolioOptimizer(0.05, 2.5),
                    "RiskParityPortfolioOptimizer(0.05,2.5)");
                yield return new TestCaseData(
                    new UnconstrainedMeanVariancePortfolioOptimizer(),
                    "UnconstrainedMeanVariancePortfolioOptimizer");
            }
        }

        [TestCaseSource(nameof(FrameworkComponents))]
        [TestCaseSource(nameof(PortfolioOptimizers))]
        public void FrameworkComponentProvidesExpectedName(object component, string expectedName)
        {
            Assert.IsInstanceOf<INamedModel>(component);
            Assert.AreEqual(expectedName, ((INamedModel)component).Name);
        }
    }
}
