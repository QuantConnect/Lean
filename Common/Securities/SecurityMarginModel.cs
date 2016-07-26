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
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a simple, constant margining model by specifying the percentages of required margin.
    /// </summary>
    public class SecurityMarginModel : ISecurityMarginModel
    {
        private decimal _initialMarginRequirement;
        private decimal _maintenanceMarginRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityMarginModel"/>
        /// </summary>
        /// <param name="initialMarginRequirement">The percentage of an order's absolute cost
        /// that must be held in free cash in order to place the order</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute
        /// cost that must be held in free cash in order to avoid a margin call</param>
        public SecurityMarginModel(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
        {
            if (initialMarginRequirement < 0 || initialMarginRequirement > 1)
            {
                throw new ArgumentException("Initial margin requirement must be between 0 and 1");
            }

            if (maintenanceMarginRequirement < 0 || maintenanceMarginRequirement > 1)
            {
                throw new ArgumentException("Maintenance margin requirement must be between 0 and 1");
            }

            _initialMarginRequirement = initialMarginRequirement;
            _maintenanceMarginRequirement = maintenanceMarginRequirement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityMarginModel"/>
        /// </summary>
        /// <param name="leverage">The leverage</param>
        public SecurityMarginModel(decimal leverage)
        {
            if (leverage < 1)
            {
                throw new ArgumentException("Leverage must be greater than or equal to 1.");
            }

            _initialMarginRequirement = 1/leverage;
            _maintenanceMarginRequirement = 1/leverage;
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public virtual decimal GetLeverage(Security security)
        {
            return 1/GetMaintenanceMarginRequirement(security);
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public virtual void SetLeverage(Security security, decimal leverage)
        {
            if (leverage < 1)
            {
                throw new ArgumentException("Leverage must be greater than or equal to 1.");
            }

            decimal margin = 1/leverage;
            _initialMarginRequirement = margin;
            _maintenanceMarginRequirement = margin;
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="security">The security to compute initial margin for</param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public virtual decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.
            var orderFees = security.FeeModel.GetOrderFee(security, order);

            var orderValue = order.GetValue(security) * GetInitialMarginRequirement(security);
            return orderValue + Math.Sign(orderValue) * orderFees;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        public virtual decimal GetMaintenanceMargin(Security security)
        {
            return security.Holdings.AbsoluteHoldingsCost*GetMaintenanceMarginRequirement(security);
        }

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        public virtual decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
        {
            var holdings = security.Holdings;

            if (direction == OrderDirection.Hold)
            {
                return portfolio.MarginRemaining;
            }

            //If the order is in the same direction as holdings, our remaining cash is our cash
            //In the opposite direction, our remaining cash is 2 x current value of assets + our cash
            if (holdings.IsLong)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return portfolio.MarginRemaining;

                    case OrderDirection.Sell:
                        return 
                            // portion of margin to close the existing position
                            GetMaintenanceMargin(security) +
                            // portion of margin to open the new position
                            security.Holdings.AbsoluteHoldingsValue * GetInitialMarginRequirement(security) +
                            portfolio.MarginRemaining;
                }
            }
            else if (holdings.IsShort)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return
                            // portion of margin to close the existing position
                            GetMaintenanceMargin(security) +
                            // portion of margin to open the new position
                            security.Holdings.AbsoluteHoldingsValue * GetInitialMarginRequirement(security) +
                            portfolio.MarginRemaining;

                    case OrderDirection.Sell:
                        return portfolio.MarginRemaining;
                }
            }

            //No holdings, return cash
            return portfolio.MarginRemaining;
        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The total margin used by the account in units of base currency</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public virtual SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin)
        {
            // leave a buffer in default implementation
            const decimal marginBuffer = 0.10m;

            if (totalMargin <= netLiquidationValue*(1 + marginBuffer))
            {
                return null;
            }

            if (!security.Holdings.Invested)
            {
                return null;
            }

            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                // check for div 0 - there's no conv rate, so we can't place an order
                return null;
            }

            // compute the amount of quote currency we need to liquidate in order to get within margin requirements
            var deltaInQuoteCurrency = (totalMargin - netLiquidationValue)/security.QuoteCurrency.ConversionRate;

            // compute the number of shares required for the order, rounding up
            var unitPriceInQuoteCurrency = security.Price * security.SymbolProperties.ContractMultiplier;
            int quantity = (int) (Math.Round(deltaInQuoteCurrency/unitPriceInQuoteCurrency, MidpointRounding.AwayFromZero)/GetMaintenanceMarginRequirement(security));

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
        /// The percentage of an order's absolute cost that must be held in free cash in order to place the order
        /// </summary>
        protected virtual decimal GetInitialMarginRequirement(Security security)
        {
            return _initialMarginRequirement;
        }

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        protected virtual decimal GetMaintenanceMarginRequirement(Security security)
        {
            return _maintenanceMarginRequirement;
        }
    }
}