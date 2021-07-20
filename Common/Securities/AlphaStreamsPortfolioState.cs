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
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Snapshot of an algorithms portfolio state
    /// </summary>
    public class AlphaStreamsPortfolioState
    {
        /// <summary>
        /// Portfolio state id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Algorithms account currency
        /// </summary>
        public string AccountCurrency { get; set; }

        /// <summary>
        /// The utc time this state was captured
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// The current total portfolio value
        /// </summary>
        public decimal TotalPortfolioValue { get; set; }

        /// <summary>
        /// The margin used
        /// </summary>
        public decimal TotalMarginUsed { get; set; }

        /// <summary>
        /// The different positions groups
        /// </summary>
        [JsonProperty("positionGroups", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PositionGroupState> PositionGroups { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only settled cash)
        /// </summary>
        [JsonProperty("cashBook", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> CashBook { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only unsettled cash)
        /// </summary>
        [JsonProperty("unsettledCashBook", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> UnsettledCashBook { get; set; }
    }

    /// <summary>
    /// Snapshot of a position group state
    /// </summary>
    public class PositionGroupState
    {
        /// <summary>
        /// Currently margin used
        /// </summary>
        public decimal MarginUsed { get; set; }

        /// <summary>
        /// The margin used by this position in relation to the total portfolio value
        /// </summary>
        public decimal PortfolioValuePercentage { get; set; }

        /// <summary>
        /// THe positions which compose this group
        /// </summary>
        public List<PositionState> Positions { get; set; }
    }

    /// <summary>
    /// Snapshot of a position state
    /// </summary>
    public class PositionState : IPosition
    {
        /// <summary>
        /// The symbol
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The unit quantity. The unit quantities of a group define the group. For example, a covered
        /// call has 100 units of stock and -1 units of call contracts.
        /// </summary>
        public decimal UnitQuantity { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public static PositionState Create(IPosition position)
        {
            return new PositionState
            {
                Symbol = position.Symbol,
                Quantity = position.Quantity,
                UnitQuantity = position.UnitQuantity
            };
        }
    }
}
