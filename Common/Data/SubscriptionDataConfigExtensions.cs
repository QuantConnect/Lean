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
using System.Linq;

namespace QuantConnect.Data
{
    /// <summary>
    /// Helper methods used to determine different configurations properties
    /// for a given set of <see cref="SubscriptionDataConfig"/>
    /// </summary>
    public static class SubscriptionDataConfigExtensions
    {
        /// <summary>
        /// Extension method used to obtain the highest <see cref="Resolution"/>
        /// for a given set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="subscriptionDataConfigs"></param>
        /// <returns>The highest resolution, <see cref="Resolution.Daily"/> if there
        /// are no subscriptions</returns>
        public static Resolution GetHighestResolution(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs)
        {
            return subscriptionDataConfigs
                .Select(x => x.Resolution)
                .DefaultIfEmpty(Resolution.Daily)
                .Min();
        }

        /// <summary>
        /// Extension method used to determine if FillForward is enabled
        /// for a given set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="subscriptionDataConfigs"></param>
        /// <returns>True, at least one subscription has it enabled</returns>
        public static bool FillForward(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs)
        {
            return subscriptionDataConfigs.Any(x => x.FillDataForward);
        }

        /// <summary>
        /// Extension method used to determine if ExtendedMarketHours is enabled
        /// for a given set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="subscriptionDataConfigs"></param>
        /// <returns>True, at least one subscription has it enabled</returns>
        public static bool ExtendedMarketHours(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs)
        {
            return subscriptionDataConfigs.Any(x => x.ExtendedMarketHours);
        }

        /// <summary>
        /// Extension method used to determine if it is custom data
        /// for a given set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="subscriptionDataConfigs"></param>
        /// <returns>True, at least one subscription is custom data</returns>
        public static bool CustomData(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs)
        {
            return subscriptionDataConfigs.Any(x => x.IsCustomData);
        }

        /// <summary>
        /// Extension method used to determine what <see cref="QuantConnect.DataNormalizationMode"/>
        /// to use for a given set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="subscriptionDataConfigs"></param>
        /// <returns>The first DataNormalizationMode,
        /// <see cref="DataNormalizationMode.Adjusted"/> if there  are no subscriptions</returns>
        public static DataNormalizationMode DataNormalizationMode(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs)
        {
            return subscriptionDataConfigs.
                Select(x => x.DataNormalizationMode)
                .DefaultIfEmpty(QuantConnect.DataNormalizationMode.Adjusted)
                .First(); ;
        }
    }
}
