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

        /// <summary>
        /// Opening price of the session
        /// </summary>
        public decimal Open => _consolidator.WorkingInstance.Open;

        /// <summary>
        /// High price of the session
        /// </summary>
        public decimal High => _consolidator.WorkingInstance.High;

        /// <summary>
        /// Low price of the session
        /// </summary>
        public decimal Low => _consolidator.WorkingInstance.Low;

        /// <summary>
        /// Closing price of the session
        /// </summary>
        public decimal Close => _consolidator.WorkingInstance.Close; 

        /// <summary>
        /// Volume traded during the session
        /// </summary>
        public decimal Volume => _consolidator.WorkingInstance.Volume;

        /// <summary>
        /// Open Interest of the session
        /// </summary>
        public decimal OpenInterest => _consolidator.WorkingInstance.OpenInterest;

        /// <summary>
        /// The symbol of the session
        /// </summary>
        public Symbol Symbol => _consolidator.WorkingInstance.Symbol;

        /// <summary>
        /// The end time of the session
        /// </summary>
        public DateTime EndTime => _consolidator.WorkingInstance.EndTime;

        /// <summary>
        ///  Initializes a new instance of the <see cref="Session"/> class
        /// </summary>
        /// <param name="tickType">The tick type to use</param>
        /// <param name="exchangeHours">The exchange hours</param>
        /// <param name="symbol">The symbol</param>
        public Session(TickType tickType, SecurityExchangeHours exchangeHours, Symbol symbol)
            : base(3)
        {
            _consolidator = new SessionConsolidator(exchangeHours, tickType, symbol);
            _consolidator.DataConsolidated += OnConsolidated;
            Add(_consolidator.WorkingInstance);
        }

        /// <summary>
        /// Updates the session with new market data and initializes the consolidator if needed
        /// </summary>
        /// <param name="data">The new data to update the session with</param>
        public void Update(BaseData data)
        {
            _consolidator.Update(data);
        }

        private void OnConsolidated(object sender, IBaseData consolidated)
        {
            // Finished current trading day
            // Add the new working session bar at [0], this will shift the previous trading day's bar to [1]
            Add(_consolidator.WorkingInstance);
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        public void Scan(DateTime currentLocalTime)
        {
            // Delegates the scan decision to the underlying consolidator.
            _consolidator.ValidateAndScan(currentLocalTime);
        }

        /// <summary>
        /// Resets the session
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _consolidator.Reset();
            // We need to add the working session bar at [0]
            Add(_consolidator.WorkingInstance);
        }

        /// <summary>
        /// Returns a string representation of current session bar with OHLCV and OpenInterest values formatted.
        /// Example: "O: 101.00 H: 112.00 L: 95.00 C: 110.00 V: 1005.00 OI: 12"
        /// </summary>
        public override string ToString()
        {
            return _consolidator.WorkingInstance.ToString();
        }
    }
}
