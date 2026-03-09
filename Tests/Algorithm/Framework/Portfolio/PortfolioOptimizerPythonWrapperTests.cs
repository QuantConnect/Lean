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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Portfolio;
using System;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio;

[TestFixture]
public class PortfolioOptimizerPythonWrapperTests
{
    [Test]
    public void OptimizeIsCalled()
    {
        using (Py.GIL())
        {
            var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    @$"
from AlgorithmImports import *

class CustomPortfolioOptimizer:
    def __init__(self):
        self.OptimizeWasCalled = False

    def Optimize(self, historicalReturns, expectedReturns = None, covariance = None):
        self.OptimizeWasCalled= True");
                
            var pyCustomOptimizer = module.GetAttr("CustomPortfolioOptimizer").Invoke();
            var wrapper = new PortfolioOptimizerPythonWrapper(pyCustomOptimizer);
            var historicalReturns = new double[,] { { -0.50, -0.13 }, { 0.81, 0.31 }, { -0.02, 0.01 } };

            wrapper.Optimize(historicalReturns);
            pyCustomOptimizer
                .GetAttr("OptimizeWasCalled")
                .TryConvert(out bool optimizerWasCalled);

            Assert.IsTrue(optimizerWasCalled);
        }
    }

    [Test]
    public void WrapperThrowsIfOptimizerDoesNotImplementInterface()
    {
        using (Py.GIL())
        {
            var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    @$"
from AlgorithmImports import *

class CustomPortfolioOptimizer:
    def __init__(self):
        self.OptimizeWasCalled = False

    def Calculate(self, historicalReturns, expectedReturns = None, covariance = None):
        pass");

            var pyCustomOptimizer = module.GetAttr("CustomPortfolioOptimizer").Invoke();

            Assert.Throws<NotImplementedException>(() => new PortfolioOptimizerPythonWrapper(pyCustomOptimizer));
        }
    }
}
