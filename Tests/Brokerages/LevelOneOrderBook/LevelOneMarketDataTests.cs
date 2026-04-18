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
using NUnit.Framework;
using QuantConnect.Brokerages.LevelOneOrderBook;

namespace QuantConnect.Tests.Brokerages.LevelOneOrderBook
{
    [TestFixture]
    public class LevelOneMarketDataTests
    {
        private readonly Symbol _aapl = Symbols.AAPL;

        private readonly DateTime _mockDateTime = new DateTime(2026, 06, 20);

        [TestCase(true)]
        [TestCase(false)]
        public void LevelOneMarketDataShouldNotRaiseEventWhenTimestampIsInvalid(bool useInvalidTimestamp)
        {
            var invalidTimestamp = useInvalidTimestamp ? default(DateTime?) : new DateTime();
            var levelOneMarketData = new LevelOneMarketData(_aapl);

            var eventRaised = default(bool);
            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateLastTrade(invalidTimestamp, null, null);
            levelOneMarketData.UpdateQuote(invalidTimestamp, null, null, null, null);
            levelOneMarketData.UpdateOpenInterest(invalidTimestamp, null);

            Assert.IsFalse(eventRaised, "BaseDataReceived should not be raised with invalid timestamp.");
        }

        [TestCase(false, null)]
        [TestCase(true, 0)]
        [TestCase(true, 100)]
        public void UpdateOpenInterestShouldRaiseEventBasedOnValuePresence(bool shouldRaiseEvent, decimal? openInterest)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl);
            var eventRaised = default(bool);

            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateOpenInterest(_mockDateTime, openInterest);

            Assert.AreEqual(shouldRaiseEvent, eventRaised, $"Expected event to {(shouldRaiseEvent ? "" : "not ")}be raised for open interest value: {openInterest?.ToStringInvariant() ?? "null"}");
        }

        [TestCase(false, null, null)]
        [TestCase(false, 1, null)]
        [TestCase(true, null, 1)]
        [TestCase(true, 1, 1)]
        public void UpdateLastTradeShouldRaiseEventBasedOnPriceAndQuantity(bool shouldRaiseEvent, decimal? lastQuantity, decimal? lastPrice)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl);
            var eventRaised = default(bool);

            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateLastTrade(_mockDateTime, lastQuantity, lastPrice);

            Assert.AreEqual(shouldRaiseEvent, eventRaised, $"Expected event {(shouldRaiseEvent ? "" : "not ")}to be raised for (Qty={lastQuantity}, Price={lastPrice}).");
        }

        [TestCase(true, 1, 1, 2, 2)]
        [TestCase(true, 1, 1, null, 2)]
        [TestCase(true, 1, 1, 2, null)]
        [TestCase(false, 1, 1, null, null)]
        public void UpdateLastTradeShouldTrackLatestTradeCorrectlyOnMultipleUpdates(bool shouldRaiseEvent, decimal? firstLastQuantity, decimal? firstLastPrice, decimal? secondLastQuantity, decimal? secondLastPrice)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl);
            var eventRaised = default(bool);

            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateLastTrade(_mockDateTime, firstLastQuantity, firstLastPrice);
            Assert.IsTrue(eventRaised, "Expected event to be raised on first update.");
            Assert.AreEqual(firstLastQuantity, levelOneMarketData.LastTradeSize, "First update: unexpected LastTradeSize.");
            Assert.AreEqual(firstLastPrice, levelOneMarketData.LastTradePrice, "First update: unexpected LastTradePrice.");

            // Reset event flag for second update
            eventRaised = false;

            levelOneMarketData.UpdateLastTrade(_mockDateTime, secondLastQuantity, secondLastPrice);

            Assert.AreEqual(shouldRaiseEvent, eventRaised, $"Second update: Expected event {(shouldRaiseEvent ? "" : "not ")}to be raised.");

            var expectedSize = secondLastQuantity ?? firstLastQuantity;
            var expectedPrice = secondLastPrice ?? firstLastPrice;

            Assert.AreEqual(expectedSize, levelOneMarketData.LastTradeSize, "Final LastTradeSize mismatch.");
            Assert.AreEqual(expectedPrice, levelOneMarketData.LastTradePrice, "Final LastTradePrice mismatch.");
        }

        [TestCase(false, null, null, null, null)]
        [TestCase(true, 1, null, 1, null)]
        [TestCase(true, 1, 0, null, null)]
        [TestCase(true, null, null, 1, 0)]
        [TestCase(true, 1, 1, 1, 0)]
        public void UpdateQuoteShouldRaiseEventBasedOnAskAndBid(bool shouldRaiseEvent, decimal? bidPrice, decimal? bidSize, decimal? askPrice, decimal? askSize)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl);
            var eventRaised = default(bool);

            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateQuote(_mockDateTime, bidPrice, bidSize, askPrice, askSize);

            Assert.AreEqual(shouldRaiseEvent, eventRaised);
        }

        [TestCase(false, 1, 1, 1, 1, null, null, null, null)]
        [TestCase(false, 1, 1, 1, 1, 1, 1, 1, 1)]
        [TestCase(true, 1, 1, 1, 1, 2, 2, 2, 2)]
        public void UpdateQuoteShouldTrackQuoteCorrectlyOnMultipleUpdates(
            bool shouldRaiseEvent,
            decimal? firstBidPrice, decimal? firstBidSize, decimal? firstAskPrice, decimal? firstAskSize,
            decimal? secondBidPrice, decimal? secondBidSize, decimal? secondAskPrice, decimal? secondAskSize)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl);
            var eventRaised = default(bool);

            levelOneMarketData.BaseDataReceived += (_, e) => eventRaised = true;

            levelOneMarketData.UpdateQuote(_mockDateTime, firstBidPrice, firstBidSize, firstAskPrice, firstAskSize);
            Assert.IsTrue(eventRaised);

            // Reset event flag for second update
            eventRaised = false;

            levelOneMarketData.UpdateQuote(_mockDateTime, secondBidPrice, secondBidSize, secondAskPrice, secondAskSize);

            Assert.AreEqual(shouldRaiseEvent, eventRaised, $"Second update: Expected event {(shouldRaiseEvent ? "" : "not ")}to be raised.");
        }

        [TestCase(true, 2, 0, 2, 0)]
        [TestCase(false, 2, 0, 2, 0)]
        public void UpdateQuoteShouldIgnoreZeroSizeUpdatesCorrectly(bool ignoreZeroSizeUpdates, decimal? bidPrice, decimal? bidSize, decimal? askPrice, decimal? askSize)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl)
            {
                IgnoreZeroSizeUpdates = ignoreZeroSizeUpdates
            };

            var expectedBestBidSize = 1;
            var expectedBestAskSize = 1;

            levelOneMarketData.UpdateQuote(_mockDateTime, 1, expectedBestBidSize, 1, expectedBestAskSize);

            levelOneMarketData.UpdateQuote(_mockDateTime, bidPrice, bidSize, askPrice, askSize);

            Assert.AreEqual(bidPrice, levelOneMarketData.BestBidPrice, "Bid price should update.");
            Assert.AreEqual(askPrice, levelOneMarketData.BestAskPrice, "Ask price should update.");

            if (ignoreZeroSizeUpdates)
            {
                Assert.AreEqual(expectedBestAskSize, levelOneMarketData.BestAskSize, "Bid size should remain unchanged when ignoring zero size.");
                Assert.AreEqual(expectedBestBidSize, levelOneMarketData.BestBidSize, "Ask size should remain unchanged when ignoring zero size.");
            }
            else
            {
                Assert.AreEqual(bidSize, levelOneMarketData.BestAskSize, "Bid size should be overwritten with 0.");
                Assert.AreEqual(askSize, levelOneMarketData.BestBidSize, "Ask size should be overwritten with 0.");
            }
        }

        [TestCase(true, 0, 2)]
        [TestCase(false, 0, 2)]
        public void UpdateLastTradeShouldIgnoreZeroSizeUpdatesCorrectly(bool ignoreZeroSizeUpdates, decimal? lastQuantity, decimal? lastPrice)
        {
            var levelOneMarketData = new LevelOneMarketData(_aapl)
            {
                IgnoreZeroSizeUpdates = ignoreZeroSizeUpdates
            };

            var expectedLastQuantity = 1;
            levelOneMarketData.UpdateLastTrade(_mockDateTime, expectedLastQuantity, 1);

            levelOneMarketData.UpdateLastTrade(_mockDateTime, lastQuantity, lastPrice);

            Assert.AreEqual(lastPrice, levelOneMarketData.LastTradePrice, "LastTradePrice should update.");

            if (ignoreZeroSizeUpdates)
            {
                Assert.AreEqual(expectedLastQuantity, levelOneMarketData.LastTradeSize);
            }
            else
            {
                Assert.AreEqual(lastQuantity, levelOneMarketData.LastTradeSize);
            }
        }
    }
}
