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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Defines the parameters for <see cref="IBuyingPowerModel.HasSufficientBuyingPowerForOrder"/>
    /// </summary>
    public class HasSufficientBuyingPowerForOrderParameters
    {
        /// <summary>
        /// Gets the algorithm's portfolio
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; }

        /// <summary>
        /// Gets the security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the order
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HasSufficientBuyingPowerForOrderParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security</param>
        /// <param name="order">The order</param>
        public HasSufficientBuyingPowerForOrderParameters(
            SecurityPortfolioManager portfolio,
            Security security,
            Order order
        )
        {
            Portfolio = portfolio;
            Security = security;
            Order = order;
        }

        /// <summary>
        /// Creates a new <see cref="HasSufficientBuyingPowerForOrderParameters"/> targeting the security's underlying.
        /// If the security does not implement <see cref="IDerivativeSecurity"/> then an <see cref="InvalidCastException"/>
        /// will be thrown. If the order's symbol does not match the underlying then an <see cref="ArgumentException"/> will
        /// be thrown.
        /// </summary>
        /// <param name="order">The new order targeting the underlying</param>
        /// <returns>New parameters instance suitable for invoking the sufficient capital method for the underlying security</returns>
        public HasSufficientBuyingPowerForOrderParameters ForUnderlying(Order order)
        {
            var derivative = (IDerivativeSecurity)Security;
            return new HasSufficientBuyingPowerForOrderParameters(
                Portfolio,
                derivative.Underlying,
                order
            );
        }

        /// <summary>
        /// Creates a new result indicating that there is sufficient buying power for the contemplated order
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Sufficient()
        {
            return new HasSufficientBuyingPowerForOrderResult(true);
        }

        /// <summary>
        /// Creates a new result indicating that there is insufficient buying power for the contemplated order
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Insufficient(string reason)
        {
            return new HasSufficientBuyingPowerForOrderResult(false, reason);
        }

        /// <summary>
        /// Creates a new result indicating that there is insufficient buying power for the contemplated order
        /// </summary>
        public HasSufficientBuyingPowerForOrderResult Insufficient(FormattableString reason)
        {
            return new HasSufficientBuyingPowerForOrderResult(false, Invariant(reason));
        }
    }
}
