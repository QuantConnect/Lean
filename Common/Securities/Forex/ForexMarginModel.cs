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

namespace QuantConnect.Securities.Forex
{
    /// <summary>
    /// Represents a simple, constant margining model by specifying the percentages of required margin.
    /// This implementation differs from the base <see cref="SecurityMarginModel"/> in that it applies
    /// conversion rates from quote currency to the account currency
    /// </summary>
    public class ForexMarginModel : SecurityMarginModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForexMarginModel"/> class
        /// </summary>
        /// <param name="initialMarginRequirement">The percentage of an order's absolute cost
        /// that must be held in free cash in order to place the order</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute
        /// cost that must be held in free cash in order to avoid a margin call</param>
        public ForexMarginModel(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
            : base(initialMarginRequirement, maintenanceMarginRequirement)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForexMarginModel"/>
        /// </summary>
        /// <param name="leverage">The leverage</param>
        public ForexMarginModel(decimal leverage)
            : base(leverage)
        {
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="security">The security to compute initial margin for</param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            var forex = (Forex)security;

            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.
            var orderFees = security.TransactionModel.GetOrderFee(security, order);

            var price = order.Status.IsFill() ? order.Price : security.Price;
            var orderCostInAccountCurrency = order.GetValue(price)*forex.QuoteCurrency.ConversionRate;
            return orderCostInAccountCurrency*InitialMarginRequirement + orderFees;
        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The totl margin used by the account in units of base currency</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public override SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin)
        {
            if (totalMargin <= netLiquidationValue)
            {
                return null;
            }
            var forex = (Forex)security;

            // we haven't begun receiving data for this yet
            if (forex.Price == 0m || forex.QuoteCurrency.ConversionRate == 0m)
            {
                return null;
            }

            // compute the amount of quote currency we need to liquidate in order to get within margin requirements
            decimal delta = (totalMargin - netLiquidationValue)/forex.QuoteCurrency.ConversionRate;

            // compute the number of shares required for the order, rounding up
            int quantity = (int) Math.Round(delta/security.Price, MidpointRounding.AwayFromZero);

            // don't try and liquidate more share than we currently hold
            quantity = Math.Min((int)security.Holdings.AbsoluteQuantity, quantity);
            if (security.Holdings.IsLong)
            {
                // adjust to a sell for long positions
                quantity *= -1;
            }

            return new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, security.LocalTime.ConvertToUtc(security.Exchange.TimeZone), "Margin Call");
        }
    }
}
