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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The settlement model <see cref="ISettlementModel.Scan(ScanSettlementModelParameters)"/> parameters
    /// </summary>
    public class ScanSettlementModelParameters
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
        /// Creates a new instance
        /// </summary>
        /// <param name="portfolio">The algorithm portfolio</param>
        /// <param name="security">The associated security type</param>
        /// <param name="timeUtc">The current utc time</param>
        public ScanSettlementModelParameters(SecurityPortfolioManager portfolio, Security security, DateTime timeUtc)
        {
            Portfolio = portfolio;
            Security = security;
            UtcTime = timeUtc;
        }
    }
}
