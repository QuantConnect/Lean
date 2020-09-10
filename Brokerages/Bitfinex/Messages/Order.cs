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
using Newtonsoft.Json;
using QuantConnect.Brokerages.Bitfinex.Converters;

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Bitfinex Order
    /// </summary>
    [JsonConverter(typeof(OrderConverter))]
    public class Order
    {
        /// <summary>
        /// Order ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Group ID
        /// </summary>
        public long GroupId { get; set; }

        /// <summary>
        /// Client Order ID
        /// </summary>
        public long ClientOrderId { get; set; }

        /// <summary>
        /// Pair (tBTCUSD, …)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Millisecond timestamp of creation
        /// </summary>
        public long MtsCreate { get; set; }

        /// <summary>
        /// Millisecond timestamp of update
        /// </summary>
        public long MtsUpdate { get; set; }

        /// <summary>
        /// Positive means buy, negative means sell.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Original amount
        /// </summary>
        public decimal AmountOrig { get; set; }

        /// <summary>
        /// The type of the order:
        /// - LIMIT, MARKET, STOP, STOP LIMIT, TRAILING STOP,
        /// - EXCHANGE MARKET, EXCHANGE LIMIT, EXCHANGE STOP, EXCHANGE STOP LIMIT,
        /// - EXCHANGE TRAILING STOP, FOK, EXCHANGE FOK, IOC, EXCHANGE IOC.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Previous order type
        /// </summary>
        public string TypePrev { get; set; }

        /// <summary>
        /// Active flags for order
        /// </summary>
        public int Flags { get; set; }

        /// <summary>
        /// Order Status:
        /// - ACTIVE,
        /// - EXECUTED @ PRICE(AMOUNT) e.g. "EXECUTED @ 107.6(-0.2)",
        /// - PARTIALLY FILLED @ PRICE(AMOUNT),
        /// - INSUFFICIENT MARGIN was: PARTIALLY FILLED @ PRICE(AMOUNT),
        /// - CANCELED,
        /// - CANCELED was: PARTIALLY FILLED @ PRICE(AMOUNT),
        /// - RSN_DUST (amount is less than 0.00000001),
        /// - RSN_PAUSE (trading is paused / paused due to AMPL rebase event)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Average price
        /// </summary>
        public decimal PriceAvg { get; set; }

        /// <summary>
        /// The trailing price
        /// </summary>
        public decimal PriceTrailing { get; set; }

        /// <summary>
        /// Auxiliary Limit price (for STOP LIMIT)
        /// </summary>
        public decimal PriceAuxLimit { get; set; }

        /// <summary>
        /// 1 if Hidden, 0 if not hidden
        /// </summary>
        public int Hidden { get; set; }

        /// <summary>
        /// If another order caused this order to be placed (OCO) this will be that other order's ID
        /// </summary>
        public int PlacedId { get; set; }

        public bool IsExchange => Type.StartsWith("EXCHANGE", StringComparison.OrdinalIgnoreCase);
    }
}
