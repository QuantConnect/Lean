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

using QuantConnect.Orders;
using System;

namespace QuantConnect.Securities.Equity
{
    /// <summary>
    /// Represents a simple margining model where margin/leverage depends on market state (open or close).
    /// During regular market hours, leverage is 4x, otherwise 2x
    /// </summary>
    public class PatternDayTradingMarginModel : SecurityMarginModel
    {
        private readonly decimal _openMarketLeverage;
        private readonly decimal _closedMarketLeverage;

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
        /// <param name="closedmarketleverage"></param>
        /// <param name="openmarketleverage"></param>
        public PatternDayTradingMarginModel(decimal closedmarketleverage, decimal openmarketleverage):
            base(openmarketleverage)
        {
            _closedMarketLeverage = closedmarketleverage;
            _openMarketLeverage = openmarketleverage;   
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public override decimal GetLeverage(Security security)
        {
            return 1 / (MaintenanceMarginRequirement * GetMarginCorrection(security));
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// Do nothing, we use a constant leverage for this model
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <remarks>
        /// If we are in regular market hours, base 4x leverage is used, otherwise leverage is reduced to 2x
        /// </remarks>
        /// <param name="security">The security to compute initial margin for</param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            var orderFees = security.FeeModel.GetOrderFee(security, order);

            return order.GetValue(security)*InitialMarginRequirement*GetMarginCorrection(security) + orderFees;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        public override decimal GetMaintenanceMargin(Security security)
        {
            return security.Holdings.AbsoluteHoldingsCost*(MaintenanceMarginRequirement* GetMarginCorrection(security));
        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The total margin used by the account in units of base currency</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public override SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin)
        {
            // leave a buffer in default implementation
            const decimal marginBuffer = 0.10m;

            if (totalMargin <= netLiquidationValue * (1 + marginBuffer))
            {
                return null;
            }

            if (!security.Holdings.Invested)
            {
                return null;
            }

            // compute the value we need to liquidate in order to get within margin requirements
            var delta = totalMargin - netLiquidationValue;

            // compute the number of shares required for the order, rounding up
            var quantity = (int)(Math.Round(delta / security.Price, MidpointRounding.AwayFromZero) / (MaintenanceMarginRequirement*GetMarginCorrection(security)));

            // don't try and liquidate more share than we currently hold, minimum value of 1, maximum value for absolute quantity
            quantity = Math.Max(1, Math.Min((int)security.Holdings.AbsoluteQuantity, quantity));
            if (security.Holdings.IsLong)
            {
                // adjust to a sell for long positions
                quantity *= -1;
            }

            return new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, security.LocalTime.ConvertToUtc(security.Exchange.TimeZone), "Margin Call");
        }

        /// <summary>
        /// Get margin correction for closed market
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        private decimal GetMarginCorrection(Security security)
        {
            return security.Exchange.ExchangeOpen ? 1 : _openMarketLeverage / _closedMarketLeverage;
        }
    }
}