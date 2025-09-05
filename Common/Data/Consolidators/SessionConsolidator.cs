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
            _workingSessionBar.Symbol = data.Symbol;

            Initialize(data);

            if (data is Tick oiTick && oiTick.TickType == TickType.OpenInterest)
            {
                // Handle open interest
                // Update the working session bar
                _workingSessionBar.OpenInterest = oiTick.Value;
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
                        // Update the working session bar
                        _workingSessionBar.Volume += volumeToAdd;
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
        }

        /// <summary>
        /// Validates the current local time and triggers Scan() if a new day is detected.
        /// </summary>
        /// <param name="currentLocalTime">The current local time.</param>
        public void ValidateAndScan(DateTime currentLocalTime)
        {
            // Trigger Scan() when a new day is detected
            var currentTime = Globals.LiveMode ? currentLocalTime.RoundDown(Time.OneSecond) : currentLocalTime;
            if (currentTime.Date != WorkingData?.Time.Date)
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
            _workingSessionBar = new SessionBar();
            _consolidatedSessionBar = null;
            _resolution = null;
        }

        /// <summary>
        /// Will forward the underlying consolidated bar to consumers on this object
        /// </summary>
        protected override void ForwardConsolidatedBar(object sender, IBaseData consolidated)
        {
            // Create the consolidated session bar
            _consolidatedSessionBar = CreateSessionBar(consolidated);

            // Reset working session bar
            _workingSessionBar = new SessionBar();

            // Forward the consolidated session bar to consumers
            base.ForwardConsolidatedBar(this, _consolidatedSessionBar);
        }

        /// <summary>
        /// Creates a session bar from the specified data
        /// </summary>
        private SessionBar CreateSessionBar(IBaseData baseData)
        {
            var openInterest = _workingSessionBar.OpenInterest;
            var volume = _workingSessionBar.Volume;
            var symbol = _workingSessionBar.Symbol;
            return baseData switch
            {
                TradeBar t => new SessionBar(t.EndTime, symbol, t.Open, t.High, t.Low, t.Close, t.Volume, openInterest),
                QuoteBar q => new SessionBar(q.EndTime, symbol, q.Open, q.High, q.Low, q.Close, volume, openInterest),

                // Fallback: in some cases, only Volume and Open Interest are available while the underlying consolidator's workingData is null.
                // OHLC values are set to 0 in this scenario.
                _ => new SessionBar(_workingSessionBar.EndTime, symbol, 0, 0, 0, 0, volume, openInterest)
            };
        }
    }
}
