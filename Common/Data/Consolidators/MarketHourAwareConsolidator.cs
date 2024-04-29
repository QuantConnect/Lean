/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2024 QuantConnect Corporation.
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
using NodaTime;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Data.Common
{
    /// <summary>
    /// Consolidator for open markets bar only, extended hours bar are not consolidated.
    /// </summary>
    public class MarketHourAwareConsolidator : IDataConsolidator
    {
        private readonly bool _extendedMarketHours;
        private readonly TimeSpan _period;
        private bool _strictEndTime;

        /// <summary>
        /// The consolidator instance
        /// </summary>
        protected IDataConsolidator Consolidator { get; }

        /// <summary>
        /// The associated security exchange hours instance
        /// </summary>
        protected SecurityExchangeHours ExchangeHours { get; set; }

        /// <summary>
        /// The associated data time zone
        /// </summary>
        protected DateTimeZone DataTimeZone { get; set; }

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated => Consolidator.Consolidated;

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => Consolidator.InputType;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public IBaseData WorkingData => Consolidator.WorkingData;

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public Type OutputType => Consolidator.OutputType;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHourAwareConsolidator"/> class.
        /// </summary>
        /// <param name="resolution">The resolution.</param>
        /// <param name="dataType">The target data type</param>
        /// <param name="tickType">The target tick type</param>
        /// <param name="extendedMarketHours">True if extended market hours should be consolidated</param>
        public MarketHourAwareConsolidator(Resolution resolution, Type dataType, TickType tickType, bool extendedMarketHours)
        {
            _period = resolution.ToTimeSpan();
            _extendedMarketHours = extendedMarketHours;

            if (dataType == typeof(Tick))
            {
                if (tickType == TickType.Trade)
                {
                    Consolidator = resolution == Resolution.Daily
                        ? new TickConsolidator(DailyStrictEndTime)
                        : new TickConsolidator(_period);
                }
                else
                {
                    Consolidator = resolution == Resolution.Daily
                        ? new TickQuoteBarConsolidator(DailyStrictEndTime)
                        : new TickQuoteBarConsolidator(_period);
                }
            }
            else if (dataType == typeof(TradeBar))
            {
                Consolidator = resolution == Resolution.Daily
                    ? new TradeBarConsolidator(DailyStrictEndTime)
                    : new TradeBarConsolidator(_period);
            }
            else if (dataType == typeof(QuoteBar))
            {
                Consolidator = resolution == Resolution.Daily
                    ? new QuoteBarConsolidator(DailyStrictEndTime)
                    : new QuoteBarConsolidator(_period);
            }
            else
            {
                throw new ArgumentNullException(nameof(dataType), $"{dataType.Name} not supported");
            }
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated
        {
            add => Consolidator.DataConsolidated += value;
            remove => Consolidator.DataConsolidated -= value;
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public virtual void Update(IBaseData data)
        {
            Initialize(data);

            if (_extendedMarketHours || ExchangeHours.IsOpen(data.Time, false))
            {
                Consolidator.Update(data);
            }
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="P:QuantConnect.Data.BaseData.Time" />)</param>
        public void Scan(DateTime currentLocalTime)
        {
            Consolidator.Scan(currentLocalTime);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Consolidator.Dispose();
        }

        /// <summary>
        /// Perform late initialization based on the datas symbol
        /// </summary>
        protected void Initialize(IBaseData data)
        {
            if (ExchangeHours == null)
            {
                var symbol = data.Symbol;
                var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                ExchangeHours = marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
                DataTimeZone = marketHoursDatabase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

                _strictEndTime = UseStrictEndTime(data.Symbol.SecurityType);
            }
        }

        /// <summary>
        /// Determines a bar start time and period
        /// </summary>
        protected virtual CalendarInfo DailyStrictEndTime(DateTime dateTime)
        {
            if (!_strictEndTime)
            {
                return new (_period > Time.OneDay ? dateTime : dateTime.RoundDown(_period), _period);
            }
            return LeanData.GetDailyCalendar(dateTime, ExchangeHours, _extendedMarketHours);
        }

        /// <summary>
        /// Useful for testing
        /// </summary>
        protected virtual bool UseStrictEndTime(SecurityType securityType)
        {
            return LeanData.UseStrictEndTime(securityType, _period);
        }
    }
}
