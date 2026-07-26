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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SubscriptionDataConfigBarPeriodTests
    {
        private static readonly Resolution[] AllResolutions =
            (Resolution[])Enum.GetValues(typeof(Resolution));

        private static SubscriptionDataConfig CreateConfig(Resolution resolution = Resolution.Minute,
            TimeSpan? barPeriod = null, Symbol symbol = null)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol ?? Symbols.SPY, resolution,
                TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false,
                barPeriod: barPeriod);
        }

        [Test]
        public void IncrementIsDerivedFromResolutionByDefault([Values] Resolution resolution)
        {
            var config = CreateConfig(resolution);

            Assert.IsNull(config.BarPeriod);
            Assert.AreEqual(resolution.ToTimeSpan(), config.Increment);
        }

        [Test]
        public void DeclaredBarPeriodDrivesIncrement()
        {
            var barPeriod = TimeSpan.FromMinutes(30);
            var config = CreateConfig(Resolution.Minute, barPeriod);

            Assert.AreEqual(barPeriod, config.BarPeriod);
            Assert.AreEqual(barPeriod, config.Increment);
            // the resolution is untouched, so data continues to be resolved from the same files
            Assert.AreEqual(Resolution.Minute, config.Resolution);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void RejectsNonPositiveBarPeriod(int minutes)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(minutes)));
        }

        [Test]
        public void RejectsBarPeriodForTickResolution()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateConfig(Resolution.Tick, TimeSpan.FromMinutes(30)));
        }

        [Test]
        public void CopyConstructorInheritsBarPeriod()
        {
            var barPeriod = TimeSpan.FromMinutes(30);
            var config = CreateConfig(Resolution.Minute, barPeriod);

            var copy = new SubscriptionDataConfig(config, symbol: Symbols.AAPL);

            Assert.AreEqual(barPeriod, copy.BarPeriod);
            Assert.AreEqual(barPeriod, copy.Increment);
        }

        [Test]
        public void CopyConstructorDropsBarPeriodWhenResolutionChanges()
        {
            var config = CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(30));

            var copy = new SubscriptionDataConfig(config, resolution: Resolution.Daily);

            Assert.IsNull(copy.BarPeriod);
            Assert.AreEqual(Resolution.Daily.ToTimeSpan(), copy.Increment);
        }

        [Test]
        public void CopyConstructorHonorsExplicitBarPeriodOverride()
        {
            var config = CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(30));

            var copy = new SubscriptionDataConfig(config, barPeriod: TimeSpan.FromMinutes(15));

            Assert.AreEqual(TimeSpan.FromMinutes(15), copy.Increment);
        }

        [Test]
        public void ConfigsDifferingOnlyByBarPeriodAreNotEqual()
        {
            var thirtyMinutes = CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(30));
            var fifteenMinutes = CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(15));

            Assert.AreNotEqual(thirtyMinutes, fifteenMinutes);
            Assert.AreNotEqual(thirtyMinutes.GetHashCode(), fifteenMinutes.GetHashCode());

            var set = new HashSet<SubscriptionDataConfig> { thirtyMinutes };
            Assert.IsTrue(set.Add(fifteenMinutes), "Configs with different bar periods must not be deduplicated");
        }

        [Test]
        public void EqualityIsUnchangedWhenNoBarPeriodIsDeclared()
        {
            var first = CreateConfig();
            var second = CreateConfig();

            Assert.AreEqual(first, second);
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// GetHighestResolutionSpan must be a drop in replacement for GetHighestResolution().ToTimeSpan()
        /// for any set of configurations that does not declare a bar period.
        /// </summary>
        [Test]
        public void HighestResolutionSpanMatchesHighestResolutionWhenNoBarPeriodDeclared()
        {
            foreach (var first in AllResolutions)
            {
                foreach (var second in AllResolutions)
                {
                    var configs = new List<SubscriptionDataConfig>
                    {
                        CreateConfig(first),
                        CreateConfig(second, symbol: Symbols.AAPL)
                    };

                    Assert.AreEqual(configs.GetHighestResolution().ToTimeSpan(), configs.GetHighestResolutionSpan(),
                        $"Mismatch for {first} and {second}");
                }
            }
        }

        [Test]
        public void HighestResolutionSpanFallsBackToDailyWhenEmpty()
        {
            var configs = new List<SubscriptionDataConfig>();

            Assert.AreEqual(configs.GetHighestResolution().ToTimeSpan(), configs.GetHighestResolutionSpan());
            Assert.AreEqual(Resolution.Daily.ToTimeSpan(), configs.GetHighestResolutionSpan());
        }

        [Test]
        public void HighestResolutionSpanHonorsDeclaredBarPeriod()
        {
            var configs = new List<SubscriptionDataConfig>
            {
                CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(30)),
                CreateConfig(Resolution.Hour, symbol: Symbols.AAPL)
            };

            Assert.AreEqual(TimeSpan.FromMinutes(30), configs.GetHighestResolutionSpan());
            // the enum based helper cannot see the declared period and reports the finer nominal resolution
            Assert.AreEqual(Resolution.Minute, configs.GetHighestResolution());
        }

        /// <summary>
        /// The fill model treats a bar as coarse, and therefore fills a resting order at the bar open, when
        /// its period exceeds one minute. That must select exactly the same resolutions as the previous
        /// Resolution.Hour/Resolution.Daily test.
        /// </summary>
        [Test]
        public void CoarseBarPredicateMatchesHourAndDailyOnly([Values] Resolution resolution)
        {
            var config = CreateConfig(resolution);

            var isCoarseByPeriod = config.Increment > Time.OneMinute;
            var isCoarseByResolution = resolution == Resolution.Hour || resolution == Resolution.Daily;

            Assert.AreEqual(isCoarseByResolution, isCoarseByPeriod, $"Mismatch for {resolution}");
        }

        [Test]
        public void DeclaredThirtyMinuteBarIsCoarse()
        {
            var config = CreateConfig(Resolution.Minute, TimeSpan.FromMinutes(30));

            Assert.Greater(config.Increment, Time.OneMinute);
        }
    }
}
