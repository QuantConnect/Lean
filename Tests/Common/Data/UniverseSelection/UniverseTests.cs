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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class UniverseTests
    {
        private SubscriptionDataConfig _config;
        private Security _security;

        [SetUp]
        public void SetUp()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.AAPL,
                Resolution.Second,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Trade,
                false);
            _security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                _config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
        }

        [Test]
        public void RoundsTimeWhenCheckingMinimumTimeInUniverse_Seconds()
        {
            var universe = new TestUniverse(_config,
                new UniverseSettings(Resolution.Daily, 1, false, false, TimeSpan.FromSeconds(30)));
            var addedTime = new DateTime(2018, 1, 1);
            universe.AddMember(addedTime, _security);

            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddSeconds(29), _security));
            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddSeconds(29.4), _security));

            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddSeconds(29.5), _security));
            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddSeconds(31), _security));
        }

        [Test]
        public void RoundsTimeWhenCheckingMinimumTimeInUniverse_Minutes()
        {
            var universe = new TestUniverse(_config,
                new UniverseSettings(Resolution.Daily, 1, false, false, TimeSpan.FromMinutes(30)));
            var addedTime = new DateTime(2018, 1, 1);
            universe.AddMember(addedTime, _security);

            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddMinutes(29), _security));
            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddMinutes(29.4), _security));

            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddMinutes(29.5), _security));
            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddMinutes(31), _security));
        }

        [Test]
        public void RoundsTimeWhenCheckingMinimumTimeInUniverse_Hour()
        {
            var universe = new TestUniverse(_config,
                new UniverseSettings(Resolution.Daily, 1, false, false, TimeSpan.FromHours(6)));
            var addedTime = new DateTime(2018, 1, 1);
            universe.AddMember(addedTime, _security);

            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddHours(5), _security));
            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddHours(5.1), _security));

            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddHours(5.5), _security));
            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddHours(6), _security));
        }

        [Test]
        public void RoundsTimeWhenCheckingMinimumTimeInUniverse_Daily()
        {
            var universe = new TestUniverse(_config,
                new UniverseSettings(Resolution.Daily, 1, false, false, TimeSpan.FromDays(1)));
            var addedTime = new DateTime(2018, 1, 1);
            universe.AddMember(addedTime, _security);

            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddHours(5), _security));
            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddHours(12), _security));

            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddHours(12.1), _security));
            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddHours(28), _security));
        }

        [Test]
        public void RoundsTimeWhenCheckingMinimumTimeInUniverse_SevenDays()
        {
            var universe = new TestUniverse(_config,
                new UniverseSettings(Resolution.Daily, 1, false, false, TimeSpan.FromDays(7)));
            var addedTime = new DateTime(2018, 1, 1);
            universe.AddMember(addedTime, _security);

            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddDays(4), _security));
            Assert.IsFalse(universe.CanRemoveMember(addedTime.AddDays(6.5), _security));

            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddDays(6.51), _security));
            Assert.IsTrue(universe.CanRemoveMember(addedTime.AddDays(8), _security));
        }

        private class TestUniverse : Universe
        {
            public TestUniverse(SubscriptionDataConfig config, UniverseSettings universeSettings)
                : base(config)
            {
                UniverseSettings = universeSettings;
            }

            public override UniverseSettings UniverseSettings { get; }
            public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
