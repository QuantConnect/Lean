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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class BaseDataExtensionsTests
    {
        private SubscriptionDataConfig _config;
        const decimal _factor = 0.5m;

        [SetUp]
        public void Setup()
        {
            _config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false
            );

            _config.DataNormalizationMode = DataNormalizationMode.Adjusted;
            _config.PriceScaleFactor = _factor;
        }

        [Test]
        public void AdjustTradeBar()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY,
                Open = 100,
                High = 200,
                Low = 300,
                Close = 400,
                Volume = 10000
            };

            var adjustedTb = tb.Clone(tb.IsFillForward)
                .Normalize(_factor, DataNormalizationMode.Adjusted, 0);

            Assert.AreEqual(tb.Open * _factor, (adjustedTb as TradeBar).Open);
            Assert.AreEqual(tb.High * _factor, (adjustedTb as TradeBar).High);
            Assert.AreEqual(tb.Low * _factor, (adjustedTb as TradeBar).Low);
            Assert.AreEqual(tb.Close * _factor, (adjustedTb as TradeBar).Close);
            Assert.AreEqual(tb.Volume / _factor, (adjustedTb as TradeBar).Volume);
        }

        [Test]
        public void AdjustTick()
        {
            var tick = new Tick
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY,
                Value = 100,
                Quantity = 10
            };

            var adjustedTick = tick.Clone(tick.IsFillForward)
                .Normalize(_factor, DataNormalizationMode.Adjusted, 0);

            Assert.AreEqual(tick.Value * _factor, (adjustedTick as Tick).Value);
            Assert.AreEqual(tick.Quantity / _factor, (adjustedTick as Tick).Quantity);
        }

        [Test]
        public void AdjustQuoteTick()
        {
            var tick = new Tick
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY,
                TickType = TickType.Quote,
                AskPrice = 100,
                BidPrice = 99,
                AskSize = 100,
                BidSize = 10
            };

            var adjustedTick = tick.Clone(tick.IsFillForward)
                .Normalize(_factor, DataNormalizationMode.Adjusted, 0);

            Assert.AreEqual(tick.AskPrice * _factor, (adjustedTick as Tick).AskPrice);
            Assert.AreEqual(tick.BidPrice * _factor, (adjustedTick as Tick).BidPrice);
            Assert.AreEqual(tick.AskSize / _factor, (adjustedTick as Tick).AskSize);
            Assert.AreEqual(tick.BidSize / _factor, (adjustedTick as Tick).BidSize);
        }

        [Test]
        public void AdjustQuoteBar()
        {
            var qb = new QuoteBar(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                new Bar(10, 10, 10, 10),
                100,
                new Bar(10, 10, 10, 10),
                100
            );

            var adjustedQb = qb.Clone(qb.IsFillForward)
                .Normalize(_factor, DataNormalizationMode.Adjusted, 0);

            Assert.AreEqual(qb.Value, qb.Close);

            // bid
            Assert.AreEqual(qb.Bid.Open * _factor, (adjustedQb as QuoteBar).Bid.Open);
            Assert.AreEqual(qb.Bid.Close * _factor, (adjustedQb as QuoteBar).Bid.Close);
            Assert.AreEqual(qb.Bid.High * _factor, (adjustedQb as QuoteBar).Bid.High);
            Assert.AreEqual(qb.Bid.Low * _factor, (adjustedQb as QuoteBar).Bid.Low);
            Assert.AreEqual(qb.LastBidSize / _factor, (adjustedQb as QuoteBar).LastBidSize);
            // ask
            Assert.AreEqual(qb.Ask.Open * _factor, (adjustedQb as QuoteBar).Ask.Open);
            Assert.AreEqual(qb.Ask.Close * _factor, (adjustedQb as QuoteBar).Ask.Close);
            Assert.AreEqual(qb.Ask.High * _factor, (adjustedQb as QuoteBar).Ask.High);
            Assert.AreEqual(qb.Ask.Low * _factor, (adjustedQb as QuoteBar).Ask.Low);
            Assert.AreEqual(qb.LastAskSize / _factor, (adjustedQb as QuoteBar).LastAskSize);
        }

        [Test]
        public void AdjustTradeBarUsingConfig()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY,
                Open = 100,
                High = 200,
                Low = 300,
                Close = 400,
                Volume = 1000
            };

            var adjustedTb = tb.Clone(tb.IsFillForward)
                .Normalize(
                    _config.PriceScaleFactor,
                    _config.DataNormalizationMode,
                    _config.SumOfDividends
                );

            Assert.AreEqual(tb.Open * _factor, (adjustedTb as TradeBar).Open);
            Assert.AreEqual(tb.High * _factor, (adjustedTb as TradeBar).High);
            Assert.AreEqual(tb.Low * _factor, (adjustedTb as TradeBar).Low);
            Assert.AreEqual(tb.Close * _factor, (adjustedTb as TradeBar).Close);
            Assert.AreEqual(tb.Volume / _factor, (adjustedTb as TradeBar).Volume);
        }

        [Test]
        public void AdjustTickUsingConfig()
        {
            var tick = new Tick
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY,
                Value = 100,
                Quantity = 10
            };

            var adjustedTick = tick.Clone(tick.IsFillForward)
                .Normalize(
                    _config.PriceScaleFactor,
                    _config.DataNormalizationMode,
                    _config.SumOfDividends
                );

            Assert.AreEqual(tick.Value * _factor, (adjustedTick as Tick).Value);
            Assert.AreEqual(tick.Quantity / _factor, (adjustedTick as Tick).Quantity);
        }

        [Test]
        public void AdjustQuoteTickUsingConfig()
        {
            var tick = new Tick
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY,
                TickType = TickType.Quote,
                AskPrice = 100,
                BidPrice = 99,
                AskSize = 100,
                BidSize = 10
            };

            var adjustedTick = tick.Clone(tick.IsFillForward)
                .Normalize(
                    _config.PriceScaleFactor,
                    _config.DataNormalizationMode,
                    _config.SumOfDividends
                );

            Assert.AreEqual(tick.AskPrice * _factor, (adjustedTick as Tick).AskPrice);
            Assert.AreEqual(tick.BidPrice * _factor, (adjustedTick as Tick).BidPrice);
            Assert.AreEqual(tick.AskSize / _factor, (adjustedTick as Tick).AskSize);
            Assert.AreEqual(tick.BidSize / _factor, (adjustedTick as Tick).BidSize);
        }

        [Test]
        public void AdjustQuoteBarUsingConfig()
        {
            var qb = new QuoteBar(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                new Bar(10, 10, 10, 10),
                100,
                new Bar(10, 10, 10, 10),
                100
            );

            var adjustedQb = qb.Clone(qb.IsFillForward)
                .Normalize(
                    _config.PriceScaleFactor,
                    _config.DataNormalizationMode,
                    _config.SumOfDividends
                );

            Assert.AreEqual(qb.Value, qb.Close);

            // bid
            Assert.AreEqual(qb.Bid.Open * _factor, (adjustedQb as QuoteBar).Bid.Open);
            Assert.AreEqual(qb.Bid.Close * _factor, (adjustedQb as QuoteBar).Bid.Close);
            Assert.AreEqual(qb.Bid.High * _factor, (adjustedQb as QuoteBar).Bid.High);
            Assert.AreEqual(qb.Bid.Low * _factor, (adjustedQb as QuoteBar).Bid.Low);
            Assert.AreEqual(qb.LastBidSize / _factor, (adjustedQb as QuoteBar).LastBidSize);
            // ask
            Assert.AreEqual(qb.Ask.Open * _factor, (adjustedQb as QuoteBar).Ask.Open);
            Assert.AreEqual(qb.Ask.Close * _factor, (adjustedQb as QuoteBar).Ask.Close);
            Assert.AreEqual(qb.Ask.High * _factor, (adjustedQb as QuoteBar).Ask.High);
            Assert.AreEqual(qb.Ask.Low * _factor, (adjustedQb as QuoteBar).Ask.Low);
            Assert.AreEqual(qb.LastAskSize / _factor, (adjustedQb as QuoteBar).LastAskSize);
        }
    }
}
