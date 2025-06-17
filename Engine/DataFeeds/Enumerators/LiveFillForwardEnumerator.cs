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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// An implementation of the <see cref="FillForwardEnumerator"/> that uses an <see cref="ITimeProvider"/>
    /// to determine if a fill forward bar needs to be emitted
    /// </summary>
    public class LiveFillForwardEnumerator : FillForwardEnumerator
    {
        private readonly TimeSpan _dataResolution;
        private readonly TimeSpan _underlyingTimeout;
        private readonly ITimeProvider _timeProvider;

        private TimeSpan _marketCloseTimeSpan;
        private TimeSpan _marketOpenTimeSpan;
        private DateTime _lastDate;

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
        /// <param name="subscriptionStartTime">The start time of the subscription</param>
        /// <param name="subscriptionEndTime">The end time of the subscription, once passing this date the enumerator will stop</param>
        /// <param name="dataResolution">The source enumerator's data resolution</param>
        /// <param name="dataTimeZone">Time zone of the underlying source data</param>
        /// <param name="dailyStrictEndTimeEnabled">True if daily strict end times are enabled</param>
        /// <param name="dataType">The configuration data type this enumerator is for</param>
        /// <param name="lastPointTracker">A reference to the last point emitted before this enumerator is first enumerated</param>
        public LiveFillForwardEnumerator(ITimeProvider timeProvider, IEnumerator<BaseData> enumerator, SecurityExchange exchange, IReadOnlyRef<TimeSpan> fillForwardResolution,
            bool isExtendedMarketHours, DateTime subscriptionStartTime, DateTime subscriptionEndTime, Resolution dataResolution, DateTimeZone dataTimeZone, bool dailyStrictEndTimeEnabled,
            Type dataType = null, LastPointTracker lastPointTracker = null)
            : base(enumerator, exchange, fillForwardResolution, isExtendedMarketHours, subscriptionStartTime, subscriptionEndTime, dataResolution.ToTimeSpan(), dataTimeZone,
                  dailyStrictEndTimeEnabled, dataType, lastPointTracker)
        {
            _timeProvider = timeProvider;
            _dataResolution = dataResolution.ToTimeSpan();
            _underlyingTimeout = GetMaximumDataTimeout(dataResolution);
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
            if (base.RequiresFillForwardData(fillForwardResolution, previous, next, out fillForward))
            {
                var underlyingTimeout = TimeSpan.Zero;
                if (fillForwardResolution >= _dataResolution && ShouldWaitForData(fillForward))
                {
                    // we enforce the underlying FF timeout when the FF resolution matches it or is bigger, not the other way round, for example:
                    // this is a daily enumerator and FF resolution is second, we are expected to emit a bar every second, we can't wait until the timeout each time
                    underlyingTimeout = _underlyingTimeout;
                }

                var nextEndTimeUtc = (fillForward.EndTime + underlyingTimeout).ConvertToUtc(Exchange.TimeZone);
                if (next != null || nextEndTimeUtc <= _timeProvider.GetUtcNow())
                {
                    // we FF if next is here but in the future or next has not come yet and we've wait enough time
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Helper method to determine if we should wait for data before emitting a fill forward bar.
        /// We only wait for data if the fill forward bar is either in the market open or close time.
        /// </summary>
        private bool ShouldWaitForData(BaseData fillForward)
        {
            if (fillForward.Symbol.SecurityType != SecurityType.Equity || Exchange.Hours.IsMarketAlwaysOpen)
            {
                return false;
            }

            // Update market open and close daily
            if (_lastDate != fillForward.EndTime.Date ||
                // Update market open and close for days with multiple sessions, e.g. early close and then late open
                fillForward.Time.TimeOfDay > _marketCloseTimeSpan)
            {
                _lastDate = fillForward.EndTime.Date;
                var marketOpen = Exchange.Hours.GetNextMarketOpen(_lastDate, false);
                var marketClose = Exchange.Hours.GetNextMarketClose(_lastDate, false);

                if (_dataResolution == Time.OneHour || (_dataResolution == Time.OneDay && !UseStrictEndTime))
                {
                    marketOpen = marketOpen.RoundDown(_dataResolution);
                    marketClose = marketClose.RoundUp(_dataResolution);
                }

                _marketOpenTimeSpan = marketOpen.TimeOfDay;
                _marketCloseTimeSpan = marketClose.TimeOfDay;
            }

            // we only wait for data if the fill forward bar is not in the market open or close time
            return fillForward.Time.TimeOfDay == _marketOpenTimeSpan || fillForward.EndTime.TimeOfDay == _marketCloseTimeSpan;
        }

        /// <summary>
        /// Helper method to know how much we should wait before fill forwarding a bar in live trading
        /// </summary>
        /// <remarks>This allows us to create bars taking into account the market auction close and open official prices. Also it will
        /// allow data providers which might have some delay on creating the bars on their end, to be consumed correctly, when available, by Lean</remarks>
        public static TimeSpan GetMaximumDataTimeout(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return TimeSpan.Zero;
                case Resolution.Second:
                    return TimeSpan.FromSeconds(0.9);
                case Resolution.Minute:
                    return TimeSpan.FromMinutes(0.9);
                case Resolution.Hour:
                    return TimeSpan.FromMinutes(10);
                case Resolution.Daily:
                    return TimeSpan.FromMinutes(10);
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }
    }
}
