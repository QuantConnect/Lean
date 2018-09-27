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
    /// This interface exposes methods for creating a list of <see cref="SubscriptionDataConfig"/> for a given configuration
    /// </summary>
    public interface ISubscriptionDataConfigBuilder
    {
        /// <summary>
        /// Creates a list of <see cref="SubscriptionDataConfig"/> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data types to use.
        /// </summary>
        List<SubscriptionDataConfig> Create(Symbol symbol, Resolution resolution,
                                            bool fillForward = true, bool extendedMarketHours = false,
                                            bool isFilteredSubscription = true,
                                            bool isInternalFeed = false, bool isCustomData = false,
                                            List<Tuple<Type, TickType>> subscriptionDataTypes = null);
    }
}