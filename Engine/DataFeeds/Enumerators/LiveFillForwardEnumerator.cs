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
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// An implementation of the <see cref="FillForwardEnumerator"/> that uses an <see cref="ITimeProvider"/>
    /// to determine if a fill forward bar needs to be emitted
    /// </summary>
    public class LiveFillForwardEnumerator : FillForwardEnumerator
    {
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveFillForwardEnumerator"/> class that accepts
        /// a reference to the fill forward resolution, useful if the fill forward resolution is dynamic
        /// and changing as the enumeration progresses
        /// </summary>
        /// <param name="timeProvider">The source of time used to gauage when this enumerator should emit extra bars when
        /// null data is returned from the source enumerator</param>
        /// <param name="enumerator">The source enumerator to be filled forward</param>
        /// <param name="exchange">The exchange used to determine when to insert fill forward data</param>
        /// <param name="fillForwardResolution">The resolution we'd like to receive data on</param>
        /// <param name="isExtendedMarketHours">True to use the exchange's extended market hours, false to use the regular market hours</param>
        /// <param name="subscriptionEndTime">The end time of the subscription, once passing this date the enumerator will stop</param>
        /// <param name="dataResolution">The source enumerator's data resolution</param>
        /// <param name="dataTimeZone">Time zone of the underlying source data</param>
        /// <param name="subscriptionStartTime">The start time of the subscription</param>
        public LiveFillForwardEnumerator(ITimeProvider timeProvider, IEnumerator<BaseData> enumerator, SecurityExchange exchange, IReadOnlyRef<TimeSpan> fillForwardResolution, bool isExtendedMarketHours, DateTime subscriptionEndTime, TimeSpan dataResolution, DateTimeZone dataTimeZone, DateTime subscriptionStartTime)
            : base(enumerator, exchange, fillForwardResolution, isExtendedMarketHours, subscriptionEndTime, dataResolution, dataTimeZone, subscriptionStartTime)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Determines whether or not fill forward is required, and if true, will produce the new fill forward data
        /// </summary>
        /// <param name="fillForwardResolution"></param>
        /// <param name="previous">The last piece of data emitted by this enumerator</param>
        /// <param name="next">The next piece of data on the source enumerator, this may be null</param>
        /// <param name="fillForward">When this function returns true, this will have a non-null value, null when the function returns false</param>
        /// <returns>True when a new fill forward piece of data was produced and should be emitted by this enumerator</returns>
        protected override bool RequiresFillForwardData(TimeSpan fillForwardResolution, BaseData previous, BaseData next, out BaseData fillForward)
        {
            // convert times to UTC for accurate comparisons and differences across DST changes
            fillForward = null;
            // Add a delay to the time we expect a data point if we've configured a delay for batching
            var nextExpectedDataPointTimeUtc = previous.EndTime.ConvertToUtc(Exchange.TimeZone) + fillForwardResolution;
            if (next != null)
            {
                // if not future data, just return the 'next'
                if (next.EndTime.ConvertToUtc(Exchange.TimeZone) <= nextExpectedDataPointTimeUtc)
                {
                    return false;
                }
                // next is future data, fill forward in between
                var clone = previous.Clone(true);
                clone.Time = previous.Time + fillForwardResolution;
                fillForward = clone;
                return true;
            }

            // the underlying enumerator returned null, check to see if time has passed for fill forwarding
            if (nextExpectedDataPointTimeUtc <= _timeProvider.GetUtcNow())
            {
                var clone = previous.Clone(true);
                clone.Time = previous.Time + fillForwardResolution;
                fillForward = clone;
                return true;
            }

            return false;
        }
    }
}
