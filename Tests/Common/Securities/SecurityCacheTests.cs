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
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityCacheTests
    {
        private static readonly DateTime ReferenceTime = new DateTime(2000, 01, 01);

        private readonly Random _rng = new Random(Seed: 123);

        [TestCase(MarketDataType.TradeBar, 10, true)]
        [TestCase(MarketDataType.TradeBar, 10, false)]
        [TestCase(MarketDataType.QuoteBar, 10, true)]
        [TestCase(MarketDataType.QuoteBar, 10, false)]
        [TestCase(MarketDataType.Tick, 10, true)]
        [TestCase(MarketDataType.Tick, 10, false)]
        public void AlwaysReturnTheLastData(MarketDataType marketDataType, int quantity, bool sameTime)
        {
            // Arrange
            var securityCache = new SecurityCache();
            var quotes = GenerateData(marketDataType, quantity, sameTime);
            // Act
            foreach (var quoteBar in quotes)
            {
                securityCache.AddData(quoteBar);
            }
            // Assert
            Assert.True(securityCache.GetData().Equals(quotes.Last()));
        }

        [Test]
        public void GivenSameTimeStampForTradeBarAndQuoteQuotebarPrioritizeQuoteBar()
        {
            // Arrange
            var securityCache = new SecurityCache();
            var time = DateTime.Now;
            var quotes = GenerateData(MarketDataType.QuoteBar, 5, false, time);
            var trades = GenerateData(MarketDataType.TradeBar, 5, false, time);
            var data = quotes.Concat(trades);
            data = data.OrderBy(d => d.EndTime);
            // Act
            foreach (var baseData in data)
            {
                securityCache.AddData(baseData);
            }
            // Assert
            Assert.True(securityCache.GetData().Equals(quotes.Last()));
            Assert.True(securityCache.GetData<TradeBar>().Equals(trades.Last()));
        }

        [Test]
        public void UseLatestTradebarIfThereIsntAvailableQuotebar()
        {
            // Arrange
            var securityCache = new SecurityCache();
            var time = DateTime.Now;
            var quotes = GenerateData(MarketDataType.QuoteBar, 5, false, time);
            foreach (var baseData in quotes)
            {
                securityCache.AddData(baseData);
            }
            // Add one last tradebar with a later timestamp
            var laterTrade = GenerateData(MarketDataType.TradeBar, 1, true, quotes.Last().Time.AddSeconds(1)).First();
            // Act
            securityCache.AddData(laterTrade);
            // Assert
            Assert.True(securityCache.GetData().Equals(laterTrade));
            Assert.True(securityCache.GetData<QuoteBar>().Equals(quotes.Last()));
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsOpenInterestValues(SecurityCache cache, SecuritySeedData seedType)
        {
            var map = new Dictionary<string, string> {{"OpenInterest", "Value"}};
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.OpenInterest, new OpenInterest
            {
                Value = 101,
                EndTime = ReferenceTime
            }, map);
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsOpenInterestTickValues(SecurityCache cache, SecuritySeedData seedType)
        {
            var map = new Dictionary<string, string> { { "OpenInterest", "Value" } };
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.OpenInterestTick, new Tick
            {
                Value = 101,
                TickType = TickType.OpenInterest,
                EndTime = ReferenceTime
            }, map);
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsQuoteTickValues(SecurityCache cache, SecuritySeedData seedType)
        {
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.QuoteTick, new Tick
            {
                AskPrice = 101,
                AskSize = 102,
                BidPrice = 103,
                BidSize = 104,
                TickType = TickType.Quote,
                EndTime = ReferenceTime
            });
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsTradeTickValues(SecurityCache cache, SecuritySeedData seedType)
        {
            var map = new Dictionary<string, string> {{"Volume", "Quantity"}};
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.TradeTick, new Tick
            {
                Value = 101,
                Quantity = 102,
                TickType = TickType.Trade,
                EndTime = ReferenceTime
            }, map);
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsTradeBarValues(SecurityCache cache, SecuritySeedData seedType)
        {
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.TradeBar, new TradeBar
            {
                Open = 101,
                High = 102,
                Low = 103,
                Close = 104,
                Volume = 105,
                EndTime = ReferenceTime
            });
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataWithSameEndTime_SetsQuoteBarValues(SecurityCache cache, SecuritySeedData seedType)
        {
            var map = new Dictionary<string, string>
            {
                {"Price", "Close"},
                {"BidPrice", "Bid.Close"},
                {"BidSize", "LastBidSize"},
                {"AskPrice", "Ask.Close"},
                {"AskSize", "LastAskSize"}
            };
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.QuoteBar, new QuoteBar
            {
                Bid = new Bar(101, 102, 103, 104),
                Ask = new Bar(105, 106, 107, 108),
                LastAskSize = 109,
                LastBidSize = 110,
                EndTime = ReferenceTime
            }, map);
        }

        [Test]
        [TestCaseSource(nameof(GetSecurityCacheInitialStates))]
        public void AddDataFundamentals_DoesNotChangeCacheValues(SecurityCache cache, SecuritySeedData seedType)
        {
            var map = new Dictionary<string, string>();
            AddDataAndAssertChanges(cache, seedType, SecuritySeedData.Fundamentals, new Fundamentals
            {
                Value = 111,
                EndTime = ReferenceTime
            }, map);
        }

        [Test]
        public void TickTypeDependencyTests()
        {
            // Arrange
            var time = DateTime.Now;
            var price = 100m;
            var bidPrice = 99m;
            var askPrice = 101m;
            var volume = 1m;

            var tick = new Tick(time, Symbols.AAPL, price, bidPrice, askPrice) { Quantity = volume }; ;

            var securityCache = new SecurityCache();
            securityCache.AddData(tick);
            Assert.AreEqual(securityCache.Price, price);
            Assert.AreEqual(securityCache.BidPrice, bidPrice);
            Assert.AreEqual(securityCache.AskPrice, askPrice);
            Assert.AreEqual(securityCache.Volume, 0m);

            tick.TickType = TickType.Trade;
            securityCache = new SecurityCache();
            securityCache.AddData(tick);
            Assert.AreEqual(securityCache.Price, price);
            Assert.AreEqual(securityCache.BidPrice, 0m);
            Assert.AreEqual(securityCache.AskPrice, 0m);
            Assert.AreEqual(securityCache.Volume, volume);
        }

        [Test]
        public void GetAllData_ReturnsListOfData()
        {
            var cache = new SecurityCache();
            cache.StoreData(new []
            {
                new CustomDataBitcoinAlgorithm.Bitcoin{Ask = 1m},
                new CustomDataBitcoinAlgorithm.Bitcoin{Ask = 2m}
            });

            var data = cache.GetAll<CustomDataBitcoinAlgorithm.Bitcoin>().ToList();
            Assert.AreEqual(2, data.Count);
            Assert.AreEqual(1m, data[0].Ask);
            Assert.AreEqual(2m, data[1].Ask);
        }

        private void AddDataAndAssertChanges(SecurityCache cache, SecuritySeedData seedType, SecuritySeedData dataType, BaseData data, Dictionary<string, string> cacheToBaseDataPropertyMap = null)
        {
            var before = JObject.FromObject(cache);
            var dataSnapshot = JObject.FromObject(data);

            cache.AddData(data);
            var after = JObject.FromObject(cache);

            var updatedCacheProperties = GetPropertiesBy(dataType);
            if (seedType == SecuritySeedData.QuoteBar && (dataType  == SecuritySeedData.QuoteBar || dataType == SecuritySeedData.TradeBar))
            {
                // these properties aren't updated when previous data is quote bar at same time as a new IBar
                updatedCacheProperties = updatedCacheProperties.Where(p =>
                    p != "Open" &&
                    p != "High" &&
                    p != "Low" &&
                    p != "Close" &&
                    p != "Price"
                ).ToArray();
            }

            foreach (var property in before.Properties())
            {
                string dataPropertyName = null;
                if (updatedCacheProperties.Contains(property.Name))
                {
                    if (cacheToBaseDataPropertyMap?.TryGetValue(property.Name, out dataPropertyName) == true)
                    {
                        // avoiding failures due to decimal <> long in JToken.DeepEquals
                        var e = dataSnapshot.SelectToken(dataPropertyName).ToString();
                        var a = after.SelectToken(property.Name).ToString();
                        Assert.AreEqual(e, a, $"{property.Name}: Expected {e}. Actual {a}");
                    }
                    else
                    {
                        dataSnapshot.IsEqualTo(after, property.Name);
                    }
                }
                else
                {
                    before.IsEqualTo(after, property.Name);
                }
            }
        }

        public enum SecuritySeedData
        {
            None,
            TradeTick,
            QuoteTick,
            OpenInterestTick,
            OpenInterest,
            TradeBar,
            QuoteBar,
            Fundamentals
        }

        public string[] GetPropertiesBy(SecuritySeedData type)
        {
            switch (type)
            {
                case SecuritySeedData.None:
                    return new string[0];

                case SecuritySeedData.OpenInterest:
                    return new[] { "OpenInterest" };

                case SecuritySeedData.OpenInterestTick:
                    return new[] { "OpenInterest" };

                case SecuritySeedData.TradeTick:
                    return new[] {"Price", "Volume"};

                case SecuritySeedData.QuoteTick:
                    return new[] {"AskPrice", "AskSize", "BidPrice", "BidSize"};

                case SecuritySeedData.TradeBar:
                    return new[] {"Price", "Volume", "Open", "High", "Low", "Close"};

                case SecuritySeedData.QuoteBar:
                    return new[] { "Price", "Open", "High", "Low", "Close", "AskPrice", "AskSize", "BidPrice", "BidSize" };

                case SecuritySeedData.Fundamentals:
                    // fundamentals data does not modify security cache properties
                    return new string[0];

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private TestCaseData[] GetSecurityCacheInitialStates()
        {
            var defaultInstance = new SecurityCache();

            var tradeTick = new SecurityCache();
            tradeTick.AddData(new Tick
            {
                Value = 5,
                Quantity = 6,
                EndTime = ReferenceTime,
                TickType = TickType.Trade
            });

            var quoteTick = new SecurityCache();
            quoteTick.AddData(new Tick
            {
                AskPrice = 1,
                AskSize = 2,
                BidPrice = 3,
                BidSize = 4,
                EndTime = ReferenceTime,
                TickType = TickType.Quote
            });

            var openInterestTick = new SecurityCache();
            openInterestTick.AddData(new Tick
            {
                Value = 24,
                EndTime = ReferenceTime,
                TickType = TickType.OpenInterest
            });

            var openInterest = new SecurityCache();
            openInterest.AddData(new OpenInterest
            {
                Value = 23,
                EndTime = ReferenceTime,
            });

            var tradeBar = new SecurityCache();
            tradeBar.AddData(new TradeBar
            {
                Open = 7,
                High = 8,
                Low = 9,
                Close = 10,
                Volume = 11,
                EndTime = ReferenceTime
            });

            var quoteBar = new SecurityCache();
            quoteBar.AddData(new QuoteBar
            {
                Ask = new Bar(12, 13, 14, 15),
                Bid = new Bar(16, 17, 18, 19),
                LastAskSize = 20,
                LastBidSize = 21,
                Value = 22,
                EndTime = ReferenceTime
            });

            var fundamentals = new SecurityCache();
            fundamentals.AddData(new Fundamentals
            {
                Value = 23
            });

            return new[]
            {
                new TestCaseData(defaultInstance, SecuritySeedData.None).SetName("Default Instance"),
                new TestCaseData(tradeTick, SecuritySeedData.TradeTick).SetName("Seeded w/ Trade Tick"),
                new TestCaseData(quoteTick, SecuritySeedData.QuoteTick).SetName("Seeded w/ Quote Tick"),
                new TestCaseData(openInterestTick, SecuritySeedData.OpenInterestTick).SetName("Seeded w/ OpenInterest Tick"),
                new TestCaseData(openInterest, SecuritySeedData.OpenInterest).SetName("Seeded w/ OpenInterest"),
                new TestCaseData(tradeBar, SecuritySeedData.TradeBar).SetName("Seeded w/ TradeBar"),
                new TestCaseData(quoteBar, SecuritySeedData.QuoteBar).SetName("Seeded w/ QuoteBar"),
                new TestCaseData(fundamentals, SecuritySeedData.Fundamentals).SetName("Seeded w/ Fundamentals")
            };
        }

        private IReadOnlyCollection<BaseData> GenerateData(MarketDataType type, int quantity, bool sameTime, DateTime? firstTimeStamp = null)
        {
            var time = firstTimeStamp ?? DateTime.Now;
            var outputTradeBars = new List<BaseData>();
            for (var i = 0; i < quantity; i++)
            {
                var rnd = _rng.Next(minValue: 50, maxValue: 150);
                var ask = new Bar { Close = 1m * rnd, High = 1.2m * rnd, Low = 0.9m * rnd, Open = 1.1m * rnd };
                var bid = new Bar { Close = 0.9m * rnd, High = 1.1m * rnd, Low = 0.8m * rnd, Open = 1m * rnd };
                BaseData data = new TradeBar();
                switch (type)
                {
                    case MarketDataType.TradeBar:
                        data = new TradeBar
                        {
                            Close = (ask.Close + bid.Close) / 2,
                            Open = (ask.Open + bid.Open) / 2,
                            High = (ask.High + bid.High) / 2,
                            Low = (ask.Low + bid.Low) / 2,
                            Volume = 1,
                            DataType = type
                        };
                        break;
                    case MarketDataType.Tick:
                        data = new Tick
                        {
                            AskPrice = ask.Close,
                            BidPrice = bid.Close,
                            AskSize = 1,
                            BidSize = 1
                        };
                        break;
                    case MarketDataType.QuoteBar:
                        data = new QuoteBar
                        {
                            Ask = ask,
                            Bid = bid,
                            LastAskSize = 1,
                            LastBidSize = 1,
                            DataType = type,
                            Value = (ask.Close + bid.Close) / 2
                        };
                        break;
                    case MarketDataType.Auxiliary:
                    case MarketDataType.OptionChain:
                    case MarketDataType.FuturesChain:
                    case MarketDataType.Base:
                        throw new NotImplementedException("Cases not tested yet");
                }
                data.Time = time;
                data.EndTime = time.AddSeconds(value: 1);
                time = sameTime ? time : time.AddSeconds(value: 1);
                outputTradeBars.Add(data);
            }
            return outputTradeBars;
        }
    }
}