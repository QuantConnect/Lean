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
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class OrderbookTests
    {
        #region Constructor Tests

        [Test]
        public void DefaultConstructorInitializesEmptyLists()
        {
            var depth = new Orderbook();

            Assert.IsNotNull(depth.Bids);
            Assert.IsNotNull(depth.Asks);
            Assert.AreEqual(0, depth.Bids.Count);
            Assert.AreEqual(0, depth.Asks.Count);
            Assert.AreEqual(10, depth.Levels);
            Assert.AreEqual(MarketDataType.Tick, depth.DataType);
        }

        [Test]
        public void ConstructorWithSymbolAndTimeSetsProperly()
        {
            var symbol = Symbols.BTCUSD;
            var time = new DateTime(2024, 1, 15, 12, 0, 0);
            var depth = new Orderbook(symbol, time);

            Assert.AreEqual(symbol, depth.Symbol);
            Assert.AreEqual(time, depth.Time);
            Assert.AreEqual(time, depth.EndTime);
            Assert.IsNotNull(depth.Bids);
            Assert.IsNotNull(depth.Asks);
        }

        #endregion

        #region CSV Reader Tests

        [Test]
        public void ReadsValidCsvWith5Levels()
        {
            // Format: timestamp, 5 bids (price,size), 5 asks (price,size)
            const string line = "18000677,100.05,10,100.04,15,100.03,20,100.02,25,100.01,30," +
                                "100.06,12,100.07,18,100.08,22,100.09,28,100.10,32";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbol.Create("BTCUSDT", SecurityType.Crypto, QuantConnect.Market.Binance),
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false) as Orderbook;

            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Bids.Count);
            Assert.AreEqual(5, result.Asks.Count);

            // Verify best bid and ask
            Assert.AreEqual(100.05m, result.Bids[0].Price);
            Assert.AreEqual(10m, result.Bids[0].Size);
            Assert.AreEqual(100.06m, result.Asks[0].Price);
            Assert.AreEqual(12m, result.Asks[0].Size);

            // Verify mid price
            Assert.AreEqual(100.055m, result.Value);
            Assert.AreEqual(100.055m, result.GetMidPrice());
        }

        [Test]
        public void ReadsValidCsvWith10Levels()
        {
            // Format: timestamp, 10 bids, 10 asks
            const string line = "18000677," +
                                "100.10,10,100.09,15,100.08,20,100.07,25,100.06,30," +
                                "100.05,35,100.04,40,100.03,45,100.02,50,100.01,55," +
                                "100.11,12,100.12,18,100.13,22,100.14,28,100.15,32," +
                                "100.16,38,100.17,42,100.18,48,100.19,52,100.20,58";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbol.Create("BTCUSDT", SecurityType.Crypto, QuantConnect.Market.Binance),
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false) as Orderbook;

            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Bids.Count);
            Assert.AreEqual(10, result.Asks.Count);
            Assert.AreEqual(10, result.Levels);

            // Verify ordering
            Assert.IsTrue(result.Bids[0].Price > result.Bids[1].Price);
            Assert.IsTrue(result.Asks[0].Price < result.Asks[1].Price);
        }

        [Test]
        public void ReadsValidCsvWithDecimalTimestamp()
        {
            const string line = "18000677.3456," +
                                "3669.12,0.004,3669.11,0.005," +
                                "3669.13,3.406,3669.14,2.100";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbol.Create("BTCUSDT", SecurityType.Crypto, QuantConnect.Market.Binance),
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false) as Orderbook;

            Assert.IsNotNull(result);
            // Decimal timestamp is truncated to long milliseconds
            var expectedTime = baseDate.AddMilliseconds(18000677);
            Assert.AreEqual(expectedTime, result.Time);
        }

        [Test]
        public void ReaderHandlesEmptyLine()
        {
            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, "", baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderHandlesCommentLine()
        {
            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, "# This is a comment", baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderHandlesInsufficientFields()
        {
            const string line = "18000677,100.05,10"; // Only 3 fields, need at least 5

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderHandlesInvalidTimestamp()
        {
            const string line = "INVALID,100.05,10,100.06,12";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderRejectsCrossedSpread()
        {
            // Bid price (100.10) >= Ask price (100.05) - invalid
            const string line = "18000677,100.10,10,100.05,12";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderRejectsUnsortedBids()
        {
            // Bids not sorted descending (100.04 > 100.05)
            const string line = "18000677,100.04,10,100.05,15,100.06,12,100.07,18";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderRejectsUnsortedAsks()
        {
            // Asks not sorted ascending (100.08 > 100.07)
            const string line = "18000677,100.04,10,100.03,15,100.08,12,100.07,18";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false);

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderSkipsInvalidPriceEntries()
        {
            // Contains some invalid entries that should be skipped
            const string line = "18000677," +
                                "100.05,10,INVALID,15,100.03,20," +
                                "100.06,12,100.07,INVALID,100.08,22";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false) as Orderbook;

            Assert.IsNotNull(result);
            // Should have skipped invalid entries
            Assert.AreEqual(2, result.Bids.Count);
            Assert.AreEqual(2, result.Asks.Count);
        }

        [Test]
        public void ReaderReturnsNullInLiveMode()
        {
            const string line = "18000677,100.05,10,100.06,12";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, true); // isLiveMode = true

            Assert.IsNull(result);
        }

        [Test]
        public void ReaderHandlesTimezoneConversion()
        {
            const string line = "18000677,100.05,10,100.06,12";

            var baseDate = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.EasternStandard, // Data timezone
                TimeZones.Utc, // Exchange timezone
                false,
                false,
                false);

            var depth = new Orderbook();
            var result = depth.Reader(config, line, baseDate, false) as Orderbook;

            Assert.IsNotNull(result);
            // Time should be converted from EST to UTC
            Assert.AreNotEqual(baseDate.AddMilliseconds(18000677), result.Time);
        }

        #endregion

        #region GetSource Tests

        [Test]
        public void GetSourceGeneratesCorrectPathForCrypto()
        {
            var depth = new Orderbook();
            var date = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbol.Create("BTCUSDT", SecurityType.Crypto, QuantConnect.Market.Binance),
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var source = depth.GetSource(config, date, false);

            Assert.AreEqual(SubscriptionTransportMedium.LocalFile, source.TransportMedium);
            StringAssert.Contains("crypto", source.Source.ToLowerInvariant());
            StringAssert.Contains("binance", source.Source.ToLowerInvariant());
            StringAssert.Contains("btcusdt", source.Source.ToLowerInvariant());
            StringAssert.Contains("20240115_depth.zip", source.Source.ToLowerInvariant());
            StringAssert.Contains("#20240115_btcusdt_tick_depth.csv", source.Source.ToLowerInvariant());
        }

        [Test]
        public void GetSourceReturnsStreamingSourceInLiveMode()
        {
            var depth = new Orderbook();
            var date = new DateTime(2024, 1, 15);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                Symbols.BTCUSD,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var source = depth.GetSource(config, date, true); // isLiveMode = true

            Assert.AreEqual(SubscriptionTransportMedium.Streaming, source.TransportMedium);
            Assert.AreEqual(string.Empty, source.Source);
        }

        [Test]
        public void GetSourceHandlesBaseSecurityTypeAsDefault()
        {
            var depth = new Orderbook();
            var date = new DateTime(2024, 1, 15);
            var symbol = Symbol.CreateBase(typeof(Orderbook), Symbol.Create("BTCUSDT", SecurityType.Crypto, QuantConnect.Market.Binance), QuantConnect.Market.Binance);

            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                symbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var source = depth.GetSource(config, date, false);

            // Should default to crypto for Base security type
            StringAssert.Contains("crypto", source.Source.ToLowerInvariant());
        }

        [Test]
        public void GetSourceValidatesSymbolForPathTraversal()
        {
            var depth = new Orderbook();
            var date = new DateTime(2024, 1, 15);

            // Create symbol with path traversal attempt
            var maliciousSymbol = Symbol.Create("../../../etc/passwd", SecurityType.Crypto, QuantConnect.Market.Binance);
            var config = new SubscriptionDataConfig(
                typeof(Orderbook),
                maliciousSymbol,
                Resolution.Tick,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false);

            var source = depth.GetSource(config, date, false);

            // Should return empty source for invalid symbol
            Assert.AreEqual(string.Empty, source.Source);
            Assert.AreEqual(SubscriptionTransportMedium.LocalFile, source.TransportMedium);
        }

        #endregion

        #region Helper Method Tests

        [Test]
        public void GetSpreadCalculatesCorrectly()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));
            depth.Asks.Add(new OrderbookLevel(100.05m, 12m));

            var spread = depth.GetSpread();

            Assert.AreEqual(0.05m, spread);
        }

        [Test]
        public void GetSpreadReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var spread = depth.GetSpread();

            Assert.AreEqual(0m, spread);
        }

        [Test]
        public void GetMidPriceCalculatesCorrectly()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));
            depth.Asks.Add(new OrderbookLevel(100.10m, 12m));

            var midPrice = depth.GetMidPrice();

            Assert.AreEqual(100.05m, midPrice);
        }

        [Test]
        public void GetMidPriceReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var midPrice = depth.GetMidPrice();

            Assert.AreEqual(0m, midPrice);
        }

        [Test]
        public void GetBestBidReturnsCorrectValues()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));
            depth.Bids.Add(new OrderbookLevel(99.95m, 15m));

            var (price, size) = depth.GetBestBid();

            Assert.AreEqual(100.00m, price);
            Assert.AreEqual(10m, size);
        }

        [Test]
        public void GetBestBidReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var (price, size) = depth.GetBestBid();

            Assert.AreEqual(0m, price);
            Assert.AreEqual(0m, size);
        }

        [Test]
        public void GetBestAskReturnsCorrectValues()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.05m, 12m));
            depth.Asks.Add(new OrderbookLevel(100.10m, 18m));

            var (price, size) = depth.GetBestAsk();

            Assert.AreEqual(100.05m, price);
            Assert.AreEqual(12m, size);
        }

        [Test]
        public void GetBestAskReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var (price, size) = depth.GetBestAsk();

            Assert.AreEqual(0m, price);
            Assert.AreEqual(0m, size);
        }

        [Test]
        public void GetBidReturnsCorrectLevel()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));
            depth.Bids.Add(new OrderbookLevel(99.95m, 15m));

            var level = depth.GetBid(1);

            Assert.IsNotNull(level);
            Assert.AreEqual(99.95m, level.Price);
            Assert.AreEqual(15m, level.Size);
        }

        [Test]
        public void GetBidReturnsNullForInvalidIndex()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));

            var level = depth.GetBid(5);

            Assert.IsNull(level);
        }

        [Test]
        public void GetAskReturnsCorrectLevel()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.05m, 12m));
            depth.Asks.Add(new OrderbookLevel(100.10m, 18m));

            var level = depth.GetAsk(1);

            Assert.IsNotNull(level);
            Assert.AreEqual(100.10m, level.Price);
            Assert.AreEqual(18m, level.Size);
        }

        [Test]
        public void GetAskReturnsNullForInvalidIndex()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.05m, 12m));

            var level = depth.GetAsk(-1);

            Assert.IsNull(level);
        }

        [Test]
        public void BidCountAndAskCountPropertiesWork()
        {
            var depth = new Orderbook();
            Assert.AreEqual(0, depth.BidCount);
            Assert.AreEqual(0, depth.AskCount);

            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));
            depth.Bids.Add(new OrderbookLevel(99.95m, 15m));
            depth.Asks.Add(new OrderbookLevel(100.05m, 12m));

            Assert.AreEqual(2, depth.BidCount);
            Assert.AreEqual(1, depth.AskCount);
        }

        #endregion

        #region Calculation Tests

        [Test]
        public void CalculateFillableQuantityForBuy()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.00m, 1.0m));  // $100
            depth.Asks.Add(new OrderbookLevel(100.50m, 2.0m));  // $201
            depth.Asks.Add(new OrderbookLevel(101.00m, 3.0m));  // $303

            var (qty, avgPrice, levels) = depth.CalculateFillableQuantity("BUY", 400m);

            // Should consume: 1.0@100 + 2.0@100.5 + 0.99@101 = 3.99 BTC
            // Total cost: 100 + 201 + 99.99 = 400.99 (approximately 400)
            Assert.AreEqual(3, levels);
            Assert.IsTrue(qty > 3.98m && qty < 4.0m);
            Assert.IsTrue(avgPrice > 100.0m && avgPrice < 101.0m);
        }

        [Test]
        public void CalculateFillableQuantityForSell()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 1.0m));  // $100
            depth.Bids.Add(new OrderbookLevel(99.50m, 2.0m));   // $199
            depth.Bids.Add(new OrderbookLevel(99.00m, 3.0m));   // $297

            var (qty, avgPrice, levels) = depth.CalculateFillableQuantity("SELL", 500m);

            // With $500 target: Level1 $100 (1.0) + Level2 $199 (2.0) + Level3 partial $201 (2.03...)
            // Total qty: 1.0 + 2.0 + 201/99 ≈ 5.03
            Assert.AreEqual(3, levels);
            Assert.IsTrue(qty > 5.0m && qty < 5.1m);
            Assert.IsTrue(avgPrice > 99.0m && avgPrice < 100.0m);
        }

        [Test]
        public void CalculateFillableQuantityReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var (qty, avgPrice, levels) = depth.CalculateFillableQuantity("BUY", 1000m);

            Assert.AreEqual(0m, qty);
            Assert.AreEqual(0m, avgPrice);
            Assert.AreEqual(0, levels);
        }

        [Test]
        public void CalculateSlippageForBuy()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.00m, 1.0m));
            depth.Asks.Add(new OrderbookLevel(100.50m, 2.0m));
            depth.Asks.Add(new OrderbookLevel(101.00m, 3.0m));

            var (avgPrice, slippageBps, levels) = depth.CalculateSlippage("BUY", 3.0m);

            // Fills: 1@100 + 2@100.5 = 3.0 BTC
            // Avg price: (100 + 201) / 3 = 100.333
            // Slippage: (100.333 - 100) / 100 * 10000 = 33.3 bps
            Assert.AreEqual(2, levels);
            Assert.IsTrue(avgPrice > 100.0m && avgPrice < 101.0m);
            Assert.IsTrue(slippageBps > 30m && slippageBps < 40m);
        }

        [Test]
        public void CalculateSlippageForSell()
        {
            var depth = new Orderbook();
            depth.Bids.Add(new OrderbookLevel(100.00m, 1.0m));
            depth.Bids.Add(new OrderbookLevel(99.50m, 2.0m));
            depth.Bids.Add(new OrderbookLevel(99.00m, 3.0m));

            var (avgPrice, slippageBps, levels) = depth.CalculateSlippage("SELL", 2.5m);

            // Fills: 1@100 + 1.5@99.5 = 2.5 BTC
            // Avg price: (100 + 149.25) / 2.5 = 99.7
            // Slippage: (100 - 99.7) / 100 * 10000 = 30 bps
            Assert.AreEqual(2, levels);
            Assert.IsTrue(avgPrice > 99.5m && avgPrice < 100.0m);
            Assert.IsTrue(slippageBps > 25m && slippageBps < 35m);
        }

        [Test]
        public void CalculateSlippageReturnsMaxValueForInsufficientLiquidity()
        {
            var depth = new Orderbook();
            depth.Asks.Add(new OrderbookLevel(100.00m, 1.0m));

            var (avgPrice, slippageBps, levels) = depth.CalculateSlippage("BUY", 10.0m);

            Assert.AreEqual(0m, avgPrice);
            Assert.AreEqual(decimal.MaxValue, slippageBps);
            Assert.AreEqual(1, levels);
        }

        [Test]
        public void CalculateSlippageReturnsZeroForEmptyOrderbook()
        {
            var depth = new Orderbook();

            var (avgPrice, slippageBps, levels) = depth.CalculateSlippage("BUY", 1.0m);

            Assert.AreEqual(0m, avgPrice);
            Assert.AreEqual(0m, slippageBps);
            Assert.AreEqual(0, levels);
        }

        #endregion

        #region Clone and ToString Tests

        [Test]
        public void CloneCreatesDeepCopy()
        {
            var original = new Orderbook(Symbols.BTCUSD, new DateTime(2024, 1, 15));
            original.Bids.Add(new OrderbookLevel(100.00m, 10m));
            original.Asks.Add(new OrderbookLevel(100.05m, 12m));
            original.Levels = 5;
            original.Value = 100.025m;

            var clone = original.Clone() as Orderbook;

            Assert.IsNotNull(clone);
            Assert.AreEqual(original.Symbol, clone.Symbol);
            Assert.AreEqual(original.Time, clone.Time);
            Assert.AreEqual(original.Value, clone.Value);
            Assert.AreEqual(original.Levels, clone.Levels);
            Assert.AreEqual(original.Bids.Count, clone.Bids.Count);
            Assert.AreEqual(original.Asks.Count, clone.Asks.Count);

            // Verify deep copy - modifying clone shouldn't affect original
            clone.Bids[0].Price = 99.00m;
            Assert.AreEqual(100.00m, original.Bids[0].Price);
            Assert.AreEqual(99.00m, clone.Bids[0].Price);
        }

        [Test]
        public void ToStringFormatsCorrectly()
        {
            var depth = new Orderbook(Symbols.BTCUSD, new DateTime(2024, 1, 15));
            depth.Bids.Add(new OrderbookLevel(100.00m, 10.5m));
            depth.Asks.Add(new OrderbookLevel(100.05m, 12.75m));
            depth.Levels = 5;

            var str = depth.ToString();

            StringAssert.Contains("BTCUSD", str);
            StringAssert.Contains("100.00", str);
            StringAssert.Contains("10.5", str);
            StringAssert.Contains("100.05", str);
            StringAssert.Contains("12.75", str);
            StringAssert.Contains("Bid:", str);
            StringAssert.Contains("Ask:", str);
        }

        [Test]
        public void ToStringHandlesEmptyOrderbook()
        {
            var depth = new Orderbook(Symbols.BTCUSD, new DateTime(2024, 1, 15));

            var str = depth.ToString();

            StringAssert.Contains("BTCUSD", str);
            StringAssert.Contains("N/A", str);
        }

        #endregion

        #region OrderbookLevel Tests

        [Test]
        public void OrderbookLevelConstructorSetsProperties()
        {
            var level = new OrderbookLevel(100.50m, 25.5m);

            Assert.AreEqual(100.50m, level.Price);
            Assert.AreEqual(25.5m, level.Size);
        }

        [Test]
        public void OrderbookLevelToStringFormatsCorrectly()
        {
            var level = new OrderbookLevel(100.50m, 25.5m);

            var str = level.ToString();

            StringAssert.Contains("100.50", str);
            StringAssert.Contains("25.5", str);
        }

        #endregion

        #region Orderbooks Collection Tests

        [Test]
        public void OrderbooksCollectionCreation()
        {
            var collection = new Orderbooks();

            Assert.IsNotNull(collection);
        }

        [Test]
        public void OrderbooksCollectionWithFrontier()
        {
            var frontier = new DateTime(2024, 1, 15);
            var collection = new Orderbooks(frontier);

            Assert.IsNotNull(collection);
        }

        [Test]
        public void OrderbooksCollectionIndexer()
        {
            var collection = new Orderbooks();
            var depth = new Orderbook(Symbols.BTCUSD, new DateTime(2024, 1, 15));
            depth.Bids.Add(new OrderbookLevel(100.00m, 10m));

            collection[Symbols.BTCUSD] = depth;
            var retrieved = collection[Symbols.BTCUSD];

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(Symbols.BTCUSD, retrieved.Symbol);
            Assert.AreEqual(1, retrieved.Bids.Count);
        }

        #endregion
    }
}
