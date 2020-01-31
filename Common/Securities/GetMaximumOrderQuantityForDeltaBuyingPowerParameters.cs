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
    /// Defines the parameters for <see cref="IBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower"/>
    /// </summary>
    public class GetMaximumOrderQuantityForDeltaBuyingPowerParameters
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
        /// The delta buying power.
        /// </summary>
        /// <remarks> Sign defines the position side to apply the delta, positive long, negative short side.</remarks>
        public decimal DeltaBuyingPower { get; }

        /// <summary>
        /// True enables the <see cref="IBuyingPowerModel"/> to skip setting <see cref="GetMaximumOrderQuantityResult.Reason"/>
        /// for non error situations, for performance
        /// </summary>
        public bool SilenceNonErrorReasons { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetValueParameters"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security</param>
        /// <param name="deltaBuyingPower">The delta buying power to apply.
        /// Sign defines the position side to apply the delta</param>
        /// <param name="silenceNonErrorReasons">True will not return <see cref="GetMaximumOrderQuantityResult.Reason"/>
        /// set for non error situation, this is for performance</param>
        public GetMaximumOrderQuantityForDeltaBuyingPowerParameters(SecurityPortfolioManager portfolio, Security security, decimal deltaBuyingPower, bool silenceNonErrorReasons = false)
        {
            Portfolio = portfolio;
            Security = security;
            DeltaBuyingPower = deltaBuyingPower;
            SilenceNonErrorReasons = silenceNonErrorReasons;
        }
    }
}
