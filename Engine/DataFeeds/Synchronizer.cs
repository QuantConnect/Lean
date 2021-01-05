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
using System.Threading;
using NodaTime;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Implementation of the <see cref="ISynchronizer"/> interface which provides the mechanism to stream data to the algorithm
    /// </summary>
    public class Synchronizer : ISynchronizer, IDataFeedTimeProvider, IDisposable
    {
        private DateTimeZone _dateTimeZone;

        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm;

        /// <summary>
        /// The subscription manager
        /// </summary>
        protected IDataFeedSubscriptionManager SubscriptionManager;

        /// <summary>
        /// The subscription synchronizer
        /// </summary>
        protected SubscriptionSynchronizer SubscriptionSynchronizer;

        /// <summary>
        /// The time slice factory
        /// </summary>
        protected TimeSliceFactory TimeSliceFactory;

        /// <summary>
        /// Continuous UTC time provider, only valid for live trading see <see cref="LiveSynchronizer"/>
        /// </summary>
        public virtual ITimeProvider TimeProvider => null;

        /// <summary>
        /// Time provider which returns current UTC frontier time
        /// </summary>
        public ITimeProvider FrontierTimeProvider => SubscriptionSynchronizer;

        /// <summary>
        /// Initializes the instance of the Synchronizer class
        /// </summary>
        public virtual void Initialize(
            IAlgorithm algorithm,
            IDataFeedSubscriptionManager dataFeedSubscriptionManager)
        {
            SubscriptionManager = dataFeedSubscriptionManager;
            Algorithm = algorithm;
            SubscriptionSynchronizer = new SubscriptionSynchronizer(
                SubscriptionManager.UniverseSelection);
        }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public virtual IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            PostInitialize();

            // GetTimeProvider() will call GetInitialFrontierTime() which
            // will consume added subscriptions so we need to do this after initialization
            SubscriptionSynchronizer.SetTimeProvider(GetTimeProvider());

            var previousEmitTime = DateTime.MaxValue;

            var enumerator = SubscriptionSynchronizer
                .Sync(SubscriptionManager.DataFeedSubscriptions, cancellationToken)
                .GetEnumerator();
            var previousWasTimePulse = false;
            // this is a just in case flag to stop looping if time does not advance
            var retried = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSlice timeSlice;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        // the enumerator ended
                        break;
                    }
                    timeSlice = enumerator.Current;
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    // notify the algorithm about the error, so it can be reported to the user
                    Algorithm.RunTimeError = err;
                    Algorithm.Status = AlgorithmStatus.RuntimeError;
                    break;
                }

                // check for cancellation
                if (timeSlice == null || cancellationToken.IsCancellationRequested) break;

                if (timeSlice.IsTimePulse && Algorithm.UtcTime == timeSlice.Time)
                {
                    previousWasTimePulse = timeSlice.IsTimePulse;
                    // skip time pulse when algorithms already at that time
                    continue;
                }

                // SubscriptionFrontierTimeProvider will return twice the same time if there are no more subscriptions or if Subscription.Current is null
                if (timeSlice.Time != previousEmitTime || previousWasTimePulse || timeSlice.UniverseData.Count != 0)
                {
                    previousEmitTime = timeSlice.Time;
                    previousWasTimePulse = timeSlice.IsTimePulse;
                    // if we emitted, clear retry flag
                    retried = false;
                    yield return timeSlice;
                }
                else
                {
                    // if the slice has data lets retry just once more... this could happen
                    // with subscriptions added after initialize using algorithm.AddSecurity() API,
                    // where the subscription start time is the current time loop (but should just happen once)
                    if (!timeSlice.Slice.HasData || retried)
                    {
                        // there's no more data to pull off, we're done (frontier is max value and no security changes)
                        break;
                    }
                    retried = true;
                }
            }

            enumerator.DisposeSafely();
            Log.Trace("Synchronizer.GetEnumerator(): Exited thread.");
        }

        /// <summary>
        /// Performs additional initialization steps after algorithm initialization
        /// </summary>
        protected virtual void PostInitialize()
        {
            SubscriptionSynchronizer.SubscriptionFinished += (sender, subscription) =>
            {
                SubscriptionManager.RemoveSubscription(subscription.Configuration);
                if (Log.DebuggingEnabled)
                {
                    Log.Debug("Synchronizer.SubscriptionFinished(): Finished subscription:" +
                              $"{subscription.Configuration} at {FrontierTimeProvider.GetUtcNow()} UTC");
                }
            };

            // this is set after the algorithm initializes
            _dateTimeZone = Algorithm.TimeZone;
            TimeSliceFactory = new TimeSliceFactory(_dateTimeZone);
            SubscriptionSynchronizer.SetTimeSliceFactory(TimeSliceFactory);
        }

        /// <summary>
        /// Gets the <see cref="ITimeProvider"/> to use. By default this will load the
        /// <see cref="RealTimeProvider"/> for live mode, else <see cref="SubscriptionFrontierTimeProvider"/>
        /// </summary>
        /// <returns>The <see cref="ITimeProvider"/> to use</returns>
        protected virtual ITimeProvider GetTimeProvider()
        {
            return new SubscriptionFrontierTimeProvider(GetInitialFrontierTime(), SubscriptionManager);
        }

        private DateTime GetInitialFrontierTime()
        {
            var frontier = DateTime.MaxValue;
            foreach (var subscription in SubscriptionManager.DataFeedSubscriptions)
            {
                var current = subscription.Current;
                if (current == null)
                {
                    continue;
                }

                // we need to initialize both the frontier time and the offset provider, in order to do
                // this we'll first convert the current.EndTime to UTC time, this will allow us to correctly
                // determine the offset in ticks using the OffsetProvider, we can then use this to recompute
                // the UTC time. This seems odd, but is necessary given Noda time's lenient mapping, the
                // OffsetProvider exists to give forward marching mapping

                // compute the initial frontier time
                if (current.EmitTimeUtc < frontier)
                {
                    frontier = current.EmitTimeUtc;
                }
            }

            if (frontier == DateTime.MaxValue)
            {
                frontier = Algorithm.StartDate.ConvertToUtc(_dateTimeZone);
            }
            return frontier;
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
