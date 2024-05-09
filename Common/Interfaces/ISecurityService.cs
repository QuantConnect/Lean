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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// This interface exposes methods for creating a new <see cref="Security" />
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        Security CreateSecurity(Symbol symbol,
            List<SubscriptionDataConfig> subscriptionDataConfigList,
            decimal leverage = 0,
            bool addToSymbolCache = true,
            Security underlying = null);

        /// <summary>
        /// Creates a new security
        /// </summary>
        /// <remarks>Following the obsoletion of Security.Subscriptions,
        /// both overloads will be merged removing <see cref="SubscriptionDataConfig"/> arguments</remarks>
        Security CreateSecurity(Symbol symbol,
            SubscriptionDataConfig subscriptionDataConfig,
            decimal leverage = 0,
            bool addToSymbolCache = true,
            Security underlying = null);

        /// <summary>
        /// Creates a new benchmark security
        /// </summary>
        Security CreateBenchmarkSecurity(Symbol symbol);
    }
}
