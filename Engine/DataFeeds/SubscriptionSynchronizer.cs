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
using NodaTime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides the ability to synchronize subscriptions into time slices
    /// </summary>
    public class SubscriptionSynchronizer
    {
        private readonly UniverseSelection _universeSelection;

        /// <summary>
        /// Event fired when a subscription is finished
        /// </summary>
        public event EventHandler<Subscription> SubscriptionFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionSynchronizer"/> class
        /// </summary>
        /// <param name="universeSelection">The universe selection instance used to handle universe
        /// selection subscription output</param>
        public SubscriptionSynchronizer(UniverseSelection universeSelection)
        {
            _universeSelection = universeSelection;
        }

        /// <summary>
        /// Syncs the specifies subscriptions at the frontier time
        /// </summary>
        /// <param name="frontier">The time used for syncing, data in the future won't be included in this time slice</param>
        /// <param name="subscriptions">The subscriptions to sync</param>
        /// <param name="sliceTimeZone">The time zone of the created slice object</param>
        /// <param name="cashBook">The cash book, used for creating the cash book updates</param>
        /// <param name="nextFrontier">The next frontier time as determined by the first piece of data in the future ahead of the frontier.
        /// This value will equal DateTime.MaxValue when the subscriptions are all finished</param>
        /// <returns>A time slice for the specified frontier time</returns>
        public TimeSlice Sync(DateTime frontier, IEnumerable<Subscription> subscriptions, DateTimeZone sliceTimeZone, CashBook cashBook, out DateTime nextFrontier)
        {
            var changes = SecurityChanges.None;
            nextFrontier = DateTime.MaxValue;
            var earlyBirdTicks = nextFrontier.Ticks;
            var data = new List<DataFeedPacket>();
            var universeData = new Dictionary<Universe, BaseDataCollection>();

            SecurityChanges newChanges;
            do
            {

                universeData.Clear();
                newChanges = SecurityChanges.None;
                foreach (var subscription in subscriptions)
                {
                    if (subscription.EndOfStream)
                    {
                        OnSubscriptionFinished(subscription);
                        continue;
                    }

                    // prime if needed
                    if (subscription.Current == null)
                    {
                        if (!subscription.MoveNext())
                        {
                            OnSubscriptionFinished(subscription);
                            continue;
                        }
                    }

                    var packet = new DataFeedPacket(subscription.Security, subscription.Configuration);
                    data.Add(packet);

                    var configuration = subscription.Configuration;
                    var offsetProvider = subscription.OffsetProvider;
                    var currentOffsetTicks = offsetProvider.GetOffsetTicks(frontier);
                    while (subscription.Current.EndTime.Ticks - currentOffsetTicks <= frontier.Ticks)
                    {
                        // we want bars rounded using their subscription times, we make a clone
                        // so we don't interfere with the enumerator's internal logic
                        var clone = subscription.Current.Clone(subscription.Current.IsFillForward);
                        clone.Time = clone.Time.ExchangeRoundDownInTimeZone(configuration.Increment, subscription.Security.Exchange.Hours, configuration.DataTimeZone, configuration.ExtendedMarketHours);

                        packet.Add(clone);

                        if (!subscription.MoveNext())
                        {
                            OnSubscriptionFinished(subscription);
                            break;
                        }
                    }

                    // we have new universe data to select based on, store the subscription data until the end
                    if (subscription.IsUniverseSelectionSubscription && packet.Count > 0)
                    {
                        // assume that if the first item is a base data collection then the enumerator handled the aggregation,
                        // otherwise, load all the the data into a new collection instance
                        var packetBaseDataCollection = packet.Data[0] as BaseDataCollection;
                        var packetData = packetBaseDataCollection == null
                            ? packet.Data
                            : packetBaseDataCollection.Data;

                        BaseDataCollection collection;
                        if (!universeData.TryGetValue(subscription.Universe, out collection))
                        {
                            if (packetBaseDataCollection is OptionChainUniverseDataCollection)
                            {
                                var current = subscription.Current as OptionChainUniverseDataCollection;
                                var underlying = current != null ? current.Underlying : null;
                                collection = new OptionChainUniverseDataCollection(frontier, subscription.Configuration.Symbol, packetData, underlying);
                            }
                            else if (packetBaseDataCollection is FuturesChainUniverseDataCollection)
                            {
                                var current = subscription.Current as FuturesChainUniverseDataCollection;
                                collection = new FuturesChainUniverseDataCollection(frontier, subscription.Configuration.Symbol, packetData);
                            }
                            else
                            {
                                collection = new BaseDataCollection(frontier, subscription.Configuration.Symbol, packetData);
                            }

                            universeData[subscription.Universe] = collection;
                        }
                        else
                        {
                            collection.Data.AddRange(packetData);
                        }
                    }

                    if (subscription.Current != null)
                    {
                        // take the earliest between the next piece of data or the next tz discontinuity
                        earlyBirdTicks = Math.Min(earlyBirdTicks, Math.Min(subscription.Current.EndTime.Ticks - currentOffsetTicks, offsetProvider.GetNextDiscontinuity()));
                    }
                }

                foreach (var kvp in universeData)
                {
                    var universe = kvp.Key;
                    var baseDataCollection = kvp.Value;
                    newChanges += _universeSelection.ApplyUniverseSelection(universe, frontier, baseDataCollection);
                }

                changes += newChanges;
            }
            while (newChanges != SecurityChanges.None);

            nextFrontier = new DateTime(Math.Max(earlyBirdTicks, frontier.Ticks), DateTimeKind.Utc);

            return TimeSlice.Create(frontier, sliceTimeZone, cashBook, data, changes);
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionFinished"/> event
        /// </summary>
        protected virtual void OnSubscriptionFinished(Subscription subscription)
        {
            var handler = SubscriptionFinished;
            if (handler != null) handler(this, subscription);
        }
    }
}