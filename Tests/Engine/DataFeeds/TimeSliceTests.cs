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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using QuandlFuture = QuantConnect.Algorithm.CSharp.QCUQuandlFutures.QuandlFuture;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class TimeSliceTests
    {
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
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            DateTime refTime = DateTime.UtcNow;

            Tick[] rawTicks = Enumerable
                .Range(0, 10)
                .Select(i => new Tick(refTime.AddSeconds(i), Symbols.EURUSD, 1.3465m, 1.34652m))
                .ToArray();

            IEnumerable<TimeSlice> timeSlices = rawTicks.Select(t => TimeSlice.Create(
                t.Time,
                TimeZones.Utc,
                new CashBook(),
                new List<DataFeedPacket> { new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>() { t }) },
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
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
                typeof(QuandlFuture), symbol1, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);
            var subscriptionDataConfig2 = new SubscriptionDataConfig(
                typeof(QuandlFuture), symbol2, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);

            var security1 = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig1,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var security2 = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig1,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var timeSlice = TimeSlice.Create(DateTime.UtcNow, TimeZones.Utc, new CashBook(),
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(security1, subscriptionDataConfig1, new List<BaseData> {new QuandlFuture { Symbol = symbol1, Time = DateTime.UtcNow.Date, Value = 15 } }),
                    new DataFeedPacket(security2, subscriptionDataConfig2, new List<BaseData> {new QuandlFuture { Symbol = symbol2, Time = DateTime.UtcNow.Date, Value = 20 } }),
                },
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());

            Assert.AreEqual(2, timeSlice.CustomData.Count);

            var data1 = timeSlice.CustomData[0].Data[0];
            var data2 = timeSlice.CustomData[1].Data[0];

            Assert.IsInstanceOf(typeof(QuandlFuture), data1);
            Assert.IsInstanceOf(typeof(QuandlFuture), data2);
            Assert.AreEqual(symbol1, data1.Symbol);
            Assert.AreEqual(symbol2, data2.Symbol);
            Assert.AreEqual(15, data1.Value);
            Assert.AreEqual(20, data2.Value);
        }

        [Test]
        public void HandlesMultipleCustomDataOfSameTypeSameSymbol()
        {
            var symbol = Symbol.Create("DFX", SecurityType.Base, Market.USA);

            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(DailyFx), symbol, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var refTime = DateTime.UtcNow;

            var timeSlice = TimeSlice.Create(refTime, TimeZones.Utc, new CashBook(),
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>
                    {
                        new DailyFx { Symbol = symbol, Time = refTime, Title = "Item 1" },
                        new DailyFx { Symbol = symbol, Time = refTime, Title = "Item 2" },
                    }),
                },
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());

            Assert.AreEqual(1, timeSlice.CustomData.Count);

            var data1 = timeSlice.CustomData[0].Data[0];
            var data2 = timeSlice.CustomData[0].Data[1];

            Assert.IsInstanceOf(typeof(DailyFx), data1);
            Assert.IsInstanceOf(typeof(DailyFx), data2);
            Assert.AreEqual(symbol, data1.Symbol);
            Assert.AreEqual(symbol, data2.Symbol);
            Assert.AreEqual("Item 1", ((DailyFx)data1).Title);
            Assert.AreEqual("Item 2", ((DailyFx)data2).Title);
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
        public void TimeSliceCreateDoesNotThrowNullReferanceWhenUnderlyingIsNull()
        {
            var optionSymbol = Symbol.Create("SVXY", SecurityType.Option, Market.USA);
            var underlyingSecurity = new Equity(optionSymbol.Underlying, SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc), new Cash("USD", 0, 1), SymbolProperties.GetDefault("USD"));
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(DailyFx), optionSymbol, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);

            var optionSecurity = new Option(optionSymbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash(CashBook.AccountCurrency, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault("USD")));

            var refTime = DateTime.UtcNow;
            var timeSlice = TimeSlice.Create(refTime, TimeZones.Utc, new CashBook(),
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(optionSecurity, subscriptionDataConfig, new List<BaseData>
                    {
                        new QuoteBar { Symbol = optionSymbol, Time = refTime, Value = 1, Ask = new Bar(1,1,1,1), Bid = new Bar(1,1,1,1) }
                    })
                },
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());
            Assert.AreEqual(timeSlice.SecurityChanges.Count, 0);
        }

        [Test]
        public void TimeSliceCreateDoesNotThrowNullReferanceWhenUnderlyingSecurityLastDataIsNull()
        {
            var optionSymbol = Symbol.Create("SVXY", SecurityType.Option, Market.USA);
            var underlyingSecurity = new Equity(optionSymbol.Underlying, SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc), new Cash("USD", 0, 1), SymbolProperties.GetDefault("USD"));
            var subscriptionDataConfig = new SubscriptionDataConfig(
                typeof(DailyFx), optionSymbol, Resolution.Daily, TimeZones.Utc, TimeZones.Utc, true, true, false, isCustom: true);

            var optionSecurity = new Option(optionSymbol,
                                            SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                            new Cash(CashBook.AccountCurrency, 0, 1m),
                                            new OptionSymbolProperties(SymbolProperties.GetDefault("USD")))
            { Underlying = underlyingSecurity };

            var refTime = DateTime.UtcNow;
            var timeSlice = TimeSlice.Create(refTime, TimeZones.Utc, new CashBook(),
                new List<DataFeedPacket>
                {
                    new DataFeedPacket(optionSecurity, subscriptionDataConfig, new List<BaseData>
                    {
                        new QuoteBar { Symbol = optionSymbol, Time = refTime, Value = 1, Ask = new Bar(1,1,1,1), Bid = new Bar(1,1,1,1) }
                    })
                },
                new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                new Dictionary<Universe, BaseDataCollection>());
            Assert.AreEqual(timeSlice.SecurityChanges.Count, 0);
        }

        private IEnumerable<Slice> GetSlices(Symbol symbol, int initialVolume)
        {
            var subscriptionDataConfig = new SubscriptionDataConfig(typeof(ZipEntryName), symbol, Resolution.Second, TimeZones.Utc, TimeZones.Utc, true, true, false);
            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), subscriptionDataConfig, new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var refTime = DateTime.UtcNow;

            return Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    var time = refTime.AddSeconds(i);
                    var bid = new Bar(100, 100, 100, 100);
                    var ask = new Bar(110, 110, 110, 110);
                    var volume = (i + 1) * initialVolume;

                    return TimeSlice.Create(
                        time,
                        TimeZones.Utc,
                        new CashBook(),
                        new List<DataFeedPacket>
                        {
                            new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>
                            {
                                new QuoteBar(time, symbol, bid, i*10, ask, (i + 1) * 11),
                                new TradeBar(time, symbol, 100, 100, 110, 106, volume)
                            }),
                        },
                        new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>()),
                        new Dictionary<Universe, BaseDataCollection>())
                        .Slice;
                });
        }
    }
}
