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
using System.Threading;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides the ability to synchronize subscriptions into time slices
    /// </summary>
    public class SubscriptionSynchronizer : ISubscriptionSynchronizer, ITimeProvider
    {
        private readonly UniverseSelection _universeSelection;
        private TimeSliceFactory _timeSliceFactory;
        private ITimeProvider _timeProvider;
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
        /// <returns>A time slice for the specified frontier time</returns>
        public SubscriptionSynchronizer(UniverseSelection universeSelection)
        {
            _universeSelection = universeSelection;
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
        /// Sets the <see cref="TimeSliceFactory"/> instance to use
        /// </summary>
        /// <param name="timeSliceFactory">Used to create the new <see cref="TimeSlice"/></param>
        public void SetTimeSliceFactory(TimeSliceFactory timeSliceFactory)
        {
            if (_timeSliceFactory != null)
            {
                throw new Exception("SubscriptionSynchronizer.SetTimeSliceFactory(): can only be called once");
            }
            _timeSliceFactory = timeSliceFactory;
        }

        /// <summary>
        /// Syncs the specified subscriptions. The frontier time used for synchronization is
        /// managed internally and dependent upon previous synchronization operations.
        /// </summary>
        /// <param name="subscriptions">The subscriptions to sync</param>
        /// <param name="cancellationToken">The cancellation token to stop enumeration</param>
        public IEnumerable<TimeSlice> Sync(IEnumerable<Subscription> subscriptions,
            CancellationToken cancellationToken)
        {
            var delayedSubscriptionFinished = new Queue<Subscription>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var changes = SecurityChanges.None;
                var data = new List<DataFeedPacket>(1);
                // NOTE: Tight coupling in UniverseSelection.ApplyUniverseSelection
                Dictionary<Universe, BaseDataCollection> universeData = null; // lazy construction for performance
                var universeDataForTimeSliceCreate = new Dictionary<Universe, BaseDataCollection>();

                var frontierUtc = _timeProvider.GetUtcNow();
                _frontierTimeProvider.SetCurrentTimeUtc(frontierUtc);

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

                        DataFeedPacket packet = null;

                        while (subscription.Current != null && subscription.Current.EmitTimeUtc <= frontierUtc)
                        {
                            if (packet == null)
                            {
                                // for performance, lets be selfish about creating a new instance
                                packet = new DataFeedPacket(
                                    subscription.Security,
                                    subscription.Configuration,
                                    subscription.RemovedFromUniverse
                                );
                            }
                            packet.Add(subscription.Current.Data);

                            if (!subscription.MoveNext())
                            {
                                delayedSubscriptionFinished.Enqueue(subscription);
                                break;
                            }
                        }

                        if (packet?.Count > 0)
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
                                if (universeData != null
                                    && universeData.TryGetValue(subscription.Universes.Single(), out collection))
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
                                        collection = new BaseDataCollection(frontierUtc, frontierUtc, subscription.Configuration.Symbol, packetData);
                                    }

                                    if (universeData == null)
                                    {
                                        universeData = new Dictionary<Universe, BaseDataCollection>();
                                    }
                                    universeData[subscription.Universes.Single()] = collection;
                                }
                            }
                        }

                        if (subscription.IsUniverseSelectionSubscription
                            && subscription.Universes.Single().DisposeRequested)
                        {
                            // we need to do this after all usages of subscription.Universes
                            OnSubscriptionFinished(subscription);
                        }
                    }

                    if (universeData != null && universeData.Count > 0)
                    {
                        // if we are going to perform universe selection we emit an empty
                        // time pulse to align algorithm time with current frontier
                        yield return _timeSliceFactory.CreateTimePulse(frontierUtc);

                        foreach (var kvp in universeData)
                        {
                            var universe = kvp.Key;
                            var baseDataCollection = kvp.Value;
                            universeDataForTimeSliceCreate[universe] = baseDataCollection;
                            newChanges += _universeSelection.ApplyUniverseSelection(universe, frontierUtc, baseDataCollection);
                        }
                        universeData.Clear();
                    }

                    changes += newChanges;
                }
                while (newChanges != SecurityChanges.None
                    || _universeSelection.AddPendingInternalDataFeeds(frontierUtc));

                var timeSlice = _timeSliceFactory.Create(frontierUtc, data, changes, universeDataForTimeSliceCreate);

                while (delayedSubscriptionFinished.Count > 0)
                {
                    // these subscriptions added valid data to the packet
                    // we need to trigger OnSubscriptionFinished after we create the TimeSlice
                    // else it will drop the data
                    var subscription = delayedSubscriptionFinished.Dequeue();
                    OnSubscriptionFinished(subscription);
                }

                yield return timeSlice;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionFinished"/> event
        /// </summary>
        protected virtual void OnSubscriptionFinished(Subscription subscription)
        {
            SubscriptionFinished?.Invoke(this, subscription);
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