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

namespace Common.Data.Consolidators
{
    internal class SessionConsolidator : MarketHourAwareConsolidator
    {
        private Resolution? _resolution;
        private readonly TickType _tickType;

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public decimal OpenInterest { get; set; }

        /// <summary>
        /// Gets the volume
        /// </summary>
        public decimal Volume { get; set; }

        public SessionConsolidator(Type dataType, TickType tickType)
            : base(false, Resolution.Daily, dataType, tickType, false)
        {
            _tickType = tickType;
        }

        public override void Update(IBaseData data)
        {
            // If a new bar was consolidated, reset volume and open interest before processing new data
            if (WorkingData == null && Consolidated != null)
            {
                ResetAfterConsolidation();
            }

            Initialize(data);

            // Handle open interest and ticks manually
            if (data is Tick oiTick && oiTick.TickType == TickType.OpenInterest)
            {
                OpenInterest = oiTick.Value;
            }

            // Handle volume manually for quotes during market hours
            if (_tickType != TickType.Trade && IsWithinMarketHours(data))
            {
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
                        Volume += volumeToAdd;
                    }
                }
            }

            // Update consolidator if we can feed it with the data
            if (InputType.IsAssignableFrom(data.GetType()))
            {
                base.Update(data);
            }

            // Scan after updating
            ValidateAndScan(data.EndTime);
        }

        protected virtual void ResetAfterConsolidation()
        {
            Volume = 0;
            OpenInterest = 0;
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
    }
}