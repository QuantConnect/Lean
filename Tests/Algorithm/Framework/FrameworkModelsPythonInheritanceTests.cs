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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Python;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class FrameworkModelsPythonInheritanceTests
    {
        [Test]
        public void ManualUniverseSelectionModelCanBeInherited()
        {
            var code = @"
from clr import AddReference
AddReference('QuantConnect.Common')

from QuantConnect import Market, SecurityType, Symbol
from Selection.ManualUniverseSelectionModel import ManualUniverseSelectionModel

class MockUniverseSelectionModel(ManualUniverseSelectionModel):
    def __init__(self):
        super().__init__([Symbol.Create('SPY', SecurityType.Equity, Market.USA)])";

            using (Py.GIL())
            {
                dynamic pyModel = PyModule.FromString(Guid.NewGuid().ToString(), code)
                    .GetAttr("MockUniverseSelectionModel");

                var model = new UniverseSelectionModelPythonWrapper(pyModel());

                var universes = model.CreateUniverses(new QCAlgorithm()).ToList();
                Assert.AreEqual(1, universes.Count);

                var universe = universes.First();
                var symbols = universe.SelectSymbols(DateTime.Now, null).ToList();
                Assert.AreEqual(1, symbols.Count);

                var expected = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                var symbol = symbols.First();
                Assert.AreEqual(expected, symbol);
            }
        }

        [Test]
        public void FundamentalUniverseSelectionModelCanBeInherited()
        {
            var code = @"
from AlgorithmImports import *
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class MockUniverseSelectionModel(FundamentalUniverseSelectionModel):
    def __init__(self):
        super().__init__(False)
    def SelectCoarse(self, algorithm, coarse):
        return [Symbol.Create('SPY', SecurityType.Equity, Market.USA)]";

            using (Py.GIL())
            {
                dynamic pyModel = PyModule.FromString(Guid.NewGuid().ToString(), code)
                    .GetAttr("MockUniverseSelectionModel");

                var model = new UniverseSelectionModelPythonWrapper(pyModel());

                var universes = model.CreateUniverses(new QCAlgorithm()).ToList();
                Assert.AreEqual(1, universes.Count);

                var data = new BaseDataCollection();
                data.Add(new CoarseFundamental());

                var universe = universes.First();
                var symbols = universe.SelectSymbols(DateTime.Now, data).ToList();
                Assert.AreEqual(1, symbols.Count);

                var expected = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                var symbol = symbols.First();
                Assert.AreEqual(expected, symbol);
            }
        }

        [Test]
        public void FundamentalUniverseSelectionModelCanBeInheritedAndOverriden()
        {
            var code = @"
from AlgorithmImports import *

class MockUniverseSelectionModel(FundamentalUniverseSelectionModel):
    def __init__(self):
        super().__init__()
        self.select_call_count = 0
        self.select_coarse_call_count = 0
        self.select_fine_call_count = 0
        self.create_coarse_call_count = 0

    def select(self, algorithm: QCAlgorithm, fundamental: list[Fundamental]) -> list[Symbol]:
        self.select_call_count += 1
        return []
    
    def select_coarse(self, algorithm, coarse):
        self.select_coarse_call_count += 1
        self.select_coarse_called = True
        
        filtered = [c for c in coarse if c.price > 10]
        return [c.symbol for c in filtered[:2]]
    
    def select_fine(self, algorithm, fine):
        self.select_fine_call_count += 1
        self.select_fine_called = True
        
        return [f.symbol for f in fine[:2]]
    
    def create_coarse_fundamental_universe(self, algorithm):
        self.create_coarse_call_count += 1
        self.create_coarse_called = True
        
        return CoarseFundamentalUniverse(
            algorithm.universe_settings, 
            self.custom_coarse_selector
        )
    
    def custom_coarse_selector(self, coarse):
        filtered = [c for c in coarse if c.has_fundamental_data]
        return [c.symbol for c in filtered[:5]]";

            using (Py.GIL())
            {
                dynamic pyModel = PyModule.FromString(Guid.NewGuid().ToString(), code)
                    .GetAttr("MockUniverseSelectionModel");

                PyObject pyModelInstance = pyModel();
                var model = new FundamentalUniverseSelectionModel();
                model.SetPythonInstance(pyModelInstance);
                var algorithm = new QCAlgorithm();

                // call the select method
                model.Select(algorithm, new List<Fundamental>());
                int selectCount = pyModelInstance.GetAttr("select_call_count").As<int>();
                Assert.Greater(selectCount, 0);

                // call the select_coarse method
                model.SelectCoarse(algorithm, new List<CoarseFundamental>());
                int selectCoarseCount = pyModelInstance.GetAttr("select_coarse_call_count").As<int>();
                Assert.Greater(selectCoarseCount, 0);

                // call the select_fine method
                model.SelectFine(algorithm, new List<FineFundamental>());
                int selectFineCount = pyModelInstance.GetAttr("select_fine_call_count").As<int>();
                Assert.Greater(selectFineCount, 0);

                // call the create_coarse_fundamental_universe method
                model.CreateCoarseFundamentalUniverse(algorithm);
                int createCoarseCount = pyModelInstance.GetAttr("create_coarse_call_count").As<int>();
                Assert.Greater(createCoarseCount, 0);
            }
        }
    }
}
