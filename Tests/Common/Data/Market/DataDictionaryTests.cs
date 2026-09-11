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
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class DataDictionaryTests
    {
        [Test]
        public void IndexerSetterRefreshesTheCachedKeysAndValues()
        {
            var dictionary = new TradeBars(new DateTime(2016, 2, 26));
            dictionary.Add(Symbols.SPY, new TradeBar { Symbol = Symbols.SPY, Close = 1 });

            // read every cached view, then add through the indexer like the option chains do
            Assert.AreEqual(1, dictionary.Keys.Count);
            Assert.AreEqual(1, dictionary.Values.Count);
            Assert.AreEqual(1, dictionary.Count());

            dictionary[Symbols.AAPL] = new TradeBar { Symbol = Symbols.AAPL, Close = 2 };

            CollectionAssert.AreEquivalent(new[] { Symbols.SPY, Symbols.AAPL }, dictionary.Keys);
            CollectionAssert.AreEquivalent(new[] { 1m, 2m }, dictionary.Values.Select(x => x.Close));
            Assert.AreEqual(2, dictionary.Count());

            // replacing an entry refreshes the values too
            dictionary[Symbols.AAPL] = new TradeBar { Symbol = Symbols.AAPL, Close = 3 };

            CollectionAssert.AreEquivalent(new[] { 1m, 3m }, dictionary.Values.Select(x => x.Close));
        }
    }
}
