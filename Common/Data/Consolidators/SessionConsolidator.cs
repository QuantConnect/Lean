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
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;

namespace Common.Data.Consolidators
{
    /// <summary>
    /// Consolidates intraday market data into a single daily <see cref="SessionBar"/> (OHLCV + OpenInterest).
    /// </summary>
    public class SessionConsolidator : PeriodCountConsolidatorBase<BaseData, SessionBar>
    {
        // TODO on consolidation set to MIN
        private readonly SecurityExchangeHours _exchangeHours;
        private readonly Symbol _symbol;
        private readonly TickType _sourceTickType;

        internal SessionBar WorkingInstance
        {
            get
            {
                return _workingBar;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeHours"></param>
        /// <param name="sourceTickType"></param>
        /// <param name="symbol"></param>
        public SessionConsolidator(SecurityExchangeHours exchangeHours, TickType sourceTickType, Symbol symbol) : base(Time.OneDay)
        {
            _symbol = symbol;
            _exchangeHours = exchangeHours;
            _sourceTickType = sourceTickType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workingBar"></param>
        /// <param name="data"></param>
        protected override void AggregateBar(ref SessionBar workingBar, BaseData data)
        {
            if (workingBar == null)
            {
                workingBar = new SessionBar()
                {
                    Time = data.Time.Date,
                    Symbol = data.Symbol,
                    Period = TimeSpan.FromDays(1)
                };
            }

            if (data.DataType == MarketDataType.Tick && data is Tick oiTick && oiTick.TickType == TickType.OpenInterest)
            {
                // Handle open interest
                // Update the working session bar
                workingBar.OpenInterest = oiTick.Value;
                return;
            }

            if (!_exchangeHours.IsOpen(data.Time, data.EndTime, false))
            {
                return;
            }

            workingBar.Aggregate(_sourceTickType, data, Consolidated as SessionBar);
        }

        /// <summary>
        /// Validates the current local time and triggers Scan() if a new day is detected.
        /// </summary>
        /// <param name="currentLocalTime">The current local time.</param>
        public void ValidateAndScan(DateTime currentLocalTime)
        {
            // Trigger Scan() when a new day is detected
            var currentTime = Globals.LiveMode ? currentLocalTime.RoundDown(Time.OneSecond) : currentLocalTime;
            if (currentTime.Date != WorkingInstance?.Time.Date)
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
        }
    }
}
