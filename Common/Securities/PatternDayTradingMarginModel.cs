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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a simple margining model where margin/leverage depends on market state (open or close).
    /// During regular market hours, leverage is 4x, otherwise 2x
    /// </summary>
    public class PatternDayTradingMarginModel : SecurityMarginModel
    {
        private readonly decimal _closedMarginCorrectionFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternDayTradingMarginModel" />
        /// </summary>
        public PatternDayTradingMarginModel()
            : this(2.0m, 4.0m)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternDayTradingMarginModel" />
        /// </summary>
        /// <param name="closedMarketLeverage">Leverage used outside regular market hours</param>
        /// <param name="openMarketLeverage">Leverage used during regular market hours</param>
        public PatternDayTradingMarginModel(decimal closedMarketLeverage, decimal openMarketLeverage)
            : base(openMarketLeverage)
        {
            _closedMarginCorrectionFactor = openMarketLeverage/closedMarketLeverage;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// Do nothing, we use a constant leverage for this model
        /// </remarks>
        /// <param name="security">The security to set leverage to</param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
        }

        /// <summary>
        /// The percentage of an order's absolute cost that must be held in free cash in order to place the order
        /// </summary>
        protected override decimal GetInitialMarginRequirement(Security security)
        {
            return base.GetInitialMarginRequirement(security)*GetMarginCorrectionFactor(security);
        }

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        public override decimal GetMaintenanceMarginRequirement(Security security)
        {
            return base.GetMaintenanceMarginRequirement(security)*GetMarginCorrectionFactor(security);
        }

        /// <summary>
        /// Get margin correction factor if not in regular market hours
        /// </summary>
        /// <param name="security">The security to apply conditional leverage to</param>
        /// <returns>The margin correction factor</returns>
        private decimal GetMarginCorrectionFactor(Security security)
        {
            // when the market is open the base type returns the correct values
            // when the market is closed, we need to multiply by a correction factor
            return security.Exchange.ExchangeOpen ? 1m : _closedMarginCorrectionFactor;
        }
    }
}
