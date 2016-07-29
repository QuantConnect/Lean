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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides logic to prevent an algorithm from adding too many data subscriptions
    /// </summary>
    public class SubscriptionLimiter
    {
        private const int MinuteMemory = 2;
        private const int SecondMemory = 10;
        private const int TickMemory = 34;

        private readonly int _tickLimit;
        private readonly int _secondLimit;
        private readonly int _minuteLimit;
        private readonly decimal _maxRamEstimate;
        private readonly Func<IEnumerable<Subscription>> _subscriptionsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionLimiter"/> class
        /// </summary>
        /// <param name="subscriptionsProvider">Delegate used to provide access to the current subscriptions</param>
        /// <param name="tickLimit">The maximum number of tick symbols</param>
        /// <param name="secondLimit">The maximum number of second symbols</param>
        /// <param name="minuteLimit">The maximum number of minute symbol</param>
        public SubscriptionLimiter(Func<IEnumerable<Subscription>> subscriptionsProvider, int tickLimit, int secondLimit, int minuteLimit)
        {
            _subscriptionsProvider = subscriptionsProvider;
            _tickLimit = tickLimit;
            _secondLimit = secondLimit;
            _minuteLimit = minuteLimit;
            _maxRamEstimate = GetRamEstimate(minuteLimit, secondLimit, tickLimit);
        }

        /// <summary>
        /// Get the number of securities that have this resolution.
        /// </summary>
        /// <param name="resolution">Search resolution value.</param>
        /// <returns>Count of the securities</returns>
        public int GetResolutionCount(Resolution resolution)
        {
            return (from subscription in _subscriptionsProvider()
                    let security = subscription.Security
                    where security.Resolution == resolution
                    // don't count feeds we auto add
                    where !subscription.Configuration.IsInternalFeed
                    select security.Resolution).Count();
        }

        /// <summary>
        /// Gets the number of available slots for the specifed resolution
        /// </summary>
        /// <param name="resolution">The resolution we want to add subscriptions at</param>
        /// <returns>The number of subscriptions we can safely add without maxing out the count (ram usage depends on other factors)</returns>
        public int GetRemaining(Resolution resolution)
        {
            return GetResolutionLimit(resolution) - GetResolutionCount(resolution);
        }

        /// <summary>
        /// Determines if we can add a subscription for the specified resolution
        /// </summary>
        /// <param name="resolution">The new subscription resolution to check</param>
        /// <param name="reason">When this function returns false, this is the reason we are unable to add the subscription</param>
        /// <returns>True if we can add a subscription for the specified resolution while
        /// remaining within our limits, false if this will put us over our limits</returns>
        public bool CanAddSubscription(Resolution resolution, out string reason)
        {
            reason = null;
            var limit = GetResolutionLimit(resolution);

            // we increment the resolution since we're about to add one
            var count = GetResolutionCount(resolution) + 1;
            
            // check max counts of symbols
            if (count >= limit)
            {
                reason = GetCountLimitReason(resolution);
                return false;
            }
            
            // check ram usage
            var ramEstimate = GetRamEstimate(
                GetResolutionCount(Resolution.Minute), 
                GetResolutionCount(Resolution.Second), 
                GetResolutionCount(Resolution.Tick)
                );

            // finally, check current estimate against the precomputed maximum
            if (ramEstimate > _maxRamEstimate)
            {
                reason = GetMaxRamReason(ramEstimate);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the max number of symbols for the specified resolution
        /// </summary>
        /// <param name="resolution">The resolution whose limit we seek</param>
        /// <returns>The specified resolution's limit</returns>
        private int GetResolutionLimit(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return _tickLimit;

                case Resolution.Second:
                    return _secondLimit;

                case Resolution.Minute:
                    return _minuteLimit;

                case Resolution.Hour:
                case Resolution.Daily:
                    return int.MaxValue;

                default:
                    throw new ArgumentOutOfRangeException("resolution", resolution, null);
            }
        }

        /// <summary>
        /// Estimated ram usage with this symbol combination:
        /// </summary>
        /// <returns>Decimal estimate of the number of MB ram the requested assets would consume</returns>
        private decimal GetRamEstimate(int minute, int second, int tick)
        {
            return MinuteMemory * minute + SecondMemory * second + TickMemory * tick;
        }

        /// <summary>
        /// Gets reason string for having a larger count than the limits
        /// </summary>
        private string GetCountLimitReason(Resolution resolution)
        {
            var limit = GetResolutionLimit(resolution);
            return string.Format("We currently only support {0} {1} at a time due to physical memory limitations", limit, resolution.ToString().ToLower());
        }

        /// <summary>
        /// Gets reason string for having a larger estimated ram usage than the limits
        /// </summary>
        private string GetMaxRamReason(decimal currentEstimatedRam)
        {
            return string.Format("We estimate you will run out of memory ({0}mb of {1}mb physically available). " +
                "Please reduce the number of symbols you're analysing or if in live trading upgrade your server to allow more memory.",
                currentEstimatedRam, _maxRamEstimate
                );
        }
    }
}