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
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Securities;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveFactorFileEventsTests
    {
        private static readonly DateTime Thursday = new(2026, 10, 22);
        private static readonly DateTime Friday = new(2026, 10, 23);
        private static readonly DateTime Saturday = new(2026, 10, 24);
        private static readonly DateTime Sunday = new(2026, 10, 25);
        private static readonly DateTime Monday = new(2026, 10, 26);

        // split effective on Monday's open, so its row is keyed on the previous trading day, Friday
        private static readonly CorporateFactorProvider WithoutSplit = new("AAPL", new[]
        {
            new CorporateFactorRow(new DateTime(2026, 8, 28), 1m, 15m, 0.282m),
            new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m, 0m)
        });
        private static readonly CorporateFactorProvider WithSplit = new("AAPL", new[]
        {
            new CorporateFactorRow(new DateTime(2026, 8, 28), 1m, 30m, 0.282m),
            new CorporateFactorRow(Friday, 1m, 2m, 0.4032m),
            new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m, 0m)
        });

        // dividend with ex-date on Monday, so its row is keyed on the previous trading day, Friday
        private static readonly CorporateFactorProvider WithoutDividend = new("AAPL", new[]
        {
            new CorporateFactorRow(new DateTime(2026, 8, 28), 0.98m, 1m, 40m),
            new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m, 0m)
        });
        private static readonly CorporateFactorProvider WithDividend = new("AAPL", new[]
        {
            new CorporateFactorRow(new DateTime(2026, 8, 28), 0.965m, 1m, 40m),
            new CorporateFactorRow(Friday, 0.9847357m, 1m, 46.35m),
            new CorporateFactorRow(new DateTime(2050, 12, 31), 1m, 1m, 0m)
        });

        [TestCase(true)]
        [TestCase(false)]
        public void EmitsEventWhoseRowArrivesAfterItsDateBeforeItTakesEffect(bool split)
        {
            var test = new TestProvider(split, Without(split));

            Assert.IsEmpty(test.GetEvents(Friday));

            test.FactorFileProvider.FactorFile = With(split);
            AssertOccurred(split, Saturday, test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
            Assert.IsEmpty(test.GetEvents(Monday));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EmitsEventWhoseRowArrivesOnTheMorningItTakesEffect(bool split)
        {
            var test = new TestProvider(split, Without(split));

            Assert.IsEmpty(test.GetEvents(Friday));
            Assert.IsEmpty(test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));

            test.FactorFileProvider.FactorFile = With(split);
            AssertOccurred(split, Monday, test.GetEvents(Monday));
            Assert.IsEmpty(test.GetEvents(Monday.AddDays(1)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SkipsEventWhoseRowArrivesAfterItTookEffect(bool split)
        {
            var test = new TestProvider(split, Without(split));

            Assert.IsEmpty(test.GetEvents(Friday));
            Assert.IsEmpty(test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
            Assert.IsEmpty(test.GetEvents(Monday));

            test.FactorFileProvider.FactorFile = With(split);
            Assert.IsEmpty(test.GetEvents(Monday.AddDays(1)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EmitsEventWhenStartingAfterItsDateBeforeItTakesEffect(bool split)
        {
            var test = new TestProvider(split, With(split));

            AssertOccurred(split, Saturday, test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
            Assert.IsEmpty(test.GetEvents(Monday));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SkipsEventWhenStartingOnTheDateItTakesEffect(bool split)
        {
            var test = new TestProvider(split, With(split));

            Assert.IsEmpty(test.GetEvents(Monday));
            Assert.IsEmpty(test.GetEvents(Monday.AddDays(1)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EmitsEventWhoseRowIsAvailableOnItsDateOnce(bool split)
        {
            var test = new TestProvider(split, With(split));

            Assert.IsEmpty(test.GetEvents(Thursday));
            var events = test.GetEvents(Friday);
            if (split)
            {
                Assert.AreEqual(SplitType.Warning, ((Split)events.Single()).Type);
            }
            else
            {
                Assert.IsEmpty(events);
            }
            AssertOccurred(split, Saturday, test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
            Assert.IsEmpty(test.GetEvents(Monday));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SkipsRefreshWithoutNewRows(bool split)
        {
            var test = new TestProvider(split, Without(split));

            Assert.IsEmpty(test.GetEvents(Friday));
            test.FactorFileProvider.FactorFile = new CorporateFactorProvider("AAPL", Without(split).Cast<CorporateFactorRow>());
            Assert.IsEmpty(test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
            Assert.IsEmpty(test.GetEvents(Monday));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SkipsEventWhoseRowArrivesAfterItsDateWithoutReferencePrice(bool split)
        {
            var test = new TestProvider(split, Without(split));

            Assert.IsEmpty(test.GetEvents(Friday));
            test.FactorFileProvider.FactorFile = new CorporateFactorProvider("AAPL", With(split).Cast<CorporateFactorRow>()
                .Select(row => new CorporateFactorRow(row.Date, row.PriceFactor, row.SplitFactor, 0m)));
            Assert.IsEmpty(test.GetEvents(Saturday));
            Assert.IsEmpty(test.GetEvents(Sunday));
        }

        [Test]
        public void LiveAuxiliaryDataEnumeratorEmitsSplitWhoseRowArrivesAfterItsDate()
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.AAPL, Resolution.Minute, TimeZones.NewYork,
                TimeZones.NewYork, true, false, false);
            var factorFileProvider = new TestFactorFileProvider { FactorFile = WithoutSplit };
            var timeProvider = new ManualTimeProvider(Friday.AddHours(9), TimeZones.NewYork);

            Assert.IsTrue(LiveAuxiliaryDataEnumerator.TryCreate(config, timeProvider, new SecurityCache(), TestGlobals.MapFileProvider,
                factorFileProvider, Friday, out var enumerator));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // the refreshed factor file is available before the next tradable date, Saturday at 8 AM
            factorFileProvider.FactorFile = WithSplit;
            timeProvider.Advance(TimeSpan.FromHours(22));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            timeProvider.Advance(TimeSpan.FromHours(1));
            Assert.IsTrue(enumerator.MoveNext());
            AssertOccurred(true, Saturday, new List<BaseData> { enumerator.Current });
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // nothing more through Monday
            for (var i = 0; i < 2; i++)
            {
                timeProvider.Advance(TimeSpan.FromDays(1));
                Assert.IsTrue(enumerator.MoveNext());
                Assert.IsNull(enumerator.Current);
            }
            enumerator.Dispose();
        }

        private static CorporateFactorProvider Without(bool split) => split ? WithoutSplit : WithoutDividend;

        private static CorporateFactorProvider With(bool split) => split ? WithSplit : WithDividend;

        private static void AssertOccurred(bool split, DateTime date, List<BaseData> events)
        {
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(date, events[0].Time);
            if (split)
            {
                var splitEvent = (Split)events[0];
                Assert.AreEqual(SplitType.SplitOccurred, splitEvent.Type);
                Assert.AreEqual(2m, splitEvent.SplitFactor);
                Assert.AreEqual(0.4032m, splitEvent.ReferencePrice);
                return;
            }

            var dividend = (Dividend)events[0];
            Assert.AreEqual(46.35m, dividend.ReferencePrice);
            Assert.AreEqual(Math.Round(46.35m - 46.35m * 0.9847357m, 2), dividend.Distribution);
        }

        private class TestProvider
        {
            private readonly ITradableDateEventProvider _provider;

            public TestFactorFileProvider FactorFileProvider { get; }

            public TestProvider(bool split, CorporateFactorProvider factorFile)
            {
                FactorFileProvider = new TestFactorFileProvider { FactorFile = factorFile };
                _provider = split ? new LiveSplitEventProvider() : new LiveDividendEventProvider();
                var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.AAPL, Resolution.Minute, TimeZones.NewYork,
                    TimeZones.NewYork, false, false, false);
                _provider.Initialize(config, FactorFileProvider, TestGlobals.MapFileProvider, Thursday);
            }

            public List<BaseData> GetEvents(DateTime date)
            {
                return _provider.GetEvents(new NewTradableDateEventArgs(date, null, Symbols.AAPL, 1m)).ToList();
            }
        }

        private class TestFactorFileProvider : IFactorFileProvider
        {
            public CorporateFactorProvider FactorFile { get; set; }

            public void Initialize(IMapFileProvider mapFileProvider, IDataProvider dataProvider)
            {
            }

            public IFactorProvider Get(Symbol symbol)
            {
                return FactorFile;
            }
        }
    }
}
