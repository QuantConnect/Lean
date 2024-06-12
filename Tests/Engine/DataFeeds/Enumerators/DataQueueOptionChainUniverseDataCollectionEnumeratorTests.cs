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
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Securities.Option;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class DataQueueOptionChainUniverseDataCollectionEnumeratorTests
    {
        [TestCase(Resolution.Tick)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        [TestCase(Resolution.Daily)]
        public void NullUnderlying(Resolution resolution)
        {
            var startTime = new DateTime(2018, 10, 17, 5, 0, 0);
            var timeProvider = new ManualTimeProvider(startTime);

            var symbolUniverse = new TestDataQueueUniverseProvider(timeProvider);

            var canonicalSymbol = Symbols.SPY_Option_Chain;
            var request = GetRequest(canonicalSymbol, startTime, resolution);
            using var underlying = new EnqueueableEnumerator<BaseData>();
            using var enumerator = new DataQueueOptionChainUniverseDataCollectionEnumerator(request, underlying, symbolUniverse, timeProvider);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            request.Universe.Dispose();
        }

        [TestCase(Resolution.Tick, "20181017 05:00", 5)]
        [TestCase(Resolution.Second, "20181017 05:00", 5)]
        [TestCase(Resolution.Minute, "20181017 05:00", 5)]
        [TestCase(Resolution.Hour, "20181017 05:00", 5)]
        [TestCase(Resolution.Daily, "20181017 05:00", 5)]

        [TestCase(Resolution.Tick, "20181017 10:00", 10)]
        [TestCase(Resolution.Second, "20181017 10:00", 10)]
        [TestCase(Resolution.Minute, "20181017 10:00", 10)]
        [TestCase(Resolution.Hour, "20181017 10:00", 10)]
        [TestCase(Resolution.Daily, "20181017 10:00", 10)]
        public void RefreshesUniverseChainOnDateChange(Resolution resolution, string dateTime, int expectedStartHour)
        {
            var startTime = Time.ParseDate(dateTime);
            Assert.AreEqual(expectedStartHour, startTime.Hour);
            var timeProvider = new ManualTimeProvider(startTime);

            var symbolUniverse = new TestDataQueueUniverseProvider(timeProvider);

            var canonicalSymbol = Symbols.SPY_Option_Chain;
            var request = GetRequest(canonicalSymbol, startTime, resolution);
            var underlyingSymbol = (request.Security as Option).Underlying.Symbol;
            using var underlying = new EnqueueableEnumerator<BaseData>();
            using var enumerator = new DataQueueOptionChainUniverseDataCollectionEnumerator(request, underlying, symbolUniverse, timeProvider);

            underlying.Enqueue(new Tick(timeProvider.GetUtcNow().ConvertFromUtc(request.Security.Exchange.TimeZone), underlyingSymbol, 9, 10));
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            var data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);
            Assert.IsFalse(enumerator.Current.IsFillForward);

            timeProvider.Advance(Time.OneSecond);
            underlying.Enqueue(new Tick(timeProvider.GetUtcNow().ConvertFromUtc(request.Security.Exchange.TimeZone), underlyingSymbol, 9, 10));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            // fill forwarding the chain selection, but time should advance
            Assert.AreEqual(data.Data.Single().Symbol, enumerator.Current.Data.Single().Symbol);
            Assert.IsTrue(enumerator.Current.EndTime > data.EndTime);
            Assert.IsTrue(enumerator.Current.IsFillForward);
            data = enumerator.Current;

            timeProvider.Advance(Time.OneMinute);
            underlying.Enqueue(new Tick(timeProvider.GetUtcNow().ConvertFromUtc(request.Security.Exchange.TimeZone), underlyingSymbol, 9, 10));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            // fill forwarding the chain selection, but time should advance
            Assert.AreEqual(data.Data.Single().Symbol, enumerator.Current.Data.Single().Symbol);
            Assert.IsTrue(enumerator.Current.EndTime > data.EndTime);
            Assert.IsTrue(enumerator.Current.IsFillForward);
            data = enumerator.Current;

            timeProvider.Advance(Time.OneDay);
            underlying.Enqueue(new Tick(timeProvider.GetUtcNow().ConvertFromUtc(request.Security.Exchange.TimeZone), underlyingSymbol, 9, 10));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            // selection should of have changed
            Assert.AreNotEqual(data.Data.Single().Symbol, enumerator.Current.Data.Single().Symbol);
            Assert.IsTrue(enumerator.Current.EndTime > data.EndTime);
            Assert.IsFalse(enumerator.Current.IsFillForward);
            data = enumerator.Current;
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);

            timeProvider.Advance(Time.OneMinute);
            underlying.Enqueue(new Tick(timeProvider.GetUtcNow().ConvertFromUtc(request.Security.Exchange.TimeZone), underlyingSymbol, 9, 10));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);
            // fill forwarding the chain selection, but time should advance
            Assert.AreEqual(data.Data.Single().Symbol, enumerator.Current.Data.Single().Symbol);
            Assert.IsTrue(enumerator.Current.EndTime > data.EndTime);
            Assert.IsTrue(enumerator.Current.IsFillForward);

            enumerator.Dispose();
            request.Universe.Dispose();
        }

        private static SubscriptionRequest GetRequest(Symbol canonicalSymbol, DateTime startTime, Resolution resolution)
        {
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(canonicalSymbol.ID.Market, canonicalSymbol, canonicalSymbol.SecurityType);
            var config = new SubscriptionDataConfig(
                typeof(ZipEntryName),
                canonicalSymbol,
                resolution,
                entry.DataTimeZone,
                entry.ExchangeHours.TimeZone,
                true,
                false,
                false,
                false,
                TickType.Quote,
                false,
                DataNormalizationMode.Raw
            );

            var algo = new AlgorithmStub();
            var underlying = algo.AddEquity("SPY");
            var option = algo.AddOption(underlying.Symbol);
            option.Underlying = underlying;

            var universeSettings = new UniverseSettings(resolution, 0, true, false, TimeSpan.Zero);
            using var universe = new OptionChainUniverse(option, universeSettings);
            return new SubscriptionRequest(true, universe, option, config, startTime, Time.EndOfTime);
        }

        private class TestDataQueueUniverseProvider : IDataQueueUniverseProvider
        {
            private readonly Symbol[] _symbolList1 = { Symbols.SPY_C_192_Feb19_2016 };
            private readonly Symbol[] _symbolList2 = { Symbols.SPY_P_192_Feb19_2016 };
            private readonly ITimeProvider _timeProvider;

            public int TotalLookupCalls { get; set; }

            public TestDataQueueUniverseProvider(ITimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
            }

            public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
            {
                TotalLookupCalls++;
                return _timeProvider.GetUtcNow().Date.Day >= 18 ? _symbolList2 : _symbolList1;
            }

            public bool CanPerformSelection() => true;
        }
    }
}
