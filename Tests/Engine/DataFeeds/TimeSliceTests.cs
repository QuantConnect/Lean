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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class TimeSliceTests
    {
        private TimeSliceFactory _timeSliceFactory;
        [SetUp]
        public void SetUp()
        {
            _timeSliceFactory = new TimeSliceFactory(TimeZones.Utc);
        }

        [Test]
        public void HandlesTicks_ExpectInOrderWithNoDuplicates()
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(Tick),
                Symbols.EURUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            DateTime refTime = DateTime.UtcNow;

            Tick[] rawTicks = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(refTime.AddSeconds(i), Symbols.EURUSD, 1.3465m, 1.34652m))
                .ToArray();

            IEnumerable<TimeSlice> timeSlices = rawTicks.Select(t => _timeSliceFactory.Create(
                t.Time,
                new List<DataFeedPacket> { new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>() { t }) },
                SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>()));

            Tick[] timeSliceTicks = timeSlices.SelectMany(ts => ts.Slice.Ticks.Values.SelectMany(x => x)).ToArray();

            Assert.AreEqual(rawTicks.Length, timeSliceTicks.Length);
            for (int i = 0; i < rawTicks.Length; i++)
            {
                Assert.IsTrue(Compare(rawTicks[i], timeSliceTicks[i]));
            }
        }

        private bool Compare(Tick expected, Tick actual)
        {
            return expected.Time == actual.Time
                   && expected.BidPrice == actual.BidPrice
                   && expected.AskPrice == actual.AskPrice
                   && expected.Quantity == actual.Quantity;
        }

        [Test]
        public void HandlesMultipleCustomDataOfSameTypeWithDifferentSymbols()
        {
            var symbol1 = Symbol.Create("SCF/CBOE_VX1_EW", SecurityType.Base, Market.USA);
            var symbol2 = Symbol.Create("SCF/CBOE_VX2_EW", SecurityType.Base, Market.USA);

            var subscriptionDataConfig1 = new SubscriptionDataConfig(
                typeof(UnlinkedData), symbol1, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);
            var subscriptionDataConfig2 = new SubscriptionDataConfig(
                typeof(UnlinkedData), symbol2, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);

            var security1 = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig1,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var security2 = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig1,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var timeSlice = _timeSliceFactory.Create(DateTime.UtcNow,
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(security1, subscriptionDataConfig1, new List<BaseData> {new UnlinkedData { Symbol = symbol1, Time = DateTime.UtcNow.Date, Value = 15 } }),
                    new DataFeedPacket(security2, subscriptionDataConfig2, new List<BaseData> {new UnlinkedData { Symbol = symbol2, Time = DateTime.UtcNow.Date, Value = 20 } }),
                },
                SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());

            Assert.AreEqual(2, timeSlice.CustomData.Count);

            var data1 = timeSlice.CustomData[0].Data[0];
            var data2 = timeSlice.CustomData[1].Data[0];

            Assert.IsInstanceOf(typeof(UnlinkedData), data1);
            Assert.IsInstanceOf(typeof(UnlinkedData), data2);
            Assert.AreEqual(symbol1, data1.Symbol);
            Assert.AreEqual(symbol2, data2.Symbol);
            Assert.AreEqual(15, data1.Value);
            Assert.AreEqual(20, data2.Value);
        }

        [Test]
        public void FutureDataHasVolume()
        {
            var initialVolume = 100;
            var slices = GetSlices(Symbols.Fut_SPY_Mar19_2016, initialVolume).ToArray();

            for (var i = 0; i < 10; i++)
            {
                var chain = slices[i].FutureChains.FirstOrDefault().Value;
                var contract = chain.FirstOrDefault();
                var expected = (i + 1) * initialVolume;
                Assert.AreEqual(expected, contract.Volume);
            }
        }

        [Test]
        public void OptionsDataHasVolume()
        {
            var initialVolume = 150;
            var slices = GetSlices(Symbols.SPY_C_192_Feb19_2016, initialVolume).ToArray();

            for (var i = 0; i < 10; i++)
            {
                var chain = slices[i].OptionChains.FirstOrDefault().Value;
                var contract = chain.FirstOrDefault();
                var expected = (i + 1) * initialVolume;
                Assert.AreEqual(expected, contract.Volume);
            }
        }

        [Test]
        public void SuspiciousTicksAreNotAddedToConsolidatorUpdateData()
        {
            var symbol = Symbols.SPY;

            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(Tick), symbol, Resolution.Tick, TimeZones.Utc, TimeZones.Utc, true, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var timeSlice = _timeSliceFactory.Create(DateTime.UtcNow,
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>
                    {
                        new Tick(DateTime.UtcNow, symbol, 280, 0, 0),
                        new Tick(DateTime.UtcNow, symbol, 500, 0, 0) { Suspicious = true },
                        new Tick(DateTime.UtcNow, symbol, 281, 0, 0)
                    })
                },
                SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());

            Assert.AreEqual(1, timeSlice.ConsolidatorUpdateData.Count);

            var data = timeSlice.ConsolidatorUpdateData[0].Data;
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual(280, data[0].Value);
            Assert.AreEqual(281, data[1].Value);
        }

        private IEnumerable<Slice> GetSlices(Symbol symbol, int initialVolume)
        {
            var dataType = symbol.SecurityType.IsOption() ? typeof(OptionUniverse) : typeof(FutureUniverse);
            var subscriptionDataConfig = new SubscriptionDataConfig(dataType, symbol, Resolution.Second, TimeZones.Utc, TimeZones.Utc, true, true, false);
            var security = GetSecurity(subscriptionDataConfig);
            var refTime = DateTime.UtcNow;

            return Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    var time = refTime.AddSeconds(i);
                    var bid = new Bar(100, 100, 100, 100);
                    var ask = new Bar(110, 110, 110, 110);
                    var volume = (i + 1) * initialVolume;

                    var packets = new List<DataFeedPacket>();
                    var packet = new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>
                    {
                        new QuoteBar(time, symbol, bid, i*10, ask, (i + 1) * 11),
                        new TradeBar(time, symbol, 100, 100, 110, 106, volume)
                    });

                    if (symbol.SecurityType == SecurityType.Option)
                    {
                        var underlying = (security as Option).Underlying;
                        packets.Add(new DataFeedPacket(underlying, underlying.SubscriptionDataConfig, new List<BaseData>
                        {
                            new QuoteBar(time, underlying.Symbol, bid, i*10, ask, (i + 1) * 11),
                            new TradeBar(time, underlying.Symbol, 100, 100, 110, 106, volume)
                        }));
                    }

                    packets.Add(packet);

                    return _timeSliceFactory.Create(
                        time,
                        packets,
                        SecurityChangesTests.CreateNonInternal(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                        new Dictionary<Universe, BaseDataCollection>())
                        .Slice;
                });
        }

        private Security GetSecurity(SubscriptionDataConfig config)
        {
            if (config.Symbol.SecurityType == SecurityType.Option)
            {
                var option = new Option(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null);
                var underlyingConfig = new SubscriptionDataConfig(typeof(TradeBar), config.Symbol.Underlying, Resolution.Second,
                    TimeZones.Utc, TimeZones.Utc, true, true, false);
                var equity = new Equity(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    underlyingConfig,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null);
                option.Underlying = equity;

                return option;
            }

            if (config.Symbol.SecurityType == SecurityType.Future)
            {
                return new Future(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null);
            }

            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }
    }
}
