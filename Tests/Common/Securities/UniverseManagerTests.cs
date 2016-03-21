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
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class UniverseManagerTests
    {
        [Test]
        public void NotifiesWhenSecurityAdded()
        {
            var manager = new UniverseManager();

            var universe = new FuncUniverse(CreateTradeBarConfig(), new UniverseSettings(Resolution.Minute, 2, true, false, TimeSpan.Zero), SecurityInitializer.Null,
                data => data.Select(x => x.Symbol)
                );

            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != universe)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to universe");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager.Add(universe.Configuration.Symbol, universe);
        }

        [Test]
        public void NotifiesWhenSecurityAddedViaIndexer()
        {
            var manager = new UniverseManager();

            var universe = new FuncUniverse(CreateTradeBarConfig(), new UniverseSettings(Resolution.Minute, 2, true, false, TimeSpan.Zero), SecurityInitializer.Null,
                data => data.Select(x => x.Symbol)
                );

            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != universe)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to universe");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager[universe.Configuration.Symbol] = universe;
        }

        [Test]
        public void NotifiesWhenSecurityRemoved()
        {
            var manager = new UniverseManager();

            var universe = new FuncUniverse(CreateTradeBarConfig(), new UniverseSettings(Resolution.Minute, 2, true, false, TimeSpan.Zero), SecurityInitializer.Null,
                data => data.Select(x => x.Symbol)
                );

            manager.Add(universe.Configuration.Symbol, universe);
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems.OfType<object>().Single() != universe)
                {
                    Assert.Fail("Expected args.OldItems to have exactly one element equal to universe");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Remove);
                    Assert.Pass();
                }
            };

            manager.Remove(universe.Configuration.Symbol);
        }

        private SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, true);
        }
    }
}
