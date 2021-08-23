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
        public void AddUniverseWithConstituentUniverseDefinitionTicker(string ticker, string market, bool isPython = false)
        {
            AssertETFConstituentUniverseDefinitionsSymbol(ticker, market, false, isPython);
        }
        
        [TestCaseSource(nameof(ETFConstituentUniverseTestCases))]
        public void AddUniverseWithConstituentUniverseDefinitionSymbol(string ticker, string market, bool isPython = false)
        {
            AssertETFConstituentUniverseDefinitionsSymbol(ticker, market, true, isPython);
        }

        private void AssertETFConstituentUniverseDefinitionsSymbol(string ticker, string market, bool isSymbol, bool isPython)
        {
            var algo = CreateAlgorithm();
            algo.SetStartDate(2021, 8, 23);
            algo.SetEndDate(2021, 8, 24);
            
            Universe etfConstituentUniverse;
            var equity = Symbol.Create(ticker, SecurityType.Equity, market ?? Market.USA);
            
            if (isSymbol)
            {
                etfConstituentUniverse = isPython 
                    ? algo.Universe.ETF(equity, algo.UniverseSettings, (PyObject)null)
                    : algo.Universe.ETF(equity, algo.UniverseSettings, CreateReturnAllFunc());
            }
            else
            {
                etfConstituentUniverse = isPython
                    ? algo.Universe.ETF(ticker, market, algo.UniverseSettings, (PyObject)null)
                    : algo.Universe.ETF(ticker, market, algo.UniverseSettings, CreateReturnAllFunc());
            }
            
            Assert.IsTrue(etfConstituentUniverse.Configuration.Symbol.HasUnderlying);
            Assert.AreEqual(equity, etfConstituentUniverse.Configuration.Symbol.Underlying);
            
            Assert.AreEqual(equity.SecurityType, etfConstituentUniverse.Configuration.Symbol.SecurityType);
            Assert.IsTrue(etfConstituentUniverse.Configuration.Symbol.ID.Symbol.StartsWithInvariant("qc-universe-etf-constituents"));
        }
        
        private static TestCaseData[] ETFConstituentUniverseTestCases()
        {
            return new[]
            {
                // C# test cases
                new TestCaseData("SPY", Market.USA, false),
                new TestCaseData("SPY", null, false),
                new TestCaseData("GDVD", Market.USA, false),
                new TestCaseData("GDVD", null, false),

                // Python test cases
                new TestCaseData("SPY", Market.USA, true),
                new TestCaseData("SPY", null, true),
                new TestCaseData("GDVD", Market.USA, true),
                new TestCaseData("GDVD", null, true)
            };
        }

        private QCAlgorithm CreateAlgorithm()
        {
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            return algo;
        }

        private Func<IEnumerable<ETFConstituentData>, IEnumerable<Symbol>> CreateReturnAllFunc()
        {
            return x => x.Select(y => y.Symbol);
        }
    }
}
