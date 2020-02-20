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
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class PortfolioConstructionModelPythonWrapperTests
    {
        [Test]
        public void PythonCompleteImplementation()
        {
            var algorithm = new AlgorithmStub();

            using (Py.GIL())
            {
                dynamic model = PythonEngine.ModuleFromString(
                    "TestPCM",
                    @"

from clr import AddReference
AddReference(""QuantConnect.Algorithm.Framework"")

from QuantConnect.Algorithm.Framework.Portfolio import *

class PyPCM(EqualWeightingPortfolioConstructionModel):
    def __init__(self):
        self.CreateTargets_WasCalled = False
        self.OnSecuritiesChanged_WasCalled = False
        self.ShouldCreateTargetForInsight_WasCalled = False
        self.IsRebalanceDue_WasCalled = False
        self.GetTargetInsights_WasCalled = False
        self.DetermineTargetPercent_WasCalled = False

    def CreateTargets(self, algorithm, insights):
        self.CreateTargets_WasCalled = True
        return super().CreateTargets(algorithm, insights)

    def OnSecuritiesChanged(self, algorithm, changes):
        self.OnSecuritiesChanged_WasCalled = True
        super().OnSecuritiesChanged(algorithm, changes)

    def ShouldCreateTargetForInsight(self, insight):
        self.ShouldCreateTargetForInsight_WasCalled = True
        return super().ShouldCreateTargetForInsight(insight)

    def IsRebalanceDue(self, insights, algorithmUtc):
        self.IsRebalanceDue_WasCalled = True
        return True

    def GetTargetInsights(self):
        self.GetTargetInsights_WasCalled = True
        return super().GetTargetInsights()

    def DetermineTargetPercent(self, activeInsights):
        self.DetermineTargetPercent_WasCalled = True
        return super().DetermineTargetPercent(activeInsights)
"
                ).GetAttr("PyPCM").Invoke();
                var now = new DateTime(2020, 1, 10);
                var wrappedModel = new PortfolioConstructionModelPythonWrapper(model);
                var aapl = algorithm.AddEquity("AAPL");
                aapl.SetMarketPrice(new Tick(now, aapl.Symbol, 10, 10));
                algorithm.SetDateTime(now);

                wrappedModel.OnSecuritiesChanged(algorithm, new SecurityChanges(SecurityChanges.Added(aapl)));
                Assert.IsTrue((bool)model.OnSecuritiesChanged_WasCalled);

                var insight = new Insight(now, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, null, null);
                var result = wrappedModel.CreateTargets(algorithm, new[] { insight }).ToList();
                Assert.AreEqual(1, result.Count);
                Assert.IsTrue((bool)model.CreateTargets_WasCalled);
                Assert.IsTrue((bool)model.GetTargetInsights_WasCalled);
                Assert.IsTrue((bool)model.IsRebalanceDue_WasCalled);
                Assert.IsTrue((bool)model.ShouldCreateTargetForInsight_WasCalled);
                Assert.IsTrue((bool)model.DetermineTargetPercent_WasCalled);
            }
        }

        [Test]
        public void PythonDoesNotImplementDetermineTargetPercent()
        {
            var algorithm = new AlgorithmStub();

            using (Py.GIL())
            {
                dynamic model = PythonEngine.ModuleFromString(
                    "TestPCM",
                    @"

from clr import AddReference
AddReference(""QuantConnect.Algorithm.Framework"")

from QuantConnect.Algorithm.Framework.Portfolio import *

class PyPCM(EqualWeightingPortfolioConstructionModel):
    def __init__(self):
        self.CreateTargets_WasCalled = False
        self.OnSecuritiesChanged_WasCalled = False
        self.ShouldCreateTargetForInsight_WasCalled = False
        self.IsRebalanceDue_WasCalled = False
        self.GetTargetInsights_WasCalled = False

    def CreateTargets(self, algorithm, insights):
        self.CreateTargets_WasCalled = True
        return super().CreateTargets(algorithm, insights)

    def OnSecuritiesChanged(self, algorithm, changes):
        self.OnSecuritiesChanged_WasCalled = True
        super().OnSecuritiesChanged(algorithm, changes)

    def ShouldCreateTargetForInsight(self, insight):
        self.ShouldCreateTargetForInsight_WasCalled = True
        return super().ShouldCreateTargetForInsight(insight)

    def IsRebalanceDue(self, insights, algorithmUtc):
        self.IsRebalanceDue_WasCalled = True
        return True

    def GetTargetInsights(self):
        self.GetTargetInsights_WasCalled = True
        return super().GetTargetInsights()
"
                ).GetAttr("PyPCM").Invoke();

                var now = new DateTime(2020, 1, 10);
                var wrappedModel = new PortfolioConstructionModelPythonWrapper(model);
                var aapl = algorithm.AddEquity("AAPL");
                aapl.SetMarketPrice(new Tick(now, aapl.Symbol, 10, 10));
                algorithm.SetDateTime(now);

                wrappedModel.OnSecuritiesChanged(algorithm, new SecurityChanges(SecurityChanges.Added(aapl)));
                Assert.IsTrue((bool)model.OnSecuritiesChanged_WasCalled);

                var insight = new Insight(now, aapl.Symbol, TimeSpan.FromDays(1), InsightType.Price, InsightDirection.Down, null, null);
                var result = wrappedModel.CreateTargets(algorithm, new[] { insight }).ToList();
                Assert.AreEqual(1, result.Count);
                Assert.IsTrue((bool)model.CreateTargets_WasCalled);
                Assert.IsTrue((bool)model.GetTargetInsights_WasCalled);
                Assert.IsTrue((bool)model.IsRebalanceDue_WasCalled);
                Assert.IsTrue((bool)model.ShouldCreateTargetForInsight_WasCalled);
            }
        }
    }
}
