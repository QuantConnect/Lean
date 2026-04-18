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
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Python;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SliceTests
    {
        private readonly DateTime _dataTime = DateTime.UtcNow;

        [Test]
        public void AccessesByDataType()
        {
            var now = DateTime.UtcNow;
            var tradeBar = new TradeBar { Symbol = Symbols.SPY, Time = now };
            var unlinkedData = new UnlinkedData { Symbol = Symbols.SPY, Time = now };
            var quoteBar = new QuoteBar { Symbol = Symbols.SPY, Time = now };
            var tick = new Tick(now, Symbols.SPY, 1.1m, 2.1m) { TickType = TickType.Trade };
            var openInterest = new OpenInterest(now, Symbols.SPY, 1);
            var split = new Split(Symbols.SPY, now, 1, 1, SplitType.SplitOccurred);
            var delisting = new Delisting(Symbols.SPY, now, 1, DelistingType.Delisted);
            var marginInterest = new MarginInterestRate { Symbol = Symbols.SPY, Time = now, InterestRate = 0.08m };

            var slice = new Slice(now, new BaseData[] { quoteBar, tradeBar, unlinkedData, tick, split, delisting, openInterest, marginInterest }, now);

            Assert.AreEqual(slice.Get(typeof(TradeBar))[Symbols.SPY], tradeBar);
            Assert.AreEqual(slice.Get(typeof(UnlinkedData))[Symbols.SPY], unlinkedData);
            Assert.AreEqual(slice.Get(typeof(QuoteBar))[Symbols.SPY], quoteBar);
            Assert.AreEqual(slice.Get(typeof(Tick))[Symbols.SPY], tick);
            Assert.AreEqual(slice.Get(typeof(Split))[Symbols.SPY], split);
            Assert.AreEqual(slice.Get(typeof(Delisting))[Symbols.SPY], delisting);
            Assert.AreEqual(slice.Get(typeof(OpenInterest))[Symbols.SPY], openInterest);
            Assert.AreEqual(slice.Get(typeof(MarginInterestRate))[Symbols.SPY], marginInterest);
        }

        [Test]
        public void AccessesByDataTypeAndSymbol()
        {
            var now = DateTime.UtcNow;
            var tradeBar = new TradeBar { Symbol = Symbols.SPY, Time = now };
            var unlinkedData = new UnlinkedData { Symbol = Symbols.SPY, Time = now };
            var quoteBar = new QuoteBar { Symbol = Symbols.SPY, Time = now };
            var tick = new Tick(now, Symbols.SPY, 1.1m, 2.1m) { TickType = TickType.Trade };
            var openInterest = new OpenInterest(now, Symbols.SPY, 1);
            var split = new Split(Symbols.SPY, now, 1, 1, SplitType.SplitOccurred);
            var delisting = new Delisting(Symbols.SPY, now, 1, DelistingType.Delisted);
            var marginInterest = new MarginInterestRate { Symbol = Symbols.SPY, Time = now, InterestRate = 0.08m };

            var slice = new Slice(now, new BaseData[] { quoteBar, tradeBar, unlinkedData, tick, split, delisting, openInterest, marginInterest }, now);

            {
                Assert.IsTrue(slice.TryGet<TradeBar>(Symbols.SPY, out var foundTradeBar));
                Assert.AreEqual(foundTradeBar, tradeBar);
                Assert.IsTrue(slice.TryGet<UnlinkedData>(Symbols.SPY, out var foundUnlinkedData));
                Assert.AreEqual(foundUnlinkedData, unlinkedData);
                Assert.IsTrue(slice.TryGet<QuoteBar>(Symbols.SPY, out var foundQuoteBar));
                Assert.AreEqual(foundQuoteBar, quoteBar);
                Assert.IsTrue(slice.TryGet<Tick>(Symbols.SPY, out var foundTick));
                Assert.AreEqual(foundTick, tick);
                Assert.IsTrue(slice.TryGet<Split>(Symbols.SPY, out var foundSplit));
                Assert.AreEqual(foundSplit, split);
                Assert.IsTrue(slice.TryGet<Delisting>(Symbols.SPY, out var foundDelisting));
                Assert.AreEqual(foundDelisting, delisting);
                Assert.IsTrue(slice.TryGet<OpenInterest>(Symbols.SPY, out var foundOpenInterest));
                Assert.AreEqual(foundOpenInterest, openInterest);
                Assert.IsTrue(slice.TryGet<MarginInterestRate>(Symbols.SPY, out var foundMarginInterest));
                Assert.AreEqual(foundMarginInterest, marginInterest);

                Assert.IsFalse(slice.TryGet<TradeBar>(Symbols.AAPL, out _));
            }

            {
                Assert.IsTrue(slice.TryGet(typeof(TradeBar), Symbols.SPY, out var foundTradeBar));
                Assert.AreEqual(foundTradeBar, tradeBar);
                Assert.IsTrue(slice.TryGet(typeof(UnlinkedData), Symbols.SPY, out var foundUnlinkedData));
                Assert.AreEqual(foundUnlinkedData, unlinkedData);
                Assert.IsTrue(slice.TryGet(typeof(QuoteBar), Symbols.SPY, out var foundQuoteBar));
                Assert.AreEqual(foundQuoteBar, quoteBar);
                Assert.IsTrue(slice.TryGet(typeof(Tick), Symbols.SPY, out var foundTick));
                Assert.AreEqual(foundTick, tick);
                Assert.IsTrue(slice.TryGet(typeof(Split), Symbols.SPY, out var foundSplit));
                Assert.AreEqual(foundSplit, split);
                Assert.IsTrue(slice.TryGet(typeof(Delisting), Symbols.SPY, out var foundDelisting));
                Assert.AreEqual(foundDelisting, delisting);
                Assert.IsTrue(slice.TryGet(typeof(OpenInterest), Symbols.SPY, out var foundOpenInterest));
                Assert.AreEqual(foundOpenInterest, openInterest);
                Assert.IsTrue(slice.TryGet(typeof(MarginInterestRate), Symbols.SPY, out var foundMarginInterest));
                Assert.AreEqual(foundMarginInterest, marginInterest);

                Assert.IsFalse(slice.TryGet(typeof(TradeBar), Symbols.AAPL, out _));
            }
        }

        [Test]
        public void AccessesBaseBySymbol()
        {
            IndicatorDataPoint tick = new IndicatorDataPoint(Symbols.SPY, DateTime.Now, 1);
            Slice slice = new Slice(DateTime.Now, new[] { tick }, DateTime.Now);

            IndicatorDataPoint data = slice[tick.Symbol];

            Assert.AreEqual(tick, data);
        }

        [Test]
        public void AccessesTradeBarBySymbol()
        {
            TradeBar tradeBar = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { tradeBar }, DateTime.Now);

            TradeBar data = slice[tradeBar.Symbol];

            Assert.AreEqual(tradeBar, data);
        }

        [Test]
        public void EquitiesIgnoreQuoteBars()
        {
            var quoteBar = new QuoteBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            var slice = new Slice(DateTime.Now, new[] { quoteBar }, DateTime.Now);

            Assert.IsFalse(slice.HasData);
            Assert.IsTrue(slice.ToList().Count == 0);
            Assert.IsFalse(slice.ContainsKey(Symbols.SPY));
            Assert.Throws<KeyNotFoundException>(() => { var data = slice[Symbols.SPY]; });
            Assert.AreEqual(0, slice.Count);

            var tickQuoteBar = new Tick { Symbol = Symbols.SPY, Time = DateTime.Now, TickType = TickType.Quote };
            slice = new Slice(DateTime.Now, new[] { tickQuoteBar }, DateTime.Now);

            Assert.IsFalse(slice.HasData);
            Assert.IsTrue(slice.ToList().Count == 0);
            Assert.IsFalse(slice.ContainsKey(Symbols.SPY));
            Assert.Throws<KeyNotFoundException>(() => { var data = slice[Symbols.SPY]; });
            Assert.AreEqual(0, slice.Count);
        }

        [Test]
        public void AccessesTradeBarCollection()
        {
            TradeBar tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            TradeBar tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { tradeBar1, tradeBar2 }, DateTime.Now);

            TradeBars tradeBars = slice.Bars;
            Assert.AreEqual(2, tradeBars.Count);
        }

        [Test]
        public void AccessesTicksBySymbol()
        {
            Tick tick1 = new Tick { Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1m, Quantity = 2m };
            Tick tick2 = new Tick { Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1.1m, Quantity = 2.1m };
            Slice slice = new Slice(DateTime.Now, new[] { tick1, tick2 }, DateTime.Now);

            List<Tick> data = slice[tick1.Symbol];
            Assert.IsInstanceOf(typeof(List<Tick>), data);
            Assert.AreEqual(2, data.Count);
        }

        [Test]
        public void AccessesTicksCollection()
        {
            Tick tick1 = new Tick { Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1, Quantity = 2 };
            Tick tick2 = new Tick { Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1.1m, Quantity = 2.1m };
            Tick tick3 = new Tick { Time = DateTime.Now, Symbol = Symbols.AAPL, Value = 1, Quantity = 2 };
            Tick tick4 = new Tick { Time = DateTime.Now, Symbol = Symbols.AAPL, Value = 1.1m, Quantity = 2.1m };
            Slice slice = new Slice(DateTime.Now, new[] { tick1, tick2, tick3, tick4 }, DateTime.Now);

            Ticks ticks = slice.Ticks;
            Assert.AreEqual(2, ticks.Count);
            Assert.AreEqual(2, ticks[Symbols.SPY].Count);
            Assert.AreEqual(2, ticks[Symbols.AAPL].Count);
        }

        [Test]
        public void DifferentCollectionsAreCorrectlyGeneratedSameSymbol()
        {
            var quoteBar = new QuoteBar(DateTime.Now, Symbols.SPY,
                new Bar(3100, 3100, 3100, 3100), 0,
                new Bar(3101, 3101, 3101, 3101), 0,
                Time.OneMinute);
            var tradeBar = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            var slice = new Slice(DateTime.Now, new BaseData[] { quoteBar, tradeBar }, DateTime.Now);

            Assert.AreEqual(1, slice.QuoteBars.Count);
            Assert.AreEqual(1, slice.Bars.Count);

            Assert.AreEqual(1, slice.Get<QuoteBar>().Count);
            Assert.AreEqual(1, slice.Get<TradeBar>().Count);
        }

        [Test]
        public void AccessesCustomGenericallyByTypeOtherTypesPresent()
        {
            var tradeBar = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now };
            var slice = new Slice(DateTime.Now, new BaseData[] { unlinkedDataSpy, tradeBar }, DateTime.Now);

            var unlinkedData = slice.Get<UnlinkedData>();
            Assert.AreEqual(1, unlinkedData.Count);
        }

        [Test]
        public void AccessesCustomGenericallyByType()
        {
            var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now };
            var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now };
            var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

            var unlinkedData = slice.Get<UnlinkedData>();
            Assert.AreEqual(2, unlinkedData.Count);
        }

        [Test]
        public void AccessesTickGenericallyByType()
        {
            Tick TickSpy = new Tick { Symbol = Symbols.SPY, Time = DateTime.Now };
            Tick TickAapl = new Tick { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { TickSpy, TickAapl }, DateTime.Now);

            DataDictionary<Tick> TickData = slice.Get<Tick>();
            Assert.AreEqual(2, TickData.Count);
        }

        [Test]
        public void AccessesTradeBarGenericallyByType()
        {
            TradeBar TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            TradeBar TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now };
            Slice slice = new Slice(DateTime.Now, new[] { TradeBarSpy, TradeBarAapl }, DateTime.Now);

            DataDictionary<TradeBar> TradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(2, TradeBarData.Count);
        }

        [Test]
        public void AccessesGenericallyByTypeAndSymbol()
        {
            var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now };
            var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now };
            var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

            var unlinkedData = slice.Get<UnlinkedData>(Symbols.SPY);
            Assert.AreEqual(unlinkedDataSpy, unlinkedData);
        }

        [Test]
        public void MergeSlice()
        {
            var tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = _dataTime };
            var tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = _dataTime, Open = 23 };
            var quoteBar1 = new QuoteBar { Symbol = Symbols.SPY, Time = _dataTime };
            var tick1 = new Tick(_dataTime, Symbols.SPY, 1.1m, 2.1m) { TickType = TickType.Trade };
            var split1 = new Split(Symbols.SPY, _dataTime, 1, 1, SplitType.SplitOccurred);
            var dividend1 = new Dividend(Symbols.SPY, _dataTime, 1, 1);
            var delisting1 = new Delisting(Symbols.SPY, _dataTime, 1, DelistingType.Delisted);
            var symbolChangedEvent1 = new SymbolChangedEvent(Symbols.SPY, _dataTime, "SPY", "SP");
            var marginInterestRate1 = new MarginInterestRate { Time = _dataTime, Symbol = Symbols.SPY, InterestRate = 8 };
            var slice1 = new Slice(_dataTime, new BaseData[] { tradeBar1, tradeBar2,
                quoteBar1, tick1, split1, dividend1, delisting1, symbolChangedEvent1, marginInterestRate1
            }, _dataTime);

            var tradeBar3 = new TradeBar { Symbol = Symbols.AAPL, Time = _dataTime, Open = 24 };
            var tradeBar4 = new TradeBar { Symbol = Symbols.SBIN, Time = _dataTime };
            var tradeBar3_4 = new TradeBar { Symbol = Symbols.BTCEUR, Time = _dataTime };
            var quoteBar2 = new QuoteBar { Symbol = Symbols.SBIN, Time = _dataTime };
            var tick2 = new Tick(_dataTime, Symbols.SBIN, 1.1m, 2.1m) { TickType = TickType.Trade };
            var split2 = new Split(Symbols.SBIN, _dataTime, 1, 1, SplitType.SplitOccurred);
            var dividend2 = new Dividend(Symbols.SBIN, _dataTime, 1, 1);
            var delisting2 = new Delisting(Symbols.SBIN, _dataTime, 1, DelistingType.Delisted);
            var symbolChangedEvent2 = new SymbolChangedEvent(Symbols.SBIN, _dataTime, "SBIN", "BIN");
            var marginInterestRate2 = new MarginInterestRate { Time = _dataTime, Symbol = Symbols.SBIN, InterestRate = 18 };
            var slice2 = new Slice(_dataTime, new BaseData[] { tradeBar3, tradeBar4, tradeBar3_4,
                quoteBar2, tick2, split2, dividend2, delisting2, symbolChangedEvent2, marginInterestRate2
            }, _dataTime);

            slice1.MergeSlice(slice2);
            Assert.AreEqual(4, slice1.Bars.Count);
            Assert.AreEqual(2, slice1.QuoteBars.Count);
            Assert.AreEqual(2, slice1.Ticks.Count);
            Assert.AreEqual(2, slice1.Splits.Count);
            Assert.AreEqual(2, slice1.Dividends.Count);
            Assert.AreEqual(2, slice1.Delistings.Count);
            Assert.AreEqual(2, slice1.SymbolChangedEvents.Count);
            Assert.AreEqual(2, slice1.MarginInterestRates.Count);
        }

        [Test]
        public void CheckMergeUpdatePrivateAttributes()
        {
            var tradeBar0 = new TradeBar { Symbol = Symbols.BTCUSD, Time = _dataTime };
            var slice1 = new Slice(_dataTime, new BaseData[] { tradeBar0 }, _dataTime);
            var tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = _dataTime };
            var tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = _dataTime, Open = 23 };
            var slice2 = new Slice(_dataTime, new BaseData[] { tradeBar1, tradeBar2 }, _dataTime);

            slice1.MergeSlice(slice2);
            // Check private _data is updated
            Assert.AreEqual(3, slice1.Values.Count);

            var tradeBar3 = new TradeBar { Symbol = Symbols.AAPL, Time = _dataTime, Open = 24 };
            var tradeBar4 = new TradeBar { Symbol = Symbols.SBIN, Time = _dataTime };
            var tradeBar3_4 = new TradeBar { Symbol = Symbols.BTCEUR, Time = _dataTime };
            var slice3 = new Slice(_dataTime, new BaseData[] { tradeBar3, tradeBar4, tradeBar3_4 }, _dataTime);

            slice1.MergeSlice(slice3);

            // Should use first non Null value
            var testTradeBar = (TradeBar)slice1.Values.Where(datum => datum.DataType == MarketDataType.TradeBar && datum.Symbol.Value == "AAPL").Single();
            Assert.AreEqual(23, testTradeBar.Open);

            // Check private _rawDataList is updated
            Assert.AreEqual(5, slice1.Values.Count);
        }

        [Test]
        public void MergeTicks()
        {
            var tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = _dataTime };
            var tick1 = new Tick(_dataTime, Symbols.SPY, 1.1m, 2.1m) { TickType = TickType.Trade };
            var slice1 = new Slice(_dataTime, new BaseData[] { tradeBar1, tick1 }, _dataTime);
            //var Use List<tick>
            var ticks = new Ticks { { Symbols.MSFT, new List<Tick> { tick1 } } };
            var slice2 = new Slice(_dataTime, new List<BaseData>(), null, null, ticks, null, null, null, null, null, null, null, _dataTime);
            slice1.MergeSlice(slice2);
            Assert.AreEqual(2, slice1.Ticks.Count);

            // Should merge only when different
            var tick2 = new Tick(_dataTime, Symbols.MSFT, 1.1m, 2.1m) { TickType = TickType.Trade };
            var slice3 = new Slice(_dataTime, new BaseData[] { tradeBar1, tick2 }, _dataTime);
            slice2.MergeSlice(slice3);
            Assert.AreEqual(1, slice2.Ticks.Count);
        }

        [TestCase(null)]
        [TestCase("")]
        public void AccessingTicksParsedSaleConditinoDoesNotThrow(string saleCondition)
        {
            var tick1 = new Tick(_dataTime, Symbols.SPY, 1.1m, 2.1m) { TickType = TickType.Trade };
            tick1.SaleCondition = saleCondition;
            Assert.DoesNotThrow(() => tick1.ParsedSaleCondition.ToString());
        }

        [Test]
        public void MergeOptionsAndFuturesChain()
        {
            // Merge optionChains and FutureChains
            var optionChain1 = new OptionChains();
            var optionChain2 = new OptionChains();
            optionChain1.Add(Symbols.SPY, new OptionChain(Symbols.SPY, _dataTime));
            optionChain2.Add(Symbols.AAPL, new OptionChain(Symbols.SPY, _dataTime));
            var futuresChain1 = new FuturesChains();
            var futuresChain2 = new FuturesChains();
            futuresChain1.Add(Symbols.SPY, new FuturesChain(Symbols.SPY, _dataTime));
            futuresChain2.Add(Symbols.AAPL, new FuturesChain(Symbols.SPY, _dataTime));
            var slice4 = new Slice(_dataTime, new List<BaseData>(),
                                new TradeBars(_dataTime), new QuoteBars(),
                                new Ticks(), optionChain1,
                                futuresChain1, new Splits(),
                                new Dividends(_dataTime), new Delistings(),
                                new SymbolChangedEvents(), new MarginInterestRates(), _dataTime);
            var slice5 = new Slice(_dataTime, new List<BaseData>(),
                new TradeBars(_dataTime), new QuoteBars(),
                new Ticks(), optionChain2,
                futuresChain2, new Splits(),
                new Dividends(_dataTime), new Delistings(),
                new SymbolChangedEvents(), new MarginInterestRates(), _dataTime);
            slice4.MergeSlice(slice5);
            Assert.AreEqual(2, slice4.OptionChains.Count);
            Assert.AreEqual(2, slice4.FutureChains.Count);
        }

        [Test]
        public void MergeCustomData()
        {
            var tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = _dataTime };
            var tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = _dataTime, Open = 23 };
            var custom1 = new FxcmVolume { DataType = MarketDataType.Base, Symbol = Symbols.MSFT };
            var custom2 = new FxcmVolume { DataType = MarketDataType.Base, Symbol = Symbols.SBIN };
            var custom3 = new FxcmVolume { DataType = MarketDataType.Base, Symbol = Symbols.MSFT };
            var custom4 = new FxcmVolume { DataType = MarketDataType.Base, Symbol = Symbols.SBIN };
            var slice6 = new Slice(_dataTime, new BaseData[] { custom1, custom2, custom3, tradeBar2 }, _dataTime);
            var slice5 = new Slice(_dataTime, new BaseData[] { tradeBar1, custom4 }, _dataTime);
            slice5.MergeSlice(slice6);
            Assert.AreEqual(4, slice5.Values.Count);
            Assert.AreEqual(2, slice5.Values.Where(x => x.DataType == MarketDataType.Base).Count());
        }

        [Test]
        public void PythonGetCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                @"
from AlgorithmImports import *

def Test(slice):
    data = slice.Get(UnlinkedData)
    return data").GetAttr("Test");
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

                var data = test(slice);
                Assert.AreEqual(2, (int)data.Count);
                Assert.AreEqual(10, (int)data[Symbols.SPY].Value);
                Assert.AreEqual(11, (int)data[Symbols.AAPL].Value);
            }
        }

        [Test]
        public void PythonCustomDataPyObjectValue()
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    @"

from AlgorithmImports import *

class CustomDataTest(PythonData):
    def Reader(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        result[""TimeTest""] = datetime.strptime(""2022-05-05"", ""%Y-%m-%d"")
        return result

def Test(slice, symbol):
    data = slice.Get(CustomDataTest)
    return data[symbol][""TimeTest""]");
                var test = testModule.GetAttr("Test");

                var type = Extensions.CreateType(testModule.GetAttr("CustomDataTest"));
                var customDataTest = new PythonData(testModule.GetAttr("CustomDataTest")());
                var config = new SubscriptionDataConfig(type, Symbols.SPY, Resolution.Daily, DateTimeZone.Utc,
                    DateTimeZone.Utc, false, false, false, isCustom: true);
                var data1 = customDataTest.Reader(config, "something", DateTime.UtcNow, false);

                var slice = new Slice(DateTime.UtcNow, new[] { data1 }, DateTime.UtcNow);

                Assert.AreEqual(new DateTime(2022, 05, 05), (DateTime)test(slice, Symbols.SPY));
            }
        }

        [TestCase("reader", "get_source", "get")]
        [TestCase("Reader", "GetSource", "get")]
        [TestCase("reader", "get_source", "Get")]
        [TestCase("Reader", "GetSource", "Get")]
        public void PythonGetPythonCustomData(string reader, string getSource, string get)
        {
            using (Py.GIL())
            {
                dynamic testModule = PyModule.FromString("testModule",
                    $@"

from AlgorithmImports import *

class CustomDataTest(PythonData):
    def {reader}(self, config, line, date, isLiveMode):
        result = CustomDataTest()
        result.Symbol = config.Symbol
        result.Value = 10
        return result
    def {getSource}(config, date, isLiveMode):
        return None

class CustomDataTest2(PythonData):
    def {reader}(self, config, line, date, isLiveMode):
        result = CustomDataTest2()
        result.Symbol = config.Symbol
        result.Value = 11
        return result
    def {getSource}(config, date, isLiveMode):
        return None

def Test(slice):
    data = slice.{get}(CustomDataTest)
    return data");
                var test = testModule.GetAttr("Test");

                var type = Extensions.CreateType(testModule.GetAttr("CustomDataTest"));
                var customDataTest = new PythonData(testModule.GetAttr("CustomDataTest")());
                var config = new SubscriptionDataConfig(type, Symbols.SPY, Resolution.Daily, DateTimeZone.Utc,
                    DateTimeZone.Utc, false, false, false, isCustom: true);
                var data1 = customDataTest.Reader(config, "something", DateTime.UtcNow, false);

                var customDataTest2 = new PythonData(testModule.GetAttr("CustomDataTest2")());
                var config2 = new SubscriptionDataConfig(config, Extensions.CreateType(testModule.GetAttr("CustomDataTest2")));
                var data2 = customDataTest2.Reader(config2, "something2", DateTime.UtcNow, false);

                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.UtcNow, Value = 10 };
                var slice = new Slice(DateTime.UtcNow, new[] { unlinkedDataSpy, data2, data1 }, DateTime.UtcNow);

                var data = test(slice);
                Assert.AreEqual(1, (int)data.Count);
                Assert.AreEqual(10, (int)data[Symbols.SPY].Value);
            }
        }

        [Test]
        public void PythonEnumerationWorks()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    for dataPoint in slice:
        return dataPoint").GetAttr("Test");
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { unlinkedDataAapl }, DateTime.Now);

                var data = test(slice) as PyObject;
                var keyValuePair = data.As<KeyValuePair<Symbol, BaseData>>();
                Assert.IsNotNull(keyValuePair);
                Assert.AreEqual(11, keyValuePair.Value.Value);
            }
        }

        [Test]
        public void PythonGetBySymbolCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

def Test(slice):
    data = slice.Get(UnlinkedData)
    value = data[Symbols.AAPL].Value
    if value != 11:
        raise Exception('Unexpected value')").GetAttr("Test");
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetAndSymbolCustomData()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

def Test(slice):
    data = slice.Get(UnlinkedData, Symbols.AAPL)
    value = data.Value
    if value != 11:
        raise Exception('Unexpected value')").GetAttr("Test");
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    data = slice.Get(TradeBar)
    return data").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { unlinkedDataSpy, TradeBarAapl, unlinkedDataAapl, TradeBarSpy }, DateTime.Now);

                var data = test(slice);
                Assert.AreEqual(2, (int)data.Count);
                Assert.AreEqual(8, (int)data[Symbols.SPY].Value);
                Assert.AreEqual(9, (int)data[Symbols.AAPL].Value);
            }
        }

        [Test]
        public void PythonGetBySymbolOpenInterest()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

def Test(slice):
    data = slice.Get(OpenInterest)
    value = data[Symbols.AAPL].Value
    if value != 33:
        raise Exception('Unexpected value')").GetAttr("Test");
                var now = DateTime.UtcNow;
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = now, Value = 9 };
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = now, Value = 11 };
                var openInterest = new OpenInterest(now, Symbols.AAPL, 33);
                var slice = new Slice(now, new BaseData[] { unlinkedDataSpy, TradeBarAapl, unlinkedDataAapl, TradeBarSpy, openInterest }, now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetBySymbolTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

def Test(slice):
    data = slice.Get(TradeBar)
    value = data[Symbols.AAPL].Value
    if value != 9:
        raise Exception('Unexpected value')").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { unlinkedDataSpy, TradeBarAapl, unlinkedDataAapl, TradeBarSpy }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetAndSymbolTradeBar()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Tests import *

def Test(slice):
    data = slice.Get(TradeBar, Symbols.AAPL)
    value = data.Value
    if value != 9:
        raise Exception('Unexpected value')").GetAttr("Test");
                var TradeBarSpy = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 8 };
                var TradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { unlinkedDataSpy, TradeBarAapl, unlinkedDataAapl, TradeBarSpy }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate_IndexedLinkedData()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Data.Custom.IconicTypes import *
from QuantConnect.Logging import *

def Test(slice):
    data = slice.Get(IndexedLinkedData)
    count = 0
    for singleData in data:
        Log.Trace(str(singleData))
        count += 1
    if count != 2:
        raise Exception('Unexpected value')").GetAttr("Test");
                var indexedLinkedDataSpy = new IndexedLinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var tradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var indexedLinkedDataAapl = new IndexedLinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new BaseData[] { indexedLinkedDataSpy, tradeBarAapl, indexedLinkedDataAapl }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate_IndexedLinkedData_Empty()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Data.Custom.IconicTypes import *

def Test(slice):
    data = slice.Get(IndexedLinkedData)
    for singleData in data:
        raise Exception('Unexpected iteration')
    for singleData in data.Values:
        raise Exception('Unexpected iteration')
    data = slice.Get(IndexedLinkedData)
    for singleData in data:
        raise Exception('Unexpected iteration')
    for singleData in data.Values:
        raise Exception('Unexpected iteration')").GetAttr("Test");
                var tradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
                var slice = new Slice(DateTime.Now, new List<BaseData> { tradeBarAapl }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void PythonGetCustomData_Iterate()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    data = slice.Get(UnlinkedData)
    count = 0
    for singleData in data:
        count += 1
    if count != 2:
        raise Exception('Unexpected value')").GetAttr("Test");
                var unlinkedDataSpy = new UnlinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
                var unlinkedDataAapl = new UnlinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
                var slice = new Slice(DateTime.Now, new[] { unlinkedDataSpy, unlinkedDataAapl }, DateTime.Now);

                Assert.DoesNotThrow(() => test(slice));
            }
        }

        [Test]
        public void EnumeratorDoesNotThrowWithTicks()
        {
            var slice = new Slice(DateTime.Now, new[]
            {
                new Tick {Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1, Quantity = 2},
                new Tick{Time = DateTime.Now, Symbol = Symbols.SPY, Value = 1.1m, Quantity = 2.1m},
                new Tick{Time = DateTime.Now, Symbol = Symbols.AAPL, Value = 1, Quantity = 2},
                new Tick{Time = DateTime.Now, Symbol = Symbols.AAPL, Value = 1.1m, Quantity = 2.1m}
            }, DateTime.Now);

            #pragma warning disable CA1829
            Assert.AreEqual(4, slice.Count());
            #pragma warning restore CA1829
        }

        [Test]
        public void AccessesTradeBarAndQuoteBarForSameSymbol()
        {
            var tradeBar = new TradeBar(DateTime.Now, Symbols.BTCUSD,
                3000, 3000, 3000, 3000, 100, Time.OneMinute);

            var quoteBar = new QuoteBar(DateTime.Now, Symbols.BTCUSD,
                    new Bar(3100, 3100, 3100, 3100), 0,
                    new Bar(3101, 3101, 3101, 3101), 0,
                    Time.OneMinute);

            var tradeBars = new TradeBars { { Symbols.BTCUSD, tradeBar } };
            var quoteBars = new QuoteBars { { Symbols.BTCUSD, quoteBar } };

            var slice = new Slice(DateTime.Now, new List<BaseData>() { tradeBar, quoteBar }, tradeBars, quoteBars, null, null, null, null, null, null, null, null, DateTime.Now);

            var tradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(1, tradeBarData.Count);
            Assert.AreEqual(3000, tradeBarData[Symbols.BTCUSD].Close);

            var quoteBarData = slice.Get<QuoteBar>();
            Assert.AreEqual(1, quoteBarData.Count);
            Assert.AreEqual(3100, quoteBarData[Symbols.BTCUSD].Bid.Close);
            Assert.AreEqual(3101, quoteBarData[Symbols.BTCUSD].Ask.Close);

            slice = new Slice(DateTime.Now, new BaseData[] { tradeBar, quoteBar }, DateTime.Now);

            tradeBarData = slice.Get<TradeBar>();
            Assert.AreEqual(1, tradeBarData.Count);
            Assert.AreEqual(3000, tradeBarData[Symbols.BTCUSD].Close);

            quoteBarData = slice.Get<QuoteBar>();
            Assert.AreEqual(1, quoteBarData.Count);
            Assert.AreEqual(3100, quoteBarData[Symbols.BTCUSD].Bid.Close);
            Assert.AreEqual(3101, quoteBarData[Symbols.BTCUSD].Ask.Close);
        }

        [Test]
        public void PythonSlice_clear()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    slice.clear()").GetAttr("Test");

                Assert.That(() => test(GetSlice()),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<InvalidOperationException>(),
                    "Slice is read-only: cannot clear the collection");
            }
        }

        [Test]
        public void PythonSlice_popitem()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    slice.popitem()").GetAttr("Test");

                Assert.That(() => test(GetSlice()),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<NotSupportedException>(),
                    $"Slice is read-only: cannot pop the value for {Symbols.SPY} from the collection");
            }
        }

        [Test]
        public void PythonSlice_pop()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    slice.pop(symbol)").GetAttr("Test");

                Assert.That(() => test(GetSlice(), Symbols.SPY),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<InvalidOperationException>(),
                    $"Slice is read-only: cannot pop the value for {Symbols.SPY} from the collection");
            }
        }

        [Test]
        public void PythonSlice_pop_default()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol, default_value):
    slice.pop(symbol, default_value)").GetAttr("Test");

                Assert.That(() => test(GetSlice(), Symbols.SPY, null),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<InvalidOperationException>(),
                    $"Slice is read-only: cannot pop the value for {Symbols.SPY} from the collection");
            }
        }

        [Test]
        public void PythonSlice_update_fails()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    item = { symbol: 1 }
    slice.update(item)").GetAttr("Test");

                Assert.That(() => test(GetSlice(), Symbols.SPY),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<InvalidOperationException>(),
                    "Slice is read-only: cannot update the collection");
            }
        }

        [Test]
        public void PythonSlice_update_success()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol, bar):
    item = { symbol: bar }
    slice.Bars.update(item)").GetAttr("Test");

                var expected = new TradeBar();
                var pythonSlice = GetSlice();
                Assert.DoesNotThrow(() => test(pythonSlice, Symbols.SPY, expected));
                Assert.AreEqual(expected, pythonSlice.Bars[Symbols.SPY]);
            }
        }

        [Test]
        public void PythonSlice_contains()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
AddReference(""QuantConnect.Tests"")
from QuantConnect.Tests.Common.Data import *

def Test(slice, symbol):
    return symbol in slice").GetAttr("Test");

                bool result = false;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                Assert.IsTrue(result);

                result = false;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                Assert.IsTrue(result);
            }
        }

        [Test, Ignore("Performance test")]
        public void PythonSlice_performance()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
AddReference(""QuantConnect.Tests"")
from QuantConnect.Tests.Common.Data import *

def Test(slice, symbol):
    msg = '__contains__'

    if 'SPY' in slice:
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = 'SPY' in slice
    span1 = (datetime.now()-now).total_seconds()

    if slice.ContainsKey('SPY'):
        msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.ContainsKey('SPY')
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    msg += '\n\n__len__'

    if len(slice) > 0:
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = len(slice)
    span1 = (datetime.now()-now).total_seconds()

    if slice.Count > 0:
        msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.Count
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    msg += '\n\nkeys()'

    if len(slice.keys()) > 0:
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.keys()
    span1 = (datetime.now()-now).total_seconds()

    if len(slice.Keys) > 0:
        msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.Keys
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    msg += '\n\nvalues()'

    if len(slice.values()) > 0:
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.values()
    span1 = (datetime.now()-now).total_seconds()

    if len(slice.Values) > 0:
        msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.Values
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    msg += '\n\nget()'

    if slice.get(symbol):
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.get(symbol)
    span1 = (datetime.now()-now).total_seconds()

    dummy = None
    if slice.TryGetValue(symbol, dummy):
        msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = slice.TryGetValue(symbol, dummy)
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    msg += '\n\nitems()'

    if slice.items():
        msg += ' Py'
    now = datetime.now()
    for i in range(0,1000000):
        result = list(slice.items())
    span1 = (datetime.now()-now).total_seconds()

    msg += ' C#\n'
    now = datetime.now()
    for i in range(0,1000000):
        result = [x for x in slice]
    span2 = (datetime.now()-now).total_seconds()

    msg += f'Py: {span1}\nC#: {span2}\nRatio: {span1/span2}'

    return msg").GetAttr("Test");

                var message = string.Empty;
                Assert.DoesNotThrow(() => message = test(GetSlice(), Symbols.SPY));

                Assert.Ignore(message);
            }
        }

        [Test]
        public void PythonSlice_len()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
AddReference(""QuantConnect.Tests"")
from QuantConnect.Tests.Common.Data import *

def Test(slice, symbol):
    return len(slice)").GetAttr("Test");

                var result = -1;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                Assert.AreEqual(2, result);

                result = -1;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                Assert.AreEqual(2, result);
            }
        }

        [Test]
        public void PythonSlice_copy()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    copy = slice.copy()
    return ', '.join([f'{k}: {v.Value}' for k,v in copy.items()])").GetAttr("Test");

                var result = string.Empty;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                Assert.AreEqual("SPY R735QTJ8XC9X: 10.0, AAPL R735QTJ8XC9X: 11.0", result);
            }
        }

        [Test]
        public void PythonSlice_items()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    return ', '.join([f'{k}: {v.Value}' for k,v in slice.items()])").GetAttr("Test");

                var result = string.Empty;
                Assert.DoesNotThrow(() => result = test(GetSlice()));
                Assert.AreEqual("SPY R735QTJ8XC9X: 10.0, AAPL R735QTJ8XC9X: 11.0", result);
            }
        }


        [Test]
        public void PythonSlice_keys()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    return slice.keys()").GetAttr("Test");

                var slice = GetSlice();
                var result = new List<Symbol>();
                Assert.DoesNotThrow(() => result = test(slice));
                foreach (var key in slice.Keys)
                {
                    Assert.IsTrue(result.Contains(key));
                }
            }
        }

        [Test]
        public void PythonSlice_values()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice):
    return slice.values()").GetAttr("Test");

                var slice = GetSlice();
                var result = new List<BaseData>();
                Assert.DoesNotThrow(() => result = test(slice));
                foreach (var value in slice.Values)
                {
                    Assert.IsTrue(result.Contains(value));
                }
            }
        }

        [Test]
        public void PythonSlice_fromkeys()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, keys):
    newDict = slice.fromkeys(keys)
    return ', '.join([f'{k}: {v.Value}' for k,v in newDict.items()])").GetAttr("Test");

                var result = string.Empty;
                Assert.DoesNotThrow(() => result = test(GetSlice(), new[] { Symbols.SPY }));
                Assert.AreEqual("SPY R735QTJ8XC9X: 10.0", result);
            }
        }

        [Test]
        public void PythonSlice_fromkeys_default()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, keys, default_value):
    newDict = slice.fromkeys(keys, default_value)
    return ', '.join([f'{k}: {v.Value}' for k,v in newDict.items()])").GetAttr("Test");

                var result = string.Empty;
                Assert.DoesNotThrow(() => result = test(GetSlice(), new[] { Symbols.EURUSD }, new Tick()));
                Assert.AreEqual("EURUSD 8G: 0.0", result);
            }
        }

        [Test]
        public void PythonSlice_get_success()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    return slice.get(symbol)").GetAttr("Test");

                var pythonSlice = GetSlice();
                dynamic expected = pythonSlice[Symbols.SPY];
                PyObject result = null;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                BaseData actual;
                Assert.IsTrue(result.TryConvert(out actual));
                Assert.AreEqual(expected.Symbol, actual.Symbol);
                Assert.AreEqual(expected.Value, actual.Value);
            }
        }

        [Test]
        public void PythonSlice_get_default()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol, default_value):
    return slice.get(symbol, default_value)").GetAttr("Test");

                var pythonSlice = GetSlice();
                var expected = new QuoteBar { Symbol = Symbols.EURUSD, Time = DateTime.Now, Value = 9 };
                PyObject result = null;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.EURUSD, expected));
                BaseData actual;
                Assert.IsTrue(result.TryConvert(out actual));
                Assert.AreEqual(expected.Symbol, actual.Symbol);
                Assert.AreEqual(expected.Value, actual.Value);
            }
        }

        [Test]
        public void PythonSlice_get_NoneIfKeyNotFound()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    return slice.get(symbol)").GetAttr("Test");

                Assert.IsNull(test(GetSlice(), Symbols.EURUSD));
            }
        }

        [Test]
        public void PythonSlice_setdefault_success()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    return slice.setdefault(symbol)").GetAttr("Test");

                var pythonSlice = GetSlice();
                dynamic expected = pythonSlice[Symbols.SPY];
                PyObject result = null;
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY));
                BaseData actual;
                Assert.IsTrue(result.TryConvert(out actual));
                Assert.AreEqual(expected.Symbol, actual.Symbol);
                Assert.AreEqual(expected.Value, actual.Value);
            }
        }

        [Test]
        public void PythonSlice_setdefault_default_success()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol, default_value):
    return slice.setdefault(symbol, default_value)").GetAttr("Test");

                var value = new Tick();
                var pythonSlice = GetSlice();
                dynamic expected = pythonSlice[Symbols.SPY];
                PyObject result = null;

                // Since SPY is found, no need to set the default. Therefore it does not throw.
                Assert.DoesNotThrow(() => result = test(GetSlice(), Symbols.SPY, value));
                BaseData actual;
                Assert.IsTrue(result.TryConvert(out actual));
                Assert.AreEqual(expected.Symbol, actual.Symbol);
                Assert.AreEqual(expected.Value, actual.Value);
            }
        }

        [Test]
        public void PythonSlice_setdefault_keynotfound()
        {
            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *

def Test(slice, symbol):
    return slice.setdefault(symbol)").GetAttr("Test");

                var symbol = Symbols.EURUSD;
                Assert.That(() => test(GetSlice(), symbol),
                    Throws.InstanceOf<ClrBubbledException>().With.InnerException.InstanceOf<KeyNotFoundException>(),
                    $"Slice is read-only: cannot set default value to  for {symbol}");
            }
        }

        [TestCaseSource(nameof(PushThroughWorksWithDifferentTypesTestCases))]
        public void PushThroughWorksWithDifferentTypes(Slice slice, Type dataType, decimal expectedValue)
        {
            decimal valuePushed = default;

            var action = new Action<IBaseData>(data => { valuePushed = data.Value; });

            var slices = new List<Slice>(){ slice };

            slices.PushThrough(action, dataType);
            Assert.AreEqual(expectedValue, valuePushed);
        }

        private Slice GetSlice()
        {
            SymbolCache.Clear();
            var indexedLinkedDataSpy = new IndexedLinkedData { Symbol = Symbols.SPY, Time = DateTime.Now, Value = 10 };
            var tradeBarAapl = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 9 };
            var indexedLinkedDataAapl = new IndexedLinkedData { Symbol = Symbols.AAPL, Time = DateTime.Now, Value = 11 };
            return new Slice(DateTime.Now, new BaseData[] { indexedLinkedDataSpy, tradeBarAapl, indexedLinkedDataAapl }, DateTime.Now);
        }

        public static object[] PushThroughWorksWithDifferentTypesTestCases =
        {
            new object[] {new Slice(
                    new DateTime(2013, 10, 3),
                    new List<BaseData>(),
                    new TradeBars(),
                    new QuoteBars() { new QuoteBar() { Symbol = Symbols.IBM, Value = 100m } },
                    new Ticks(),
                    new OptionChains(),
                    new FuturesChains(),
                    new Splits(),
                    new Dividends(),
                    new Delistings(),
                    new SymbolChangedEvents(),
                    new MarginInterestRates(),
                    DateTime.UtcNow), typeof(QuoteBar), 100m},
            new object[] {new Slice(
                    new DateTime(2013, 10, 3),
                    new List<BaseData>(),
                    new TradeBars() { new TradeBar() { Symbol = Symbols.IBM, Value = 100m } },
                    new QuoteBars(),
                    new Ticks(),
                    new OptionChains(),
                    new FuturesChains(),
                    new Splits(),
                    new Dividends(),
                    new Delistings(),
                    new SymbolChangedEvents(),
                    new MarginInterestRates(),
                    DateTime.UtcNow), typeof(TradeBar), 100m},
            new object[] {new Slice(
                    new DateTime(2013, 10, 3),
                    new List<BaseData>(),
                    new TradeBars(),
                    new QuoteBars(),
                    new Ticks() { { Symbols.IBM, new Tick() { Value = 100m } } },
                    new OptionChains(),
                    new FuturesChains(),
                    new Splits(),
                    new Dividends(),
                    new Delistings(),
                    new SymbolChangedEvents(),
                    new MarginInterestRates(),
                    DateTime.UtcNow), typeof(Tick), 100m},
            new object[] {new Slice(
                    new DateTime(2013, 10, 3),
                    new List<BaseData>() { new TradeBar() { Symbol = Symbols.IBM, Value = 100m } },
                    new TradeBars(),
                    new QuoteBars(),
                    new Ticks(),
                    new OptionChains(),
                    new FuturesChains(),
                    new Splits(),
                    new Dividends(),
                    new Delistings(),
                    new SymbolChangedEvents(),
                    new MarginInterestRates(),
                    DateTime.UtcNow), null, 100m},
            new object[] {new Slice(
                    new DateTime(2013, 10, 3),
                    new List<BaseData>() { new CustomData() { Symbol = Symbols.IBM, Value = 100m } },
                    new TradeBars(),
                    new QuoteBars(),
                    new Ticks(),
                    new OptionChains(),
                    new FuturesChains(),
                    new Splits(),
                    new Dividends(),
                    new Delistings(),
                    new SymbolChangedEvents(),
                    new MarginInterestRates(),
                    DateTime.UtcNow), typeof(CustomData), 100m}
        };
    }

    public class PublicArrayTest
    {
        public int[] items { get; set; }

        public PublicArrayTest()
        {
            items = new int[5] { 0, 1, 2, 3, 4 };
        }
    }

    public class CustomData: BaseData
    {
    }
}
