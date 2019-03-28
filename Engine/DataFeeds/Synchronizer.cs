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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Implementation of the <see cref="ISynchronizer"/> interface which provides the mechanism to stream data to the algorithm
    /// </summary>
    public class Synchronizer : ISynchronizer, IDataFeedTimeProvider
    {
        private SubscriptionSynchronizer _subscriptionSynchronizer;
        private IDataFeedSubscriptionManager _subscriptionManager;
        private TimeSliceFactory _timeSliceFactory;
        private IAlgorithm _algorithm;
        private DateTimeZone _dateTimeZone;
        private bool _liveMode;
        private readonly ManualResetEvent _newLiveDataEmitted = new ManualResetEvent(false);

        /// <summary>
        /// Continuous UTC time provider
        /// </summary>
        public ITimeProvider TimeProvider { get; private set; }

        /// <summary>
        /// Time provider which returns current UTC frontier time
        /// </summary>
        public ITimeProvider FrontierTimeProvider => _subscriptionSynchronizer;

        /// <summary>
        /// Initializes the instance of the Synchronizer class
        /// </summary>
        public void Initialize(
            IAlgorithm algorithm,
            IDataFeedSubscriptionManager dataFeedSubscriptionManager,
            bool liveMode)
        {
            _subscriptionManager = dataFeedSubscriptionManager;
            _algorithm = algorithm;
            _liveMode = liveMode;
            _subscriptionSynchronizer = new SubscriptionSynchronizer(
                _subscriptionManager.UniverseSelection);

            if (_liveMode)
            {
                TimeProvider = GetTimeProvider();
                _subscriptionSynchronizer.SetTimeProvider(TimeProvider);

                // attach event handlers to subscriptions
                dataFeedSubscriptionManager.SubscriptionAdded += (sender, subscription) =>
                {
                    subscription.NewDataAvailable += OnSubscriptionNewDataAvailable;
                };

                dataFeedSubscriptionManager.SubscriptionRemoved += (sender, subscription) =>
                {
                    subscription.NewDataAvailable -= OnSubscriptionNewDataAvailable;
                };
            }
        }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            PostInitialize();

            var shouldSendExtraEmptyPacket = false;
            var nextEmit = DateTime.MinValue;
            var previousEmitTime = DateTime.MaxValue;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_liveMode)
                {
                    _newLiveDataEmitted.WaitOne(TimeSpan.FromMilliseconds(500));
                }

                TimeSlice timeSlice;
                try
                {
                    timeSlice = _subscriptionSynchronizer.Sync(_subscriptionManager.DataFeedSubscriptions);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    // notify the algorithm about the error, so it can be reported to the user
                    _algorithm.RunTimeError = err;
                    _algorithm.Status = AlgorithmStatus.RuntimeError;
                    shouldSendExtraEmptyPacket = _liveMode;
                    break;
                }

                if (_liveMode)
                {
                    _newLiveDataEmitted.Reset();
                }

                // check for cancellation
                if (cancellationToken.IsCancellationRequested) break;

                if (_liveMode)
                {
                    var frontierUtc = FrontierTimeProvider.GetUtcNow();
                    // emit on data or if we've elapsed a full second since last emit or there are security changes
                    if (timeSlice.SecurityChanges != SecurityChanges.None
                        || timeSlice.Data.Count != 0
                        || frontierUtc >= nextEmit)
                    {
                        yield return timeSlice;
                        // force emitting every second since the data feed is
                        // the heartbeat of the application
                        nextEmit = frontierUtc.RoundDown(Time.OneSecond).Add(Time.OneSecond);
                    }
                    // take a short nap
                    Thread.Sleep(1);
                }
                else
                {
                    // SubscriptionFrontierTimeProvider will return twice the same time if there are no more subscriptions or if Subscription.Current is null
                    if (timeSlice.Time != previousEmitTime)
                    {
                        previousEmitTime = timeSlice.Time;
                        yield return timeSlice;
                    }
                    else if (timeSlice.SecurityChanges == SecurityChanges.None)
                    {
                        // there's no more data to pull off, we're done (frontier is max value and no security changes)
                        break;
                    }
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
                    var timeSlice = _timeSliceFactory.Create(
                        nextEmit,
                        new List<DataFeedPacket>(),
                        SecurityChanges.None,
                        new Dictionary<Universe, BaseDataCollection>());
                    yield return timeSlice;
                }
            }
            Log.Trace("Synchronizer.GetEnumerator(): Exited thread.");
        }

        private void OnSubscriptionNewDataAvailable(object sender, EventArgs args)
        {
            _newLiveDataEmitted.Set();
        }

        private void PostInitialize()
        {
            _subscriptionSynchronizer.SubscriptionFinished += (sender, subscription) =>
            {
                _subscriptionManager.RemoveSubscription(subscription.Configuration);
                Log.Debug("Synchronizer.SubscriptionFinished(): Finished subscription:" +
                    $"{subscription.Configuration} at {FrontierTimeProvider.GetUtcNow()} UTC");
            };

            // this is set after the algorithm initializes
            _dateTimeZone = _algorithm.TimeZone;
            _timeSliceFactory = new TimeSliceFactory(_dateTimeZone);
            _subscriptionSynchronizer.SetTimeSliceFactory(_timeSliceFactory);

            if (!_liveMode)
            {
                // GetTimeProvider() will call GetInitialFrontierTime() which
                // will consume added subscriptions so we need to do this after initialization
                TimeProvider = GetTimeProvider();
                _subscriptionSynchronizer.SetTimeProvider(TimeProvider);
            }
        }

        /// <summary>
        /// Gets the <see cref="ITimeProvider"/> to use. By default this will load the
        /// <see cref="RealTimeProvider"/> for live mode, else <see cref="SubscriptionFrontierTimeProvider"/>
        /// </summary>
        /// <returns>The <see cref="ITimeProvider"/> to use</returns>
        protected virtual ITimeProvider GetTimeProvider()
        {
            if (_liveMode)
            {
                return new RealTimeProvider();
            }
            return new SubscriptionFrontierTimeProvider(GetInitialFrontierTime(), _subscriptionManager);
        }

        private DateTime GetInitialFrontierTime()
        {
            var frontier = DateTime.MaxValue;
            foreach (var subscription in _subscriptionManager.DataFeedSubscriptions)
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
                frontier = _algorithm.StartDate.ConvertToUtc(_dateTimeZone);
            }
            return frontier;
        }
    }
}
