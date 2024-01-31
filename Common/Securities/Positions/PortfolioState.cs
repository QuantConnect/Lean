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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Snapshot of an algorithms portfolio state
    /// </summary>
    public class PortfolioState
    {
        /// <summary>
        /// Utc time this portfolio snapshot was taken
        /// </summary>
        public DateTime Time { get; set; }

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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PositionGroupState> PositionGroups { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only settled cash)
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> CashBook { get; set; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only unsettled cash)
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, Cash> UnsettledCashBook { get; set; }

        /// <summary>
        /// Helper method to create the portfolio state snapshot
        /// </summary>
        public static PortfolioState Create(SecurityPortfolioManager portfolioManager, DateTime utcNow, decimal currentPortfolioValue)
        {
            try
            {
                var totalMarginUsed = 0m;
                var positionGroups = new List<PositionGroupState>(portfolioManager.Positions.Groups.Count);
                foreach (var group in portfolioManager.Positions.Groups)
                {
                    var buyingPowerForPositionGroup = group.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(portfolioManager, group);

                    var positionGroupState = new PositionGroupState
                    {
                        MarginUsed = buyingPowerForPositionGroup,
                        Positions = group.Positions.ToList()
                    };
                    if (currentPortfolioValue != 0)
                    {
                        positionGroupState.PortfolioValuePercentage = (buyingPowerForPositionGroup / currentPortfolioValue).RoundToSignificantDigits(4);
                    }

                    positionGroups.Add(positionGroupState);
                    totalMarginUsed += buyingPowerForPositionGroup;
                }

                var result = new PortfolioState
                {
                    Time = utcNow,
                    TotalPortfolioValue = currentPortfolioValue,
                    TotalMarginUsed = totalMarginUsed,
                    CashBook = portfolioManager.CashBook.Where(pair => pair.Value.Amount != 0).ToDictionary(pair => pair.Key, pair => pair.Value)
                };

                var unsettledCashBook = portfolioManager.UnsettledCashBook
                    .Where(pair => pair.Value.Amount != 0)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                if (positionGroups.Count > 0)
                {
                    result.PositionGroups = positionGroups;
                }
                if (unsettledCashBook.Count > 0)
                {
                    result.UnsettledCashBook = unsettledCashBook;
                }
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e);
                return null;
            }
        }
    }

    /// <summary>
    /// Snapshot of a position group state
    /// </summary>
    public class PositionGroupState
    {
        /// <summary>
        /// Name of this position group
        /// </summary>
        [JsonIgnore]
        public string Name { get; set; }

        /// <summary>
        /// Currently margin used
        /// </summary>
        public decimal MarginUsed { get; set; }

        /// <summary>
        /// The margin used by this position in relation to the total portfolio value
        /// </summary>
        public decimal PortfolioValuePercentage { get; set; }

        /// <summary>
        /// The positions which compose this group
        /// </summary>
        public List<IPosition> Positions { get; set; }
    }
}
