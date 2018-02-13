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
    public class SubscriptionSynchronizer : ISubscriptionSynchronizer
    {
        private static readonly long MaxDateTimeTicks = DateTime.MaxValue.Ticks;

        private DateTime _frontier;
        private readonly CashBook _cashBook;
        private readonly DateTimeZone _sliceTimeZone;
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
        /// <param name="sliceTimeZone">The time zone of the created slice object</param>
        /// <param name="cashBook">The cash book, used for creating the cash book updates</param>
        /// <returns>A time slice for the specified frontier time</returns>
        /// <param name="frontierUtc">The initial UTC frontier time to syncronize at</param>
        public SubscriptionSynchronizer(UniverseSelection universeSelection, DateTimeZone sliceTimeZone, CashBook cashBook, DateTime frontierUtc)
        {
            _frontier = frontierUtc;
            _universeSelection = universeSelection;
            _sliceTimeZone = sliceTimeZone;
            _cashBook = cashBook;
        }

        /// <summary>
        /// Syncs the specified subscriptions. The frontier time used for synchronization is
        /// managed internally and dependent upon previous synchronization operations.
        /// </summary>
        /// <param name="subscriptions">The subscriptions to sync</param>
        public TimeSlice Sync(IEnumerable<Subscription> subscriptions)
        {
            long earlyBirdTicks;
            var changes = SecurityChanges.None;
            var data = new List<DataFeedPacket>();
            var universeData = new Dictionary<Universe, BaseDataCollection>();

            SecurityChanges newChanges;
            do
            {
                earlyBirdTicks = MaxDateTimeTicks;
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

                    var configuration = subscription.Configuration;
                    var offsetProvider = subscription.OffsetProvider;
                    var currentOffsetTicks = offsetProvider.GetOffsetTicks(_frontier);
                    while (subscription.Current.EndTime.Ticks - currentOffsetTicks <= _frontier.Ticks)
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
                            if (universeData.TryGetValue(subscription.Universe, out collection))
                            {
                                collection.Data.AddRange(packetData);
                            }
                            else
                            {
                                if (packetBaseDataCollection is OptionChainUniverseDataCollection)
                                {
                                    var current = packetBaseDataCollection as OptionChainUniverseDataCollection;
                                    collection = new OptionChainUniverseDataCollection(_frontier, subscription.Configuration.Symbol, packetData, current?.Underlying);
                                }
                                else if (packetBaseDataCollection is FuturesChainUniverseDataCollection)
                                {
                                    var current = subscription.Current as FuturesChainUniverseDataCollection;
                                    collection = new FuturesChainUniverseDataCollection(_frontier,
                                        subscription.Configuration.Symbol, packetData);
                                }
                                else
                                {
                                    collection = new BaseDataCollection(_frontier, subscription.Configuration.Symbol,
                                        packetData);
                                }

                                universeData[subscription.Universe] = collection;
                            }
                        }
                    }

                    if (subscription.Current != null)
                    {
                        if (earlyBirdTicks == MaxDateTimeTicks)
                        {
                            earlyBirdTicks = subscription.Current.EndTime.ConvertToUtc(subscription.TimeZone).Ticks;
                        }
                        else
                        {
                            // take the earliest between the next piece of data or the next tz discontinuity
                            earlyBirdTicks = Math.Min(earlyBirdTicks, Math.Min(subscription.Current.EndTime.Ticks - currentOffsetTicks, offsetProvider.GetNextDiscontinuity()));
                        }
                    }
                }

                foreach (var kvp in universeData)
                {
                    var universe = kvp.Key;
                    var baseDataCollection = kvp.Value;
                    newChanges += _universeSelection.ApplyUniverseSelection(universe, _frontier, baseDataCollection);
                }

                changes += newChanges;
            }
            while (newChanges != SecurityChanges.None);

            var timeSlice = TimeSlice.Create(_frontier, _sliceTimeZone, _cashBook, data, changes);

            // next frontier time
            _frontier = new DateTime(Math.Max(earlyBirdTicks, _frontier.Ticks), DateTimeKind.Utc);

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
    }
}