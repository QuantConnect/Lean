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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveAuxiliaryDataEnumeratorTests
    {
        [TestCase(DataMappingMode.OpenInterest, "20130616", false)]
        [TestCase(DataMappingMode.FirstDayMonth, "20130602", false)]
        [TestCase(DataMappingMode.LastTradingDay, "20130623", false)]
        [TestCase(DataMappingMode.OpenInterest, "20130616", true)]
        [TestCase(DataMappingMode.FirstDayMonth, "20130602", true)]
        [TestCase(DataMappingMode.LastTradingDay, "20130623", true)]
        public void EmitsMappingEventsBasedOnCurrentMapFileAndTime(DataMappingMode dataMappingMode, string mappingDate, bool delayed)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.ES_Future_Chain,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false,
                dataMappingMode: dataMappingMode);
            var symbolMaps = new List<SubscriptionDataConfig.NewSymbolEventArgs>();
            config.NewSymbol += (sender, args) => symbolMaps.Add(args);
            var time = new DateTime(2013, 05, 28);
            var cache = new SecurityCache();

            cache.AddData(new Tick(time, config.Symbol, 20, 10));
            var timeProvider = new ManualTimeProvider(time);

            var futureTicker1 = "es vhle2yxr5blt";
            TestMapFileResolver.MapFile = new MapFile(Futures.Indices.SP500EMini, new[]
            {
                new MapFileRow(Time.BeginningOfTime, Futures.Indices.SP500EMini, Exchange.CME),
                new MapFileRow(new DateTime(2013,06,01), futureTicker1, Exchange.CME, DataMappingMode.FirstDayMonth),
                new MapFileRow(new DateTime(2013,06,15), futureTicker1, Exchange.CME, DataMappingMode.OpenInterest),
                new MapFileRow(new DateTime(2013,06,22), futureTicker1, Exchange.CME, DataMappingMode.LastTradingDay),
            });

            IEnumerator<BaseData> enumerator;
            Assert.IsTrue(LiveAuxiliaryDataEnumerator.TryCreate(config, timeProvider, cache, new TestMapFileProvider(), TestGlobals.FactorFileProvider, time, out enumerator));

            // get's mapped right away!
            Assert.AreEqual(futureTicker1.ToUpper(), config.MappedSymbol);

            Assert.AreEqual(1, symbolMaps.Count);
            Assert.AreEqual(Symbols.ES_Future_Chain, symbolMaps[0].Old);
            Assert.AreEqual(Futures.Indices.SP500EMini, symbolMaps[0].Old.ID.Symbol);
            Assert.AreEqual(Symbols.ES_Future_Chain, symbolMaps[0].New);
            Assert.AreEqual(futureTicker1.ToUpper(), symbolMaps[0].New.Underlying.ID.ToString());

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var expectedMappingDate = DateTime.ParseExact(mappingDate, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            if (delayed)
            {
                // we advance to the mapping date, without any new mapFile!
                timeProvider.Advance(expectedMappingDate.ConvertToUtc(config.ExchangeTimeZone) - timeProvider.GetUtcNow() + Time.LiveAuxiliaryDataOffset);
            }
            else
            {
                // just advance a day to show nothing happens until mapping time
                timeProvider.Advance(TimeSpan.FromDays(1));
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var futureTicker2 = "es vk2zrh843z7l";
            TestMapFileResolver.MapFile = new MapFile(Futures.Indices.SP500EMini, TestMapFileResolver.MapFile.Concat(
                new[]
                {
                    new MapFileRow(new DateTime(2013,09,01), futureTicker2, Exchange.CME, DataMappingMode.FirstDayMonth),
                    new MapFileRow(new DateTime(2013,09,14), futureTicker2, Exchange.CME, DataMappingMode.OpenInterest),
                    new MapFileRow(new DateTime(2013,09,21), futureTicker2, Exchange.CME, DataMappingMode.LastTradingDay),
                }));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            if (delayed)
            {
                // we got a new mapFile! advance the date and expect mapping to have happened
                timeProvider.Advance(TimeSpan.FromDays(1));
            }
            else
            {
                // we advance to the mapping date
                timeProvider.Advance(expectedMappingDate.ConvertToUtc(config.ExchangeTimeZone) - timeProvider.GetUtcNow() + Time.LiveAuxiliaryDataOffset);
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            Assert.AreEqual(2, symbolMaps.Count);
            Assert.AreEqual(Symbols.ES_Future_Chain, symbolMaps[1].Old);
            Assert.AreEqual(futureTicker1.ToUpper(), symbolMaps[1].Old.Underlying.ID.ToString());
            Assert.AreEqual(Symbols.ES_Future_Chain, symbolMaps[1].New);
            Assert.AreEqual(futureTicker2.ToUpper(), symbolMaps[1].New.Underlying.ID.ToString());

            Assert.AreEqual(futureTicker2.ToUpper(), config.MappedSymbol);

            Assert.AreEqual(futureTicker2.ToUpper(), (enumerator.Current as SymbolChangedEvent).NewSymbol);
            Assert.AreEqual(futureTicker1.ToUpper(), (enumerator.Current as SymbolChangedEvent).OldSymbol);
            Assert.AreEqual(config.Symbol, (enumerator.Current as SymbolChangedEvent).Symbol);
            Assert.AreEqual(timeProvider.GetUtcNow().Date, (enumerator.Current as SymbolChangedEvent).Time);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [Test]
        public void EmitsDelistingEventsBasedOnCurrentTime()
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY_C_192_Feb19_2016,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var delistingDate = config.Symbol.GetDelistingDate();
            var time = delistingDate.AddDays(-10);
            var cache = new SecurityCache();
            cache.AddData(new Tick(DateTime.UtcNow, config.Symbol, 20, 10));
            var timeProvider = new ManualTimeProvider(time);

            IEnumerator<BaseData> enumerator;
            Assert.IsTrue(LiveAuxiliaryDataEnumerator.TryCreate(config, timeProvider, cache, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider, config.Symbol.ID.Date, out enumerator));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // advance until delisting date, take into account 5 hour offset of NY + TradableDateOffset
            timeProvider.Advance(TimeSpan.FromDays(10));
            timeProvider.Advance(TimeSpan.FromHours(5));
            timeProvider.Advance(Time.LiveAuxiliaryDataOffset);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(DelistingType.Warning, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(config.Symbol, (enumerator.Current as Delisting).Symbol);
            Assert.AreEqual(delistingDate, (enumerator.Current as Delisting).Time);
            Assert.AreEqual(15, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // when the day ends the delisted event will pass through, respecting the offset
            timeProvider.Advance(TimeSpan.FromDays(1));

            cache.AddData(new Tick(DateTime.UtcNow, config.Symbol, 40, 20));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(DelistingType.Delisted, (enumerator.Current as Delisting).Type);
            Assert.AreEqual(config.Symbol, (enumerator.Current as Delisting).Symbol);
            Assert.AreEqual(delistingDate.AddDays(1), (enumerator.Current as Delisting).Time);
            Assert.AreEqual(30, (enumerator.Current as Delisting).Price);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EquityEmitsDelistingEventsBasedOnCurrentTime(bool delayed)
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var time = new DateTime(2013, 05, 28);
            var cache = new SecurityCache();

            cache.AddData(new Tick(time, config.Symbol, 20, 10));
            var timeProvider = new ManualTimeProvider(time);

            TestMapFileResolver.MapFile = new MapFile(config.Symbol.ID.Symbol, new[]
            {
                new MapFileRow(Time.BeginningOfTime, config.Symbol.ID.Symbol),
                new MapFileRow(Time.EndOfTime, config.Symbol.ID.Symbol),
            });

            IEnumerator<BaseData> enumerator;
            Assert.IsTrue(LiveAuxiliaryDataEnumerator.TryCreate(config, timeProvider, cache, new TestMapFileProvider(), TestGlobals.FactorFileProvider, time, out enumerator));

            // get's mapped right away!
            Assert.AreEqual("SPY", config.MappedSymbol);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var delistingDate = time.AddDays(2);
            var delistedMapFile = new MapFile(config.Symbol.ID.Symbol, new[]
            {
                new MapFileRow(Time.BeginningOfTime, config.Symbol.ID.Symbol),
                new MapFileRow(delistingDate, config.Symbol.ID.Symbol),
            });

            // just advance a day to show nothing happens until mapping time
            timeProvider.Advance(TimeSpan.FromDays(1));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            if (!delayed)
            {
                TestMapFileResolver.MapFile = delistedMapFile;
            }

            // we advance to the mapping date, without any new mapFile!
            timeProvider.Advance(delistingDate.ConvertToUtc(config.ExchangeTimeZone) - timeProvider.GetUtcNow() + Time.LiveAuxiliaryDataOffset);

            if (delayed)
            {
                // nothing happens
                Assert.IsTrue(enumerator.MoveNext());
                Assert.IsNull(enumerator.Current);

                TestMapFileResolver.MapFile = delistedMapFile;

                timeProvider.Advance(Time.OneDay);
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            var delisted = enumerator.Current as Delisting;
            Assert.IsNotNull(delisted);
            Assert.AreEqual(DelistingType.Warning, delisted.Type);

            if (!delayed)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.IsNull(enumerator.Current);

                // delisting passed
                timeProvider.Advance(TimeSpan.FromDays(1));
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            delisted = enumerator.Current as Delisting;
            Assert.IsNotNull(delisted);
            Assert.AreEqual(DelistingType.Delisted, delisted.Type);
        }

        private class TestMapFileProvider : IMapFileProvider
        {
            public void Initialize(IDataProvider dataProvider)
            {
            }

            public MapFileResolver Get(AuxiliaryDataKey auxiliaryDataKey)
            {
                return new TestMapFileResolver();
            }
        }

        private class TestMapFileResolver : MapFileResolver
        {
            public static MapFile MapFile { get; set; }
            public TestMapFileResolver()
                : base(new[] { MapFile })
            {
            }
        }
    }
}
