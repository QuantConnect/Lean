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

using System.Collections.Generic;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Defines some features of the algorithm, such as security types traded and custom charting
    /// </summary>
    public class AlgorithmFeatures
    {
        /// <summary>
        /// The algorithm has traded equities
        /// </summary>
        public bool HasEquity { get; set; }

        /// <summary>
        /// The algorithm has traded options
        /// </summary>
        public bool HasOption { get; set; }

        /// <summary>
        /// the algorithm has traded commodities
        /// </summary>
        public bool HasCommodity { get; set; }

        /// <summary>
        /// The algorithm has traded forex
        /// </summary>
        public bool HasForex { get; set; }

        /// <summary>
        /// The algorithm has traded futures
        /// </summary>
        public bool HasFuture { get; set; }

        /// <summary>
        /// The algorithm has traded CFDs
        /// </summary>
        public bool HasCfd { get; set; }

        /// <summary>
        /// The algorithm has traded crypto currencies
        /// </summary>
        public bool HasCrypto { get; set; }

        /// <summary>
        /// The algorithm has defined custom charts
        /// </summary>
        public bool HasCustomChart { get; set; }

        /// <summary>
        /// The algorithm diversifies its capital amongst various assets/classes
        /// </summary>
        public bool IsDiversified { get; set; }

        /// <summary>
        /// The algorithm doesn't put all of it's eggs in one basket :)
        /// </summary>
        public bool RiskControlled { get; set; }

        /// <summary>
        /// The algorithm has traded over a significant period of time
        /// </summary>
        public bool HasSignificantPeriod { get; set; }

        /// <summary>
        /// The algorithm has executed a significant number of trades given the period over which it ran
        /// </summary>
        public bool HasSignificantTrading { get; set; }

        /// <summary>
        /// Set the has-security type properties.
        /// </summary>
        /// <param name="securityTypes">SecurityTypes from executed orders</param>
        public void AddSecurityTypesTraded(IEnumerable<SecurityType> securityTypes)
        {
            var set = securityTypes.ToHashSet();
            if (set.Contains(SecurityType.Equity))
            {
                HasEquity = true;
            }
            if (set.Contains(SecurityType.Option))
            {
                HasOption = true;
            }
            if (set.Contains(SecurityType.Commodity))
            {
                HasCommodity = true;
            }
            if (set.Contains(SecurityType.Forex))
            {
                HasForex = true;
            }
            if (set.Contains(SecurityType.Cfd))
            {
                HasCfd = true;
            }
            if (set.Contains(SecurityType.Crypto))
            {
                HasCrypto = true;
            }
        }
    }
}
