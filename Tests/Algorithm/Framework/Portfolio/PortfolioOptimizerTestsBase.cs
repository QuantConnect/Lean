/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by aaplicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio;

public abstract class PortfolioOptimizerTestsBase
{
    protected IList<double[,]> HistoricalReturns { get; set; }

    protected IList<double[]> ExpectedReturns { get; set; }

    protected IList<double[,]> Covariances { get; set; }

    protected IList<double[]> ExpectedResults { get; set; }

    protected abstract IPortfolioOptimizer CreateOptimizer();

    public virtual void OptimizeWeightings(int testCaseNumber)
    {
        var testOptimizer = CreateOptimizer();

        var result = testOptimizer.Optimize(
            HistoricalReturns[testCaseNumber],
            ExpectedReturns[testCaseNumber],
            Covariances[testCaseNumber]);

        Assert.AreEqual(ExpectedResults[testCaseNumber], result.Select(x => Math.Round(x, 6)));
    }

    [Test]
    public virtual void EmptyPortfolioReturnsEmptyArrayOfDouble()
    {
        var testOptimizer = CreateOptimizer();
        var historicalReturns = new double[,] { { } };
        var expectedResult = Array.Empty<double>();

        var result = testOptimizer.Optimize(historicalReturns);

        Assert.AreEqual(result, expectedResult);
    }
}
