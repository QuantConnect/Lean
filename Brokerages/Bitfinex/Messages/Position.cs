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

using Newtonsoft.Json;
using QuantConnect.Brokerages.Bitfinex.Converters;

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Bitfinex position
    /// </summary>
    [JsonConverter(typeof(PositionConverter))]
    public class Position
    {
        /// <summary>
        /// Pair (tBTCUSD, …).
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Status (ACTIVE, CLOSED).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Size of the position. A positive value indicates a long position; a negative value indicates a short position.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Base price of the position. (Average traded price of the previous orders of the position)
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// The amount of funding being used for this position.
        /// </summary>
        public decimal MarginFunding { get; set; }

        /// <summary>
        /// 0 for daily, 1 for term.
        /// </summary>
        public int MarginFundingType { get; set; }

        /// <summary>
        /// Profit &amp; Loss
        /// </summary>
        public decimal ProfitLoss { get; set; }

        /// <summary>
        /// Profit &amp; Loss Percentage
        /// </summary>
        public decimal ProfitLossPerc { get; set; }

        /// <summary>
        /// Liquidation price
        /// </summary>
        public decimal PriceLiq { get; set; }

        /// <summary>
        /// Leverage used for the position
        /// </summary>
        public decimal Leverage { get; set; }

        /// <summary>
        /// Position ID
        /// </summary>
        public long PositionId { get; set; }

        /// <summary>
        /// Millisecond timestamp of creation
        /// </summary>
        public long MtsCreate { get; set; }

        /// <summary>
        /// Millisecond timestamp of update
        /// </summary>
        public long MtsUpdate { get; set; }

        /// <summary>
        /// Identifies the type of position, 0 = Margin position, 1 = Derivatives position
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// The amount of collateral applied to the open position
        /// </summary>
        public decimal Collateral { get; set; }

        /// <summary>
        /// The minimum amount of collateral required for the position
        /// </summary>
        public decimal CollateralMin { get; set; }

        /// <summary>
        /// Additional meta information about the position (JSON string)
        /// </summary>
        public object Meta { get; set; }
    }
}
