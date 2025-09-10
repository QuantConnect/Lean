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
        private readonly bool _dailyStrictEndTimeEnabled;
        private readonly bool _extendedMarketHours;
        private bool _useStrictEndTime;

        /// <summary>
        /// The consolidation period requested
        /// </summary>
        protected TimeSpan Period { get; }

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
        public MarketHourAwareConsolidator(bool dailyStrictEndTimeEnabled, Resolution resolution, Type dataType, TickType tickType, bool extendedMarketHours)
        {
            _dailyStrictEndTimeEnabled = dailyStrictEndTimeEnabled;
            Period = resolution.ToTimeSpan();
            _extendedMarketHours = extendedMarketHours;

            if (dataType == typeof(Tick))
            {
                if (tickType == TickType.Trade)
                {
                    Consolidator = resolution == Resolution.Daily
                        ? new TickConsolidator(DailyStrictEndTime)
                        : new TickConsolidator(Period);
                }
                else
                {
                    Consolidator = resolution == Resolution.Daily
                        ? new TickQuoteBarConsolidator(DailyStrictEndTime)
                        : new TickQuoteBarConsolidator(Period);
                }
            }
            else if (dataType == typeof(TradeBar))
            {
                Consolidator = resolution == Resolution.Daily
                    ? new TradeBarConsolidator(DailyStrictEndTime)
                    : new TradeBarConsolidator(Period);
            }
            else if (dataType == typeof(QuoteBar))
            {
                Consolidator = resolution == Resolution.Daily
                    ? new QuoteBarConsolidator(DailyStrictEndTime)
                    : new QuoteBarConsolidator(Period);
            }
            else
            {
                throw new ArgumentNullException(nameof(dataType), $"{dataType.Name} not supported");
            }
            Consolidator.DataConsolidated += ForwardConsolidatedBar;
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public virtual void Update(IBaseData data)
        {
            Initialize(data);

            // US equity hour data from the database starts at 9am but the exchange opens at 9:30am. Thus, we need to handle
            // this case specifically to avoid skipping the first hourly bar. To avoid this, we assert the period is daily,
            // the data resolution is hour and the exchange opens at any point in time over the data.Time to data.EndTime interval
            if (_extendedMarketHours || IsWithinMarketHours(data))
            {
                Consolidator.Update(data);
            }
        }

        /// <summary>
        /// Checks if the data is within the market hours
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected bool IsWithinMarketHours(IBaseData data)
        {
            return ExchangeHours.IsOpen(data.Time, false) ||
                (Period == Time.OneDay && (data.EndTime - data.Time == Time.OneHour) && ExchangeHours.IsOpen(data.Time, data.EndTime, false));
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
            Consolidator.DataConsolidated -= ForwardConsolidatedBar;
            Consolidator.Dispose();
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public void Reset()
        {
            _useStrictEndTime = false;
            ExchangeHours = null;
            DataTimeZone = null;
            Consolidator.Reset();
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

                _useStrictEndTime = UseStrictEndTime(data.Symbol);
            }
        }

        /// <summary>
        /// Determines a bar start time and period
        /// </summary>
        protected virtual CalendarInfo DailyStrictEndTime(DateTime dateTime)
        {
            if (!_useStrictEndTime)
            {
                return new(Period > Time.OneDay ? dateTime : dateTime.RoundDown(Period), Period);
            }
            return LeanData.GetDailyCalendar(dateTime, ExchangeHours, _extendedMarketHours);
        }

        /// <summary>
        /// Useful for testing
        /// </summary>
        protected virtual bool UseStrictEndTime(Symbol symbol)
        {
            return LeanData.UseStrictEndTime(_dailyStrictEndTimeEnabled, symbol, Period, ExchangeHours);
        }

        /// <summary>
        /// Will forward the underlying consolidated bar to consumers on this object
        /// </summary>
        protected virtual void ForwardConsolidatedBar(object sender, IBaseData consolidated)
        {
            DataConsolidated?.Invoke(this, consolidated);
        }
    }
}
