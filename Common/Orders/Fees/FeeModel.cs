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

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Base class for any order fee model
    /// </summary>
    /// <remarks>Please use <see cref="FeeModel"/> as the base class for
    /// any implementations of <see cref="IFeeModel"/></remarks>
    public class FeeModel : IFeeModel
    {
        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in a <see cref="CashAmount"/> instance</returns>
        public virtual OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            return new OrderFee(new CashAmount(
                0,
                "USD"));
        }

        /// <summary>
        /// Gets the value of the order in the given currency, instead of the account currency of <see cref="Order.GetValue"/>
        /// </summary>
        /// <param name="order">The order to value</param>
        /// <param name="security">The security matching the order's symbol</param>
        /// <param name="currency">The currency to value the order in</param>
        /// <returns>The value of the order in the given currency, or zero when it is the security's quote or base currency and has no conversion rate yet</returns>
        internal static decimal GetOrderValue(Order order, Security security, string currency)
        {
            decimal conversionRate;
            if (security.QuoteCurrency.Symbol == currency)
            {
                conversionRate = security.QuoteCurrency.ConversionRate;
            }
            else if (security is IBaseCurrencySymbol baseCurrencySymbol && baseCurrencySymbol.BaseCurrency.Symbol == currency)
            {
                conversionRate = baseCurrencySymbol.BaseCurrency.ConversionRate;
            }
            else
            {
                conversionRate = security.Holdings.CurrencyConverter.ConvertToAccountCurrency(new CashAmount(1, currency)).Amount;
            }

            return conversionRate == 0 ? 0 : order.GetValue(security) / conversionRate;
        }
    }
}