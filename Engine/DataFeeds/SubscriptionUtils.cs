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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Utilities related to data <see cref="Subscription"/>
    /// </summary>
    public static class SubscriptionUtils
    {
        /// <summary>
        /// Creates a new <see cref="Subscription"/> which will directly consume the provided enumerator
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription Create(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator)
        {
            var exchangeHours = request.Security.Exchange.Hours;
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var dataEnumerator = new SubscriptionDataEnumerator(
                request.Configuration,
                exchangeHours,
                timeZoneOffsetProvider,
                enumerator
            );
            return new Subscription(request, dataEnumerator, timeZoneOffsetProvider);
        }


        /// <summary>
        /// Setups a new <see cref="Subscription"/> which will consume a blocking <see cref="EnqueueableEnumerator{T}"/>
        /// that will be feed by a worker task
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <param name="firstLoopLimit">The first loop data point count for which the worker will stop</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription CreateAndScheduleWorker(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator,
            int firstLoopLimit = 50)
        {
            var upperThreshold = GetUpperThreshold(request.Configuration.Resolution);
            var lowerThreshold = GetLowerThreshold(request.Configuration.Resolution);
            if (request.Configuration.Type == typeof(CoarseFundamental))
            {
                // the lower threshold will be when we start the worker again, if he is stopped
                lowerThreshold = 200;
                // the upper threshold will stop the worker from loading more data. This is roughly 1 GB
                upperThreshold = 500;
            }

            var exchangeHours = request.Security.Exchange.Hours;
            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);

            // The first loop of a backtest can load hundreds of subscription feeds, resulting in long delays while the thresholds
            // for the buffer are reached. For the first loop start up with just 50 items in the buffer.
            var firstLoop = Ref.Create(true);
            Action produce = () =>
            {
                try
                {
                    var count = 0;
                    while (enumerator.MoveNext())
                    {
                        // subscription has been removed, no need to continue enumerating
                        if (enqueueable.HasFinished)
                        {
                            enumerator.DisposeSafely();
                            return;
                        }

                        var subscriptionData = SubscriptionData.Create(subscription.Configuration, exchangeHours,
                            subscription.OffsetProvider, enumerator.Current);

                        // drop the data into the back of the enqueueable
                        enqueueable.Enqueue(subscriptionData);

                        count++;

                        // stop executing if we have more data than the upper threshold in the enqueueable, we don't want to fill the ram
                        if (count > upperThreshold || count > firstLoopLimit && firstLoop.Value)
                        {
                            // we use local count for the outside if, for performance, and adjust here
                            count = enqueueable.Count;
                            if (count > upperThreshold || firstLoop.Value)
                            {
                                firstLoop.Value = false;
                                // we will be re scheduled to run by the consumer, see EnqueueableEnumerator

                                // if the consumer is already waiting for us wake him up, he will rescheduled us if required
                                enqueueable.CancellationTokenSource.Cancel();
                                return;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, $"Subscription worker task exception {request.Configuration}.");
                }

                // we made it here because MoveNext returned false or we exploded, stop the enqueueable
                enqueueable.Stop();
                // we have to dispose of the enumerator
                enumerator.DisposeSafely();
            };

            enqueueable.SetProducer(produce, lowerThreshold);

            return subscription;
        }

        private static int GetLowerThreshold(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return 500;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    return 250;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }

        private static int GetUpperThreshold(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return 10000;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    return 5000;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }
    }
}
