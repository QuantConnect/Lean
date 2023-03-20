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

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Data.Consolidators 
{
    /// <summary>
    /// Implementation of a range bar consolidator.
    /// Ref 1: https://help.quantower.com/quantower/analytics-panels/chart/chart-types/range-bars
    /// Ref 2: https://help.cqg.com/cqgic/20/default.htm#!Documents/rangebarrb.htm
    /// </summary>
    public class RangeBarConsolidator : DataConsolidator<Tick>
    {
        private RangeBar _currentBar;
        private readonly decimal _length;

        /// <summary>
        /// Creates a new instance of <see cref="RangeBarConsolidator"/> of given price length.
        /// </summary>
        /// <param name="length">Price length</param>
        public RangeBarConsolidator(decimal length)
        {
            _length = length;
        }

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public new event EventHandler<RangeBar> DataConsolidated;

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        public override Type OutputType => typeof(RangeBar);

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public override IBaseData WorkingData => _currentBar;

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(Tick data)
        {
            if (data.TickType != TickType.Trade) 
            {
                return;
            }

            _currentBar ??= new RangeBar(data, _length);
            
            var toBeClosed = _currentBar.Update(data);
            if (!toBeClosed)
            {
                return;
            }

            OnDataConsolidated(_currentBar);
            
            _currentBar = new RangeBar(data, _length);
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public override void Scan(DateTime currentLocalTime)
        {
        }

        /// <summary>
        /// Event invocator for the DataConsolidated event. This should be invoked
        /// by derived classes when they have consolidated a new piece of data.
        /// </summary>
        /// <param name="consolidated">The newly consolidated data</param>
        protected void OnDataConsolidated(RangeBar consolidated) 
        {
            base.OnDataConsolidated(consolidated);
            DataConsolidated?.Invoke(this, consolidated);
        }
    }
}
