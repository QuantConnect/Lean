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
    /// Event arguments for the <see cref="SecurityHolding.QuantityChanged"/> event.
    /// The event data contains the previous quantity/price. The current quantity/price
    /// can be accessed via the <see cref="SecurityEventArgs.Security"/> property
    /// </summary>
    public class SecurityHoldingQuantityChangedEventArgs : SecurityEventArgs
    {
        /// <summary>
        /// Gets the holdings quantity before this change
        /// </summary>
        public decimal PreviousQuantity { get; }

        /// <summary>
        /// Gets the average holdings price before this change
        /// </summary>
        public decimal PreviousAveragePrice { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHoldingQuantityChangedEventArgs"/> class
        /// </summary>
        /// <param name="security">The security</param>
        /// <param name="previousAveragePrice">The security's previous average holdings price</param>
        /// <param name="previousQuantity">The security's previous holdings quantity</param>
        public SecurityHoldingQuantityChangedEventArgs(
            Security security,
            decimal previousAveragePrice,
            decimal previousQuantity
        )
            : base(security)
        {
            PreviousQuantity = previousQuantity;
            PreviousAveragePrice = previousAveragePrice;
        }
    }
}
