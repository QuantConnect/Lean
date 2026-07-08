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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Consolidator for open markets bar only, extended hours bar are not consolidated.
    /// </summary>
    public class MarketHourAwareConsolidator : ConsolidatorBase
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
        /// Gets the type consumed by this consolidator
        /// </summary>
        public override Type InputType => Consolidator.InputType;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => Consolidator.WorkingData;

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType => Consolidator.OutputType;

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

            Consolidator = CreateConsolidator(resolution, dataType, tickType);
            Consolidator.DataConsolidated += ForwardConsolidatedBar;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHourAwareConsolidator"/> class for an arbitrary period.
        /// Intraday periods are anchored to the market open without extending past the close.
        /// </summary>
        /// <param name="dailyStrictEndTimeEnabled">True if daily strict end times should be enabled</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="dataType">The target data type</param>
        /// <param name="tickType">The target tick type</param>
        /// <param name="extendedMarketHours">True if extended market hours should be consolidated</param>
        public MarketHourAwareConsolidator(bool dailyStrictEndTimeEnabled, TimeSpan period, Type dataType, TickType tickType, bool extendedMarketHours)
        {
            _dailyStrictEndTimeEnabled = dailyStrictEndTimeEnabled;
            Period = period;
            _extendedMarketHours = extendedMarketHours;

            // when the period exactly matches a standard resolution, reuse the resolution based consolidation so its
            // well-tested behavior is preserved; only arbitrary periods need the market-open anchored intraday calendar
            var resolution = period.ToHigherResolutionEquivalent(false);
            if (resolution.ToTimeSpan() == period)
            {
                Consolidator = CreateConsolidator(resolution, dataType, tickType);
            }
            else
            {
                Func<DateTime, CalendarInfo> calendar = period < Time.OneDay ? IntradayCalendar : DailyStrictEndTime;
                Consolidator = CreateConsolidator(calendar, dataType, tickType);
            }
            Consolidator.DataConsolidated += ForwardConsolidatedBar;
        }

        /// <summary>
        /// Creates the inner consolidator that produces the requested <paramref name="dataType"/> output.
        /// </summary>
        protected virtual IDataConsolidator CreateConsolidator(Resolution resolution, Type dataType, TickType tickType)
        {
            if (dataType == typeof(Tick))
            {
                if (tickType == TickType.Trade)
                {
                    return resolution == Resolution.Daily
                        ? new TickConsolidator(DailyStrictEndTime)
                        : new TickConsolidator(Period);
                }
                return resolution == Resolution.Daily
                    ? new TickQuoteBarConsolidator(DailyStrictEndTime)
                    : new TickQuoteBarConsolidator(Period);
            }
            if (dataType == typeof(TradeBar))
            {
                return resolution == Resolution.Daily
                    ? new TradeBarConsolidator(DailyStrictEndTime)
                    : new TradeBarConsolidator(Period);
            }
            if (dataType == typeof(QuoteBar))
            {
                return resolution == Resolution.Daily
                    ? new QuoteBarConsolidator(DailyStrictEndTime)
                    : new QuoteBarConsolidator(Period);
            }
            throw new ArgumentNullException(nameof(dataType), $"{dataType.Name} not supported");
        }

        /// <summary>
        /// Creates the underlying calendar based consolidator for the given data type, used for arbitrary periods
        /// </summary>
        protected virtual IDataConsolidator CreateConsolidator(Func<DateTime, CalendarInfo> calendar, Type dataType, TickType tickType)
        {
            if (dataType == typeof(Tick))
            {
                return tickType == TickType.Trade
                    ? new TickConsolidator(calendar)
                    : new TickQuoteBarConsolidator(calendar);
            }
            if (dataType == typeof(TradeBar))
            {
                return new TradeBarConsolidator(calendar);
            }
            if (dataType == typeof(QuoteBar))
            {
                return new QuoteBarConsolidator(calendar);
            }
            throw new ArgumentNullException(nameof(dataType), $"{dataType.Name} not supported");
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            Initialize(data);

            // US equity hour data from the database starts at 9am but the exchange opens at 9:30am. Thus, we need to handle
            // this case specifically to avoid skipping the first hourly bar. To avoid this, we assert the period is daily,
            // the data resolution is hour and the exchange opens at any point in time over the data.Time to data.EndTime interval
            if (_extendedMarketHours ||
                ExchangeHours.IsOpen(data.Time, false) ||
                (Period == Time.OneDay && (data.EndTime - data.Time >= Time.OneHour) && ExchangeHours.IsOpen(data.Time, data.EndTime, false)))
            {
                Consolidator.Update(data);
            }
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="P:QuantConnect.Data.BaseData.Time" />)</param>
        public override void Scan(DateTime currentLocalTime)
        {
            Consolidator.Scan(currentLocalTime);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            Consolidator.DataConsolidated -= ForwardConsolidatedBar;
            Consolidator.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public override void Reset()
        {
            _useStrictEndTime = false;
            ExchangeHours = null;
            DataTimeZone = null;
            Consolidator.Reset();
            base.Reset();
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
            // strict end times describe a single daily bar, so periods larger than a day fall back to standard period consolidation
            if (!_useStrictEndTime || Period > Time.OneDay)
            {
                return new(Period > Time.OneDay ? dateTime : dateTime.RoundDown(Period), Period);
            }
            return LeanData.GetDailyCalendar(dateTime, ExchangeHours, _extendedMarketHours);
        }

        /// <summary>
        /// Determines a bar start time and period for intraday consolidation, anchored to the market open
        /// without extending past the market close so a bar never spans across closed market hours
        /// </summary>
        protected virtual CalendarInfo IntradayCalendar(DateTime dateTime)
        {
            if (ExchangeHours == null || ExchangeHours.IsMarketAlwaysOpen)
            {
                return new(dateTime.RoundDown(Period), Period);
            }
            return LeanData.GetIntradayCalendar(dateTime, Period, ExchangeHours, _extendedMarketHours);
        }

        /// <summary>
        /// Useful for testing
        /// </summary>
        protected virtual bool UseStrictEndTime(Symbol symbol)
        {
            return LeanData.UseStrictEndTime(_dailyStrictEndTimeEnabled, symbol, Period, ExchangeHours);
        }

        /// <summary>
        /// Will forward the underlying consolidated bar to consumers on this object.
        /// This wrapper keeps its own rolling window in addition to the inner consolidator's window.
        /// </summary>
        protected virtual void ForwardConsolidatedBar(object sender, IBaseData consolidated)
        {
            OnDataConsolidated(consolidated);
        }
    }
}
