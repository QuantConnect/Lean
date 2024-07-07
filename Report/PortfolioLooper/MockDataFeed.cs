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

using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;

namespace QuantConnect.Report
{
    /// <summary>
    /// Fake IDataFeed
    /// </summary>
    public class MockDataFeed : IDataFeed
    {
        /// <summary>
        /// Bool if the feed is active
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Initialize the data feed
        /// This implementation does nothing
        /// </summary>
        public void Initialize(
            IAlgorithm algorithm,
            AlgorithmNodePacket job,
            IResultHandler resultHandler,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider,
            IDataFeedSubscriptionManager subscriptionManager,
            IDataFeedTimeProvider dataFeedTimeProvider,
            IDataChannelProvider dataChannelProvider
        ) { }

        /// <summary>
        /// Create Subscription
        /// </summary>
        /// <param name="request">Subscription request to use</param>
        /// <returns>Always null</returns>
        public Subscription CreateSubscription(SubscriptionRequest request)
        {
            return null;
        }

        /// <summary>
        /// Remove Subscription; Not implemented
        /// </summary>
        /// <param name="subscription">Subscription to remove</param>
        public void RemoveSubscription(Subscription subscription) { }

        /// <summary>
        /// DataFeed Exit
        /// </summary>
        public void Exit() { }
    }
}
