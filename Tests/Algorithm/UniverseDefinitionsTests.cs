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
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class UniverseDefinitionsTests
    {
        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ETFOverloadsAreAllUsable(Language language)
        {
            var algorithm = new QCAlgorithm();
            var universeDefinitions = algorithm.Universe;
            var universeSettings = new UniverseSettings(Resolution.Minute, Security.NullLeverage, true, false, TimeSpan.FromDays(1));

            List<Universe> etfs = null;

            if (language == Language.CSharp)
            {
                etfs = new()
                {
                    universeDefinitions.ETF("SPY"),
                    universeDefinitions.ETF("SPY", Market.USA),
                    universeDefinitions.ETF("SPY", Market.USA, universeSettings),
                    universeDefinitions.ETF("SPY", Market.USA, universeSettings, Filter),
                    universeDefinitions.ETF("SPY", Market.USA, universeFilterFunc: Filter),
                    universeDefinitions.ETF("SPY", universeSettings: universeSettings),
                    universeDefinitions.ETF("SPY", universeFilterFunc: Filter),
                    universeDefinitions.ETF("SPY", universeSettings: universeSettings, universeFilterFunc: Filter),
                    universeDefinitions.ETF(Symbols.SPY),
                    universeDefinitions.ETF(Symbols.SPY, universeSettings),
                    universeDefinitions.ETF(Symbols.SPY, universeFilterFunc: Filter),
                    universeDefinitions.ETF(Symbols.SPY, universeSettings, Filter),
                };
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                        @"
from typing import List
from AlgorithmImports import *

def getETFs(algorithm: QCAlgorithm, symbol: Symbol, universeSettings: UniverseSettings) -> List[Universe]:
    universeDefinitions = algorithm.Universe;
    return [
        universeDefinitions.ETF('SPY'),
        universeDefinitions.ETF('SPY', Market.USA),
        universeDefinitions.ETF('SPY', Market.USA, universeSettings),
        universeDefinitions.ETF('SPY', Market.USA, universeSettings, filterETFs),
        universeDefinitions.ETF('SPY', Market.USA, universeFilterFunc=filterETFs),
        universeDefinitions.ETF('SPY', universeSettings, filterETFs),
        universeDefinitions.ETF('SPY', universeSettings=universeSettings),
        universeDefinitions.ETF('SPY', universeFilterFunc=filterETFs),
        universeDefinitions.ETF('SPY', universeSettings=universeSettings, universeFilterFunc=filterETFs),
        universeDefinitions.ETF(symbol),
        universeDefinitions.ETF(symbol, universeSettings),
        universeDefinitions.ETF(symbol, universeFilterFunc=filterETFs),
        universeDefinitions.ETF(symbol, universeSettings, filterETFs),
    ]

def filterETFs(constituents: List[ETFConstituentData]) -> List[Symbol]:
    return [x.Symbol for x in constituents]
        ");

                    var getETFs = testModule.GetAttr("getETFs");

                    etfs = getETFs.Invoke(algorithm.ToPython(), Symbols.SPY.ToPython(), universeSettings.ToPython()).As<List<Universe>>();
                }
            }

            AssertETFConstituentsUniverses(etfs);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void IndexOverloadsAreAllUsable(Language language)
        {
            var algorithm = new QCAlgorithm();
            var universeDefinitions = algorithm.Universe;
            var universeSettings = new UniverseSettings(Resolution.Minute, Security.NullLeverage, true, false, TimeSpan.FromDays(1));

            List<Universe> indexes = null;

            if (language == Language.CSharp)
            {
                indexes = new()
                {
                    universeDefinitions.Index("SPY"),
                    universeDefinitions.Index("SPY", Market.USA),
                    universeDefinitions.Index("SPY", Market.USA, universeSettings),
                    universeDefinitions.Index("SPY", Market.USA, universeSettings, Filter),
                    universeDefinitions.Index("SPY", Market.USA, universeFilterFunc: Filter),
                    universeDefinitions.Index("SPY", universeSettings: universeSettings),
                    universeDefinitions.Index("SPY", universeFilterFunc: Filter),
                    universeDefinitions.Index("SPY", universeSettings: universeSettings, universeFilterFunc: Filter),
                    universeDefinitions.Index(Symbols.SPY),
                    universeDefinitions.Index(Symbols.SPY, universeSettings),
                    universeDefinitions.Index(Symbols.SPY, universeFilterFunc: Filter),
                    universeDefinitions.Index(Symbols.SPY, universeSettings, Filter),
                };
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("testModule",
                        @"
from typing import List
from AlgorithmImports import *

def getIndexes(algorithm: QCAlgorithm, symbol: Symbol, universeSettings: UniverseSettings) -> List[Universe]:
    universeDefinitions = algorithm.Universe;
    return [
        universeDefinitions.Index('SPY'),
        universeDefinitions.Index('SPY', Market.USA),
        universeDefinitions.Index('SPY', Market.USA, universeSettings),
        universeDefinitions.Index('SPY', Market.USA, universeSettings, filterIndexes),
        universeDefinitions.Index('SPY', Market.USA, universeFilterFunc=filterIndexes),
        universeDefinitions.Index('SPY', universeSettings, filterIndexes),
        universeDefinitions.Index('SPY', universeSettings=universeSettings),
        universeDefinitions.Index('SPY', universeFilterFunc=filterIndexes),
        universeDefinitions.Index('SPY', universeSettings=universeSettings, universeFilterFunc=filterIndexes),
        universeDefinitions.Index(symbol),
        universeDefinitions.Index(symbol, universeSettings),
        universeDefinitions.Index(symbol, universeFilterFunc=filterIndexes),
        universeDefinitions.Index(symbol, universeSettings, filterIndexes),
    ]

def filterIndexes(constituents: List[ETFConstituentData]) -> List[Symbol]:
    return [x.Symbol for x in constituents]
        ");

                    var getIndexes = testModule.GetAttr("getIndexes");

                    indexes = getIndexes.Invoke(algorithm.ToPython(), Symbols.SPY.ToPython(), universeSettings.ToPython()).As<List<Universe>>();
                }
            }

            AssertETFConstituentsUniverses(indexes);
        }

        private static void AssertETFConstituentsUniverses(List<Universe> universes)
        {
            CollectionAssert.AllItemsAreNotNull(universes, "Universes should not be null");
            CollectionAssert.AllItemsAreInstancesOfType(universes, typeof(ETFConstituentsUniverseFactory),
                "Universes should be of type ETFConstituentsUniverse");
        }

        private static IEnumerable<Symbol> Filter(IEnumerable<ETFConstituentUniverse> constituents)
        {
            return constituents.Select(x => x.Symbol);
        }
    }
}
