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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using ProtoBuf;
using QuantConnect.Data;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ProtobufSerializationTests
    {
        private static readonly Dictionary<Type, BaseData> _iconicInstances = new Dictionary<Type, BaseData>
        {
            { typeof(IndexedLinkedData), new IndexedLinkedData { Count = 1024 } },
            { typeof(IndexedLinkedData2), new IndexedLinkedData2 { Count = 2048 } },
            { typeof(LinkedData), new LinkedData { Count = 4096 } },
            { typeof(UnlinkedData), new UnlinkedData { Ticker = "ABCDEF" } },
            { typeof(UnlinkedDataTradeBar), new UnlinkedDataTradeBar
                {
                    Open = 10m,
                    High = 11m,
                    Low = 9m,
                    Close = 10.99m,
                    Volume = 9999m
                }
            }
        };
        
        [TestCase(typeof(IndexedLinkedData), true)]
        [TestCase(typeof(IndexedLinkedData2), true)]
        [TestCase(typeof(LinkedData), true)]
        [TestCase(typeof(UnlinkedData), false)]
        [TestCase(typeof(UnlinkedDataTradeBar), false)]
        public void SerializeRoundTripIconicDataTypes(Type baseDataType, bool hasUnderlyingSymbol)
        {
            var item = CreateNewInstance(baseDataType, hasUnderlyingSymbol);
            var serialized = item.ProtobufSerialize(new Guid());
            
            using (var stream = new MemoryStream(serialized))
            {
                var deserialized = Serializer.Deserialize<IEnumerable<BaseData>>(stream).Single();
                AssertAreEqual(item, deserialized);
            }
        }
        
        [Test]
        public void SymbolRoundTrip()
        {
            var symbol = Symbols.AAPL;

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, symbol);

                stream.Position = 0;

                var result = Serializer.Deserialize<Symbol>(stream);

                Assert.AreEqual(symbol, result);
                Assert.AreEqual(symbol.GetHashCode(), result.GetHashCode());
            }
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void TickListSerializationRoundTrip(int tickCount)
        {
            var time = DateTime.UtcNow;
            var ticks = new List<Tick>();
            for (int i = 0; i < tickCount; i++)
            {
                var tick = new Tick
                {
                    Symbol = Symbols.AAPL,
                    AskPrice = i,
                    AskSize = i,
                    Time = time + TimeSpan.FromMilliseconds(i),
                    Quantity = i,
                    DataType = MarketDataType.Tick,
                    Exchange = "NASDAQ",
                    SaleCondition = "VerySold",
                    TickType = TickType.Quote,
                    Value = i,
                    BidPrice = i,
                    BidSize = i
                };
                ticks.Add(tick);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var serializedTick = ticks.ProtobufSerialize(new Guid());
            stopwatch.Stop();

            Log.Trace($"Took {stopwatch.ElapsedMilliseconds}ms. TickCount : {tickCount}.");

            // verify its correct
            using (var stream = new MemoryStream(serializedTick))
            {
                var results = Serializer.Deserialize<List<Tick>>(stream);

                Assert.AreEqual(tickCount, results.Count);

                for (int i = 0; i < tickCount; i++)
                {
                    var result = results[i];
                    Assert.AreEqual(i, result.AskPrice);
                    Assert.AreEqual(i, result.AskSize);
                    Assert.AreEqual(time + TimeSpan.FromMilliseconds(i), result.Time);
                    Assert.AreEqual(i, result.Quantity);
                    Assert.AreEqual(MarketDataType.Tick, result.DataType);
                    Assert.AreEqual("NASDAQ", result.Exchange);
                    Assert.IsNull(result.SaleCondition);
                    Assert.AreEqual(TickType.Quote, result.TickType);
                    Assert.AreEqual(time + TimeSpan.FromMilliseconds(i), result.EndTime);
                    Assert.AreEqual(i, result.Value);
                    Assert.AreEqual(i, result.BidPrice);
                    Assert.AreEqual(i, result.BidSize);
                    Assert.IsNull(result.Symbol);
                }
            }
        }

        [Test]
        public void OpenInterestSerializationRoundTrip()
        {
            var openInterest = new OpenInterest(DateTime.UtcNow, Symbols.AAPL, 10);

            var serializedTick = openInterest.ProtobufSerialize(new Guid());

            // verify its correct
            using (var stream = new MemoryStream(serializedTick))
            {
                var result = (Tick)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.IsNull(result.Symbol);
                Assert.AreEqual(openInterest.Time, result.Time);
                Assert.AreEqual(openInterest.EndTime, result.EndTime);
                Assert.AreEqual(openInterest.Value, result.Value);
            }
        }

        [Test]
        public void TickSerializationRoundTrip()
        {
            var tick = new Tick
            {
                Symbol = Symbols.AAPL,
                AskPrice = 10,
                AskSize = 10,
                Time = DateTime.UtcNow,
                Quantity = 10,
                DataType = MarketDataType.Tick,
                Exchange = "NASDAQ",
                SaleCondition = "VerySold",
                TickType = TickType.Quote,
                EndTime = DateTime.UtcNow,
                Value = 10,
                BidPrice = 100,
                BidSize = 100
            };

            var serializedTick = tick.ProtobufSerialize(new Guid());

            // verify its correct
            using (var stream = new MemoryStream(serializedTick))
            {
                var result = (Tick) Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(tick.AskPrice, result.AskPrice);
                Assert.AreEqual(tick.AskSize, result.AskSize);
                Assert.AreEqual(tick.Time, result.Time);
                Assert.AreEqual(tick.Quantity, result.Quantity);
                Assert.AreEqual(tick.DataType, result.DataType);
                Assert.AreEqual("NASDAQ", result.Exchange);
                Assert.IsNull(result.SaleCondition);
                Assert.AreEqual(tick.TickType, result.TickType);
                Assert.AreEqual(tick.EndTime, result.EndTime);
                Assert.AreEqual(tick.Value, result.Value);
                Assert.AreEqual(tick.BidPrice, result.BidPrice);
                Assert.AreEqual(tick.BidSize, result.BidSize);
            }
        }

        [Test]
        public void TradeBarSerializationRoundTrip()
        {
            var tradeBar = new TradeBar
            {
                Symbol = Symbols.AAPL,
                Volume = 10,
                Time = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Value = 10,
                Close = 10,
                High = 100,
                Low = 100,
                Open = 100,
                Period = TimeSpan.FromMinutes(1)
            };

            var serializedTradeBar = tradeBar.ProtobufSerialize(new Guid());
            using (var stream = new MemoryStream(serializedTradeBar))
            {
                // verify its correct
                var result = (TradeBar) Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(tradeBar.Time, result.Time);
                Assert.AreEqual(tradeBar.DataType, result.DataType);
                Assert.AreEqual(tradeBar.EndTime, result.EndTime);
                Assert.AreEqual(tradeBar.Value, result.Value);
                Assert.AreEqual(tradeBar.Volume, result.Volume);
                Assert.AreEqual(tradeBar.Close, result.Close);
                Assert.AreEqual(tradeBar.High, result.High);
                Assert.AreEqual(tradeBar.Low, result.Low);
                Assert.AreEqual(tradeBar.Open, result.Open);
                Assert.AreEqual(tradeBar.Period, result.Period);
            }
        }

        [Test]
        public void QuoteBarSerializationRoundTrip()
        {
            var quoteBar = new QuoteBar
            {
                Symbol = Symbols.AAPL,
                Time = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Value = 10,
                LastAskSize = 10,
                LastBidSize = 100,
                Ask = new Bar(1, 2, 3, 4),
                Bid = new Bar(11, 22, 33, 44),
                Period = TimeSpan.FromMinutes(1)
            };

            var serializedQuoteBar = quoteBar.ProtobufSerialize(new Guid());
            using (var stream = new MemoryStream(serializedQuoteBar))
            {
                // verify its correct
                var result = (QuoteBar)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(quoteBar.Time, result.Time);
                Assert.AreEqual(quoteBar.DataType, result.DataType);
                Assert.AreEqual(quoteBar.EndTime, result.EndTime);
                Assert.AreEqual(quoteBar.Value, result.Value);
                Assert.AreEqual(quoteBar.Close, result.Close);
                Assert.AreEqual(quoteBar.High, result.High);
                Assert.AreEqual(quoteBar.Low, result.Low);
                Assert.AreEqual(quoteBar.Open, result.Open);
                Assert.AreEqual(quoteBar.Period, result.Period);

                Assert.AreEqual(quoteBar.Ask.Close, result.Ask.Close);
                Assert.AreEqual(quoteBar.Ask.High, result.Ask.High);
                Assert.AreEqual(quoteBar.Ask.Low, result.Ask.Low);
                Assert.AreEqual(quoteBar.Ask.Open, result.Ask.Open);

                Assert.AreEqual(quoteBar.Bid.Close, result.Bid.Close);
                Assert.AreEqual(quoteBar.Bid.High, result.Bid.High);
                Assert.AreEqual(quoteBar.Bid.Low, result.Bid.Low);
                Assert.AreEqual(quoteBar.Bid.Open, result.Bid.Open);
            }
        }

        [Test]
        public void DividendRoundTrip()
        {
            var dividend = new Dividend
            {
                DataType = MarketDataType.Auxiliary,
                Distribution = 0.5m,
                ReferencePrice = decimal.MaxValue - 10000m,

                Symbol = Symbols.AAPL,
                Time = DateTime.UtcNow,
                Value = 0.5m
            };

            var serializedDividend = dividend.ProtobufSerialize(new Guid());
            using (var stream = new MemoryStream(serializedDividend))
            {
                var result = (Dividend)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(dividend.DataType, result.DataType);
                Assert.AreEqual(dividend.Distribution, result.Distribution);
                Assert.AreEqual(dividend.ReferencePrice, result.ReferencePrice);
                Assert.AreEqual(dividend.Time, result.Time);
                Assert.AreEqual(dividend.EndTime, result.EndTime);
                Assert.AreEqual(dividend.Value, result.Value);
            }
        }

        [Test]
        public void SplitRoundTrip()
        {
            var split = new Split(Symbols.AAPL, DateTime.UtcNow, decimal.MaxValue, decimal.MinValue, SplitType.SplitOccurred);

            var serializedSplit = split.ProtobufSerialize(new Guid());
            using (var stream = new MemoryStream(serializedSplit))
            {
                var result = (Split)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(split.Type, result.Type);
                Assert.AreEqual(split.DataType, result.DataType);
                Assert.AreEqual(split.SplitFactor, result.SplitFactor);
                Assert.AreEqual(split.ReferencePrice, result.ReferencePrice);
                Assert.AreEqual(split.Time, result.Time);
                Assert.AreEqual(split.EndTime, result.EndTime);
                Assert.AreEqual(split.Value, result.Value);
            }
        }

        [Test, Ignore("Performance test")]
        public void SpeedTest()
        {
            var symbols = new List<Symbol>
            {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda),
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase),
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex),
                Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM),
                Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 1, DateTime.UtcNow),
                Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, DateTime.UtcNow)
            };

            var now = DateTime.UtcNow;
            var ticks = new List<Tick>();
            for (var i = 0; i < 10000; i++)
            {
                foreach (var symbol in symbols)
                {
                    ticks.Add(new Tick
                    {
                        Symbol = symbol,
                        AskPrice = i * 10,
                        AskSize = i * 10,
                        Time = now,
                        Quantity = 10,
                        DataType = MarketDataType.Tick,
                        Exchange = "Pinocho",
                        SaleCondition = "VerySold",
                        TickType = TickType.Quote,
                        EndTime = now,
                        Value = i * 10,
                        BidPrice = i * 100,
                        BidSize = i * 100
                    });
                }
            }

            var guid = new Guid();
            {
                // warmup
                var serialized = ticks.ProtobufSerialize(guid);
                using (var stream = new MemoryStream(serialized))
                {
                    // verify its correct
                    var result = Serializer.Deserialize<List<Tick>>(stream);
                }

                var start = DateTime.UtcNow;
                for (var i = 0; i < 10; i++)
                {
                    serialized = ticks.ProtobufSerialize(guid);
                }
                var end = DateTime.UtcNow;
                Log.Trace($"PROTO BUF TOOK {end - start}");
            }

            {
                // warmup
                var serialized = JsonConvert.SerializeObject(ticks);

                var start = DateTime.UtcNow;
                for (var i = 0; i < 10; i++)
                {
                    serialized = JsonConvert.SerializeObject(ticks);
                }
                var end = DateTime.UtcNow;

                Log.Trace($"JSON TOOK {end - start}");
            }
        }

        private static BaseData CreateNewInstance(Type baseDataType, bool hasUnderlyingSymbol)
        {
            var instance = _iconicInstances[baseDataType];
            
            instance.Symbol = hasUnderlyingSymbol
                ? Symbol.CreateBase(baseDataType, Symbols.AAPL, QuantConnect.Market.USA)
                : Symbol.Create("ABCDEF", SecurityType.Base, Market.USA, baseDataType: baseDataType);
            
            instance.Time = new DateTime(2021, 6, 5);
            
            return instance;
        }

        private void AssertAreEqual(object expected, object result)
        {
            foreach (var propertyInfo in expected.GetType().GetProperties())
            {
                if (propertyInfo.CustomAttributes.Any(data => data.AttributeType == typeof(ProtoMemberAttribute)))
                {
                    var expectedValue = propertyInfo.GetValue(expected);
                    var resultValue = propertyInfo.GetValue(result);
                    if (expectedValue is IList)
                    {
                        var expectedValueList = (IList) expectedValue;
                        var resultValueList = (IList) resultValue;
                        for (var i = 0; i < expectedValueList.Count; i++)
                        {
                            AssertAreEqual(expectedValueList[i], resultValueList[i]);
                        }
                    }
                    else if (expectedValue is IDictionary)
                    {
                        var expectedValueDictionary = (IDictionary) expectedValue;
                        var resultValueDictionary = (IDictionary) resultValue;
                        foreach (dynamic kvp in expectedValueDictionary)
                        {
                            AssertAreEqual(kvp.Key, resultValueDictionary.Contains(kvp.Key));
                            AssertAreEqual(kvp.Value, resultValueDictionary[kvp.Key]);
                        }
                    }
                    else
                    {
                        if (expectedValue is OrderEvent || expectedValue is OrderFee)
                        {
                            AssertAreEqual(expectedValue, resultValue);
                        }
                        else
                        {
                            Assert.AreEqual(expectedValue, resultValue);
                        }
                    }
                }
            }
            foreach (var fieldInfo in expected.GetType().GetFields())
            {
                if (fieldInfo.CustomAttributes.Any(data => data.AttributeType == typeof(ProtoMemberAttribute)))
                {
                    Assert.AreEqual(fieldInfo.GetValue(expected), fieldInfo.GetValue(result));
                }
            }
        }
    }
}
