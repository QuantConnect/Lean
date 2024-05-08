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
using Newtonsoft.Json;
using QuantConnect.Configuration;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Provides parameters that control the behavior of a leaky bucket rate limiting algorithm. The
    /// parameter names below are phrased in the positive, such that the bucket is filled up over time
    /// vs leaking out over time.
    /// </summary>
    public class LeakyBucketControlParameters
    {
        // defaults represent 2 hour max capacity refilling at one seventh the capacity (~17.2 => 18) each day.
        // rounded up to 18 to prevent a very small decrease in refilling, IOW, if it's defaulting to 17, then
        // after 7 days have passed, we'll end up being at 119 and not completely refilled, but at 18, on the 6th
        // day we'll reach 108 and on the seventh day it will top off at 120 since it's not permitted to exceed the max
        /// <summary>
        /// Default capacity for leaky bucket
        /// </summary>
        public static int DefaultCapacity = Config.GetInt("scheduled-event-leaky-bucket-capacity", 2 * 60);

        /// <summary>
        /// Default time interval
        /// </summary>
        public static int DefaultTimeInterval = Config.GetInt("scheduled-event-leaky-bucket-time-interval-minutes", 1440);

        /// <summary>
        /// Default refill amount
        /// </summary>
        public static int DefaultRefillAmount = Config.GetInt("scheduled-event-leaky-bucket-refill-amount", (int)Math.Ceiling(DefaultCapacity/7.0));

        /// <summary>
        /// Sets the total capacity of the bucket in a leaky bucket algorithm. This is the maximum
        /// number of 'units' the bucket can hold and also defines the maximum burst rate, assuming
        /// instantaneous usage of 'units'. In reality, the usage of 'units' takes times, and so it
        /// is possible for the bucket to incrementally refill while consuming from the bucket.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Sets the refill amount of the bucket. This defines the quantity of 'units' that become available
        /// to a consuming entity after the time interval has elapsed. For example, if the refill amount is
        /// equal to one, then each time interval one new 'unit' will be made available for a consumer that is
        /// throttled by the leaky bucket.
        /// </summary>
        public int RefillAmount;

        /// <summary>
        /// Sets the time interval for the refill amount of the bucket, in minutes. After this amount of wall-clock
        /// time has passed, the bucket will refill the refill amount, thereby making more 'units' available
        /// for a consumer. For example, if the refill amount equals 10 and the time interval is 30 minutes, then
        /// every 30 minutes, 10 more 'units' become available for a consumer. The available 'units' will
        /// continue to increase until the bucket capacity is reached.
        /// </summary>
        public int TimeIntervalMinutes;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeakyBucketControlParameters"/> using default values
        /// </summary>
        public LeakyBucketControlParameters()
        {
            Capacity = DefaultCapacity;
            RefillAmount = DefaultRefillAmount;
            TimeIntervalMinutes = DefaultTimeInterval;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeakyBucketControlParameters"/> with the specified value
        /// </summary>
        /// <param name="capacity">The total capacity of the bucket in minutes</param>
        /// <param name="refillAmount">The number of additional minutes to add to the bucket
        /// after <paramref name="timeIntervalMinutes"/> has elapsed</param>
        /// <param name="timeIntervalMinutes">The interval, in minutes, that must pass before the <paramref name="refillAmount"/>
        /// is added back to the bucket for reuse</param>
        public LeakyBucketControlParameters(int capacity, int refillAmount, int timeIntervalMinutes)
        {
            Capacity = capacity;
            RefillAmount = refillAmount;
            TimeIntervalMinutes = timeIntervalMinutes;
        }
    }
}
