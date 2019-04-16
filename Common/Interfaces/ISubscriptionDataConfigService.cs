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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// This interface exposes methods for creating a list of <see cref="SubscriptionDataConfig" /> for a given
    /// configuration
    /// </summary>
    public interface ISubscriptionDataConfigService : ISubscriptionDataConfigProvider
    {
        /// <summary>
        /// Creates and adds a list of <see cref="SubscriptionDataConfig" /> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data type to use.
        /// If the config already existed will return existing instance instead
        /// </summary>
        SubscriptionDataConfig Add(
            Type dataType,
            Symbol symbol,
            Resolution resolution,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false,
            DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted
            );

        /// <summary>
        /// Creates and adds a list of <see cref="SubscriptionDataConfig" /> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data types to use.
        /// If the config already existed will return existing instance instead
        /// </summary>
        List<SubscriptionDataConfig> Add(
            Symbol symbol,
            Resolution resolution,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false,
            List<Tuple<Type, TickType>> subscriptionDataTypes = null,
            DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted
            );

        /// <summary>
        /// Get the data feed types for a given <see cref="SecurityType" /> <see cref="Resolution" />
        /// </summary>
        /// <param name="symbolSecurityType">The <see cref="SecurityType" /> used to determine the types</param>
        /// <param name="resolution">The resolution of the data requested</param>
        /// <param name="isCanonical">Indicates whether the security is Canonical (future and options)</param>
        /// <returns>Types that should be added to the <see cref="SubscriptionDataConfig" /></returns>
        List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(
            SecurityType symbolSecurityType,
            Resolution resolution,
            bool isCanonical
            );

        /// <summary>
        /// Gets the available data types
        /// </summary>
        Dictionary<SecurityType, List<TickType>> AvailableDataTypes { get; }
    }
}
