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
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityManagerTests
    {
        private SubscriptionManager _subscriptionManager;

        [SetUp]
        public void Setup()
        {
            SymbolCache.Clear();

            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            _subscriptionManager = new SubscriptionManager();
            _subscriptionManager.SetDataManager(new DataManagerStub(timeKeeper));
        }

        [Test]
        public void NotifiesWhenSecurityAdded()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager.Add(security.Symbol, security);
        }

        [Test]
        public void NotifiesWhenSecurityAddedViaIndexer()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager[security.Symbol] = security;
        }

        [Test]
        public void NotifiesWhenSecurityRemoved()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            manager.Add(security.Symbol, security);
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Remove);
                    Assert.Pass();
                }
            };

            manager.Remove(security.Symbol);
        }


        [Test]
        public void Option_SubscriptionDataConfigList_CanOnlyHave_RawDataNormalization()
        {
            var option = new Symbol(SecurityIdentifier.GenerateOption(SecurityIdentifier.DefaultDate, Symbols.SPY.ID, Market.USA, 0, default(OptionRight), default(OptionStyle)), "?SPY");

            var subscriptionDataConfigList = new SubscriptionDataConfigList(option);

            Assert.DoesNotThrow(() => { subscriptionDataConfigList.SetDataNormalizationMode(DataNormalizationMode.Raw); });

            Assert.Throws(typeof(ArgumentException), () => { subscriptionDataConfigList.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.Throws(typeof(ArgumentException), () => { subscriptionDataConfigList.SetDataNormalizationMode(DataNormalizationMode.SplitAdjusted); });
            Assert.Throws(typeof(ArgumentException), () => { subscriptionDataConfigList.SetDataNormalizationMode(DataNormalizationMode.Adjusted); });
            Assert.Throws(typeof(ArgumentException), () => { subscriptionDataConfigList.SetDataNormalizationMode(DataNormalizationMode.TotalReturn); });
        }

        private SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
