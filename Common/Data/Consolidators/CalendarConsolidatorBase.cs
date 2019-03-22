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
using System.Globalization;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Type of Calendar of the CalendarConsolidator : monthly or weekly
    /// </summary>
    public enum CalendarType
    {
        /// <summary>
        /// Monthly Resolution -> Starts on the 1st of each month
        /// </summary>
        Monthly,
        /// <summary>
        /// Weekly Resolution -> Starts on the Sunday of each month
        /// </summary>
        Weekly
    }

    /// <summary>
    /// Provides a base class for consolidators that emit data based on the passing of a period of time
    /// or after seeing a max count of data points.
    /// </summary>
    /// <typeparam name="T">The input type of the consolidator</typeparam>
    /// <typeparam name="TConsolidated">The output type of the consolidator</typeparam>
    public abstract class CalendarConsolidatorBase<T, TConsolidated> : DataConsolidator<T>
        where T : IBaseData
        where TConsolidated : BaseData
    {
        private readonly Calendar _calendar = CultureInfo.InvariantCulture.Calendar;

        /// <summary>
        /// Type of calendar to consolidate: weekly or monthly
        /// </summary>
        private CalendarType _calendarType = CalendarType.Weekly;

        // The working bar used for aggregating the data
        private TConsolidated _workingBar;

        // The last week we emitted an consolidated bar
        private int _lastCalendarValue = -1;

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType => typeof(TConsolidated);

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _workingBar?.Clone();

        /// <summary>
        /// Event handler that fires when a new piece of data is produced. We define this as a 'new'
        /// event so we can expose it as a <typeparamref name="TConsolidated"/> instead of a <see cref="BaseData"/> instance
        /// </summary>
        public new event EventHandler<TConsolidated> DataConsolidated;

        /// <summary>
        /// Sets the calendar type
        /// </summary>
        /// <param name="calendarType"></param>
        public void SetCalendarType(CalendarType calendarType) => _calendarType = calendarType;

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event
        /// In time span mode, the bar range is closed on the left and open on the right: [T, T+TimeSpan).
        /// For example, if time span is 1 minute, we have [10:00, 10:01): so data at 10:01 is not 
        /// included in the bar starting at 10:00.
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(T data)
        {
            if (!ShouldProcess(data))
            {
                // first allow the base class a chance to filter out data it doesn't want
                // before we start incrementing counts and what not
                return;
            }

            var calendarValue = GetCalendarValue(data.Time);

            // Event is fired with the week of the year changes and the _working bar is not null
            if (calendarValue != _lastCalendarValue && _workingBar != null)
            {
                AggregateBar(ref _workingBar, data);

                var period = GetRoundedBarTime(data.EndTime) - _workingBar.Time;

                var workingTradeBar = _workingBar as TradeBar;
                if (workingTradeBar != null) workingTradeBar.Period = period;

                var workingQuoteBar = _workingBar as QuoteBar;
                if (workingQuoteBar != null) workingQuoteBar.Period = period;

                OnDataConsolidated(_workingBar);
                _lastCalendarValue = calendarValue;
                _workingBar = null;
            }

            AggregateBar(ref _workingBar, data);

            // Updates _lastWeekOfYear in the first iteration
            if (_lastCalendarValue < 0)
            {
                _lastCalendarValue = calendarValue;
            }
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
            if (_workingBar == null) return;

            var calendarValue = GetCalendarValue(currentLocalTime);

            if (calendarValue != _lastCalendarValue)
            {
                OnDataConsolidated(_workingBar);
                _lastCalendarValue = calendarValue;
                _workingBar = null;
            }
        }

        /// <summary>
        /// Gets the time period for this consolidator
        /// </summary>
        protected TimeSpan Period => TimeSpan.FromDays(_calendarType == CalendarType.Monthly ? 30 : 7);

        /// <summary>
        /// Determines whether or not the specified data should be processd
        /// </summary>
        /// <param name="data">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected virtual bool ShouldProcess(T data) => true;

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new consolidated bar</param>
        /// <param name="data">The new data</param>
        protected abstract void AggregateBar(ref TConsolidated workingBar, T data);

        /// <summary>
        /// Gets a rounded-down bar time. Called by AggregateBar in derived classes.
        /// The weekly bars starts on Sunday mid-night
        /// </summary>
        /// <param name="time">The bar time to be rounded down</param>
        /// <returns>The rounded bar time</returns>
        protected DateTime GetRoundedBarTime(DateTime time)
        {
            if (_calendarType == CalendarType.Monthly)
            {
                return new DateTime(time.Year, time.Month, 1);
            }

            while (time.DayOfWeek != DayOfWeek.Sunday)
            {
                time = time.AddDays(-1);
            }
            return time.Date;
        }

        /// <summary>
        /// Event invocator for the <see cref="DataConsolidated"/> event
        /// </summary>
        /// <param name="e">The consolidated data</param>
        protected virtual void OnDataConsolidated(TConsolidated e)
        {
            base.OnDataConsolidated(e);
            DataConsolidated?.Invoke(this, e);
        }

        /// <summary>
        /// Returns the week of the year that includes the date in the specified <see cref="DateTime"/> value
        /// </summary>
        /// <param name="time">A date and time value</param>
        /// <returns>Integer representing the week of the year</returns>
        private int GetCalendarValue(DateTime time)
        {
            return _calendarType == CalendarType.Monthly ? time.Month
                : _calendar.GetWeekOfYear(time, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }
    }
}