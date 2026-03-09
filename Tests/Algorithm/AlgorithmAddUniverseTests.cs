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
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    public class AlgorithmAddUniverseTests
    {
        [TestCaseSource(nameof(ETFConstituentUniverseTestCases))]
        public void AddUniverseWithETFConstituentUniverseDefinitionTicker(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, false, false, true);
        }

        [TestCaseSource(nameof(ETFConstituentUniverseTestCases))]
        public void AddUniverseWithETFConstituentUniverseDefinitionTickerPython(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, false, true, true);
        }
        
        [TestCaseSource(nameof(ETFConstituentUniverseTestCases))]
        public void AddUniverseWithETFConstituentUniverseDefinitionSymbol(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, true, false, true);
        }

        [TestCaseSource(nameof(ETFConstituentUniverseTestCases))]
        public void AddUniverseWithETFConstituentUniverseDefinitionSymbolPython(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, true, true, true);
        }

        [TestCaseSource(nameof(IndexConstituentUniverseTestCases))]
        public void AddUniverseWithIndexConstituentUniverseDefinitionTicker(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, false, false, false);
        }

        [TestCaseSource(nameof(IndexConstituentUniverseTestCases))]
        public void AddUniverseWithIndexConstituentUniverseDefinitionSymbol(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, true, false, false);
        }

        [TestCaseSource(nameof(IndexConstituentUniverseTestCases))]
        public void AddUniverseWithIndexConstituentUniverseDefinitionTickerPython(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, false, true, false);
        }

        [TestCaseSource(nameof(IndexConstituentUniverseTestCases))]
        public void AddUniverseWithIndexConstituentUniverseDefinitionSymbolPython(string ticker, string market)
        {
            AssertConstituentUniverseDefinitionsSymbol(ticker, market, true, true, false);
        }

        private void AssertConstituentUniverseDefinitionsSymbol(string ticker, string market, bool isSymbol, bool isPython, bool isEtf)
        {
            var algo = CreateAlgorithm();
            algo.SetStartDate(2021, 8, 23);
            algo.SetEndDate(2021, 8, 24);

            Universe constituentUniverse;
            var symbol = Symbol.Create(ticker, isEtf ? SecurityType.Equity : SecurityType.Index, market ?? Market.USA);

            if (isSymbol && isEtf)
            {
                constituentUniverse = isPython
                    ? algo.Universe.ETF(symbol, algo.UniverseSettings, (PyObject) null)
                    : algo.Universe.ETF(symbol, algo.UniverseSettings, CreateReturnAllFunc());
            }
            else if (isEtf)
            {
                constituentUniverse = isPython
                    ? algo.Universe.ETF(ticker, market, algo.UniverseSettings, (PyObject) null)
                    : algo.Universe.ETF(ticker, market, algo.UniverseSettings, CreateReturnAllFunc());
            }
            else if (isSymbol)
            {
                constituentUniverse = isPython
                    ? algo.Universe.Index(symbol, algo.UniverseSettings, (PyObject) null)
                    : algo.Universe.Index(symbol, algo.UniverseSettings, CreateReturnAllFunc());
            }
            else
            {
                constituentUniverse = isPython
                    ? algo.Universe.Index(ticker, market, algo.UniverseSettings, (PyObject) null)
                    : algo.Universe.Index(ticker, market, algo.UniverseSettings, CreateReturnAllFunc());
            }

            Assert.IsTrue(constituentUniverse.Configuration.Symbol.HasUnderlying);
            Assert.AreEqual(symbol, constituentUniverse.Configuration.Symbol.Underlying);
            
            Assert.AreEqual(symbol.SecurityType, constituentUniverse.Configuration.Symbol.SecurityType);
            Assert.IsTrue(constituentUniverse.Configuration.Symbol.ID.Symbol.StartsWithInvariant("qc-universe-"));
        }
        
        private static TestCaseData[] ETFConstituentUniverseTestCases()
        {
            return new[]
            {
                new TestCaseData("SPY", Market.USA),
                new TestCaseData("SPY", null),
                new TestCaseData("GDVD", Market.USA),
                new TestCaseData("GDVD", null)
            };
        }

        private static TestCaseData[] IndexConstituentUniverseTestCases()
        {
            return new[]
            {
                new TestCaseData("SPX", Market.USA),
                new TestCaseData("SPX", null),
                new TestCaseData("NDX", Market.USA),
                new TestCaseData("NDX", null)
            };
        }

        private QCAlgorithm CreateAlgorithm()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            return algo;
        }

        private Func<IEnumerable<ETFConstituentUniverse>, IEnumerable<Symbol>> CreateReturnAllFunc()
        {
            return x => x.Select(y => y.Symbol);
        }
    }
}
