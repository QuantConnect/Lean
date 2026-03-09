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
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;
using Moq;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class ETFConstituentsUniverseSelectionModelTests
    {
        [TestCase("from Selection.ETFConstituentsUniverseSelectionModel import *", "Selection.ETFConstituentsUniverseSelectionModel.ETFConstituentsUniverseSelectionModel")]
        [TestCase("from QuantConnect.Algorithm.Framework.Selection import *", "QuantConnect.Algorithm.Framework.Selection.ETFConstituentsUniverseSelectionModel")]
        public void TestPythonAndCSharpImports(string importStatement, string expected)
        {
            using (Py.GIL())
            {
                dynamic module = PyModule.FromString("testModule",
                    @$"{importStatement}
class ETFConstituentsFrameworkAlgorithm(QCAlgorithm):
    def initialize(self):
        self.universe_settings.resolution = Resolution.DAILY
        symbol = Symbol.create('SPY', SecurityType.EQUITY, Market.USA)
        selection_model = ETFConstituentsUniverseSelectionModel(symbol, self.universe_settings, self.etf_constituents_filter)
        self.universe_type = str(type(selection_model))

    def etf_constituents_filter(self, constituents):
        return [c.symbol for c in constituents]");

                dynamic algorithm = module.GetAttr("ETFConstituentsFrameworkAlgorithm").Invoke();
                algorithm.initialize();
                string universeTypeStr = algorithm.universe_type.ToString();
                Assert.IsTrue(universeTypeStr.Contains(expected, StringComparison.InvariantCulture));
            }
        }


        [TestCase("'SPY'")]
        [TestCase("'SPY', None")]
        [TestCase("'SPY', None, None")]
        [TestCase("'SPY', self.universe_settings")]
        [TestCase("'SPY', self.universe_settings, None")]
        [TestCase("'SPY', None, self.etf_constituents_filter")]
        [TestCase("'SPY', self.universe_settings, self.etf_constituents_filter")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA)")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), None, None")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), self.universe_settings")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), self.universe_settings, None")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), None, self.etf_constituents_filter")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), self.universe_settings, self.etf_constituents_filter")]
        [TestCase("Symbol.create('SPY', SecurityType.EQUITY, Market.USA), universe_filter_func=self.etf_constituents_filter")]
        public void ETFConstituentsUniverseSelectionModelWithVariousConstructor(string constructorParameters)
        {
            using (Py.GIL())
            {
                dynamic module = PyModule.FromString("testModule",
                    @$"from AlgorithmImports import *
from Selection.ETFConstituentsUniverseSelectionModel import *
class ETFConstituentsFrameworkAlgorithm(QCAlgorithm):
    selection_model = None
    def initialize(self):
        self.universe_settings.resolution = Resolution.DAILY
        self.selection_model = ETFConstituentsUniverseSelectionModel({constructorParameters})

    def etf_constituents_filter(self, constituents):
        return [c.symbol for c in constituents]");

                dynamic algorithm = module.GetAttr("ETFConstituentsFrameworkAlgorithm").Invoke();
                algorithm.initialize();
                Assert.IsNotNull(algorithm.selection_model);
                Assert.IsTrue(algorithm.selection_model.etf_symbol.GetPythonType().ToString().Contains($"{nameof(Symbol)}", StringComparison.InvariantCulture));
                Assert.IsTrue(algorithm.selection_model.etf_symbol.ToString().Contains(Symbols.SPY, StringComparison.InvariantCulture));
            }
        }

        [TestCase("TSLA")]
        public void ETFConstituentsUniverseSelectionModelGetNoCachedSymbol(string ticker)
        {
            using (Py.GIL())
            {
                var etfAlgorithm = GetETFConstituentsFrameworkAlgorithm(ticker);
                etfAlgorithm.initialize();

                Assert.IsNotNull(etfAlgorithm.selection_model);
                Assert.IsTrue(etfAlgorithm.selection_model.etf_symbol.GetPythonType().ToString().Contains($"{nameof(Symbol)}", StringComparison.InvariantCulture));

                var etfSymbol = (Symbol)etfAlgorithm.selection_model.etf_symbol;

                Assert.IsTrue(etfSymbol.Value.Contains(ticker, StringComparison.InvariantCulture));
                Assert.IsTrue(etfSymbol.SecurityType == SecurityType.Equity);
            }
        }

        [TestCase("SPY", "CACHED")]
        public void ETFConstituentsUniverseSelectionModelGetCachedSymbol(string ticker, string expectedAlias)
        {
            using (Py.GIL())
            {
                var etfAlgorithm = GetETFConstituentsFrameworkAlgorithm(ticker);
                etfAlgorithm.initialize();

                Assert.IsNotNull(etfAlgorithm.selection_model);
                Assert.IsTrue(etfAlgorithm.selection_model.etf_symbol.GetPythonType().ToString().Contains($"{nameof(Symbol)}", StringComparison.InvariantCulture));

                var etfSymbol = (Symbol)etfAlgorithm.selection_model.etf_symbol;

                Assert.IsTrue(etfSymbol.Value.Contains(expectedAlias, StringComparison.InvariantCulture));
                Assert.IsTrue(etfSymbol.ID == Symbols.SPY.ID);
                Assert.IsTrue(etfSymbol.SecurityType == SecurityType.Equity);
            }
        }


        [Test]
        public void ETFConstituentsUniverseSelectionModelTestAllConstructor()
        {
            int numberOfOperation = 0;
            var ticker = "SPY";
            var symbol = Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            var universeSettings = new UniverseSettings(Resolution.Minute, Security.NullLeverage, true, false, TimeSpan.FromDays(1));

            do
            {
                ETFConstituentsUniverseSelectionModel etfConstituents = numberOfOperation switch
                {
                    0 => new ETFConstituentsUniverseSelectionModel(ticker),
                    1 => new ETFConstituentsUniverseSelectionModel(ticker, universeSettings),
                    2 => new ETFConstituentsUniverseSelectionModel(ticker, ETFConstituentsFilter),
                    3 => new ETFConstituentsUniverseSelectionModel(ticker, universeSettings, ETFConstituentsFilter),
                    4 => new ETFConstituentsUniverseSelectionModel(ticker, universeSettings, default(PyObject)),
                    5 => new ETFConstituentsUniverseSelectionModel(symbol),
                    6 => new ETFConstituentsUniverseSelectionModel(symbol, universeSettings),
                    7 => new ETFConstituentsUniverseSelectionModel(symbol, ETFConstituentsFilter),
                    8 => new ETFConstituentsUniverseSelectionModel(symbol, universeSettings, ETFConstituentsFilter),
                    9 => new ETFConstituentsUniverseSelectionModel(symbol, universeSettings, default(PyObject)),
                    _ => throw new ArgumentException("Not recognize number of operation")
                };

                var universe = etfConstituents.CreateUniverses(new QCAlgorithm()).First();

                Assert.IsNotNull(etfConstituents);
                Assert.IsNotNull(universe);

                Assert.IsTrue(universe.Configuration.Symbol.HasUnderlying);
                Assert.AreEqual(symbol, universe.Configuration.Symbol.Underlying);

                Assert.AreEqual(symbol.SecurityType, universe.Configuration.Symbol.SecurityType);
                Assert.IsTrue(universe.Configuration.Symbol.ID.Symbol.StartsWithInvariant("qc-universe-"));
                var data = new Mock<BaseDataCollection>();
                Assert.DoesNotThrow(() => universe.PerformSelection(DateTime.UtcNow, data.Object));

            } while (++numberOfOperation <= 9) ;
        }

        private IEnumerable<Symbol> ETFConstituentsFilter(IEnumerable<ETFConstituentUniverse> constituents)
        {
            return constituents.Select(c => c.Symbol);
        }

        private static dynamic GetETFConstituentsFrameworkAlgorithm(string etfTicker, string cachedAlias = "CACHED")
        {

            dynamic module = PyModule.FromString("testModule",
@$"from AlgorithmImports import *
from Selection.ETFConstituentsUniverseSelectionModel import *
class ETFConstituentsFrameworkAlgorithm(QCAlgorithm):
    selection_model = None
    def initialize(self):
        SymbolCache.set('SPY', Symbol.create('SPY', SecurityType.EQUITY, Market.USA, '{cachedAlias}'))
        self.universe_settings.resolution = Resolution.DAILY
        self.selection_model = ETFConstituentsUniverseSelectionModel(""{etfTicker}"")"
);

            dynamic algorithm = module.GetAttr("ETFConstituentsFrameworkAlgorithm").Invoke();
            return algorithm;
        }
    }
}
