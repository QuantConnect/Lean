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

using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SubscriptionDataConfigTests
    {
        [Test]
        public void UsesValueEqualsSemantics()
        {
            var config1 = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Trade,
                false
            );
            var config2 = new SubscriptionDataConfig(config1);
            Assert.AreEqual(config1, config2);
        }

        [Test]
        public void UsedAsDictionaryKey()
        {
            var set = new HashSet<SubscriptionDataConfig>();
            var config1 = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Trade,
                false
            );
            Assert.IsTrue(set.Add(config1));
            var config2 = new SubscriptionDataConfig(config1);
            Assert.IsFalse(set.Add(config2));
        }

        [Test]
        public void CanRemoveConsolidatorWhileEnumeratingList()
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Trade,
                false
            );
            var consolidator1 = new TradeBarConsolidator(1);
            var consolidator2 = new TradeBarConsolidator(2);
            config.Consolidators.Add(consolidator1);
            config.Consolidators.Add(consolidator2);
            foreach (var consolidator in config.Consolidators)
            {
                Assert.DoesNotThrow(() => config.Consolidators.Remove(consolidator));
            }
        }

        [TestCase(1, 0, DataMappingMode.OpenInterest, DataMappingMode.OpenInterest)]
        [TestCase(0, 0, DataMappingMode.OpenInterest, DataMappingMode.FirstDayMonth)]
        public void NotEqualsMappingAndOffset(
            int offsetA,
            int offsetB,
            DataMappingMode mappingModeA,
            DataMappingMode mappingModeB
        )
        {
            var configA = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                dataMappingMode: mappingModeA,
                contractDepthOffset: (uint)offsetA
            );
            var configB = new SubscriptionDataConfig(
                configA,
                dataMappingMode: mappingModeB,
                contractDepthOffset: (uint)offsetB
            );

            Assert.AreNotEqual(configA, configB);
            Assert.AreNotEqual(configA.GetHashCode(), configB.GetHashCode());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EqualityMapped(bool mapped)
        {
            var configA = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false
            );
            var configB = new SubscriptionDataConfig(configA, mappedConfig: mapped);

            if (mapped)
            {
                Assert.AreNotEqual(configA, configB);
                Assert.AreNotEqual(configA.GetHashCode(), configB.GetHashCode());
            }
            else
            {
                Assert.AreEqual(configA, configB);
                Assert.AreEqual(configA.GetHashCode(), configB.GetHashCode());
            }
        }
    }
}
