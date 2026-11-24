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
*/

using System;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The exchange time zones of the assets an indicator combines, so that it publishes the values of assets of
    /// different exchange time zones in one time zone, and groups their inputs by their UTC times
    /// </summary>
    internal class AssetTimeZones
    {
        private static readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        private readonly Dictionary<Symbol, DateTimeZone> _exchangeTimeZones = new();
        private DateTimeZone _timeZone;
        private DateTimeZone _firstAssetTimeZone;
        private Resolution? _resolution;

        /// <summary>
        /// The time zone of the indicator's values: by default, the exchange time zone of its first asset
        /// </summary>
        public DateTimeZone TimeZone
        {
            get => _timeZone ?? _firstAssetTimeZone;
            set => _timeZone = value;
        }

        /// <summary>
        /// Adds an asset: the first one's exchange time zone is the default time zone of the indicator's values
        /// </summary>
        public void Add(Symbol symbol)
        {
            _firstAssetTimeZone ??= GetExchangeTimeZone(symbol);
        }

        /// <summary>
        /// Gets the input's end time in the time zone of the indicator's values
        /// </summary>
        public DateTime GetEndTime(IBaseData input)
        {
            var exchangeTimeZone = GetExchangeTimeZone(input.Symbol);
            var timeZone = TimeZone;
            // Without both time zones, such as for an asset the market hours database doesn't have, keep the input's time
            if (exchangeTimeZone == null || timeZone == null)
            {
                return input.EndTime;
            }
            return input.EndTime.ConvertTo(exchangeTimeZone, timeZone);
        }

        /// <summary>
        /// Gets the input's start and end times in UTC, truncated to the resolution of the first input, so that the inputs
        /// of the same period share them whatever their exchange time zones
        /// </summary>
        public (DateTime StartTimeUtc, DateTime EndTimeUtc) GetUtcPeriod(IBaseData input)
        {
            // An asset the market hours database doesn't have keeps its input's times, as before they were in UTC
            var exchangeTimeZone = GetExchangeTimeZone(input.Symbol) ?? TimeZones.Utc;
            _resolution ??= GetResolution(input);
            return (AlignToResolution(input.Time.ConvertToUtc(exchangeTimeZone), _resolution),
                AlignToResolution(input.EndTime.ConvertToUtc(exchangeTimeZone), _resolution));
        }

        /// <summary>
        /// Forgets the resolution of the inputs, so that the next input sets it again
        /// </summary>
        public void Reset()
        {
            _resolution = null;
        }

        /// <summary>
        /// Determines the resolution of the input data based on the time difference between its start and end times.
        /// Resolution will <see cref="Resolution.Daily"/> if the difference exceeds 1 hour; otherwise, calculates a higher equivalent resolution.
        /// </summary>
        public static Resolution GetResolution(IBaseData input)
        {
            var timeDifference = input.EndTime - input.Time;
            return timeDifference.TotalHours > 1 ? Resolution.Daily : timeDifference.ToHigherResolutionEquivalent(false);
        }

        /// <summary>
        /// Truncates the given DateTime based on the specified resolution (Daily, Hourly, Minute, or Second).
        /// </summary>
        /// <param name="date">The DateTime to truncate.</param>
        /// <param name="resolution">The resolution to truncate to</param>
        /// <returns>A DateTime truncated to the specified resolution.</returns>
        public static DateTime AlignToResolution(DateTime date, Resolution? resolution)
        {
            switch (resolution)
            {
                case Resolution.Daily:
                    return date.Date;
                case Resolution.Hour:
                    return date.Date.AddHours(date.Hour);
                case Resolution.Minute:
                    return date.Date.AddHours(date.Hour).AddMinutes(date.Minute);
                default:
                    return date;
            }
        }

        private DateTimeZone GetExchangeTimeZone(Symbol symbol)
        {
            if (symbol == null || symbol == Symbol.Empty)
            {
                return null;
            }
            if (!_exchangeTimeZones.TryGetValue(symbol, out var exchangeTimeZone))
            {
                exchangeTimeZone = _marketHoursDatabase.TryGetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType, out var entry)
                    ? entry.ExchangeHours.TimeZone
                    : null;
                _exchangeTimeZones[symbol] = exchangeTimeZone;
            }
            return exchangeTimeZone;
        }
    }
}
