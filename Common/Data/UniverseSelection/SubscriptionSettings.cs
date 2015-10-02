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

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines settings required when adding a subscription
    /// </summary>
    public class SubscriptionSettings
    {
        /// <summary>
        /// The resolution to be used
        /// </summary>
        public Resolution Resolution;

        /// <summary>
        /// The leverage to be used
        /// </summary>
        public decimal Leverage;

        /// <summary>
        /// True to fill data forward, false otherwise
        /// </summary>
        public bool FillForward;

        /// <summary>
        /// True to allow extended market hours data, false otherwise
        /// </summary>
        public bool ExtendedMarketHours;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionSettings"/> class
        /// </summary>
        /// <param name="resolution">The resolution</param>
        /// <param name="leverage">The leverage to be used</param>
        /// <param name="fillForward">True to fill data forward, false otherwise</param>
        /// <param name="extendedMarketHours">True to allow exended market hours data, false otherwise</param>
        public SubscriptionSettings(Resolution resolution, decimal leverage, bool fillForward, bool extendedMarketHours)
        {
            Resolution = resolution;
            Leverage = leverage;
            FillForward = fillForward;
            ExtendedMarketHours = extendedMarketHours;
        }
    }
}