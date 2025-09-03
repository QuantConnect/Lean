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
using QuantConnect.Data.Common;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using Common.Data.Market;

namespace Common.Data.Consolidators
{
    /// <summary>
    /// Consolidates intraday market data into a single daily <see cref="SessionBar"/> (OHLCV + OpenInterest).
    /// </summary>
    internal class SessionConsolidator : MarketHourAwareConsolidator
    {
        private Resolution? _resolution;
        private readonly TickType _tickType;
        private decimal _openInterest;
        private decimal _volume;
        private SessionBar _workingSessionBar;
        private SessionBar _consolidatedSessionBar;

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType => typeof(SessionBar);

        /// <summary>
        /// Gets the most recently consolidated piece of data
        /// </summary>
        public override SessionBar Consolidated => _consolidatedSessionBar;

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override SessionBar WorkingData => _workingSessionBar;


        /// <summary>
        /// Initializes a new instance of the <see cref="SessionConsolidator"/> class
        /// </summary>
        /// <param name="dataType">The target data type</param>
        /// <param name="tickType">The target tick type</param>
        public SessionConsolidator(Type dataType, TickType tickType)
            : base(false, Resolution.Daily, dataType, tickType, false)
        {
            _tickType = tickType;
            _workingSessionBar = new SessionBar();
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(IBaseData data)
        {
            Initialize(data);

            if (data is Tick oiTick && oiTick.TickType == TickType.OpenInterest)
            {
                // Handle open interest
                _openInterest = oiTick.Value;
                // Update the working session bar
                _workingSessionBar.Update(_volume, _openInterest);
            }
            else if (_tickType != TickType.Trade && IsWithinMarketHours(data))
            {
                // Handle volume during market hours
                Resolution? currentResolution = null;
                var volumeToAdd = 0m;

                if (data.DataType == MarketDataType.TradeBar && data is TradeBar tradeBar)
                {
                    currentResolution = tradeBar.Period.ToHigherResolutionEquivalent(false);
                    volumeToAdd = tradeBar.Volume;
                }
                else if (data.DataType == MarketDataType.Tick && data is Tick tick && tick.TickType == TickType.Trade)
                {
                    currentResolution = Resolution.Tick;
                    volumeToAdd = tick.Quantity;
                }

                // Process only if data is a TradeBar or Tick(Trade)
                if (currentResolution.HasValue)
                {
                    if (_resolution == null)
                    {
                        // Set resolution from the first received data
                        _resolution = currentResolution.Value;
                    }
                    if (_resolution == currentResolution.Value)
                    {
                        // Only process data with the same resolution as the first received
                        _volume += volumeToAdd;
                        // Update the working session bar
                        _workingSessionBar.Update(_volume, _openInterest);
                    }
                }
            }

            // Update consolidator if we can feed it with the data
            if (InputType.IsAssignableFrom(data.GetType()))
            {
                base.Update(data);
                // Update the working session bar
                _workingSessionBar.Update(base.WorkingData);
            }

            // Scan after updating
            ValidateAndScan(data.EndTime);
        }

        /// <summary>
        /// Validates the current local time and triggers Scan() either for market data updates or at midnight for time events.
        /// </summary>
        /// <param name="currentLocalTime">The current local time.</param>
        /// <param name="isEventTime">Indicates if the call comes from a event (OnTimeUpdated) rather than market data.</param>
        public void ValidateAndScan(DateTime currentLocalTime, bool isEventTime = false)
        {
            // Scan() is triggered in two cases:
            //  1. Update() -> during market hours, after each new data point is processed
            //                 (IsEventTime = false) that means always Scan
            //  2. OnTimeUpdated() -> Scan only at the end of the day (midnight)

            var currentTime = Globals.LiveMode ? currentLocalTime.RoundDown(Time.OneSecond) : currentLocalTime;
            if (!isEventTime || (currentTime.TimeOfDay == TimeSpan.Zero))
            {
                Scan(currentLocalTime);
            }
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _volume = 0;
            _openInterest = 0;
            _workingSessionBar = new SessionBar();
            _consolidatedSessionBar = null;
        }

        /// <summary>
        /// Will forward the underlying consolidated bar to consumers on this object
        /// </summary>
        protected override void ForwardConsolidatedBar(object sender, IBaseData consolidated)
        {
            _consolidatedSessionBar = CreateSessionBar(consolidated);
            base.ForwardConsolidatedBar(this, _consolidatedSessionBar);

            // Reset working session bar, volume and open interest
            _workingSessionBar = new SessionBar();
            _volume = 0;
            _openInterest = 0;
        }

        /// <summary>
        /// Creates a session bar from the specified data
        /// </summary>
        private SessionBar CreateSessionBar(IBaseData baseData)
        {
            return baseData switch
            {
                TradeBar t => new SessionBar(t.EndTime, t.Open, t.High, t.Low, t.Close, t.Volume, _openInterest),
                QuoteBar q => new SessionBar(q.EndTime, q.Open, q.High, q.Low, q.Close, _volume, _openInterest),
                _ => new SessionBar(DateTime.MinValue, 0, 0, 0, 0, _volume, _openInterest)
            };
        }
    }
}