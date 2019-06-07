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
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents the data required for a data feed to process a single subscription
    /// </summary>
    public class Subscription : IEnumerator<SubscriptionData>
    {
        private bool _removedFromUniverse;
        private readonly IEnumerator<SubscriptionData> _enumerator;
        private List<SubscriptionRequest> _subscriptionRequests;

        /// <summary>
        /// Event fired when a new data point is available
        /// </summary>
        public event EventHandler NewDataAvailable;

        /// <summary>
        /// Gets the universe for this subscription
        /// </summary>
        public IEnumerable<Universe> Universes => _subscriptionRequests
            .Where(x => x.Universe != null)
            .Select(x => x.Universe);

        /// <summary>
        /// Gets the security this subscription points to
        /// </summary>
        public readonly ISecurityPrice Security;

        /// <summary>
        /// Gets the configuration for this subscritions
        /// </summary>
        public readonly SubscriptionDataConfig Configuration;

        /// <summary>
        /// Gets the exchange time zone associated with this subscription
        /// </summary>
        public DateTimeZone TimeZone { get; }

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
        public bool IsUniverseSelectionSubscription { get; }

        /// <summary>
        /// Gets the start time of this subscription in UTC
        /// </summary>
        public DateTime UtcStartTime { get; }

        /// <summary>
        /// Gets the end time of this subscription in UTC
        /// </summary>
        public DateTime UtcEndTime { get; }

        /// <summary>
        /// Gets whether or not this subscription has been removed from its parent universe
        /// </summary>
        public IReadOnlyRef<bool> RemovedFromUniverse { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class with a universe
        /// </summary>
        /// <param name="subscriptionRequest">Specified for universe subscriptions</param>
        /// <param name="enumerator">The subscription's data source</param>
        /// <param name="timeZoneOffsetProvider">The offset provider used to convert data local times to utc</param>
        public Subscription(
            SubscriptionRequest subscriptionRequest,
            IEnumerator<SubscriptionData> enumerator,
            TimeZoneOffsetProvider timeZoneOffsetProvider)
        {
            _subscriptionRequests = new List<SubscriptionRequest> { subscriptionRequest };
            _enumerator = enumerator;
            Security = subscriptionRequest.Security;
            IsUniverseSelectionSubscription = subscriptionRequest.IsUniverseSubscription;
            Configuration = subscriptionRequest.Configuration;
            OffsetProvider = timeZoneOffsetProvider;
            TimeZone = subscriptionRequest.Security.Exchange.TimeZone;
            UtcStartTime = subscriptionRequest.StartTimeUtc;
            UtcEndTime = subscriptionRequest.EndTimeUtc;

            RemovedFromUniverse = Ref.CreateReadOnly(() => _removedFromUniverse);
        }

        /// <summary>
        /// Adds a <see cref="SubscriptionRequest"/> for this subscription
        /// </summary>
        /// <param name="subscriptionRequest">The <see cref="SubscriptionRequest"/> to add</param>
        public bool AddSubscriptionRequest(SubscriptionRequest subscriptionRequest)
        {
            if (IsUniverseSelectionSubscription
                || subscriptionRequest.IsUniverseSubscription)
            {
                throw new Exception("Subscription.AddSubscriptionRequest(): Universe selection" +
                    " subscriptions should not have more than 1 SubscriptionRequest");
            }

            // this shouldn't happen but just in case..
            if (subscriptionRequest.Configuration != Configuration)
            {
                throw new Exception("Subscription.AddSubscriptionRequest(): Requesting to add" +
                    "a different SubscriptionDataConfig");
            }

            // Only allow one subscription request per universe
            if (!Universes.Contains(subscriptionRequest.Universe))
            {
                _subscriptionRequests.Add(subscriptionRequest);
                // TODO this might update the 'UtcStartTime' and 'UtcEndTime' of this subscription
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes one or all <see cref="SubscriptionRequest"/> from this subscription
        /// </summary>
        /// <param name="universe">Universe requesting to remove <see cref="SubscriptionRequest"/>.
        /// Default value, null, will remove all universes</param>
        /// <returns>True, if the subscription is empty and ready to be removed</returns>
        public bool RemoveSubscriptionRequest(Universe universe = null)
        {
            // TODO this might update the 'UtcStartTime' and 'UtcEndTime' of this subscription
            IEnumerable<Universe> removedUniverses;
            if (universe == null)
            {
                var subscriptionRequests = _subscriptionRequests;
                _subscriptionRequests = new List<SubscriptionRequest>();
                removedUniverses = subscriptionRequests.Where(x => x.Universe != null)
                    .Select(x => x.Universe);
            }
            else
            {
                _subscriptionRequests.RemoveAll(x => x.Universe == universe);
                removedUniverses = new[] {universe};
            }

            var emptySubscription = !_subscriptionRequests.Any();
            if (emptySubscription)
            {
                // if the security is no longer a member of the universe, then mark the subscription properly
                // universe may be null for internal currency conversion feeds
                // TODO : Put currency feeds in their own internal universe
                if (!removedUniverses.Any(x => x.Securities.ContainsKey(Configuration.Symbol)))
                {
                    MarkAsRemovedFromUniverse();
                }
            }

            return emptySubscription;
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
        object IEnumerator.Current => Current;

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
        /// Mark this subscription as having been removed from the universe.
        /// Data for this time step will be discarded.
        /// </summary>
        public void MarkAsRemovedFromUniverse()
        {
            _removedFromUniverse = true;
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
            return Configuration.GetHashCode();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>
        /// <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            var subscription = obj as Subscription;
            if (subscription == null)
            {
                return false;
            }

            return subscription.Configuration.Equals(Configuration);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Configuration.ToString();
        }

        /// <summary>
        /// Event invocator for the <see cref="NewDataAvailable"/> event
        /// </summary>
        public void OnNewDataAvailable()
        {
            NewDataAvailable?.Invoke(this, EventArgs.Empty);
        }
    }
}
