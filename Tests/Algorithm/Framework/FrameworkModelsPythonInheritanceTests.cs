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
using QuantConnect.Data.UniverseSelection;
using System;
using System.Linq;

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
                dynamic pyModel = PythonEngine
                    .ModuleFromString(Guid.NewGuid().ToString(), code)
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
from clr import AddReference
AddReference('QuantConnect.Common')

from QuantConnect import Market, SecurityType, Symbol
from Selection.FundamentalUniverseSelectionModel import FundamentalUniverseSelectionModel

class MockUniverseSelectionModel(FundamentalUniverseSelectionModel):
    def __init__(self):
        super().__init__(False)
    def SelectCoarse(self, algorithm, coarse):
        return [Symbol.Create('SPY', SecurityType.Equity, Market.USA)]";

            using (Py.GIL())
            {
                dynamic pyModel = PythonEngine
                    .ModuleFromString(Guid.NewGuid().ToString(), code)
                    .GetAttr("MockUniverseSelectionModel");

                var model = new UniverseSelectionModelPythonWrapper(pyModel());

                var universes = model.CreateUniverses(new QCAlgorithm()).ToList();
                Assert.AreEqual(1, universes.Count);

                var data = new BaseDataCollection();
                data.Data.Add(new CoarseFundamental());

                var universe = universes.First();
                var symbols = universe.SelectSymbols(DateTime.Now, data).ToList();
                Assert.AreEqual(1, symbols.Count);

                var expected = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                var symbol = symbols.First();
                Assert.AreEqual(expected, symbol);
            }
        }
    }
}