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
using QuantConnect.Indicators;
using QuantConnect.Data.Common;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Rolling window of <see cref="SessionBar"/> that represents a trading session
    /// with consolidated OHLCV and open interest data.
    /// </summary>
    public class Session : RollingWindow<SessionBar>
    {
        private readonly List<TickType> _supportedTickTypes;
        private IAlgorithmSettings _algorithmSettings;
        private SessionConsolidator _consolidator;

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => Count > 0;

        /// <summary>
        /// Opening price of the session
        /// </summary>
        public decimal Open => GetValue(x => x.Open);

        /// <summary>
        /// High price of the session
        /// </summary>
        public decimal High => GetValue(x => x.High);

        /// <summary>
        /// Low price of the session
        /// </summary>
        public decimal Low => GetValue(x => x.Low);

        /// <summary>
        /// Closing price of the session
        /// </summary>
        public decimal Close => GetValue(x => x.Close);

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        public decimal Volume => GetValue(x => x.Volume);

        /// <summary>
        /// Gets the time of the bar
        /// </summary>
        public DateTime Time => this[0].Time;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickTypes">The tick types to use</param>
        /// <param name="algorithmSettings">The algorithm settings</param>
        public Session(IEnumerable<TickType> tickTypes, IAlgorithmSettings algorithmSettings = null) : base(2)
        {
            _algorithmSettings = algorithmSettings ?? new AlgorithmSettings();
            _supportedTickTypes = tickTypes.ToList();
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickType">The tick type to use</param>
        /// <param name="algorithmSettings">The algorithm settings</param>
        public Session(TickType tickType, IAlgorithmSettings algorithmSettings = null)
            : this([tickType], algorithmSettings)
        {
        }

        /// <summary>
        /// Updates the session with new market data and initializes the consolidator if needed
        /// </summary>
        public void Update(BaseData data)
        {
            if (_consolidator == null)
            {
                switch (data)
                {
                    case Tick tick:
                        if (!_supportedTickTypes.Contains(tick.TickType))
                        {
                            // Skip if tick type not supported
                            return;
                        }
                        // Initialize consolidator for ticks
                        CreateConsolidator(Resolution.Tick, typeof(Tick), tick.TickType);
                        break;

                    case TradeBar tradeBar:
                        // Initialize consolidator for trade bars
                        CreateConsolidator(tradeBar.Period.ToHigherResolutionEquivalent(false), typeof(TradeBar));
                        break;

                    case QuoteBar quoteBar:
                        // Initialize consolidator for quote bars
                        CreateConsolidator(quoteBar.Period.ToHigherResolutionEquivalent(false), typeof(QuoteBar));
                        break;
                }
            }
            _consolidator?.Update(data);
        }

        private void CreateConsolidator(Resolution resolution, Type dataType, TickType? tickType = null)
        {
            _consolidator = new SessionConsolidator(_algorithmSettings.DailyPreciseEndTime, resolution, dataType, tickType ?? TickType.Trade);
            _consolidator.DataConsolidated += OnConsolidated;
        }

        private void OnConsolidated(object sender, IBaseData consolidated)
        {
            // Convert consolidated data into a SessionBar
            SessionBar sessionBar = consolidated switch
            {
                TradeBar t => new SessionBar(t.EndTime, t.Open, t.High, t.Low, t.Close, t.Volume, _consolidator.OpenInterest),
                QuoteBar q => new SessionBar(q.EndTime, q.Open, q.High, q.Low, q.Close, _consolidator.Volume, _consolidator.OpenInterest),
                _ => null
            };

            // Reset temporary volume and open interest in consolidator
            _consolidator.OpenInterest = 0;
            _consolidator.Volume = 0;

            if (sessionBar != null)
            {
                // Add to rolling window
                Add(sessionBar);
            }
        }

        private decimal GetValue(Func<SessionBar, decimal> selector)
        {
            // Return value from the most recent session bar or 0 if empty
            if (Count > 0)
            {
                return selector(this[0]);
            }
            return 0;
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _consolidator?.Reset();
        }
    }

    internal class SessionConsolidator : MarketHourAwareConsolidator
    {
        private Resolution _resolution;
        private readonly TickType _tickType;

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public decimal OpenInterest { get; set; }

        /// <summary>
        /// Gets the volume
        /// </summary>
        public decimal Volume { get; set; }

        public SessionConsolidator(bool dailyStrictEndTimeEnabled, Resolution resolution, Type dataType, TickType tickType)
            : base(dailyStrictEndTimeEnabled, Resolution.Daily, dataType, tickType, false)
        {
            _tickType = tickType;
            _resolution = resolution;
        }

        public override void Update(IBaseData data)
        {
            // Handle open interest and ticks manually
            if (data.DataType == MarketDataType.Tick)
            {
                if (data is OpenInterest openInterest)
                {
                    OpenInterest = openInterest.Value;
                }
                else if (data is Tick tick)
                {
                    OpenInterest = tick.Value;
                }
            }

            // Handle volume manually for quotes during market hours
            if (_tickType != TickType.Trade && IsWithinMarketHours(data))
            {
                if (data.DataType == MarketDataType.TradeBar && data is TradeBar tradeBar)
                {
                    var period = tradeBar.Period;
                    // Only add volume if resolution matches session to avoid double counting
                    if (period == _resolution.ToTimeSpan())
                    {
                        Volume += tradeBar.Volume;
                    }
                }
                else if (data.DataType == MarketDataType.Tick && data is Tick tick && tick.TickType == TickType.Trade)
                {
                    Volume += tick.Quantity;
                }
            }

            // Update consolidator if we can feed it with the data
            if (InputType.IsAssignableFrom(data.GetType()))
            {
                base.Update(data);
            }
        }
    }

    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    public class SessionBar : IBar
    {
        /// <summary>
        /// Current time marker.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; private set; }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        public decimal High { get; private set; }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        public decimal Low { get; private set; }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close { get; private set; }

        /// <summary>
        /// Volume:
        /// </summary>
        public decimal Volume { get; private set; }

        /// <summary>
        /// Open Interest:
        /// </summary>
        public decimal OpenInterest { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionBar"/> class
        /// </summary>
        public SessionBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal openInterest)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            OpenInterest = openInterest;
        }
    }
}
