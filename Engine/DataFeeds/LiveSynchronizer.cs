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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Implementation of the <see cref="ISynchronizer"/> interface which provides the mechanism to stream live data to the algorithm
    /// </summary>
    public class LiveSynchronizer : Synchronizer
    {
        private readonly AutoResetEvent _newLiveDataEmitted = new AutoResetEvent(false);

        /// <summary>
        /// Initializes the instance of the Synchronizer class
        /// </summary>
        public override void Initialize(
            IAlgorithm algorithm,
            IDataFeedSubscriptionManager dataFeedSubscriptionManager)
        {
            base.Initialize(algorithm, dataFeedSubscriptionManager);

            TimeProvider = GetTimeProvider();
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
        }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public override IEnumerable<TimeSlice> StreamData(CancellationToken cancellationToken)
        {
            PostInitialize();

            var shouldSendExtraEmptyPacket = false;
            var nextEmit = DateTime.MinValue;

            while (!cancellationToken.IsCancellationRequested)
            {
                _newLiveDataEmitted.WaitOne(TimeSpan.FromMilliseconds(500));

                TimeSlice timeSlice;
                try
                {
                    timeSlice = SubscriptionSynchronizer.Sync(SubscriptionManager.DataFeedSubscriptions);
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
                if (cancellationToken.IsCancellationRequested) break;

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
            Log.Trace("Synchronizer.GetEnumerator(): Exited thread.");
        }

        /// <summary>
        /// Gets the <see cref="ITimeProvider"/> to use. By default this will load the
        /// <see cref="RealTimeProvider"/> for live mode, else <see cref="SubscriptionFrontierTimeProvider"/>
        /// </summary>
        /// <returns>The <see cref="ITimeProvider"/> to use</returns>
        protected override ITimeProvider GetTimeProvider()
        {
            return new RealTimeProvider();
        }

        private void OnSubscriptionNewDataAvailable(object sender, EventArgs args)
        {
            _newLiveDataEmitted.Set();
        }
    }
}
