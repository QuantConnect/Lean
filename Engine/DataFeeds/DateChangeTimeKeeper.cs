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
using QuantConnect.Util;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Securities;
using System.Runtime.CompilerServices;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Time keeper specialization to keep time for a subscription both in data and exchange time zones.
    /// It also emits events when the exchange date changes, which is useful to emit date change events
    /// required for some daily actions like mapping symbols, delistings, splits, etc.
    /// </summary>
    internal class DateChangeTimeKeeper : TimeKeeper, IDisposable
    {
        private IEnumerator<DateTime> _tradableDatesInDataTimeZone;
        private SubscriptionDataConfig _config;
        private SecurityExchangeHours _exchangeHours;
        private DateTime _delistingDate;

        private DateTime _previousNewExchangeDate;

        private bool _needsMoveNext;
        private bool _initialized;

        private DateTime _exchangeTime;
        private DateTime _dataTime;
        private bool _exchangeTimeNeedsUpdate;
        private bool _dataTimeNeedsUpdate;

        /// <summary>
        /// The current time in the data time zone
        /// </summary>
        public DateTime DataTime
        {
            get
            {
                if (_dataTimeNeedsUpdate)
                {
                    _dataTime = GetTimeIn(_config.DataTimeZone);
                    _dataTimeNeedsUpdate = false;
                }
                return _dataTime;
            }
        }

        /// <summary>
        /// The current time in the exchange time zone
        /// </summary>
        public DateTime ExchangeTime
        {
            get
            {
                if (_exchangeTimeNeedsUpdate)
                {
                    _exchangeTime = GetTimeIn(_config.ExchangeTimeZone);
                    _exchangeTimeNeedsUpdate = false;
                }
                return _exchangeTime;
            }
        }

        /// <summary>
        /// Event that fires every time the exchange date changes
        /// </summary>
        public event EventHandler<DateTime> NewExchangeDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateChangeTimeKeeper"/> class
        /// </summary>
        /// <param name="tradableDatesInDataTimeZone">The tradable dates in data time zone</param>
        /// <param name="config">The subscription data configuration this instance will keep track of time for</param>
        /// <param name="exchangeHours">The exchange hours</param>
        /// <param name="delistingDate">The symbol's delisting date</param>
        public DateChangeTimeKeeper(IEnumerable<DateTime> tradableDatesInDataTimeZone, SubscriptionDataConfig config,
            SecurityExchangeHours exchangeHours, DateTime delistingDate)
            : base(Time.BeginningOfTime, new[] { config.DataTimeZone, config.ExchangeTimeZone })
        {
            _tradableDatesInDataTimeZone = tradableDatesInDataTimeZone.GetEnumerator();
            _config = config;
            _exchangeHours = exchangeHours;
            _delistingDate = delistingDate;
            _exchangeTimeNeedsUpdate = true;
            _dataTimeNeedsUpdate = true;
            _needsMoveNext = true;
        }

        /// <summary>
        /// Disposes the resources
        /// </summary>
        public void Dispose()
        {
            _tradableDatesInDataTimeZone.DisposeSafely();
        }

        public override void SetUtcDateTime(DateTime utcDateTime)
        {
            base.SetUtcDateTime(utcDateTime);
            _exchangeTimeNeedsUpdate = true;
            _dataTimeNeedsUpdate = true;
        }

        /// <summary>
        /// Advances the time keeper until the target exchange time, emitting a new exchange date event if the date changes
        /// </summary>
        public void AdvanceUntilExchangeTime(DateTime targetExchangeTime)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException($"The time keeper has not been initialized. " +
                    $"{nameof(TryAdvanceUntilNextDataDate)} needs to be called at least once to flush the first date before advancing.");
            }

            var currentExchangeTime = ExchangeTime;
            // Already past the end time, no need to move. Catch this here so that the time keeper time is not updated
            if (targetExchangeTime <= currentExchangeTime)
            {
                return;
            }

            while (currentExchangeTime < targetExchangeTime)
            {
                var newExchangeTime = currentExchangeTime + Time.OneDay;
                if (newExchangeTime > targetExchangeTime)
                {
                    newExchangeTime = targetExchangeTime;
                }

                var newExchangeDate = newExchangeTime.Date;

                // We found a new exchange date before the target time, emit it first
                if (newExchangeDate != currentExchangeTime.Date &&
                    _exchangeHours.IsDateOpen(newExchangeDate, _config.ExtendedMarketHours))
                {
                    // Stop here, set the new exchange date
                    SetExchangeTime(newExchangeDate);
                    EmitNewExchangeDate(newExchangeDate);
                    return;
                }

                currentExchangeTime = newExchangeTime;
            }

            // We reached the target time, set it
            SetExchangeTime(targetExchangeTime);
        }

        /// <summary>
        /// Advances the time keeper until the next data date, emitting the new exchange date if this happens before the new data date
        /// </summary>
        public bool TryAdvanceUntilNextDataDate()
        {
            if (!_initialized)
            {
                return EmitFirstExchangeDate();
            }

            // Before moving forward, check whether we need to emit a new exchange date
            if (TryEmitPassedExchangeDate())
            {
                return true;
            }

            if (!_needsMoveNext || _tradableDatesInDataTimeZone.MoveNext())
            {
                var nextDataDate = _tradableDatesInDataTimeZone.Current;
                var nextExchangeTime = nextDataDate.ConvertTo(_config.DataTimeZone, _config.ExchangeTimeZone);
                var nextExchangeDate = nextExchangeTime.Date;

                if (nextExchangeDate > _delistingDate)
                {
                    // We are done, but an exchange date might still need to be emitted
                    TryEmitPassedExchangeDate();
                    _needsMoveNext = false;
                    return false;
                }

                // If the exchange is not behind the data, the data might have not been enough to emit the exchange date,
                // which already passed if we are moving on to the next data date. So we need to check if we need to emit it here.
                // e.g. moving data date from tuesday to wednesday, but the exchange date is already past the end of tuesday
                // (by N hours, depending on the time zones offset). If data didn't trigger the exchange date change, we need to do it here.
                if (!IsExchangeBehindData(nextExchangeTime, nextDataDate) && nextExchangeDate > _previousNewExchangeDate)
                {
                    EmitNewExchangeDate(nextExchangeDate);
                    SetExchangeTime(nextExchangeDate);
                    // nextExchangeDate == DataTime means time zones are synchronized, need to move next only when exchange is actually ahead
                    _needsMoveNext = nextExchangeDate == DataTime;
                    return true;
                }

                _needsMoveNext = true;
                SetDataTime(nextDataDate);
                return true;
            }

            _needsMoveNext = false;
            return false;
        }

        private bool EmitFirstExchangeDate()
        {
            if (_initialized)
            {
                return false;
            }

            if (!_tradableDatesInDataTimeZone.MoveNext())
            {
                _initialized = true;
                return false;
            }

            var firstDataDate = _tradableDatesInDataTimeZone.Current;
            var firstExchangeTime = firstDataDate.ConvertTo(_config.DataTimeZone, _config.ExchangeTimeZone);
            var firstExchangeDate = firstExchangeTime.Date;

            DateTime exchangeDateToEmit;
            // The exchange is ahead of the data, so we need to emit the current exchange date, which already passed
            if (firstExchangeTime < firstDataDate && _exchangeHours.IsDateOpen(firstExchangeDate, _config.ExtendedMarketHours))
            {
                exchangeDateToEmit = firstExchangeDate;
                SetExchangeTime(exchangeDateToEmit);
                // Don't move, the current data date still needs to be consumed
                _needsMoveNext = false;
            }
            // The exchange is behind of (or in sync with) data: exchange has not passed to this new date, but with emit it here
            // so that first daily things are done (mappings, delistings, etc.)
            else
            {
                exchangeDateToEmit = firstDataDate;
                SetDataTime(firstDataDate);
                _needsMoveNext = true;
            }

            EmitNewExchangeDate(exchangeDateToEmit);
            _initialized = true;
            return true;
        }

        /// <summary>
        /// Determines whether the exchange time zone is behind the data time zone
        /// </summary>
        /// <returns></returns>
        public bool IsExchangeBehindData()
        {
            return IsExchangeBehindData(ExchangeTime, DataTime);
        }

        /// <summary>
        /// Determines whether the exchange time zone is behind the data time zone
        /// </summary>
        private static bool IsExchangeBehindData(DateTime exchangeTime, DateTime dataTime)
        {
            return dataTime > exchangeTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetExchangeTime(DateTime exchangeTime)
        {
            SetUtcDateTime(exchangeTime.ConvertToUtc(_config.ExchangeTimeZone));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDataTime(DateTime dataTime)
        {
            SetUtcDateTime(dataTime.ConvertToUtc(_config.DataTimeZone));
        }

        private bool TryEmitPassedExchangeDate()
        {
            if (_needsMoveNext && _tradableDatesInDataTimeZone.Current != default)
            {
                // This data date passed, and it should have emitted as an exchange tradable date when detected
                // as a date change in the data itself, if not, emit it now before moving to the next data date
                var currentDataDate = _tradableDatesInDataTimeZone.Current;
                if (_previousNewExchangeDate < currentDataDate &&
                    _exchangeHours.IsDateOpen(currentDataDate, _config.ExtendedMarketHours))
                {
                    var nextExchangeDate = currentDataDate;
                    SetExchangeTime(nextExchangeDate);
                    EmitNewExchangeDate(nextExchangeDate);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Emits a new exchange date event
        /// </summary>
        private void EmitNewExchangeDate(DateTime newExchangeDate)
        {
            NewExchangeDate?.Invoke(this, newExchangeDate);
            _previousNewExchangeDate = newExchangeDate;
        }
    }
}
