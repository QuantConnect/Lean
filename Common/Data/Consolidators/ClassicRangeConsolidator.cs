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
using QuantConnect.Data.Market;
using Python.Runtime;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// This consolidator can transform a stream of <see cref="IBaseData"/> instances into a stream of <see cref="RangeBar"/>.
    /// The difference between this consolidator and <see cref="RangeConsolidator"/>, is that this last one creates intermediate/
    /// phantom RangeBar's (RangeBar's with zero volume) if the price rises up or falls down by above/below two times the range 
    /// size. Therefore, <see cref="RangeConsolidator"/> leaves no space between two adyacent RangeBar's since it always start 
    /// a new RangeBar one range above the last RangeBar's High value or one range below the last RangeBar's Low value, where 
    /// one range equals to one minimum price change.
    /// </summary>
    public class ClassicRangeConsolidator : RangeConsolidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicRangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The Range interval sets the range in which the price moves, which in turn initiates the formation of a new bar.
        /// One range equals to one minimum price change, where this last value is defined depending of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar, except if the input is a TradeBar.</param>
        public ClassicRangeConsolidator(
            int range,
            Func<IBaseData, decimal> selector = null,
            Func<IBaseData, decimal> volumeSelector = null)
            : base(range, selector, volumeSelector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeConsolidator" /> class.
        /// </summary>
        /// <param name="range">The Range interval sets the range in which the price moves, which in turn initiates the formation of a new bar.
        /// One range equals to one minimum price change, where this last value is defined depending of the RangeBar's symbol</param>
        /// <param name="selector">Extracts the value from a data instance to be formed into a <see cref="RangeBar"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        public ClassicRangeConsolidator(int range,
            PyObject selector,
            PyObject volumeSelector = null)
            : base(range, selector, volumeSelector)
        {
        }

        /// <summary>
        /// Updates the current RangeBar being created with the given data.
        /// Additionally, if it's the case, it consolidates the current RangeBar
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected override void UpdateBar(DateTime time, decimal currentValue, decimal volume)
        {
            CurrentBar.Update(time, currentValue, volume);

            if (CurrentBar.IsClosed)
            {
                OnDataConsolidated(CurrentBar);
                CurrentBar = null;
            }
        }
    }
}
