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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Defines the parameters for the <see cref="IFillModel"/> method
    /// </summary>
    public class FillModelParameters
    {
        /// <summary>
        /// Gets the <see cref="Security"/>
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Gets the <see cref="Order"/>
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// Gets the <see cref="SubscriptionDataConfig"/> provider
        /// </summary>
        public ISubscriptionDataConfigProvider ConfigProvider { get; }

        /// <summary>
        /// Gets the minimum time span elapsed to consider a market fill price as stale (defaults to one hour)
        /// </summary>
        public TimeSpan StalePriceTimeSpan { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="security">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <param name="configProvider">The <see cref="ISubscriptionDataConfigProvider"/> to use</param>
        /// <param name="stalePriceTimeSpan">The minimum time span elapsed to consider a fill price as stale</param>
        public FillModelParameters(
            Security security,
            Order order,
            ISubscriptionDataConfigProvider configProvider,
            TimeSpan stalePriceTimeSpan)
        {
            Security = security;
            Order = order;
            ConfigProvider = configProvider;
            StalePriceTimeSpan = stalePriceTimeSpan;
        }
    }
}