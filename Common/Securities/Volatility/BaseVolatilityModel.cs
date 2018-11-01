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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Securities.Volatility
{
    /// <summary>
    /// Represents a base model that computes the volatility of a security
    /// </summary>
    public class BaseVolatilityModel : IVolatilityModel
    {
        /// <summary>
        /// Provides access to registered <see cref="SubscriptionDataConfig"/>
        /// </summary>
        protected ISubscriptionDataConfigProvider SubscriptionDataConfigProvider;

        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        public virtual decimal Volatility { get; }

        /// <summary>
        /// Sets the <see cref="ISubscriptionDataConfigProvider"/> instance to use.
        /// </summary>
        /// <param name="subscriptionDataConfigProvider">Provides access to registered <see cref="SubscriptionDataConfig"/></param>
        public virtual void SetSubscriptionDataConfigProvider(
            ISubscriptionDataConfigProvider subscriptionDataConfigProvider)
        {
            SubscriptionDataConfigProvider = subscriptionDataConfigProvider;
        }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data">The new data used to update the model</param>
        public virtual void Update(Security security, BaseData data)
        {
        }

        /// <summary>
        /// Returns history requirements for the volatility model expressed in the form of history request
        /// </summary>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date/time of the request</param>
        /// <returns>History request object list, or empty if no requirements</returns>
        public virtual IEnumerable<HistoryRequest> GetHistoryRequirements(
            Security security,
            DateTime utcTime
            )
        {
            return Enumerable.Empty<HistoryRequest>();
        }
    }
}
