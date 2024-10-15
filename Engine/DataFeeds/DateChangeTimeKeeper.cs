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

        private DateTime _previousNewExchangeDate;

        private bool _needsMoveNext = true;

        /// <summary>
        /// The current time in the data time zone
        /// </summary>
        public DateTime DataTime => GetTimeIn(_config.DataTimeZone);

        /// <summary>
        /// The current time in the exchange time zone
        /// </summary>
        public DateTime ExchangeTime => GetTimeIn(_config.ExchangeTimeZone);

        /// <summary>
        /// Event that fires every time the exchange date changes
        /// </summary>
        public event EventHandler<DateTime> NewExchangeDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateChangeTimeKeeper"/> class
        /// </summary>
        /// <param name="tradableDatesInDataTimeZone">The tradable dates in data time zone</param>
        /// <param name="config">The subscription data configuration this instance will keep track of time for</param>
        public DateChangeTimeKeeper(IEnumerable<DateTime> tradableDatesInDataTimeZone, SubscriptionDataConfig config)
            : base(Time.BeginningOfTime, new[] { config.DataTimeZone, config.ExchangeTimeZone })
        {
            _tradableDatesInDataTimeZone = tradableDatesInDataTimeZone.GetEnumerator();
            _config = config;
            _exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(config);
        }

        /// <summary>
        /// Disposes the resources
        /// </summary>
        public void Dispose()
        {
            _tradableDatesInDataTimeZone.DisposeSafely();
        }

        /// <summary>
        /// Advances the time keeper until the target exchange time, emitting a new exchange date event if the date changes
        /// </summary>
        public void AdvanceUntilExchangeTime(DateTime targetExchangeTime)
        {
            var currentExchangeTime = ExchangeTime;
            // Already past the end time, no need to move
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

                var previousExchangeDate = ExchangeTime.Date;
                var newExchangeDate = newExchangeTime.Date;

                if (newExchangeDate != previousExchangeDate &&
                    _exchangeHours.IsDateOpen(newExchangeDate, _config.ExtendedMarketHours))
                {
                    // Stop here, set the new exchange
                    SetUtcDateTime(newExchangeDate.ConvertToUtc(_config.ExchangeTimeZone));
                    NewExchangeDate?.Invoke(this, newExchangeDate);
                    _previousNewExchangeDate = newExchangeDate;
                    return;
                }

                currentExchangeTime = newExchangeTime;
            }

            SetUtcDateTime(targetExchangeTime.ConvertToUtc(_config.ExchangeTimeZone));
        }

        /// <summary>
        /// Advances the time keeper until the next data date, emitting the new exchange date if this happens before the new data date
        /// </summary>
        public bool TryAdvanceUntilNextDataDate()
        {
            // Before moving forward, check whether we need to emit a new exchange date
            if (_needsMoveNext && _tradableDatesInDataTimeZone.Current != default)
            {
                // This data date passed, and it should have emitted as an exchange tradable date when detected
                // as a date change in the data itself, if not, emit it now before moving to the next data date
                var currentDataDate = _tradableDatesInDataTimeZone.Current;
                if (_previousNewExchangeDate < currentDataDate &&
                    _exchangeHours.IsDateOpen(currentDataDate, _config.ExtendedMarketHours))
                {
                    var nextExchangeDate = currentDataDate;
                    EmitNewExchangeDate(nextExchangeDate);
                    SetUtcDateTime(nextExchangeDate.ConvertTo(_config.ExchangeTimeZone, _config.DataTimeZone));
                    return true;
                }
            }

            if (!_needsMoveNext || _tradableDatesInDataTimeZone.MoveNext())
            {
                _needsMoveNext = true;
                var nextDataDate = _tradableDatesInDataTimeZone.Current;
                var nextExchangeTime = nextDataDate.ConvertTo(_config.DataTimeZone, _config.ExchangeTimeZone);
                var nextExchangeDate = nextExchangeTime.Date;

                DateTime exchangeDateToEmit = default;
                var emittingFirstDataDateAsFirstExchangeDate = false;
                // Emit a new exchange date if:
                // 1. This is the first date (to do first daily things, like mappings)
                if (_previousNewExchangeDate == default)
                {
                    if (nextExchangeTime < nextDataDate && _exchangeHours.IsDateOpen(nextExchangeDate, _config.ExtendedMarketHours))
                    {
                        exchangeDateToEmit = nextExchangeDate;
                    }
                    else
                    {
                        exchangeDateToEmit = nextDataDate;
                        emittingFirstDataDateAsFirstExchangeDate = true;
                    }
                }
                // 2. Or, exchange tz is ahead of data tz (date changes at exchange before) and exchange date changed
                else if (!IsExchangeBehindData(nextExchangeTime, nextDataDate) && nextExchangeDate > _previousNewExchangeDate)
                {
                    exchangeDateToEmit = nextExchangeDate;
                }

                if (exchangeDateToEmit != default)
                {
                    EmitNewExchangeDate(exchangeDateToEmit);
                    // Don't move the enumerator next time, we are emitting an exchange date change
                    if (!emittingFirstDataDateAsFirstExchangeDate)
                    {
                        nextDataDate = exchangeDateToEmit.ConvertTo(_config.ExchangeTimeZone, _config.DataTimeZone);
                        _needsMoveNext = exchangeDateToEmit == nextDataDate;
                    }
                }

                SetUtcDateTime(nextDataDate.ConvertToUtc(_config.DataTimeZone));
                return true;
            }

            return false;
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
