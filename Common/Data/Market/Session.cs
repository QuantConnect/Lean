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
        private IAlgorithmSettings _algorithmSettings;
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
            _algorithmSettings = algorithmSettings ?? new AlgorithmSettings();
            _supportedTickTypes = tickTypes.ToList();
            // This will be our working data, we'll keep updating it until a bar is consolidated
            Add(null);
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

            // Update the current session bar with the working data
            var workingData = _consolidator?.WorkingData;
            SessionBar sessionBar = null;
            if (workingData is TradeBar workingTradeBar)
            {
                sessionBar = new SessionBar(workingTradeBar.EndTime, workingTradeBar.Open, workingTradeBar.High, workingTradeBar.Low, workingTradeBar.Close, workingTradeBar.Volume, _consolidator.OpenInterest);
            }
            else if (workingData is QuoteBar workingQuoteBar)
            {
                sessionBar = new SessionBar(workingQuoteBar.EndTime, workingQuoteBar.Open, workingQuoteBar.High, workingQuoteBar.Low, workingQuoteBar.Close, _consolidator.Volume, _consolidator.OpenInterest);
            }

            this[0] = sessionBar;
        }

        private void CreateConsolidator(Type dataType, TickType? tickType = null)
        {
            _consolidator = new SessionConsolidator(_algorithmSettings.DailyPreciseEndTime, dataType, tickType ?? TickType.Trade);
            _consolidator.DataConsolidated += OnConsolidated;
        }

        private void OnConsolidated(object sender, IBaseData consolidated)
        {
            // Convert consolidated data into a SessionBar
            var sessionBar = consolidated switch
            {
                TradeBar t => new SessionBar(t.EndTime, t.Open, t.High, t.Low, t.Close, t.Volume, _consolidator.OpenInterest),
                QuoteBar q => new SessionBar(q.EndTime, q.Open, q.High, q.Low, q.Close, _consolidator.Volume, _consolidator.OpenInterest),
                _ => null
            };

            // Update the current session bar with the consolidated data
            this[0] = sessionBar;

            // Reset temporary volume and open interest in consolidator
            _consolidator.OpenInterest = 0;
            _consolidator.Volume = 0;

            // This will move the consolidated bar to the next index
            Add(null);
            // Now the current is at index 0 -> null
            // Previous is at index 1 -> consolidated
        }

        private decimal GetValue(Func<SessionBar, decimal> selector)
        {
            // If the latest bar is null:
            // - return 0 if no data was consolidated
            // - otherwise use the previous bar
            if (this[0] == null)
            {
                return (Count == 1) ? 0 : selector(this[1]);
            }

            // Otherwise, use the latest bar
            return selector(this[0]);
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
    public class SessionBar : IBar
    {
        /// <summary>
        /// Current time marker.
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; }

        /// <summary>
        /// High price of the bar during the time period.
        /// </summary>
        public decimal High { get; }

        /// <summary>
        /// Low price of the bar during the time period.
        /// </summary>
        public decimal Low { get; }

        /// <summary>
        /// Closing price of the bar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close { get; }

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
