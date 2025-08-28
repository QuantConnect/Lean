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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Common.Data.Consolidators;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Rolling window of <see cref="SessionBar"/> that represents a trading session
    /// with consolidated OHLCV and open interest data.
    /// </summary>
    public class Session : RollingWindow<SessionBar>
    {
        private readonly List<TickType> _supportedTickTypes;
        private SessionConsolidator _consolidator;

        /// <summary>
        /// True if we have at least one trading day data
        /// </summary>
        public bool IsTradingDayDataReady => Count > 1;

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
        /// Open Interest of the session
        /// </summary>
        public decimal OpenInterest => GetValue(x => x.OpenInterest);

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
                        CreateConsolidator(typeof(Tick), tick.TickType);
                        break;

                    case TradeBar:
                        // Initialize consolidator for trade bars
                        CreateConsolidator(typeof(TradeBar));
                        break;

                    case QuoteBar:
                        // Initialize consolidator for quote bars
                        CreateConsolidator(typeof(QuoteBar));
                        break;
                }
            }
            _consolidator?.Update(data);

            // Keep the current OHLCV bar at index [0]
            this[0] = _consolidator.WorkingData;
        }

        private void CreateConsolidator(Type dataType, TickType? tickType = null)
        {
            _consolidator = new SessionConsolidator(dataType, tickType ?? TickType.Trade);
            _consolidator.DataConsolidated += OnConsolidated;
        }

        private void OnConsolidated(object sender, SessionBar consolidated)
        {
            // Store consolidated OHLCV at [1] as the previous day
            this[1] = consolidated;
        }

        private decimal GetValue(Func<SessionBar, decimal> selector)
        {
            return this[0] != null ? selector(this[0]) : 0;
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        public void Scan(DateTime currentLocalTime, bool isEventTime = false)
        {
            // Delegates the scan decision to the underlying consolidator.
            // When isEventTime = true, it means the call comes from a time update (not market data).
            _consolidator?.ValidateAndScan(currentLocalTime, isEventTime);
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Add(null);
            _consolidator?.Reset();
        }
    }

    /// <summary>
    /// Contains OHLCV data for a single session
    /// </summary>
    public class SessionBar : Bar
    {
        /// <summary>
        /// Current time marker.
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Volume:
        /// </summary>
        public decimal Volume { get; }

        /// <summary>
        /// Open Interest:
        /// </summary>
        public decimal OpenInterest { get; }

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
