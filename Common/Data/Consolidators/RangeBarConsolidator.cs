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
    public class RangeBarConsolidator : DataConsolidator<Tick>
    {
        private RangeBar _currentBar;
        private readonly int _length;

        /// <summary>
        /// Creates a new instance of <see cref="RangeBarConsolidator"/> of given length (pips).
        /// </summary>
        /// <param name="length"></param>
        public RangeBarConsolidator(int length)
        {
            _length = length;
        }

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

            // Init new range bar from closed bar last tick
            var closedBarLastTick = _currentBar.LastTick;
            _currentBar = new RangeBar(closedBarLastTick, _length);

            // Push new tick
            // If unsuccessful, example: price difference between last tick and current tick is more then range-bar length,
            // then create a new range bar from current data (tick)
            var isOutOfLengthScope = _currentBar.Update(data);
            if (isOutOfLengthScope)
            {
                _currentBar = new RangeBar(data, _length);
            }
        }

        public override void Scan(DateTime currentLocalTime)
        {
        }
    }
}
