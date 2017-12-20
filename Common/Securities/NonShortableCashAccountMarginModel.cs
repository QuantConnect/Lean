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
    /// Provides an implementation of <see cref="ISecurityMarginModel"/> that requires cash of the correct
    /// currency to exist in the portfolio to make trades. This also only applies to securities where shorting
    /// is not an option. IOW, holdings.Quantity >= 0 at all times.
    /// </summary>
    public class NonShortableCashAccountMarginModel : ISecurityMarginModel
    {
        private readonly CashBook _cashBook;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonShortableCashAccountMarginModel"/> class
        /// </summary>
        /// <param name="cashBook">The portfolio's cashbook</param>
        public NonShortableCashAccountMarginModel(CashBook cashBook)
        {
            _cashBook = cashBook;
        }

        /// <summary>
        /// Cash accounts require the full value of the order plus any fees
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            var fees = security.FeeModel.GetOrderFee(security, order);
            var marginRequiredInAccountCurrency = order.GetValue(security) + fees;

            switch (order.Direction)
            {
                case OrderDirection.Hold:
                case OrderDirection.Buy:
                    // convert the order value to the quote currency
                    return marginRequiredInAccountCurrency / security.QuoteCurrency.ConversionRate;

                case OrderDirection.Sell:
                    // remaining margin in units of the base currency ( can't sell what we don't have )
                    var baseCurrency = security as IBaseCurrencySymbol;
                    if (baseCurrency != null)
                    {
                        // we have this much 'cash' that can be sold
                        var feesInBaseCurrency = _cashBook.Convert(fees, CashBook.AccountCurrency, baseCurrency.BaseCurrencySymbol);
                        return order.AbsoluteQuantity + feesInBaseCurrency;
                    }

                    // assuming these are 'units' of stock priced in the account currency
                    return marginRequiredInAccountCurrency;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets the margin cash available for a trade in units of the currency required.
        /// When increasing a position, this is the quote currency, when decreasing a position this is the base currency
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        public decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
        {
            switch (direction)
            {
                case OrderDirection.Hold:
                case OrderDirection.Buy:
                    // increasing position, purchasing in units of the quote currency
                    return security.QuoteCurrency.Amount;

                case OrderDirection.Sell:
                    // remaining margin in units of the base currency ( can't sell what we don't have )
                    var baseCurrency = security as IBaseCurrencySymbol;
                    if (baseCurrency != null)
                    {
                        // we have this much 'cash' that can be sold
                        return _cashBook[baseCurrency.BaseCurrencySymbol].Amount;
                    }

                    // we have this much stock value that can be sold... this is in the account currency,
                    // the assumption being that since the security doesn't implement IBaseCurrencySymbol
                    // that the holdings are either 'units' of stock or similar and not currency swaps/virtual positions
                    return security.Holdings.AbsoluteHoldingsValue;

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>
        /// Returns 1. Cash accounts have no leverage.
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public decimal GetLeverage(Security security)
        {
            return 1m;
        }

        /// <summary>
        /// No action performed. This model always uses a leverage = 1
        /// </summary>
        /// <param name="security">The security to set leverage for</param>
        /// <param name="leverage">The new leverage</param>
        public void SetLeverage(Security security, decimal leverage)
        {
            // NOP
        }

        /// <summary>
        /// Returns 0. Since we're purchasing stock out right, the position doesn't continue to weigh down the portfolio
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        public decimal GetMaintenanceMargin(Security security)
        {
            return 0m;
        }

        /// <summary>
        /// Returns 1. Cash accounts have no leverage and require 100% at the time of order
        /// </summary>
        public decimal GetInitialMarginRequirement(Security security)
        {
            return 1m;
        }

        /// <summary>
        /// Returns 0. Since we're purchasing currencies outright, the position doesn't consume margin
        /// </summary>
        public decimal GetMaintenanceMarginRequirement(Security security)
        {
            // since we purchased the stock/currency outright, there is no maintenance requirement
            return 0m;
        }
    }
}
