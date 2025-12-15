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
using System.Globalization;
using System.IO;
using System.Linq;
using ProtoBuf;
using QuantConnect.Logging;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a single price level in the orderbook
    /// </summary>
    [ProtoContract]
    public class OrderbookLevel
    {
        /// <summary>
        /// Price at this level
        /// </summary>
        [ProtoMember(1)]
        public decimal Price { get; set; }

        /// <summary>
        /// Size (quantity) at this price level
        /// </summary>
        [ProtoMember(2)]
        public decimal Size { get; set; }

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public OrderbookLevel()
        {
        }

        /// <summary>
        /// Creates a new orderbook level
        /// </summary>
        public OrderbookLevel(decimal price, decimal size)
        {
            Price = price;
            Size = size;
        }

        /// <summary>
        /// Returns a string representation
        /// </summary>
        public override string ToString()
        {
            return $"{Price:F2} @ {Size:F4}";
        }
    }

    /// <summary>
    /// Orderbook: Multi-level orderbook depth data
    /// Contains bid and ask price levels for analyzing market depth
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class Orderbook : Tick
    {
        private const int DefaultLevels = 10;

        /// <summary>
        /// Bid levels, sorted descending (best bid first)
        /// </summary>
        [ProtoMember(201)]
        public List<OrderbookLevel> Bids { get; set; }

        /// <summary>
        /// Ask levels, sorted ascending (best ask first)
        /// </summary>
        [ProtoMember(202)]
        public List<OrderbookLevel> Asks { get; set; }

        /// <summary>
        /// Number of depth levels
        /// </summary>
        [ProtoMember(203)]
        public int Levels { get; set; }

        /// <summary>
        /// Number of bid levels (Python-friendly property)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int BidCount => Bids?.Count ?? 0;

        /// <summary>
        /// Number of ask levels (Python-friendly property)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int AskCount => Asks?.Count ?? 0;

        /// <summary>
        /// The closing time of this data point
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time; }
            set { Time = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Orderbook()
        {
            Bids = new List<OrderbookLevel>();
            Asks = new List<OrderbookLevel>();
            Levels = DefaultLevels;
            TickType = TickType.Orderbook;
            DataType = MarketDataType.Tick;
        }

        /// <summary>
        /// Constructor with symbol and time
        /// </summary>
        public Orderbook(Symbol symbol, DateTime time)
            : this()
        {
            Symbol = symbol;
            Time = time;
        }

        /// <summary>
        /// Calculate the bid-ask spread at the best price level
        /// </summary>
        /// <returns>
        /// The difference between best ask and best bid price.
        /// Returns 0 if either side is empty.
        /// </returns>
        /// <example>
        /// If best bid is $100.00 and best ask is $100.05, returns $0.05
        /// </example>
        public decimal GetSpread()
        {
            if (Bids.Count > 0 && Asks.Count > 0)
            {
                return Asks[0].Price - Bids[0].Price;
            }
            return 0m;
        }

        /// <summary>
        /// Calculate the mid-market price (average of best bid and ask)
        /// </summary>
        /// <returns>
        /// The arithmetic mean of best bid and best ask prices.
        /// Returns 0 if either side is empty.
        /// </returns>
        /// <example>
        /// If best bid is $100.00 and best ask is $100.10, returns $100.05
        /// </example>
        public decimal GetMidPrice()
        {
            if (Bids.Count > 0 && Asks.Count > 0)
            {
                return (Bids[0].Price + Asks[0].Price) / 2m;
            }
            return 0m;
        }

        /// <summary>
        /// Get the best bid price and size
        /// </summary>
        /// <returns>
        /// Tuple containing (price, size) of the best bid level.
        /// Returns (0, 0) if no bids exist.
        /// </returns>
        /// <example>
        /// var (price, size) = depth.GetBestBid();
        /// // price = 100.00, size = 50.5
        /// </example>
        public (decimal price, decimal size) GetBestBid()
        {
            if (Bids.Count > 0)
            {
                return (Bids[0].Price, Bids[0].Size);
            }
            return (0m, 0m);
        }

        /// <summary>
        /// Get the best ask price and size
        /// </summary>
        /// <returns>
        /// Tuple containing (price, size) of the best ask level.
        /// Returns (0, 0) if no asks exist.
        /// </returns>
        /// <example>
        /// var (price, size) = depth.GetBestAsk();
        /// // price = 100.05, size = 75.0
        /// </example>
        public (decimal price, decimal size) GetBestAsk()
        {
            if (Asks.Count > 0)
            {
                return (Asks[0].Price, Asks[0].Size);
            }
            return (0m, 0m);
        }

        /// <summary>
        /// Get bid level at specified index (Python-friendly accessor)
        /// </summary>
        /// <param name="index">Zero-based index of bid level</param>
        /// <returns>OrderbookLevel at index, or null if index is invalid</returns>
        public OrderbookLevel GetBid(int index)
        {
            if (Bids == null || index < 0 || index >= Bids.Count)
                return null;
            return Bids[index];
        }

        /// <summary>
        /// Get ask level at specified index (Python-friendly accessor)
        /// </summary>
        /// <param name="index">Zero-based index of ask level</param>
        /// <returns>OrderbookLevel at index, or null if index is invalid</returns>
        public OrderbookLevel GetAsk(int index)
        {
            if (Asks == null || index < 0 || index >= Asks.Count)
                return null;
            return Asks[index];
        }

        /// <summary>
        /// Calculate how much quantity can be filled for a given dollar value
        /// </summary>
        /// <param name="side">"BUY" or "SELL" - BUY uses Ask liquidity, SELL uses Bid liquidity</param>
        /// <param name="targetValue">Dollar amount to fill</param>
        /// <returns>
        /// Tuple containing:
        /// - quantity: Total quantity that can be filled
        /// - weightedAvgPrice: Volume-weighted average price
        /// - levelsUsed: Number of price levels consumed
        /// Returns (0, 0, 0) if orderbook is empty.
        /// </returns>
        /// <example>
        /// // Calculate how much BTC can be bought with $10,000
        /// var (qty, avgPrice, levels) = depth.CalculateFillableQuantity("BUY", 10000m);
        /// // qty = 0.095, avgPrice = 105263.16, levels = 3
        /// </example>
        public (decimal quantity, decimal weightedAvgPrice, int levelsUsed) CalculateFillableQuantity(string side, decimal targetValue)
        {
            var levels = side == "BUY" ? Asks : Bids;

            if (levels.Count == 0)
            {
                return (0m, 0m, 0);
            }

            decimal remainingValue = targetValue;
            decimal totalQuantity = 0m;
            decimal totalCost = 0m;
            int levelsUsed = 0;

            foreach (var level in levels)
            {
                decimal levelValue = level.Price * level.Size;

                if (remainingValue >= levelValue)
                {
                    // Take entire level
                    totalQuantity += level.Size;
                    totalCost += levelValue;
                    remainingValue -= levelValue;
                    levelsUsed++;
                }
                else
                {
                    // Partial level
                    decimal partialQuantity = remainingValue / level.Price;
                    totalQuantity += partialQuantity;
                    totalCost += remainingValue;
                    levelsUsed++;
                    break;
                }
            }

            decimal weightedAvgPrice = totalQuantity > 0 ? totalCost / totalQuantity : 0m;
            return (totalQuantity, weightedAvgPrice, levelsUsed);
        }

        /// <summary>
        /// Calculate average execution price and slippage for a market order of given size
        /// </summary>
        /// <param name="side">"BUY" or "SELL" - BUY uses Ask liquidity, SELL uses Bid liquidity</param>
        /// <param name="quantity">Order quantity to fill</param>
        /// <returns>
        /// Tuple containing:
        /// - weightedAvgPrice: Volume-weighted average execution price
        /// - slippageBps: Slippage in basis points (1 bp = 0.01%) relative to best price
        /// - levelsUsed: Number of price levels needed to fill the order
        /// Returns (0, decimal.MaxValue, levelsUsed) if insufficient liquidity.
        /// </returns>
        /// <example>
        /// // Calculate slippage for buying 1000 shares
        /// var (avgPrice, slippageBps, levels) = depth.CalculateSlippage("BUY", 1000m);
        /// // avgPrice = 100.15, slippageBps = 15 (0.15% slippage), levels = 5
        /// </example>
        public (decimal weightedAvgPrice, decimal slippageBps, int levelsUsed) CalculateSlippage(string side, decimal quantity)
        {
            var levels = side == "BUY" ? Asks : Bids;

            if (levels.Count == 0)
            {
                return (0m, 0m, 0);
            }

            decimal bestPrice = levels[0].Price;
            decimal remainingQuantity = quantity;
            decimal totalCost = 0m;
            int levelsUsed = 0;

            foreach (var level in levels)
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                if (remainingQuantity >= level.Size)
                {
                    // Take entire level
                    totalCost += level.Price * level.Size;
                    remainingQuantity -= level.Size;
                    levelsUsed++;
                }
                else
                {
                    // Partial level
                    totalCost += level.Price * remainingQuantity;
                    levelsUsed++;
                    remainingQuantity = 0;
                    break;
                }
            }

            if (remainingQuantity > 0)
            {
                // Not enough liquidity
                return (0m, decimal.MaxValue, levelsUsed);
            }

            decimal weightedAvgPrice = totalCost / quantity;
            decimal slippageBps = Math.Abs(weightedAvgPrice - bestPrice) / bestPrice * 10000m;

            return (weightedAvgPrice, slippageBps, levelsUsed);
        }

        /// <summary>
        /// Orderbook Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType</param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Orderbook object or null</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // In live mode, data comes from brokerage WebSocket
                return null;
            }

            // Skip empty lines or comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                return null;
            }

            try
            {
                // Parse CSV without specifying fixed field count (dynamic level support)
                var csv = line.Split(',');

                // Validate CSV is not empty
                if (csv == null || csv.Length == 0)
                {
                    Log.Error($"Orderbook.Reader(): CSV parsing returned empty result for {config.Symbol}");
                    return null;
                }

                // Validate minimum field count (timestamp + at least 1 bid + 1 ask = 5 fields)
                if (csv.Length < 5)
                {
                    Log.Error($"Orderbook.Reader(): Insufficient fields ({csv.Length}) in CSV for {config.Symbol}");
                    return null;
                }

                var depth = new Orderbook
                {
                    Symbol = config.Symbol
                };

                // Parse timestamp using TryParse for safety (support both integer and decimal formats)
                if (!decimal.TryParse(csv[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal millisecondsDecimal))
                {
                    Log.Error($"Orderbook.Reader(): Invalid timestamp in CSV for {config.Symbol}");
                    return null;
                }
                long milliseconds = (long)millisecondsDecimal;

                // Convert milliseconds since midnight to DateTime
                depth.Time = date.Date.AddMilliseconds(milliseconds)
                    .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);

                // Dynamically determine number of levels from CSV
                // Format: 1 timestamp + N levels * 2 (price,size) for bids + N levels * 2 for asks
                // Total fields = 1 + 2*N + 2*N = 1 + 4*N
                int numLevels = (csv.Length - 1) / 4;

                // Validate level count is reasonable (1-20 levels)
                if (numLevels < 1 || numLevels > 20)
                {
                    Log.Error($"Orderbook.Reader(): Invalid level count {numLevels} for {config.Symbol}");
                    return null;
                }

                // Parse bid levels (best bid first) with TryParse for safety
                int index = 1;
                for (int i = 0; i < numLevels && index + 1 < csv.Length; i++)
                {
                    if (!decimal.TryParse(csv[index], out decimal price) ||
                        !decimal.TryParse(csv[index + 1], out decimal size))
                    {
                        // Skip invalid entries silently (don't spam logs for minor data issues)
                        index += 2;
                        continue;
                    }

                    if (price > 0 && size > 0)
                    {
                        depth.Bids.Add(new OrderbookLevel(price, size));
                    }
                    index += 2;
                }

                // Parse ask levels (best ask first) with TryParse for safety
                for (int i = 0; i < numLevels && index + 1 < csv.Length; i++)
                {
                    if (!decimal.TryParse(csv[index], out decimal price) ||
                        !decimal.TryParse(csv[index + 1], out decimal size))
                    {
                        // Skip invalid entries silently
                        index += 2;
                        continue;
                    }

                    if (price > 0 && size > 0)
                    {
                        depth.Asks.Add(new OrderbookLevel(price, size));
                    }
                    index += 2;
                }

                // Validate orderbook has data on both sides
                if (depth.Bids.Count == 0 || depth.Asks.Count == 0)
                {
                    return null;
                }

                // Validate bid-ask spread (best bid must be < best ask)
                if (depth.Bids[0].Price >= depth.Asks[0].Price)
                {
                    Log.Error($"Orderbook.Reader(): Invalid orderbook - crossed spread for {config.Symbol}. " +
                              $"Bid: {depth.Bids[0].Price}, Ask: {depth.Asks[0].Price}");
                    return null;
                }

                // Validate bids are sorted descending (best price first)
                for (int i = 1; i < depth.Bids.Count; i++)
                {
                    if (depth.Bids[i].Price > depth.Bids[i - 1].Price)
                    {
                        Log.Error($"Orderbook.Reader(): Bids not sorted descending for {config.Symbol}");
                        return null;
                    }
                }

                // Validate asks are sorted ascending (best price first)
                for (int i = 1; i < depth.Asks.Count; i++)
                {
                    if (depth.Asks[i].Price < depth.Asks[i - 1].Price)
                    {
                        Log.Error($"Orderbook.Reader(): Asks not sorted ascending for {config.Symbol}");
                        return null;
                    }
                }

                // Set Value as mid-price (required by BaseData)
                depth.Value = depth.GetMidPrice();
                depth.Levels = depth.Bids.Count;

                return depth;
            }
            catch (Exception err)
            {
                Log.Error(Invariant($"Orderbook.Reader(): Error parsing line: '{line}', Symbol: {config.Symbol.Value}, ") +
                          Invariant($"Resolution: {config.Resolution}, Date: {date.ToStringInvariant("yyyy-MM-dd")}, Message: {err}")
                );
                return null;
            }
        }

        /// <summary>
        /// Get Source for Orderbook data
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String source location of the file</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // This data type is streamed in live mode
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.Streaming);
            }

            // Validate and sanitize symbol to prevent path traversal attacks
            var symbol = config.Symbol.Value.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(symbol) ||
                symbol.Contains("..") ||
                symbol.Contains("/") ||
                symbol.Contains("\\"))
            {
                Log.Error($"Orderbook.GetSource(): Invalid symbol contains path characters: {config.Symbol}");
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile);
            }

            // Validate market
            var market = config.Symbol.ID.Market.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(market))
            {
                Log.Error($"Orderbook.GetSource(): Invalid market for symbol: {config.Symbol}");
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile);
            }

            // Infer SecurityType from Symbol, fallback to crypto for Base type
            // This allows Orderbook to support multiple asset classes (crypto, equity, forex, etc.)
            // When subscribed via AddData(), SecurityType is Base, so we default to crypto
            var securityType = config.Symbol.SecurityType == SecurityType.Base
                ? "crypto"  // Default for custom data subscriptions
                : Enum.GetName(typeof(SecurityType), config.Symbol.SecurityType).ToLowerInvariant();

            var resolution = config.Resolution.ResolutionToLower();
            var dateStr = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

            // Build path using Globals.DataFolder for absolute path resolution
            // Format: {Globals.DataFolder}/{securityType}/{market}/{resolution}/{symbol}/{date}_depth.zip
            var source = Path.Combine(Globals.DataFolder, securityType, market, resolution, symbol, $"{dateStr}_depth.zip");

            // Add ZIP entry name
            var entryName = $"{dateStr}_{symbol}_tick_depth.csv";
            source += "#" + entryName;

            // Only log in debug mode to avoid spamming logs
            if (Log.DebuggingEnabled)
            {
                Log.Debug($"Orderbook.GetSource(): {config.Symbol} -> {source}");
            }

            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Return a new instance clone of this orderbook depth, used in fill forward
        /// </summary>
        /// <returns>A clone of the current orderbook depth</returns>
        public override BaseData Clone()
        {
            return new Orderbook
            {
                Bids = new List<OrderbookLevel>(Bids.Select(b => new OrderbookLevel(b.Price, b.Size))),
                Asks = new List<OrderbookLevel>(Asks.Select(a => new OrderbookLevel(a.Price, a.Size))),
                Levels = Levels,
                Symbol = Symbol,
                Time = Time,
                Value = Value,
                DataType = DataType
            };
        }

        /// <summary>
        /// Formats a string with the Orderbook information
        /// </summary>
        public override string ToString()
        {
            var bidStr = Bids.Count > 0 ? Bids[0].ToString() : "N/A";
            var askStr = Asks.Count > 0 ? Asks[0].ToString() : "N/A";
            return $"{Symbol}: Bid: {bidStr}, Ask: {askStr}, Levels: {Levels}";
        }
    }
}
