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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// DataManager will manage the subscriptions for both the DataFeeds and the SubscriptionManager
    /// </summary>
    public class DataManager : IAlgorithmSubscriptionManager, IDataFeedSubscriptionManager, IDataManager
    {
        private readonly IDataFeed _dataFeed;
        private readonly IAlgorithmSettings _algorithmSettings;

        /// There is no ConcurrentHashSet collection in .NET,
        /// so we use ConcurrentDictionary with byte value to minimize memory usage
        private readonly ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig> _subscriptionManagerSubscriptions
            = new ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig>();

        /// <summary>
        /// Creates a new instance of the DataManager
        /// </summary>
        public DataManager(IDataFeed dataFeed, UniverseSelection universeSelection, IAlgorithmSettings algorithmSettings)
        {
            _dataFeed = dataFeed;
            UniverseSelection = universeSelection;
            _algorithmSettings = algorithmSettings;
        }

        #region IDataFeedSubscriptionManager

        /// <summary>
        /// Gets the data feed subscription collection
        /// </summary>
        public SubscriptionCollection DataFeedSubscriptions { get; } = new SubscriptionCollection();

        #endregion

        #region IAlgorithmSubscriptionManager

        /// <summary>
        /// Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        public IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions => _subscriptionManagerSubscriptions.Select(x => x.Key);

        /// <summary>
        /// Gets existing or adds new <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <returns>Returns the SubscriptionDataConfig instance used</returns>
        public SubscriptionDataConfig SubscriptionManagerGetOrAdd(SubscriptionDataConfig newConfig)
        {
            var config = _subscriptionManagerSubscriptions.GetOrAdd(newConfig, newConfig);

            // if the reference is not the same, means it was already there and we did not add anything new
            if (!ReferenceEquals(config, newConfig))
            {
                Log.Trace("DataManager.SubscriptionManagerGetOrAdd(): subscription already added: " + config);
            }
            else
            {
                // count data subscriptions by symbol, ignoring multiple data types
                var uniqueCount = SubscriptionManagerSubscriptions
                    .Where(x => !x.Symbol.IsCanonical())
                    // TODO should limit subscriptions or unique securities
                    .DistinctBy(x => x.Symbol.Value)
                    .Count();

                if (uniqueCount > _algorithmSettings.DataSubscriptionLimit)
                {
                    throw new Exception(
                        $"The maximum number of concurrent market data subscriptions was exceeded ({_algorithmSettings.DataSubscriptionLimit})." +
                        "Please reduce the number of symbols requested or increase the limit using Settings.DataSubscriptionLimit.");
                }
            }

            return config;
        }

        /// <summary>
        /// Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        public int SubscriptionManagerCount()
        {
            return _subscriptionManagerSubscriptions.Skip(0).Count();
        }

        #endregion

        #region IDataManager

        /// <summary>
        /// Get the universe selection instance
        /// </summary>
        public UniverseSelection UniverseSelection { get; }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public IEnumerable<TimeSlice> StreamData()
        {
            return _dataFeed;
        }

        #endregion
    }
}
