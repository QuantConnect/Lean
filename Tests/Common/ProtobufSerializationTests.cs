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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using ProtoBuf;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Data.Custom.Estimize;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ProtobufSerializationTests
    {
        [Test]
        public void SerializeRoundTripEstimizeRelease()
        {
            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeRelease), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeRelease
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Eps = 2,
                Revenue = null,
                ReleaseDate = time
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, item);
                stream.Position = 0;

                var deserialized = Serializer.Deserialize<EstimizeRelease>(stream);

                Assert.AreEqual("123", deserialized.Id);
                Assert.AreEqual(2020, deserialized.FiscalYear);
                Assert.AreEqual(1, deserialized.FiscalQuarter);
                Assert.AreEqual(2, deserialized.Eps);
                Assert.AreEqual(null, deserialized.Revenue);
                Assert.AreEqual(time, deserialized.ReleaseDate);
                Assert.AreEqual(time, deserialized.Time);
                Assert.AreEqual(time, deserialized.EndTime);
            }
        }

        [Test]
        public void SerializeRoundTripEstimizeConsensus()
        {
            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeConsensus), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeConsensus
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Source = Source.WallStreet,
                Type = QuantConnect.Data.Custom.Estimize.Type.Eps,
                Count = 3,
                Mean = 2,
                UpdatedAt = time
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, item);
                stream.Position = 0;

                var deserialized = Serializer.Deserialize<EstimizeConsensus>(stream);

                Assert.AreEqual("123", deserialized.Id);
                Assert.AreEqual(2020, deserialized.FiscalYear);
                Assert.AreEqual(1, deserialized.FiscalQuarter);
                Assert.AreEqual(Source.WallStreet, deserialized.Source);
                Assert.AreEqual(QuantConnect.Data.Custom.Estimize.Type.Eps, deserialized.Type);
                Assert.AreEqual(3, deserialized.Count);
                Assert.AreEqual(2, deserialized.Mean);
                Assert.AreEqual(time, deserialized.UpdatedAt);
                Assert.AreEqual(time, deserialized.Time);
                Assert.AreEqual(time, deserialized.EndTime);
            }
        }

        [Test]
        public void SerializeRoundTripEstimizeEstimate()
        {
            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(EstimizeEstimate), underlyingSymbol, QuantConnect.Market.USA);

            var item = new EstimizeEstimate
            {
                Id = "123",
                Symbol = symbol,
                FiscalYear = 2020,
                FiscalQuarter = 1,
                Eps = 2,
                Revenue = null,
                CreatedAt = time
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, item);
                stream.Position = 0;

                var deserialized = Serializer.Deserialize<EstimizeEstimate>(stream);
                Assert.AreEqual("123", deserialized.Id);
                Assert.AreEqual(2020, deserialized.FiscalYear);
                Assert.AreEqual(1, deserialized.FiscalQuarter);
                Assert.AreEqual(2, deserialized.Eps);
                Assert.AreEqual(null, deserialized.Revenue);
                Assert.AreEqual(time, deserialized.CreatedAt);
                Assert.AreEqual(time, deserialized.Time);
                Assert.AreEqual(time, deserialized.EndTime);
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
                    Exchange = "Pinocho",
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
            var serializedTick = ticks.ProtobufSerialize();
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
                    Assert.AreEqual("Pinocho", result.Exchange);
                    Assert.AreEqual("VerySold", result.SaleCondition);
                    Assert.AreEqual(TickType.Quote, result.TickType);
                    Assert.AreEqual(time + TimeSpan.FromMilliseconds(i), result.EndTime);
                    Assert.AreEqual(i, result.Value);
                    Assert.AreEqual(i, result.BidPrice);
                    Assert.AreEqual(i, result.BidSize);
                }
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
                Exchange = "Pinocho",
                SaleCondition = "VerySold",
                TickType = TickType.Quote,
                EndTime = DateTime.UtcNow,
                Value = 10,
                BidPrice = 100,
                BidSize = 100
            };

            var serializedTick = tick.ProtobufSerialize();

            // verify its correct
            using (var stream = new MemoryStream(serializedTick))
            {
                var result = (Tick) Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(tick.AskPrice, result.AskPrice);
                Assert.AreEqual(tick.AskSize, result.AskSize);
                Assert.AreEqual(tick.Time, result.Time);
                Assert.AreEqual(tick.Quantity, result.Quantity);
                Assert.AreEqual(tick.DataType, result.DataType);
                Assert.AreEqual(tick.Exchange, result.Exchange);
                Assert.AreEqual(tick.SaleCondition, result.SaleCondition);
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

            var serializedTradeBar = tradeBar.ProtobufSerialize();
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

            var serializedQuoteBar = quoteBar.ProtobufSerialize();
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

            var serializedDividend = dividend.ProtobufSerialize();
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

            var serializedSplit = split.ProtobufSerialize();
            using (var stream = new MemoryStream(serializedSplit))
            {
                var result = (Split)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(split.Type, result.Type);
                Assert.AreEqual(split.DataType, result.DataType);
                Assert.AreEqual(split.SplitFactor, result.SplitFactor);
                Assert.AreEqual(split.ReferencePrice, result.ReferencePrice);
                Assert.AreEqual(split.Time, result.Time);
                Assert.AreEqual(split.Value, result.Value);
            }
        }

        [Test]
        public void TiingoNewsRoundTrip()
        {
            var symbol = Symbol.CreateBase(
                typeof(TiingoNews),
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Market.USA);

            var news = new TiingoNews
            {
                ArticleID = "1000",
                CrawlDate = DateTime.UtcNow.AddDays(-1),
                DataType = MarketDataType.Base,
                Description = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                PublishedDate = DateTime.UtcNow.Date,
                Source = "News Corp.",
                Symbols = new List<Symbol>
                {
                    Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                    Symbol.Create("CBS", SecurityType.Equity, Market.USA),
                    Symbol.Create("GLD", SecurityType.Equity, Market.USA)
                },
                Tags = new List<string>
                {
                    "mining",
                    "space",
                    "aero-space",
                    "technology"
                },
                Title = "Round Trip To Planet Protobuf Successful",
                Url = "https://tiingo.com",

                Time = DateTime.UtcNow,
                Symbol = symbol
            };

            var serializedNews = news.ProtobufSerialize();
            using (var stream = new MemoryStream(serializedNews))
            {
                var result = (TiingoNews)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();
                Assert.AreEqual(news.ArticleID, result.ArticleID);
                Assert.AreEqual(news.CrawlDate, result.CrawlDate);
                Assert.AreEqual(news.DataType, result.DataType);
                Assert.AreEqual(news.Description, result.Description);
                Assert.AreEqual(news.PublishedDate, result.PublishedDate);
                Assert.AreEqual(news.Source, result.Source);
                Assert.IsTrue(news.Symbols.SequenceEqual(result.Symbols));
                Assert.IsTrue(news.Tags.SequenceEqual(result.Tags));
                Assert.AreEqual(news.Title, result.Title);
                Assert.AreEqual(news.Url, result.Url);
                Assert.AreEqual(news.Time, result.Time);
            }
        }

        [Test]
        public void TiingoNewsEmptySymbolsTagsRoundTrip()
        {
            var symbol = Symbol.CreateBase(
                typeof(TiingoNews),
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Market.USA);

            var news = new TiingoNews
            {
                ArticleID = "1000",
                CrawlDate = DateTime.UtcNow.AddDays(-1),
                DataType = MarketDataType.Base,
                Description = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                PublishedDate = DateTime.UtcNow.Date,
                Source = "News Corp.",
                Symbols = new List<Symbol>(),
                Tags = new List<string>(),
                Title = "Round Trip To Planet Protobuf Successful",
                Url = "https://tiingo.com",

                Time = DateTime.UtcNow,
                Symbol = symbol
            };

            var serializedNews = news.ProtobufSerialize();
            using (var stream = new MemoryStream(serializedNews))
            {
                var result = (TiingoNews)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(news.ArticleID, result.ArticleID);
                Assert.AreEqual(news.CrawlDate, result.CrawlDate);
                Assert.AreEqual(news.DataType, result.DataType);
                Assert.AreEqual(news.Description, result.Description);
                Assert.AreEqual(news.PublishedDate, result.PublishedDate);
                Assert.AreEqual(news.Source, result.Source);
                Assert.IsTrue(news.Symbols.SequenceEqual(result.Symbols));
                Assert.IsTrue(news.Tags.SequenceEqual(result.Tags));
                Assert.AreEqual(news.Title, result.Title);
                Assert.AreEqual(news.Url, result.Url);
                Assert.AreEqual(news.Time, result.Time);
            }
        }

        [Test]
        public void BenzingaNewsRoundTrip()
        {
            var symbol = Symbol.CreateBase(
                typeof(BenzingaNews),
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Market.USA);

            var news = new BenzingaNews
            {
                Id = 1000,
                Author = "Benzinga",
                DataType = MarketDataType.Base,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Title = "Round Trip To Planet Protobuf Successful",
                Teaser = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                Contents = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                Categories = new List<string>
                {
                    "aerospace",
                    "technology"
                },
                Symbols = new List<Symbol>
                {
                    Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                    Symbol.Create("CBS", SecurityType.Equity, Market.USA),
                    Symbol.Create("GLD", SecurityType.Equity, Market.USA)
                },
                Tags = new List<string>
                {
                    "mining",
                    "space",
                    "aero-space",
                    "technology"
                },

                Time = DateTime.UtcNow,
                Symbol = symbol
            };
        }

        [Test]
        public void BenzingaNewsEmptyTagsSymbolsRoundTrip()
        {
            var symbol = Symbol.CreateBase(
                typeof(BenzingaNews),
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Market.USA);

            var news = new BenzingaNews
            {
                Id = 1000,
                Author = "Benzinga",
                DataType = MarketDataType.Base,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Title = "Round Trip To Planet Protobuf Successful",
                Teaser = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                Contents = "Round trip to planet Protobuf is successful, ushering us into a new era for mining",
                Categories = new List<string>
                {
                    "aerospace",
                    "technology"
                },
                Symbols = new List<Symbol>(),
                Tags = new List<string>(),

                Time = DateTime.UtcNow,
                Symbol = symbol
            };

            var serializedNews = news.ProtobufSerialize();
            using (var stream = new MemoryStream(serializedNews))
            {
                var result = (BenzingaNews)Serializer.Deserialize<IEnumerable<BaseData>>(stream).First();

                Assert.AreEqual(news.Id, result.Id);
                Assert.AreEqual(news.Author, result.Author);
                Assert.AreEqual(news.DataType, result.DataType);
                Assert.AreEqual(news.CreatedAt, result.CreatedAt);
                Assert.AreEqual(news.UpdatedAt, result.UpdatedAt);
                Assert.AreEqual(news.Title, result.Title);
                Assert.AreEqual(news.Teaser, result.Teaser);
                Assert.AreEqual(news.Contents, result.Contents);
                Assert.IsTrue(news.Categories.SequenceEqual(result.Categories));
                Assert.IsTrue(news.Symbols.SequenceEqual(result.Symbols));
                Assert.IsTrue(news.Tags.SequenceEqual(result.Tags));
                Assert.AreEqual(news.Time, result.Time);
            }
        }

        [Test, Ignore("Performance test")]
        public void SpeedTest()
        {
            var symbols = new List<Symbol>
            {
                Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                Symbol.Create("DE30EUR", SecurityType.Cfd, Market.Oanda),
                Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX),
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

            {
                // warmup
                var serialized = ticks.ProtobufSerialize();
                using (var stream = new MemoryStream(serialized))
                {
                    // verify its correct
                    var result = Serializer.Deserialize<List<Tick>>(stream);
                }

                var start = DateTime.UtcNow;
                for (var i = 0; i < 10; i++)
                {
                    serialized = ticks.ProtobufSerialize();
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
    }
}
