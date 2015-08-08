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
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents the data required for a live data feed to process a single subscription
    /// </summary>
    public class LiveSubscription : Subscription
    {
        private DateTime _nextUpdateTimeUtc;

        /// <summary>
        /// Gets true if this subscription should be checked for new data, false to skip
        /// </summary>
        public bool NeedsUpdate
        {
            get { return DateTime.UtcNow >= _nextUpdateTimeUtc; }
        }

        /// <summary>
        /// Gets true if we need to advance this subscription. This is
        /// used to prevent future data from being pumped into the algorithm
        /// This is only valid when <see cref="IsCustomData"/> is true.
        /// </summary>
        public bool NeedsMoveNext { get; set; }

        /// <summary>
        /// Gets true if this is a custom data subscription, false otherwise
        /// </summary>
        public bool IsCustomData { get; private set; }

        /// <summary>
        /// Gets the stream store used to aggregate bars.
        /// </summary>
        public StreamStore StreamStore { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveSubscription"/> class
        /// </summary>
        /// <param name="security">The security this subscription is for</param>
        /// <param name="enumerator">The subscription's data source</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <param name="isUserDefined">True if the user explicitly defined this subscription, false otherwise</param>
        /// <param name="isFundamentalSubscription">True if this subscription is used to define the times to perform universe selection
        /// for a specific market, false for all other subscriptions</param>
        public LiveSubscription(Security security, IEnumerator<BaseData> enumerator, DateTime utcStartTime, DateTime utcEndTime, bool isUserDefined, bool isFundamentalSubscription)
            : base(security, enumerator, utcStartTime, utcEndTime, isUserDefined, isFundamentalSubscription)
        {
            NeedsMoveNext = true;
            IsCustomData = security.IsDynamicallyLoadedData;
            StreamStore = new StreamStore(Configuration, security);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public override bool MoveNext()
        {
            var moveNext = base.MoveNext();
            if (Current == null)
            {
                // if we couldn't get a data point, try again in a second
                _nextUpdateTimeUtc = GetRoundedUtcNow(Time.OneSecond);
                return moveNext;
            }
            _nextUpdateTimeUtc = GetRoundedUtcNow(Configuration.Increment);
            return moveNext;
        }

        /// <summary>
        /// Sets the real time price
        /// </summary>
        /// <param name="price">The current market price</param>
        public void SetRealtimePrice(decimal price)
        {
            RealtimePrice = price;
        }

        /// <summary>
        /// Sets the next update time in reference to now
        /// </summary>
        /// <param name="duration">The duration to wait before NeedsUpdate = true</param>
        public void SetNextUpdateTime(TimeSpan duration)
        {
            _nextUpdateTimeUtc = DateTime.UtcNow.Add(duration);
        }

        private static DateTime GetRoundedUtcNow(TimeSpan increment)
        {
            return DateTime.UtcNow.Add(increment).RoundDown(increment);
        }
    }
}