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
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class ManualUniverseSelectionModelTests
    {
        [Test]
        public void ExcludesCanonicalSymbols()
        {
            var symbols = new[]
            {
                Symbols.SPY,
                Symbol.CreateOption(Symbols.SPY, Market.USA, default(OptionStyle), default(OptionRight), 0m, SecurityIdentifier.DefaultDate, "?SPY")
            };

            var model = new ManualUniverseSelectionModel(symbols);
            var universe = model.CreateUniverses(new QCAlgorithm()).Single();
            var selectedSymbols = universe.SelectSymbols(default(DateTime), null).ToList();

            Assert.AreEqual(1, selectedSymbols.Count);
            Assert.AreEqual(Symbols.SPY, selectedSymbols[0]);
            Assert.IsFalse(selectedSymbols.Any(s => s.IsCanonical()));
        }
    }
}
