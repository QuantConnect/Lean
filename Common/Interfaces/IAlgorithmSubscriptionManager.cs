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
    /// AlgorithmSubscriptionManager interface will manage the subscriptions for the SubscriptionManager
    /// </summary>
    public interface IAlgorithmSubscriptionManager : ISubscriptionDataConfigBuilder
    {
        /// <summary>
        /// Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions { get; }

        /// <summary>
        /// Gets existing or adds new <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <returns>Returns the SubscriptionDataConfig instance used</returns>
        SubscriptionDataConfig SubscriptionManagerGetOrAdd(SubscriptionDataConfig config);

        /// <summary>
        /// Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        int SubscriptionManagerCount();

        /// <summary>
        /// Get the data feed types for a given <see cref="SecurityType"/> <see cref="Resolution"/>
        /// </summary>
        /// <param name="symbolSecurityType">The <see cref="SecurityType"/> used to determine the types</param>
        /// <param name="resolution">The resolution of the data requested</param>
        /// <param name="isCanonical">Indicates whether the security is Canonical (future and options)</param>
        /// <returns>Types that should be added to the <see cref="SubscriptionDataConfig"/></returns>
        List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(SecurityType symbolSecurityType,
                                                                      Resolution resolution, bool isCanonical);
        /// <summary>
        /// Sets up the available data types
        /// </summary>
        void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes);
    }
}
