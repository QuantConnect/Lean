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
using QuantConnect.Data;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Live trading data provider. Provides an entrypoint for external live
    /// data sources into the LEAN engine.
    /// </summary>
    /// <remarks>
    /// You can implement this interface to provide LEAN with data from an external
    /// source. The LiveTradingDataFeed consumes implementers of this interface.
    /// You will need a class deriving from <see cref="BaseData"/>, and you need to convert your data
    /// into an instance of the class deriving from BaseData.
    /// </remarks>
    public interface ILiveDataProvider
    {
        /// <summary>
        /// Gets the data that has been queued by the implementation
        /// since the last time this method was called. The implementation
        /// should/will get rid of any data provided to you when the
        /// method is called. No duplicate data points will be provided to
        /// the consumer of this method.
        /// </summary>
        /// <returns>BaseData instances which can represent any data type (TradeBar, Tick, custom, etc.)</returns>
        IEnumerable<BaseData> Next();

        /// <summary>
        /// Informs the ILiveDataProvider implementation to add the provided <see cref="Symbol"/>s
        /// into its internal subscription ledger. In principle, only data that matches the
        /// subscriptions provided should be returned, but there is no hard requirement that mandates
        /// it to do so. It is preferred to only return data in <see cref="Next"/> that has a matching subscription.
        /// </summary>
        /// <remarks>
        /// We provide <see cref="SubscriptionDataConfig"/> rather than <see cref="Symbol"/> because it allows
        /// for better filtering/selection of data to return, such as by Resolution.
        /// </remarks>
        /// <param name="subscriptions">Subscriptions to add to the implementing ILiveDataProvider</param>
        void Subscribe(IEnumerable<SubscriptionDataConfig> subscriptions);

        /// <summary>
        /// Informs the ILiveDataProvider implementation to remove the provided <see cref="Symbol"/>s
        /// from its internal subscription ledger. In principle, this method should not throw if
        /// the provided Symbol is not found inside the current subscriptions, but should rather
        /// silently fail since it results in nothing being done. It is preferred that data should
        /// stop being streamed for a given <see cref="Symbol"/> once it has been removed, but
        /// there is no hard requirement to do so.
        /// </summary>
        /// <param name="subscriptions">Subscriptions to remove from the implementing ILiveDataProvider</param>
        void Unsubscribe(IEnumerable<SubscriptionDataConfig> subscriptions);
    }
}
