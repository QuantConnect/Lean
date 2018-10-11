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
using System.Threading;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides an abstract implementation of <see cref="IHistoryProvider"/>
    /// which provides synchronization of multiple history results
    /// </summary>
    public abstract class SynchronizingHistoryProvider : IHistoryProvider
    {
        private int _dataPointCount;

        /// <summary>
        /// Event fired when an error message should be sent to the algorithm
        /// </summary>
        public abstract event EventHandler<ErrorMessageEventArgs> ErrorMessage;

        /// <summary>
        /// Event fired when a debug message should be sent to the algorithm
        /// </summary>
        public abstract event EventHandler<DebugMessageEventArgs> DebugMessage;

        /// <summary>
        /// Event fired when a runtime error should be sent to the algorithm
        /// </summary>
        public abstract event EventHandler<RuntimeErrorEventArgs> RuntimeError;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount
        {
            get { return _dataPointCount; }
        }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public abstract void Initialize(HistoryProviderInitializeParameters parameters);

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public abstract IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone);

        /// <summary>
        /// Enumerates the subscriptions into slices
        /// </summary>
        protected IEnumerable<Slice> CreateSliceEnumerableFromSubscriptions(List<Subscription> subscriptions, DateTimeZone sliceTimeZone)
        {
            // required by TimeSlice.Create, but we don't need it's behavior
            var cashBook = new CashBook();
            cashBook.Clear();
            var frontier = DateTime.MinValue;
            while (true)
            {
                var earlyBirdTicks = long.MaxValue;
                var data = new List<DataFeedPacket>();
                foreach (var subscription in subscriptions)
                {
                    if (subscription.EndOfStream) continue;

                    var packet = new DataFeedPacket(subscription.Security, subscription.Configuration);

                    while (subscription.Current.EmitTimeUtc <= frontier)
                    {
                        packet.Add(subscription.Current.Data);
                        Interlocked.Increment(ref _dataPointCount);
                        if (!subscription.MoveNext())
                        {
                            break;
                        }
                    }
                    // only add if we have data
                    if (packet.Count != 0) data.Add(packet);
                    // udate our early bird ticks (next frontier time)
                    if (subscription.Current != null)
                    {
                        // take the earliest between the next piece of data or the next tz discontinuity
                        earlyBirdTicks = Math.Min(earlyBirdTicks, subscription.Current.EmitTimeUtc.Ticks);
                    }
                }

                // end of subscriptions
                if (earlyBirdTicks == long.MaxValue) break;

                if (data.Count != 0)
                {
                    // reuse the slice construction code from TimeSlice.Create
                    yield return TimeSlice.Create(frontier, sliceTimeZone, cashBook, data, SecurityChanges.None, new Dictionary<Universe, BaseDataCollection>()).Slice;
                }

                frontier = new DateTime(Math.Max(earlyBirdTicks, frontier.Ticks), DateTimeKind.Utc);
            }

            // make sure we clean up after ourselves
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
