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

namespace QuantConnect.Interfaces
{
    /// <summary>
    ///     AlgorithmSubscriptionManager interface will manage the subscriptions for the SubscriptionManager
    /// </summary>
    public interface IAlgorithmSubscriptionManager : ISubscriptionDataConfigService
    {
        /// <summary>
        ///     Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions { get; }

        /// <summary>
        ///     Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        int SubscriptionManagerCount();

        /// <summary>
        ///     Flags the existence of custom data in the subscriptions
        /// </summary>
        bool HasCustomData { get; set; }
    }
}
