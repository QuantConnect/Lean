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
        private readonly SecurityExchangeHours _exchangeHours;
        private readonly TickType _sourceTickType;
        private readonly Symbol _symbol;
        private bool _initialized;
        internal SessionBar WorkingInstance
        {
            get
            {
                if (_workingBar == null)
                {
                    InitializeWorkingBar();
                }
                return _workingBar;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionConsolidator"/> class.
        /// </summary>
        /// <param name="exchangeHours">The exchange hours</param>
        /// <param name="sourceTickType">Type of the source tick</param>
        /// <param name="symbol">The symbol</param>
        public SessionConsolidator(SecurityExchangeHours exchangeHours, TickType sourceTickType, Symbol symbol) : base(Time.OneDay)
        {
            _symbol = symbol;
            _exchangeHours = exchangeHours;
            _sourceTickType = sourceTickType;
            InitializeWorkingBar();
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref SessionBar workingBar, BaseData data)
        {
            if (!_initialized)
            {
                workingBar.Time = data.Time.Date;
                workingBar.Period = TimeSpan.FromDays(1);
                _initialized = true;
            }

            // Handle open interest
            if (data.DataType == MarketDataType.Tick && data is Tick oiTick && oiTick.TickType == TickType.OpenInterest)
            {
                // Update the working session bar with the latest open interest
                workingBar.OpenInterest = oiTick.Value;
                return;
            }

            if (!_exchangeHours.IsOpen(data.Time, data.EndTime, false))
            {
                return;
            }

            // Update the working session bar
            workingBar.Update(data, Consolidated);
        }

        /// <summary>
        /// Validates the current local time and triggers Scan() if a new day is detected.
        /// </summary>
        /// <param name="currentLocalTime">The current local time.</param>
        public void ValidateAndScan(DateTime currentLocalTime)
        {
            if (!_initialized)
            {
                return;
            }
            if (currentLocalTime.Date != WorkingInstance?.Time.Date)
            {
                Scan(currentLocalTime);
            }
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        //public event DataConsolidatedHandler DataConsolidated;

        protected override void OnDataConsolidated(SessionBar e)
        {
            _workingBar = null;
            base.OnDataConsolidated(e);
        }

        /// <summary>
        /// Resets the working bar
        /// </summary>
        protected override void ResetWorkingBar()
        {
        }

        /// <summary>
        /// Resets the consolidator
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            InitializeWorkingBar();
        }

        private void InitializeWorkingBar()
        {
            _workingBar = new SessionBar(_sourceTickType)
            {
                Time = DateTime.MaxValue,
                Symbol = _symbol
            };
            _initialized = false;
        }
    }
}
