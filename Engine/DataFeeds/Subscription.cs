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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents the data required for a data feed to process a single subsciption
    /// </summary>
    public class Subscription : IEnumerator<SubscriptionData>
    {
        private readonly IEnumerator<SubscriptionData> _enumerator;

        /// <summary>
        /// Gets the universe for this subscription
        /// </summary>
        public Universe Universe
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the security this subscription points to
        /// </summary>
        public readonly Security Security;

        /// <summary>
        /// Gets the configuration for this subscritions
        /// </summary>
        public readonly SubscriptionDataConfig Configuration;

        /// <summary>
        /// Gets the exchange time zone associated with this subscription
        /// </summary>
        public DateTimeZone TimeZone
        {
            get { return Security.Exchange.TimeZone; }
        }

        /// <summary>
        /// Gets the offset provider for time zone conversions to and from the data's local time
        /// </summary>
        public readonly TimeZoneOffsetProvider OffsetProvider;

        /// <summary>
        /// Gets the most current value from the subscription source
        /// </summary>
        public decimal RealtimePrice { get; set; }

        /// <summary>
        /// Gets true if this subscription is finished, false otherwise
        /// </summary>
        public bool EndOfStream { get; private set; }

        /// <summary>
        /// Gets true if this subscription is used in universe selection
        /// </summary>
        public bool IsUniverseSelectionSubscription { get; private set; }

        /// <summary>
        /// Gets the start time of this subscription in UTC
        /// </summary>
        public DateTime UtcStartTime { get; private set; }

        /// <summary>
        /// Gets the end time of this subscription in UTC
        /// </summary>
        public DateTime UtcEndTime { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class with a universe
        /// </summary>
        /// <param name="universe">Specified for universe subscriptions</param>
        /// <param name="security">The security this subscription is for</param>
        /// <param name="configuration">The subscription configuration that was used to generate the enumerator</param>
        /// <param name="enumerator">The subscription's data source</param>
        /// <param name="timeZoneOffsetProvider">The offset provider used to convert data local times to utc</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <param name="isUniverseSelectionSubscription">True if this is a subscription for universe selection,
        /// that is, the configuration is used to produce the used to perform universe selection, false for a
        /// normal data subscription, i.e, SPY</param>
        public Subscription(Universe universe,
            Security security,
            SubscriptionDataConfig configuration,
            IEnumerator<SubscriptionData> enumerator,
            TimeZoneOffsetProvider timeZoneOffsetProvider,
            DateTime utcStartTime,
            DateTime utcEndTime,
            bool isUniverseSelectionSubscription)
        {
            Universe = universe;
            Security = security;
            _enumerator = enumerator;
            IsUniverseSelectionSubscription = isUniverseSelectionSubscription;
            Configuration = configuration;
            OffsetProvider = timeZoneOffsetProvider;

            UtcStartTime = utcStartTime;
            UtcEndTime = utcEndTime;
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
        public SubscriptionData Current { get; private set; }

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
            EndOfStream = true;
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

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Configuration.ToString();
        }
    }
}
