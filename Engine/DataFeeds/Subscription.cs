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
using System.Collections;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents the data required for a data feed to process a single subsciption
    /// </summary>
    public class Subscription : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _enumerator;

        /// <summary>
        /// Gets the security this subscription points to
        /// </summary>
        public readonly Security Security;

        /// <summary>
        /// Gets the configuration for this subscritions
        /// </summary>
        public readonly SubscriptionDataConfig Configuration;

        /// <summary>
        /// Gets the time zone associated with this subscription
        /// </summary>
        public DateTimeZone TimeZone
        {
            get { return Configuration.TimeZone; }
        }

        /// <summary>
        /// Gets the offset provider for time zone conversions to and from the data's local time
        /// </summary>
        public readonly TimeZoneOffsetProvider OffsetProvider;

        /// <summary>
        /// Gets the most current value from the subscription source
        /// </summary>
        public decimal RealtimePrice { get; protected set; }

        /// <summary>
        /// Gets true if this subscription is finished, false otherwise
        /// </summary>
        public bool EndOfStream { get; private set; }

        /// <summary>
        /// Gets true if the user explicitly defined this subscription, false if the
        /// system generated through some mechanism (universe selection, currency feeds, ect)
        /// </summary>
        public bool IsUserDefined { get; private set; }

        /// <summary>
        /// Gets true if this subscription is used to produce dates for a given market's universe
        /// selection logic. Data from this subscription is never intended to be forwarded into
        /// an algorithm
        /// </summary>
        public bool IsFundamentalSubscription { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class
        /// </summary>
        /// <param name="security">The security this subscription is for</param>
        /// <param name="enumerator">The subscription's data source</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <param name="isUserDefined">True if the user explicitly defined this subscription, false otherwise</param>
        /// <param name="isFundamentalSubscription">True if this subscription is used to define the times to perform universe selection
        /// for a specific market, false for all other subscriptions</param>
        public Subscription(Security security, IEnumerator<BaseData> enumerator, DateTime utcStartTime, DateTime utcEndTime, bool isUserDefined, bool isFundamentalSubscription)
        {
            Security = security;
            _enumerator = enumerator;
            IsUserDefined = isUserDefined;
            IsFundamentalSubscription = isFundamentalSubscription;
            Configuration = security.SubscriptionDataConfig;
            OffsetProvider = new TimeZoneOffsetProvider(security.SubscriptionDataConfig.TimeZone, utcStartTime, utcEndTime);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public virtual bool MoveNext()
        {
            if (EndOfStream)
            {
                return false;
            }

            var moveNext = _enumerator.MoveNext();
            EndOfStream = !moveNext;
            Current = _enumerator.Current;
            if (Current != null)
            {
                RealtimePrice = Current.Value;
            }
            return moveNext;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current { get; private set; }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return Configuration.Symbol.GetHashCode();
        }
    }
}