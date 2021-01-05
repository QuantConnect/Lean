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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Class in charge of handling Leans internal subscriptions
    /// </summary>
    public class InternalSubscriptionManager
    {
        private readonly Dictionary<Symbol, List<SubscriptionRequest>> _subscriptionRequests;
        private readonly Resolution _resolution;
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Event fired when a new internal subscription request is to be added
        /// </summary>
        public EventHandler<SubscriptionRequest> Added;

        /// <summary>
        /// Event fired when an existing internal subscription should be removed
        /// </summary>
        public EventHandler<SubscriptionRequest> Removed;

        /// <summary>
        /// Creates a new instances
        /// </summary>
        /// <param name="algorithm">The associated algorithm</param>
        /// <param name="resolution">The resolution to use for the internal subscriptions</param>
        public InternalSubscriptionManager(IAlgorithm algorithm, Resolution resolution)
        {
            _algorithm = algorithm;
            _resolution = resolution;
            _subscriptionRequests = new Dictionary<Symbol, List<SubscriptionRequest>>();
        }

        /// <summary>
        /// Notifies about a removed subscription request
        /// </summary>
        /// <param name="request">The removed subscription request</param>
        public void AddedSubscriptionRequest(SubscriptionRequest request)
        {
            if (PreFilter(request))
            {
                var lowResolution = request.Configuration.Resolution > Resolution.Minute;
                List<SubscriptionRequest> internalRequests;
                var existing = _subscriptionRequests.TryGetValue(request.Configuration.Symbol, out internalRequests);
                var alreadyInternal = existing && internalRequests.Any(internalRequest => internalRequest.Configuration.Type == request.Configuration.Type
                    && request.Configuration.TickType == internalRequest.Configuration.TickType);

                if (lowResolution && !alreadyInternal)
                {
                    // low resolution subscriptions we will add internal Resolution.Minute subscriptions
                    // if we don't already have this symbol added
                    var config = new SubscriptionDataConfig(request.Configuration, resolution: _resolution, isInternalFeed: true, extendedHours: true, isFilteredSubscription: false);
                    var internalRequest = new SubscriptionRequest(false, null, request.Security, config, request.StartTimeUtc, request.EndTimeUtc);
                    if (existing)
                    {
                        _subscriptionRequests[request.Configuration.Symbol].Add(internalRequest);
                    }
                    else
                    {
                        _subscriptionRequests[request.Configuration.Symbol] = new List<SubscriptionRequest>{ internalRequest };
                    }
                    Added?.Invoke(this, internalRequest);
                }
                else if (!lowResolution && alreadyInternal)
                {
                    _subscriptionRequests.Remove(request.Configuration.Symbol);
                    // the user added a higher resolution configuration, we can remove the internal we added
                    foreach (var subscriptionRequest in internalRequests)
                    {
                        Removed?.Invoke(this, subscriptionRequest);
                    }
                }
            }
        }

        /// <summary>
        /// Notifies about an added subscription request
        /// </summary>
        /// <param name="request">The added subscription request</param>
        public void RemovedSubscriptionRequest(SubscriptionRequest request)
        {
            if (PreFilter(request) && _subscriptionRequests.ContainsKey(request.Configuration.Symbol))
            {
                var userConfigs = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(request.Configuration.Symbol).ToList();

                if (userConfigs.Count == 0 || userConfigs.Any(config => config.Resolution <= Resolution.Minute))
                {
                    var requests = _subscriptionRequests[request.Configuration.Symbol];
                    _subscriptionRequests.Remove(request.Configuration.Symbol);
                    // if we had a config and the user no longer has a config for this symbol we remove the internal subscription
                    foreach (var subscriptionRequest in requests)
                    {
                        Removed?.Invoke(this, subscriptionRequest);
                    }
                }
            }
        }

        /// <summary>
        /// True for for live trading, non internal, non universe subscriptions, non custom data subscriptions
        /// </summary>
        private bool PreFilter(SubscriptionRequest request)
        {
            return _algorithm.LiveMode && !request.Configuration.IsInternalFeed && !request.IsUniverseSubscription && !request.Configuration.IsCustomData;
        }
    }
}
