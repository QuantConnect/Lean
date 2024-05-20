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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class OptionChainUniverseSubscriptionEnumeratorFactoryTests
    {
        [Test]
        public void RefreshesOptionChainUniverseOnDateChange()
        {
            var startTime = new DateTime(2018, 10, 19, 10, 0, 0);
            var timeProvider = new ManualTimeProvider(startTime);

            var canonicalSymbol = Symbol.Create("SPY", SecurityType.Option, Market.USA, "?SPY");

            var quoteCurrency = new Cash(Currencies.USD, 0, 1);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, canonicalSymbol, SecurityType.Option);
            var config = new SubscriptionDataConfig(
                typeof(ZipEntryName),
                canonicalSymbol,
                Resolution.Minute,
                TimeZones.Utc,
                TimeZones.NewYork,
                true,
                false,
                false,
                false,
                TickType.Quote,
                false,
                DataNormalizationMode.Raw
            );

            var option = new Option(
                canonicalSymbol,
                exchangeHours,
                quoteCurrency,
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                null
            );

            var fillForwardResolution = Ref.CreateReadOnly(() => Resolution.Minute.ToTimeSpan());
            var symbolUniverse = new TestDataQueueUniverseProvider(timeProvider);
            EnqueueableEnumerator<BaseData> underlyingEnumerator = null;
            Func<SubscriptionRequest, IEnumerator<BaseData>> underlyingEnumeratorFunc =
                (req) =>
                {
                    underlyingEnumerator = new EnqueueableEnumerator<BaseData>();
                    return new LiveFillForwardEnumerator(
                        timeProvider,
                        underlyingEnumerator,
                        option.Exchange,
                        fillForwardResolution,
                        false,
                        Time.EndOfTime,
                        Resolution.Minute,
                        TimeZones.Utc, false);
                };
            var factory = new OptionChainUniverseSubscriptionEnumeratorFactory(underlyingEnumeratorFunc, symbolUniverse, timeProvider);

            var universeSettings = new UniverseSettings(Resolution.Minute, 0, true, false, TimeSpan.Zero);
            var universe = new OptionChainUniverse(option, universeSettings);
            var request = new SubscriptionRequest(true, universe, option, config, startTime, Time.EndOfTime);
            var enumerator = (DataQueueOptionChainUniverseDataCollectionEnumerator) factory.CreateEnumerator(request, TestGlobals.DataProvider);

            // 2018-10-19 10:00 AM UTC
            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            // 2018-10-19 10:01 AM UTC
            timeProvider.Advance(Time.OneMinute);

            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            var data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);
            Assert.IsNotNull(data.Underlying);

            // 2018-10-19 10:02 AM UTC
            timeProvider.Advance(Time.OneMinute);

            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);
            Assert.IsNotNull(data.Underlying);

            // 2018-10-19 10:03 AM UTC
            timeProvider.Advance(Time.OneMinute);

            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);
            Assert.IsNotNull(data.Underlying);

            // 2018-10-20 10:03 AM UTC
            timeProvider.Advance(Time.OneDay);

            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.Data.Count);
            Assert.IsNotNull(data.Underlying);

            // 2018-10-20 10:04 AM UTC
            timeProvider.Advance(Time.OneMinute);

            underlyingEnumerator.Enqueue(new Tick { Symbol = Symbols.SPY, Value = 280m });

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current;
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.Data.Count);
            Assert.IsNotNull(data.Underlying);

            enumerator.Dispose();
        }

        public class TestDataQueueUniverseProvider : IDataQueueUniverseProvider
        {
            private readonly Symbol[] _symbolList1 =
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 280m, new DateTime(2018, 10, 19))
            };
            private readonly Symbol[] _symbolList2 =
            {
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 280m, new DateTime(2018, 10, 19)),
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Put, 280m, new DateTime(2018, 10, 19))
            };

            private readonly ITimeProvider _timeProvider;

            public int TotalLookupCalls { get; set; }

            public TestDataQueueUniverseProvider(ITimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
            }

            public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
            {
                TotalLookupCalls++;

                return _timeProvider.GetUtcNow().Date.Day >= 20 ? _symbolList2 : _symbolList1;
            }

            public bool CanPerformSelection()
            {
                return true;
            }
        }
    }
}
