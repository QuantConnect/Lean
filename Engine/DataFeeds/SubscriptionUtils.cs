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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Utilities related to data <see cref="Subscription"/>
    /// </summary>
    public static class SubscriptionUtils
    {
        /// <summary>
        /// Setups a new <see cref="Subscription"/> which will consume a blocking <see cref="EnqueueableEnumerator{T}"/>
        /// that will be feed by a worker task
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <param name="lowerThreshold">The lower threshold for the worker task, for which the consumer will trigger the worker
        /// if it has stopped <see cref="EnqueueableEnumerator{T}.TriggerProducer"/></param>
        /// <param name="upperThreshold">The upper threshold for the worker task, after which it will stop producing until requested
        /// by the consumer <see cref="EnqueueableEnumerator{T}.TriggerProducer"/></param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription CreateAndScheduleWorker(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator,
            int lowerThreshold,
            int upperThreshold)
        {
            var exchangeHours = request.Security.Exchange.Hours;
            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);

            Action produce = () =>
            {
                var count = 0;
                while (enumerator.MoveNext())
                {
                    // subscription has been removed, no need to continue enumerating
                    if (enqueueable.HasFinished)
                    {
                        enumerator.Dispose();
                        return;
                    }

                    var subscriptionData = SubscriptionData.Create(subscription.Configuration, exchangeHours, subscription.OffsetProvider, enumerator.Current);

                    // drop the data into the back of the enqueueable
                    enqueueable.Enqueue(subscriptionData);

                    count++;

                    // stop executing if we have more data than the upper threshold in the enqueueable, we don't want to fill the ram
                    if (count > upperThreshold)
                    {
                        // we use local count for the outside if, for performance, and adjust here
                        count = enqueueable.Count;
                        if (count > upperThreshold)
                        {
                            // we will be re scheduled to run by the consumer, see EnqueueableEnumerator
                            return;
                        }
                    }
                }

                // we made it here because MoveNext returned false, stop the enqueueable
                enqueueable.Stop();
                // we have to dispose of the enumerator
                enumerator.Dispose();
            };

            enqueueable.SetProducer(produce, lowerThreshold);

            return subscription;
        }
    }
}
