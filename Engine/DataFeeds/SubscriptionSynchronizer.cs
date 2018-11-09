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
using System.Linq;
using NodaTime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides the ability to synchronize subscriptions into time slices
    /// </summary>
    public class SubscriptionSynchronizer : ISubscriptionSynchronizer, ITimeProvider
    {
        private ITimeProvider _timeProvider;
        private readonly CashBook _cashBook;
        private DateTimeZone _sliceTimeZone;
        private readonly UniverseSelection _universeSelection;
        private ManualTimeProvider _frontierTimeProvider;

        /// <summary>
        /// Event fired when a <see cref="Subscription"/> is finished
        /// </summary>
        public event EventHandler<Subscription> SubscriptionFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionSynchronizer"/> class
        /// </summary>
        /// <param name="universeSelection">The universe selection instance used to handle universe
        /// selection subscription output</param>
        /// <param name="cashBook">The cash book, used for creating the cash book updates</param>
        /// <returns>A time slice for the specified frontier time</returns>
        public SubscriptionSynchronizer(UniverseSelection universeSelection,
                                        CashBook cashBook)
        {
            _universeSelection = universeSelection;
            _cashBook = cashBook;
        }

        /// <summary>
        /// Sets the time provider. If already set will throw.
        /// </summary>
        /// <param name="timeProvider">The time provider, used to obtain the current frontier UTC value</param>
        public void SetTimeProvider(ITimeProvider timeProvider)
        {
            if (_timeProvider != null)
            {
                throw new Exception("SubscriptionSynchronizer.SetTimeProvider(): can only be called once");
            }
            _timeProvider = timeProvider;
            _frontierTimeProvider = new ManualTimeProvider(_timeProvider.GetUtcNow());
        }

        /// <summary>
        /// Sets the time zone of the created slice object. If already set will throw.
        /// </summary>
        /// <param name="timeZone">The time zone of the created slice object</param>
        public void SetSliceTimeZone(DateTimeZone timeZone)
        {
            if (_sliceTimeZone != null)
            {
                throw new Exception("SubscriptionSynchronizer.SetSliceTimeZone(): can only be called once");
            }
            _sliceTimeZone = timeZone;
        }

        /// <summary>
        /// Syncs the specified subscriptions. The frontier time used for synchronization is
        /// managed internally and dependent upon previous synchronization operations.
        /// </summary>
        /// <param name="subscriptions">The subscriptions to sync</param>
        public TimeSlice Sync(IEnumerable<Subscription> subscriptions)
        {
            var delayedSubscriptionFinished = false;
            var changes = SecurityChanges.None;
            var data = new List<DataFeedPacket>();
            // NOTE: Tight coupling in UniverseSelection.ApplyUniverseSelection
            var universeData = new Dictionary<Universe, BaseDataCollection>();
            var universeDataForTimeSliceCreate = new Dictionary<Universe, BaseDataCollection>();

            _frontierTimeProvider.SetCurrentTimeUtc(_timeProvider.GetUtcNow());
            var frontierUtc = _frontierTimeProvider.GetUtcNow();

            SecurityChanges newChanges;
            do
            {
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

                    var packet = new DataFeedPacket(subscription.Security, subscription.Configuration, subscription.RemovedFromUniverse);

                    while (subscription.Current != null && subscription.Current.EmitTimeUtc <= frontierUtc)
                    {
                        packet.Add(subscription.Current.Data);

                        if (!subscription.MoveNext())
                        {
                            delayedSubscriptionFinished = true;
                            break;
                        }
                    }

                    if (packet.Count > 0)
                    {
                        // we have new universe data to select based on, store the subscription data until the end
                        if (!subscription.IsUniverseSelectionSubscription)
                        {
                            data.Add(packet);
                        }
                        else
                        {
                            // assume that if the first item is a base data collection then the enumerator handled the aggregation,
                            // otherwise, load all the the data into a new collection instance
                            var packetBaseDataCollection = packet.Data[0] as BaseDataCollection;
                            var packetData = packetBaseDataCollection == null
                                ? packet.Data
                                : packetBaseDataCollection.Data;

                            BaseDataCollection collection;
                            if (universeData.TryGetValue(subscription.Universes.Single(), out collection))
                            {
                                collection.Data.AddRange(packetData);
                            }
                            else
                            {
                                if (packetBaseDataCollection is OptionChainUniverseDataCollection)
                                {
                                    var current = packetBaseDataCollection as OptionChainUniverseDataCollection;
                                    collection = new OptionChainUniverseDataCollection(frontierUtc, subscription.Configuration.Symbol, packetData, current?.Underlying);
                                }
                                else if (packetBaseDataCollection is FuturesChainUniverseDataCollection)
                                {
                                    collection = new FuturesChainUniverseDataCollection(frontierUtc, subscription.Configuration.Symbol, packetData);
                                }
                                else
                                {
                                    collection = new BaseDataCollection(frontierUtc, subscription.Configuration.Symbol, packetData);
                                }

                                universeData[subscription.Universes.Single()] = collection;
                            }
                        }
                    }

                    if (subscription.IsUniverseSelectionSubscription
                        && subscription.Universes.Single().DisposeRequested
                        || delayedSubscriptionFinished)
                    {
                        delayedSubscriptionFinished = false;
                        // we need to do this after all usages of subscription.Universes
                        OnSubscriptionFinished(subscription);
                    }
                }

                foreach (var kvp in universeData)
                {
                    var universe = kvp.Key;
                    var baseDataCollection = kvp.Value;
                    universeDataForTimeSliceCreate[universe] = baseDataCollection;
                    newChanges += _universeSelection.ApplyUniverseSelection(universe, frontierUtc, baseDataCollection);
                }
                universeData.Clear();

                changes += newChanges;
            }
            while (newChanges != SecurityChanges.None);

            var timeSlice = TimeSlice.Create(frontierUtc, _sliceTimeZone, _cashBook, data, changes, universeDataForTimeSliceCreate);

            return timeSlice;
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionFinished"/> event
        /// </summary>
        protected virtual void OnSubscriptionFinished(Subscription subscription)
        {
            var handler = SubscriptionFinished;
            if (handler != null) handler(this, subscription);
        }

        /// <summary>
        /// Returns the current UTC frontier time
        /// </summary>
        public DateTime GetUtcNow()
        {
            return _frontierTimeProvider.GetUtcNow();
        }
    }
}