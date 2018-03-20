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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityCacheTests
    {
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


        private IEnumerable<BaseData> GenerateData(MarketDataType type, int quantity, bool sameTime,
                                                   DateTime? firstTimeStamp = null)
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