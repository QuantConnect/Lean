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
using QuantConnect.Configuration;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Implementation of the <see cref="ISynchronizer"/> interface which provides the mechanism to stream live data to the algorithm
    /// </summary>
    public class LiveSynchronizer : Synchronizer
    {
        private ITimeProvider _timeProvider;
        private RealTimeScheduleEventService _realTimeScheduleEventService;
        private readonly int _batchingDelay = Config.GetInt("consumer-batching-timeout-ms");
        private readonly ManualResetEventSlim _newLiveDataEmitted = new ManualResetEventSlim(false);

        /// <summary>
        /// Continuous UTC time provider
        /// </summary>
        public override ITimeProvider TimeProvider => _timeProvider;

        /// <summary>
        /// Initializes the instance of the Synchronizer class
        /// </summary>
        public override void Initialize(
            IAlgorithm algorithm,
            IDataFeedSubscriptionManager dataFeedSubscriptionManager)
        {
            base.Initialize(algorithm, dataFeedSubscriptionManager);

            _timeProvider = GetTimeProvider();
            SubscriptionSynchronizer.SetTimeProvider(TimeProvider);

            // attach event handlers to subscriptions
            dataFeedSubscriptionManager.SubscriptionAdded += (sender, subscription) =>
            {
                subscription.NewDataAvailable += OnSubscriptionNewDataAvailable;
            };

            dataFeedSubscriptionManager.SubscriptionRemoved += (sender, subscription) =>
            {
                subscription.NewDataAvailable -= OnSubscriptionNewDataAvailable;
            };

            _realTimeScheduleEventService = new RealTimeScheduleEventService(new RealTimeProvider());
            // this schedule event will be our time pulse
            _realTimeScheduleEventService.NewEvent += (sender, args) => _newLiveDataEmitted.Set();
        }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public override IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            PostInitialize();

            var shouldSendExtraEmptyPacket = false;
            var nextEmit = DateTime.MinValue;
            var lastLoopStart = DateTime.UtcNow;

            var enumerator = SubscriptionSynchronizer
                .Sync(SubscriptionManager.DataFeedSubscriptions, cancellationToken)
                .GetEnumerator();

            var previousWasTimePulse = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                if (!previousWasTimePulse)
                {
                    if (!_newLiveDataEmitted.IsSet)
                    {
                        // if we just crossed into the next second let's loop again, we will flush any consolidator bar
                        // else we will wait to be notified by the subscriptions or our scheduled event service every second
                        if (lastLoopStart.Second == now.Second)
                        {
                            _realTimeScheduleEventService.ScheduleEvent(TimeSpan.FromMilliseconds(GetPulseDueTime(now)), now);
                            _newLiveDataEmitted.Wait();
                        }
                    }
                    _newLiveDataEmitted.Reset();
                }

                lastLoopStart = now;

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
                    shouldSendExtraEmptyPacket = true;
                    break;
                }

                // check for cancellation
                if (timeSlice == null || cancellationToken.IsCancellationRequested) break;

                var frontierUtc = FrontierTimeProvider.GetUtcNow();
                // emit on data or if we've elapsed a full second since last emit or there are security changes
                if (timeSlice.SecurityChanges != SecurityChanges.None
                    || timeSlice.IsTimePulse
                    || timeSlice.Data.Count != 0
                    || frontierUtc >= nextEmit)
                {
                    previousWasTimePulse = timeSlice.IsTimePulse;
                    yield return timeSlice;

                    // force emitting every second since the data feed is
                    // the heartbeat of the application
                    nextEmit = frontierUtc.RoundDown(Time.OneSecond).Add(Time.OneSecond);
                }
            }

            if (shouldSendExtraEmptyPacket)
            {
                // send last empty packet list before terminating,
                // so the algorithm manager has a chance to detect the runtime error
                // and exit showing the correct error instead of a timeout
                nextEmit = FrontierTimeProvider.GetUtcNow().RoundDown(Time.OneSecond);
                if (!cancellationToken.IsCancellationRequested)
                {
                    var timeSlice = TimeSliceFactory.Create(
                        nextEmit,
                        new List<DataFeedPacket>(),
                        SecurityChanges.None,
                        new Dictionary<Universe, BaseDataCollection>());
                    yield return timeSlice;
                }
            }

            enumerator.DisposeSafely();
            Log.Trace("LiveSynchronizer.GetEnumerator(): Exited thread.");
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public override void Dispose()
        {
            _newLiveDataEmitted.Set();
            _newLiveDataEmitted?.DisposeSafely();
            _realTimeScheduleEventService?.DisposeSafely();
        }

        /// <summary>
        /// Gets the <see cref="ITimeProvider"/> to use. By default this will load the
        /// <see cref="RealTimeProvider"/> for live mode, else <see cref="SubscriptionFrontierTimeProvider"/>
        /// </summary>
        /// <returns>The <see cref="ITimeProvider"/> to use</returns>
        protected override ITimeProvider GetTimeProvider()
        {
            return RealTimeProvider.Instance;
        }

        /// <summary>
        /// Will return the amount of milliseconds that are missing for the next time pulse
        /// </summary>
        protected virtual int GetPulseDueTime(DateTime now)
        {
            // let's wait until the next second starts
            return 1000 - now.Millisecond + _batchingDelay;
        }

        protected virtual void OnSubscriptionNewDataAvailable(object sender, EventArgs args)
        {
            _newLiveDataEmitted.Set();
        }
    }
}
