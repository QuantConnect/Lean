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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// The FillForwardEnumerator wraps an existing base data enumerator and inserts extra 'base data' instances
    /// on a specified fill forward resolution
    /// </summary>
    public class FillForwardEnumerator : IEnumerator<BaseData>
    {
        private DateTime? _delistedTime;
        private BaseData _previous;
        private bool _ended;
        private bool _isFillingForward;

        private readonly bool _useStrictEndTime;
        private readonly TimeSpan _dataResolution;
        private readonly DateTimeZone _dataTimeZone;
        private readonly bool _isExtendedMarketHours;
        private readonly DateTime _subscriptionEndTime;
        private readonly CalendarInfo _subscriptionEndDataCalendar;
        private readonly IEnumerator<BaseData> _enumerator;
        private readonly IReadOnlyRef<TimeSpan> _fillForwardResolution;

        /// <summary>
        /// The exchange used to determine when to insert fill forward data
        /// </summary>
        protected readonly SecurityExchange Exchange;

        /// <summary>
        /// Initializes a new instance of the <see cref="FillForwardEnumerator"/> class that accepts
        /// a reference to the fill forward resolution, useful if the fill forward resolution is dynamic
        /// and changing as the enumeration progresses
        /// </summary>
        /// <param name="enumerator">The source enumerator to be filled forward</param>
        /// <param name="exchange">The exchange used to determine when to insert fill forward data</param>
        /// <param name="fillForwardResolution">The resolution we'd like to receive data on</param>
        /// <param name="isExtendedMarketHours">True to use the exchange's extended market hours, false to use the regular market hours</param>
        /// <param name="subscriptionEndTime">The end time of the subscrition, once passing this date the enumerator will stop</param>
        /// <param name="dataResolution">The source enumerator's data resolution</param>
        /// <param name="dataTimeZone">The time zone of the underlying source data. This is used for rounding calculations and
        /// is NOT the time zone on the BaseData instances (unless of course data time zone equals the exchange time zone)</param>
        /// <param name="dailyStrictEndTimeEnabled">True if daily strict end times are enabled</param>
        public FillForwardEnumerator(IEnumerator<BaseData> enumerator,
            SecurityExchange exchange,
            IReadOnlyRef<TimeSpan> fillForwardResolution,
            bool isExtendedMarketHours,
            DateTime subscriptionEndTime,
            TimeSpan dataResolution,
            DateTimeZone dataTimeZone,
            bool dailyStrictEndTimeEnabled
            )
        {
            _subscriptionEndTime = subscriptionEndTime;
            Exchange = exchange;
            _enumerator = enumerator;
            _dataResolution = dataResolution;
            _dataTimeZone = dataTimeZone;
            _fillForwardResolution = fillForwardResolution;
            _isExtendedMarketHours = isExtendedMarketHours;
            _useStrictEndTime = dailyStrictEndTimeEnabled;

            // '_dataResolution' and '_subscriptionEndTime' are readonly they won't change, so lets calculate this once here since it's expensive
            if (_useStrictEndTime)
            {
                var lastDayCalendar = LeanData.GetDailyCalendar(_subscriptionEndTime, Exchange.Hours, _isExtendedMarketHours);
                while (lastDayCalendar.End > _subscriptionEndTime)
                {
                    lastDayCalendar = LeanData.GetDailyCalendar(lastDayCalendar.Start.AddDays(-1), Exchange.Hours, _isExtendedMarketHours);
                }
                _subscriptionEndDataCalendar = lastDayCalendar;
            }
            else
            {
                _subscriptionEndDataCalendar = new (RoundDown(_subscriptionEndTime, _dataResolution), _dataResolution);
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            if (_delistedTime.HasValue)
            {
                // don't fill forward after data after the delisted date
                if (_previous == null || _previous.EndTime >= _delistedTime.Value)
                {
                    return false;
                }
            }

            if (Current != null && Current.DataType != MarketDataType.Auxiliary)
            {
                // only set the _previous if the last item we emitted was NOT auxilliary data,
                // since _previous is used for fill forward behavior
                _previous = Current;
            }

            BaseData fillForward;

            if (!_isFillingForward)
            {
                // if we're filling forward we don't need to move next since we haven't emitted _enumerator.Current yet
                if (!_enumerator.MoveNext())
                {
                    _ended = true;
                    if (_delistedTime.HasValue)
                    {
                        // don't fill forward delisted data
                        return false;
                    }

                    // check to see if we ran out of data before the end of the subscription
                    if (_previous == null || _previous.EndTime >= _subscriptionEndTime)
                    {
                        // we passed the end of subscription, we're finished
                        return false;
                    }

                    // we can fill forward the rest of this subscription if required
                    var endOfSubscription = (Current ?? _previous).Clone(true);
                    endOfSubscription.Time = _subscriptionEndDataCalendar.Start;
                    endOfSubscription.EndTime = endOfSubscription.Time + _subscriptionEndDataCalendar.Period;
                    if (RequiresFillForwardData(_fillForwardResolution.Value, _previous, endOfSubscription, out fillForward))
                    {
                        // don't mark as filling forward so we come back into this block, subscription is done
                        //_isFillingForward = true;
                        Current = fillForward;
                        return true;
                    }

                    // don't emit the last bar if the market isn't considered open!
                    if (!Exchange.IsOpenDuringBar(endOfSubscription.Time, endOfSubscription.EndTime, _isExtendedMarketHours))
                    {
                        return false;
                    }

                    if (Current != null && Current.EndTime == endOfSubscription.EndTime
                        // TODO this changes stats, why would the FF enumerator emit a data point beyoned the end time he was requested
                        //|| endOfSubscription.EndTime > _subscriptionEndTime
                        )
                    {
                        return false;
                    }
                    Current = endOfSubscription;
                    return true;
                }
            }
            // If we are filling forward and the underlying is null, let's MoveNext() as long as it didn't end.
            // This only applies for live trading, so that the LiveFillForwardEnumerator does not stall whenever
            // we generate a fill-forward bar. The underlying enumerator is advanced so that we don't get stuck
            // in a cycle of generating infinite fill-forward bars.
            else if (_enumerator.Current == null && !_ended)
            {
                _ended = _enumerator.MoveNext();
            }

            var underlyingCurrent = _enumerator.Current;
            if (underlyingCurrent != null && underlyingCurrent.DataType == MarketDataType.Auxiliary)
            {
                var delisting = underlyingCurrent as Delisting;
                if (delisting?.Type == DelistingType.Delisted)
                {
                    _delistedTime = delisting.EndTime;
                }
            }

            if (_previous == null)
            {
                // first data point we dutifully emit without modification
                Current = underlyingCurrent;
                return true;
            }

            if (RequiresFillForwardData(_fillForwardResolution.Value, _previous, underlyingCurrent, out fillForward))
            {
                if (_previous.EndTime >= _subscriptionEndTime)
                {
                    // we passed the end of subscription, we're finished
                    return false;
                }
                // we require fill forward data because the _enumerator.Current is too far in future
                _isFillingForward = true;
                Current = fillForward;
                return true;
            }

            _isFillingForward = false;
            Current = underlyingCurrent;
            return true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Determines whether or not fill forward is required, and if true, will produce the new fill forward data
        /// </summary>
        /// <param name="fillForwardResolution"></param>
        /// <param name="previous">The last piece of data emitted by this enumerator</param>
        /// <param name="next">The next piece of data on the source enumerator</param>
        /// <param name="fillForward">When this function returns true, this will have a non-null value, null when the function returns false</param>
        /// <returns>True when a new fill forward piece of data was produced and should be emitted by this enumerator</returns>
        protected virtual bool RequiresFillForwardData(TimeSpan fillForwardResolution, BaseData previous, BaseData next, out BaseData fillForward)
        {
            // in live trading next can be null, in which case we create a potential FF bar and the live FF enumerator will decide what to do
            var nextCalculatedEndTimeUtc = DateTime.MaxValue;
            if (next != null)
            {
                // convert times to UTC for accurate comparisons and differences across DST changes
                var previousTimeUtc = previous.Time.ConvertToUtc(Exchange.TimeZone);
                var nextTimeUtc = next.Time.ConvertToUtc(Exchange.TimeZone);
                var nextEndTimeUtc = next.EndTime.ConvertToUtc(Exchange.TimeZone);

                if (nextEndTimeUtc < previousTimeUtc)
                {
                    Log.Error("FillForwardEnumerator received data out of order. Symbol: " + previous.Symbol.ID);
                    fillForward = null;
                    return false;
                }

                // check to see if the gap between previous and next warrants fill forward behavior
                var nextPreviousTimeUtcDelta = nextTimeUtc - previousTimeUtc;
                if (nextPreviousTimeUtcDelta <= fillForwardResolution && nextPreviousTimeUtcDelta <= _dataResolution)
                {
                    fillForward = null;
                    return false;
                }

                var period = _dataResolution;
                if (_useStrictEndTime)
                {
                    // the period is not the data resolution (1 day) and can actually change dynamically, for example early close/late open
                    period = next.EndTime - next.Time;
                }
                else if (next.Time == next.EndTime)
                {
                    // we merge corporate event data points (mapping, delisting, splits, dividend) which do not have
                    // a period or resolution
                    period = TimeSpan.Zero;
                }
                nextCalculatedEndTimeUtc = nextTimeUtc + period;
            }

            // every bar emitted MUST be of the data resolution.

            // compute end times of the four potential fill forward scenarios
            // 1. the next fill forward bar. 09:00-10:00 followed by 10:00-11:00 where 01:00 is the fill forward resolution
            // 2. the next data resolution bar, same as above but with the data resolution instead
            // 3. the next fill forward bar following the next market open, 15:00-16:00 followed by 09:00-10:00 the following open market day
            // 4. the next data resolution bar following the next market open, same as above but with the data resolution instead

            // the precedence for validation is based on the order of the end times, obviously if a potential match
            // is before a later match, the earliest match should win.

            foreach (var item in GetSortedReferenceDateIntervals(previous, fillForwardResolution, _dataResolution))
            {
                // issue GH 4925 , more description https://github.com/QuantConnect/Lean/pull/4941
                // To build Time/EndTime we always use '+'/'-' dataResolution
                // DataTime TZ = UTC -5; Exchange TZ = America/New York (-5/-4)
                // Standard TimeZone    00:00:00 + 1 day = 1.00:00:00
                // Daylight Time        01:00:00 + 1 day = 1.01:00:00

                // daylight saving time starts/end at 2 a.m. on Sunday
                // Having this information we find that the specific bar of Sunday
                // Starts in one TZ (Standard TZ), but finishes in another (Daylight TZ) (consider winter => summer)
                // During simple arithmetic operations like +/- we shift the time, but not the time zone
                // which is sensitive for specific dates (daylight movement) if we are  in Exchange TimeZone, for example
                // We have 00:00:00 + 1 day = 1.00:00:00, so both are in Standard TZ, but we expect endTime in Daylight, i.e. 1.01:00:00

                // futher down double Convert (Exchange TZ => data TZ => Exchange TZ)
                // allows us to calculate Time using it's own TZ (aka reapply)
                // and don't rely on TZ of bar start/end time
                // i.e. 00:00:00 + 1 day = 1.01:00:00, both start and end are in their own TZ
                // it's interesting that NodaTime  consider next
                // if time great or equal than 01:00 AM it's considered as "moved" (Standard, not Daylight)
                // when time less than 01:00 AM it's considered as previous TZ (Standard, not Daylight)
                // it's easy to fix this behavior by substract 1 tick  before first convert, and then return it back.
                // so we work with 0:59:59.. AM instead.
                // but now follow native behavior

                // all above means, that all Time values, calculated using simple +/- operations
                // sticks to original Time Zone, swallowing its own TZ and movement i.e.
                // EndTime = Time + resolution, both Time and EndTime in the TZ of Time (Standard/Daylight)
                // Time = EndTime - resolution, both Time and EndTime in the TZ of EndTime (Standard/Daylight)

                // next.EndTime sticks to Time TZ,
                // potentialBarEndTime should be calculated in the same way as bar.EndTime, i.e. Time + resolution
                // round down doesn't make sense for daily data using strict times
                var startTime = (_useStrictEndTime && item.Period > Time.OneHour) ? item.Start : RoundDown(item.Start, item.Period);
                var potentialBarEndTime = startTime.ConvertToUtc(Exchange.TimeZone) + item.Period;


                // to avoid duality it's necessary to compare potentialBarEndTime with
                // next.EndTime calculated as Time + resolution,
                // and both should be based on the same TZ (for example UTC)
                if (potentialBarEndTime < nextCalculatedEndTimeUtc
                    // let's fill forward based on previous (which isn't auxiliary) if next is auxiliary and they share the end time
                    // we do allow emitting both an auxiliary data point and a Filled Forwared data for the same end time
                    || next != null && next.DataType == MarketDataType.Auxiliary && potentialBarEndTime == nextCalculatedEndTimeUtc)
                {
                    // to check open hours we need to convert potential
                    // bar EndTime into exchange time zone
                    var potentialBarEndTimeInExchangeTZ =
                        potentialBarEndTime.ConvertFromUtc(Exchange.TimeZone);
                    var nextFillForwardBarStartTime = potentialBarEndTimeInExchangeTZ - item.Period;

                    if (Exchange.IsOpenDuringBar(nextFillForwardBarStartTime, potentialBarEndTimeInExchangeTZ, _isExtendedMarketHours))
                    {
                        fillForward = previous.Clone(true);

                        // bar are ALWAYS of the data resolution
                        var expectedPeriod = _dataResolution;
                        if (_useStrictEndTime)
                        {
                            // TODO: what about extended market hours
                            expectedPeriod = Exchange.Hours.RegularMarketDuration;
                        }
                        fillForward.Time = (potentialBarEndTime - expectedPeriod).ConvertFromUtc(Exchange.TimeZone);
                        fillForward.EndTime = potentialBarEndTimeInExchangeTZ;
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }

            // the next is before the next fill forward time, so do nothing
            fillForward = null;
            return false;
        }

        private IEnumerable<CalendarInfo> GetSortedReferenceDateIntervals(BaseData previous, TimeSpan fillForwardResolution, TimeSpan dataResolution)
        {
            if (fillForwardResolution < dataResolution)
            {
                return GetReferenceDateIntervals(previous.EndTime, fillForwardResolution, dataResolution);
            }

            if (fillForwardResolution > dataResolution)
            {
                return GetReferenceDateIntervals(previous.EndTime, dataResolution, fillForwardResolution);
            }

            return GetReferenceDateIntervals(previous.EndTime, fillForwardResolution);
        }

        /// <summary>
        /// Get potential next fill forward bars.
        /// </summary>
        /// <remarks>Special case where fill forward resolution and data resolution are equal</remarks>
        private IEnumerable<CalendarInfo> GetReferenceDateIntervals(DateTime previousEndTime, TimeSpan resolution)
        {
            // say daily bar goes from 9:30 to 16:00, if resolution is 1 day, IsOpenDuringBar can return true but it's not what we want
            if (!_useStrictEndTime && Exchange.IsOpenDuringBar(previousEndTime, previousEndTime + resolution, _isExtendedMarketHours))
            {
                // if next in market us it
                yield return new (previousEndTime, resolution);
            }

            // now we can try the bar after next market open
            var marketOpen = Exchange.Hours.GetNextMarketOpen(previousEndTime, _isExtendedMarketHours);
            if (_useStrictEndTime)
            {
                yield return LeanData.GetDailyCalendar(marketOpen, Exchange.Hours, _isExtendedMarketHours);
            }
            else
            {
                yield return new (marketOpen, resolution);
            }
        }

        /// <summary>
        /// Get potential next fill forward bars.
        /// </summary>
        private IEnumerable<CalendarInfo> GetReferenceDateIntervals(DateTime previousEndTime, TimeSpan smallerResolution, TimeSpan largerResolution)
        {
            List<CalendarInfo> result = null;
            if (Exchange.IsOpenDuringBar(previousEndTime, previousEndTime + smallerResolution, _isExtendedMarketHours))
            {
                if (_useStrictEndTime)
                {
                    // case A
                    result = new()
                    {
                        new(previousEndTime, smallerResolution)
                    };
                }
                else
                {
                    // at the end of this method we perform an OrderBy which does not apply for this case because the consumer of this method
                    // will perform a round down that will end up using an unexpected FF bar. This behavior is covered by tests
                    yield return new (previousEndTime, smallerResolution);
                }
            }
            result ??= new List<CalendarInfo>(4);

            // we need to round down because previous end time could be of the smaller resolution, in data TZ!
            if (_useStrictEndTime)
            {
                // case B: say smaller resolution (FF res) is 1 hour, larget resolution (daily data resolution) is 1 day
                // For example for SPX we need to emit the daily FF bar from 8:30->15:15, even before the 'A' case above which would be 15->16 bar
                var dailyCalendar = LeanData.GetDailyCalendar(previousEndTime, Exchange.Hours, _isExtendedMarketHours);
                if (previousEndTime < (dailyCalendar.Start + dailyCalendar.Period))
                {
                    result.Add(new(dailyCalendar.Start, dailyCalendar.Period));
                }
            }
            else
            {
                var start = RoundDown(previousEndTime, largerResolution);
                if (Exchange.IsOpenDuringBar(start, start + largerResolution, _isExtendedMarketHours))
                {
                    result.Add(new(start, largerResolution));
                }
            }

            // this is typically daily data being filled forward on a higher resolution
            // since the previous bar was not in market hours then we can just fast forward
            // to the next market open
            var marketOpen = Exchange.Hours.GetNextMarketOpen(previousEndTime, _isExtendedMarketHours);
            result.Add(new (marketOpen, smallerResolution));
            if (_useStrictEndTime)
            {
                result.Add(LeanData.GetDailyCalendar(marketOpen, Exchange.Hours, _isExtendedMarketHours));
            }

            // we need to order them because they might not be in an incremental order and consumer expects them to be
            foreach (var referenceDateInterval in result.OrderBy(interval => interval.Start + interval.Period))
            {
                yield return referenceDateInterval;
            }
        }

        /// <summary>
        /// We need to round down in data timezone.
        /// For example GH issue 4392: Forex daily data, exchange tz time is 8PM, but time in data tz is 12AM
        /// so rounding down on exchange tz will crop it, while rounding on data tz will return the same data point time.
        /// Why are we even doing this? being able to determine the next valid data point for a resolution from a data point that might be in another resolution
        /// </summary>
        private DateTime RoundDown(DateTime value, TimeSpan interval)
        {
            return value.RoundDownInTimeZone(interval, Exchange.TimeZone, _dataTimeZone);
        }
    }
}
