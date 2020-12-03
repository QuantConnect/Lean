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
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.ToolBox.IQFeed;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{

    /// <summary>
    ///  Test fixture is explicit, because tests are dependent on network and are long
    /// </summary>
    [TestFixture, Ignore("Tests are dependent on network and are long")]
    public class IQFeedRealtimeDataFeedTests
    {
        [Test]
        public void IQFeedSanityCheckIfDataIsLoaded()
        {
            var symbolUniverse = new IQFeedDataQueueUniverseProvider();

            var lookup = symbolUniverse as IDataQueueUniverseProvider;
            var mapper = symbolUniverse as ISymbolMapper;

            Assert.IsTrue(symbolUniverse.LookupSymbols("SPY", SecurityType.Option, false).Any());
            Assert.IsTrue(symbolUniverse.LookupSymbols("SPY", SecurityType.Equity, false).Count() == 1);

            Assert.IsTrue(lookup.LookupSymbols(Symbol.Create("SPY", SecurityType.Option, Market.USA), false).Any());
            Assert.IsTrue(lookup.LookupSymbols(Symbol.Create("SPY", SecurityType.Equity, Market.USA), false).Count() == 1);

            Assert.IsTrue(!string.IsNullOrEmpty(mapper.GetBrokerageSymbol(Symbols.SPY)));
            Assert.IsTrue(mapper.GetLeanSymbol("SPY", SecurityType.Equity, "") != Symbol.Empty);

            symbolUniverse.DisposeSafely();
        }
    }
}
