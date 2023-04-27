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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Helper parameters class for <see cref="ISettlementModel.ApplyFunds(ApplyFundsSettlementModelParameters)"/>
    /// </summary>
    public class ApplyFundsSettlementModelParameters
    {
        /// <summary>
        /// The algorithm portfolio instance
        /// </summary>
        public SecurityPortfolioManager Portfolio { get; set; }

        /// <summary>
        /// The associated security type
        /// </summary>
        public Security Security { get; set; }

        /// <summary>
        /// The current Utc time
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// The funds to apply
        /// </summary>
        public CashAmount CashAmount { get; set; }

        /// <summary>
        /// The associated fill event
        /// </summary>
        public OrderEvent Fill { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The fill's security</param>
        /// <param name="applicationTimeUtc">The fill time (in UTC)</param>
        /// <param name="cashAmount">The amount to settle</param>
        /// <param name="fill">The associated fill</param>
        public ApplyFundsSettlementModelParameters(SecurityPortfolioManager portfolio, Security security, DateTime applicationTimeUtc, CashAmount cashAmount, OrderEvent fill)
        {
            Portfolio = portfolio;
            Security = security;
            UtcTime = applicationTimeUtc;
            CashAmount = cashAmount;
            Fill = fill;
        }
    }
}
