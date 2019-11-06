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

using System;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class MappingEventProviderTests
    {
        private SubscriptionDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            var symbol = Symbol.Create("FOXA", SecurityType.Equity, Market.USA);

            _config = new SubscriptionDataConfig(typeof(TradeBar),
                symbol,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
        }

        [Test]
        public void InitialMapping()
        {
            var provider = new MappingEventProvider();

            Assert.AreEqual("FOXA", _config.MappedSymbol);

            provider.Initialize(_config,
                null,
                MapFile.Read("FOXA", Market.USA),
                new DateTime(2006, 1, 1));

            Assert.AreEqual("NWSA", _config.MappedSymbol);
        }

        [Test]
        public void MappingEvent()
        {
            var provider = new MappingEventProvider();
            provider.Initialize(_config,
                null,
                MapFile.Read("FOXA", Market.USA),
                new DateTime(2006, 1, 1));

            Assert.AreEqual("NWSA", _config.MappedSymbol);

            var symbolEvent = (SymbolChangedEvent)provider
                .GetEvents(new NewTradableDateEventArgs(
                    new DateTime(2013, 6, 29),
                    null,
                    _config.Symbol)).Single();

            Assert.AreEqual("FOXA", symbolEvent.NewSymbol);
            Assert.AreEqual("NWSA", symbolEvent.OldSymbol);
            Assert.AreEqual("FOXA", _config.MappedSymbol);
        }
    }
}
