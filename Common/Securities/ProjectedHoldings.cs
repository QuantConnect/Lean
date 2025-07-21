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
    /// DTO for the projected holdings of a security
    /// </summary>
    public class ProjectedHoldings
    {
        /// <summary>
        /// The current holdings for the security
        /// </summary>
        public decimal HoldingsQuantity { get; set; }

        /// <summary>
        /// The currently open orders quantity for the security
        /// </summary>
        public decimal OpenOrdersQuantity { get; set; }

        /// <summary>
        /// Gets the projected holdings for the specified security, which is the sum of the current holdings
        /// plus the sum of the open orders quantity.
        /// </summary>
        public decimal ProjectedQuantity => HoldingsQuantity + OpenOrdersQuantity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedHoldings"/> class.
        /// </summary>
        /// <param name="holdingsQuantity">The current holdings quantity</param>
        /// <param name="openOrdersQuantity">The currently open orders quantity for the security</param>
        public ProjectedHoldings(decimal holdingsQuantity, decimal openOrdersQuantity)
        {
            HoldingsQuantity = holdingsQuantity;
            OpenOrdersQuantity = openOrdersQuantity;
        }
    }
}
