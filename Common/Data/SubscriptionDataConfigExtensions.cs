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
        public static bool IsFillForward(
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
        public static bool IsExtendedMarketHours(
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
        public static bool IsCustomData(
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
                .First();
        }

        /// <summary>
        /// Sets the data normalization mode to be used by
        /// this set of <see cref="SubscriptionDataConfig"/>
        /// </summary>
        public static void SetDataNormalizationMode(
            this IEnumerable<SubscriptionDataConfig> subscriptionDataConfigs,
            DataNormalizationMode mode)
        {
            foreach (var subscription in subscriptionDataConfigs)
            {
                subscription.DataNormalizationMode = mode;
            }
        }

        /// <summary>
        /// Will determine if mapping should be used for this subscription configuration
        /// </summary>
        /// <param name="config">The subscription data configuration we are processing</param>
        /// <remarks>One of the objectives of this method is to normalize the 'use mapping'
        /// check and void code duplication and related issues</remarks>
        /// <returns>True if ticker should be mapped</returns>
        public static bool TickerShouldBeMapped(this SubscriptionDataConfig config)
        {
            // we create an instance of the data type, if it is a custom type
            // it can override RequiresMapping else it will use security type\
            return config.GetBaseDataInstance().RequiresMapping();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseData"/> type defined in <paramref name="config"/> with the symbol properly set
        /// </summary>
        public static BaseData GetBaseDataInstance(this SubscriptionDataConfig config)
        {
            var instance = config.Type.GetBaseDataInstance();
            instance.Symbol = config.Symbol;
            return instance;
        }
    }
}
