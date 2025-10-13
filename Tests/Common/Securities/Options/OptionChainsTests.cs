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
using NUnit.Framework;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionChainsTests
    {
        private Symbol _spyEquity;
        private Symbol _spyOptionCanonical;
        private Symbol _spxIndex;
        private Symbol _spxOptionCanonical;
        private Symbol _esFuture;
        private Symbol _esFutureOptionCanonical;

        [SetUp]
        public void SetUp()
        {
            // Equity
            _spyEquity = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            _spyOptionCanonical = Symbol.CreateCanonicalOption(_spyEquity);

            // Index  
            _spxIndex = Symbol.Create("SPX", SecurityType.Index, Market.USA);
            _spxOptionCanonical = Symbol.CreateCanonicalOption(_spxIndex);

            // Future
            _esFuture = Symbol.CreateFuture("ES", Market.CME, new DateTime(2024, 3, 19));
            _esFutureOptionCanonical = Symbol.CreateCanonicalOption(_esFuture);
        }

        [Test]
        [TestCase(SecurityType.Equity, true)]
        [TestCase(SecurityType.Equity, false)]
        [TestCase(SecurityType.Index, true)]
        [TestCase(SecurityType.Index, false)]
        [TestCase(SecurityType.Future, true)]
        [TestCase(SecurityType.Future, false)]
        public void OptionChainCanBeAccessedByCanonicalOrUnderlying(SecurityType securityType, bool setByCanonical)
        {
            var chains = new OptionChains();

            // Get the symbols based on security type
            var (underlyingSymbol, canonicalSymbol) = GetSymbolsForSecurityType(securityType);
            var expectedChain = new OptionChain(canonicalSymbol, new DateTime(2024, 1, 1));

            // Set using either canonical or underlying
            var setSymbol = setByCanonical ? canonicalSymbol : underlyingSymbol;
            chains[setSymbol] = expectedChain;

            // Should be accessible by BOTH symbols regardless of which one was used to set
            // Test access by canonical
            var chainByCanonical = chains[canonicalSymbol];
            Assert.AreEqual(expectedChain, chainByCanonical);
            Assert.IsTrue(chains.TryGetValue(canonicalSymbol, out var chainByTryGetCanonical));
            Assert.AreEqual(expectedChain, chainByTryGetCanonical);

            // Test access by underlying
            var chainByUnderlying = chains[underlyingSymbol];
            Assert.AreEqual(expectedChain, chainByUnderlying);
            Assert.IsTrue(chains.TryGetValue(underlyingSymbol, out var chainByTryGetUnderlying));
            Assert.AreEqual(expectedChain, chainByTryGetUnderlying);
        }

        [Test]
        public void ContainsKeyWorksWithUnderlyingOrCanonical()
        {
            var chains = new OptionChains();
            var (underlyingSymbol, canonicalSymbol) = GetSymbolsForSecurityType(SecurityType.Equity);
            var expectedChain = new OptionChain(canonicalSymbol, new DateTime(2024, 1, 1));
            chains[canonicalSymbol] = expectedChain;

            // Should be accessible by BOTH regardless of which was used to add
            Assert.IsTrue(chains.ContainsKey(underlyingSymbol));
            Assert.IsTrue(chains.ContainsKey(canonicalSymbol));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void AddWorksWithUnderlyingOrCanonical(bool useCanonical)
        {
            var chains = new OptionChains();
            var expectedChain = new OptionChain(_spyOptionCanonical, new DateTime(2024, 1, 1));

            var addSymbol = useCanonical ? _spyOptionCanonical : _spyEquity;
            chains.Add(addSymbol, expectedChain);

            // Should be accessible by BOTH regardless of which was used to add
            Assert.AreEqual(expectedChain, chains[_spyOptionCanonical]);
            Assert.AreEqual(expectedChain, chains[_spyEquity]);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void RemoveWorksWithUnderlyingOrCanonical(bool useCanonical)
        {
            var chains = new OptionChains();
            var expectedChain = new OptionChain(_spyOptionCanonical, new DateTime(2024, 1, 1));
            chains[_spyOptionCanonical] = expectedChain;

            var removeSymbol = useCanonical ? _spyOptionCanonical : _spyEquity;
            var result = chains.Remove(removeSymbol);

            Assert.IsTrue(result);
            Assert.IsFalse(chains.ContainsKey(_spyOptionCanonical));
            Assert.IsFalse(chains.ContainsKey(_spyEquity));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ContainsWorksWithUnderlyingOrCanonicalKeyValuePair(bool useCanonical)
        {
            var chains = new OptionChains();
            var expectedChain = new OptionChain(_spyOptionCanonical, new DateTime(2024, 1, 1));
            chains[_spyOptionCanonical] = expectedChain;

            var containsSymbol = useCanonical ? _spyOptionCanonical : _spyEquity;
            var result = chains.Contains(new KeyValuePair<Symbol, OptionChain>(containsSymbol, expectedChain));

            Assert.IsTrue(result);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void RemoveWorksWithUnderlyingOrCanonicalKeyValuePair(bool useCanonical)
        {
            var chains = new OptionChains();
            var expectedChain = new OptionChain(_spyOptionCanonical, new DateTime(2024, 1, 1));
            chains[_spyOptionCanonical] = expectedChain;

            var removeSymbol = useCanonical ? _spyOptionCanonical : _spyEquity;
            var result = chains.Remove(new KeyValuePair<Symbol, OptionChain>(removeSymbol, expectedChain));

            Assert.IsTrue(result);
            Assert.IsFalse(chains.ContainsKey(_spyOptionCanonical));
            Assert.IsFalse(chains.ContainsKey(_spyEquity));
        }

        private (Symbol underlying, Symbol canonical) GetSymbolsForSecurityType(SecurityType securityType)
        {
            return securityType switch
            {
                SecurityType.Equity => (_spyEquity, _spyOptionCanonical),
                SecurityType.Index => (_spxIndex, _spxOptionCanonical),
                SecurityType.Future => (_esFuture, _esFutureOptionCanonical),
                _ => throw new ArgumentException($"Unsupported security type: {securityType}")
            };
        }
    }
}
