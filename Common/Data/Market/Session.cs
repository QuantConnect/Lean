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
using QuantConnect.Securities;
using Common.Data.Consolidators;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Provides a rolling window of <see cref="SessionBar"/> with size 2,
    /// where [0] contains the current session values in progress (OHLCV + OpenInterest),
    /// and [1] contains the fully consolidated data of the previous trading day.
    /// </summary>
    public class Session : RollingWindow<SessionBar>, IBar
    {
        private readonly SessionConsolidator _consolidator;
        private SessionBar _holder;
        private bool _initialized;

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
        ///  Initializes a new instance of the <see cref="Session"/> class
        /// </summary
        /// <param name="tickType">The tick type to use</param>
        /// <param name="exchangeHours"></param>
        /// <param name="symbol"></param>
        public Session(TickType tickType, SecurityExchangeHours exchangeHours, Symbol symbol)
            : base(3)
        {
            _consolidator = new SessionConsolidator(exchangeHours, tickType);
            _consolidator.DataConsolidated += OnConsolidated;
            _holder = new SessionBar();
            Add(_holder);
        }

        /// <summary>
        /// Updates the session with new market data and initializes the consolidator if needed
        /// </summary>
        /// <param name="data">The new data to update the session with</param>
        public void Update(BaseData data)
        {
            _consolidator.Update(data);

            if (_consolidator.WorkingInstance != null && !_initialized)
            {
                this[0] = _consolidator.WorkingInstance;
                _initialized = true;
            }
        }

        private void OnConsolidated(object sender, IBaseData consolidated)
        {
            // Finished current trading day
            // Add the new working session bar at [0], this will shift the previous trading day's bar to [1]
            _holder = new SessionBar();
            _initialized = false;
            Add(_holder);
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        public void Scan(DateTime currentLocalTime)
        {
            // Delegates the scan decision to the underlying consolidator.
            _consolidator?.ValidateAndScan(currentLocalTime);
        }

        private decimal GetValue(Func<SessionBar, decimal> selector)
        {
            return _consolidator?.WorkingInstance != null ? selector(_consolidator.WorkingInstance) : 0;
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _consolidator?.Reset();
            if (_consolidator != null)
            {
                // We need to add the working session bar at [0]
                Add(_consolidator.WorkingInstance);
            }
            _initialized = false;
        }

        /// <summary>
        /// Returns a string representation of current session bar with OHLCV and OpenInterest values formatted.
        /// Example: "O: 101.00 H: 112.00 L: 95.00 C: 110.00 V: 1005.00 OI: 12"
        /// </summary>
        public override string ToString()
        {
            if (_consolidator?.WorkingData != null)
            {
                return _consolidator.WorkingData.ToString();
            }
            return string.Empty;
        }
    }
}
