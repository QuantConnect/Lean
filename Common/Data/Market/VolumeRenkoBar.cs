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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents a bar sectioned not by time, but by some amount of movement in volume
    /// </summary>
    public class VolumeRenkoBar : BaseData, IBaseDataBar
    {
        /// <summary>
        /// Gets the opening value that started this bar.
        /// </summary>
        public decimal Open { get; private set; }

        /// <summary>
        /// Gets the closing value or the current value if the bar has not yet closed.
        /// </summary>
        public decimal Close
        {
            get { return Value; }
            private set { Value = value; }
        }
        
        /// <summary>
        /// Gets the highest value encountered during this bar
        /// </summary>
        public decimal High { get; private set; }

        /// <summary>
        /// Gets the lowest value encountered during this bar
        /// </summary>
        public decimal Low { get; private set; }

        /// <summary>
        /// Gets the volume of trades during the bar.
        /// </summary>
        public decimal Volume { get; private set; }

        /// <summary>
        /// Gets the volume capacity of the bar
        /// </summary>
        public decimal BrickSize { get; private set; }

        /// <summary>
        /// Gets the end time of this renko bar or the most recent update time if it <see cref="IsClosed"/>
        /// </summary>
        public override DateTime EndTime { get; set; }
        
        /// <summary>
        /// Gets the time this bar started
        /// </summary>
        public DateTime Start
        {
            get { return Time; }
            private set { Time = value; }
        }

        /// <summary>
        /// Gets whether or not this bar is considered closed.
        /// </summary>
        public bool IsClosed => Volume >= BrickSize;

        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type { get; private set; }

        /// <summary>
        /// Updates this <see cref="VolumeRenkoBar"/> with the specified values and returns whether or not this bar is closed
        /// </summary>
        /// <param name="symbol">symbol of the data</param>
        /// <param name="start">The current data start time</param>
        /// <param name="endTime">The current data end time</param>
        /// <param name="brickSize">The preset volume capacity of this bar</param>
        /// <param name="open">The current data open value</param>
        /// <param name="high">The current data high value</param>
        /// <param name="low">The current data low value</param>
        /// <param name="close">The current data close value</param>
        /// <param name="volume">The current data volume</param>
        public VolumeRenkoBar(Symbol symbol, DateTime start, DateTime endTime, decimal brickSize, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            Type = RenkoType.Classic;

            Symbol = symbol;
            BrickSize = brickSize;
            Start = start;
            EndTime = endTime;
            Open = open;
            Close = close;
            Volume = volume;
            High = high;
            Low = low;
        }

        /// <summary>
        /// Updates this <see cref="VolumeRenkoBar"/> with the specified values and returns whether or not this bar is closed
        /// </summary>
        /// <param name="time">The current data end time</param>
        /// <param name="high">The current data high value</param>
        /// <param name="low">The current data low value</param>
        /// <param name="close">The current data close value</param>
        /// <param name="volume">The current data volume</param>
        /// <returns>The excess volume that the current bar cannot absorb</returns>
        public decimal Update(DateTime time, decimal high, decimal low, decimal close, decimal volume)
        {
            if (Type == RenkoType.Wicked)
                throw new InvalidOperationException("A \"Wicked\" RenkoBar cannot be updated!");

            // can't update a closed renko bar
            if (IsClosed) return 0m;
            if (Start == DateTime.MinValue) Start = time;
            EndTime = time;

            var excessVolume = Volume + volume - BrickSize;
            if (excessVolume > 0)
            {
                Volume = BrickSize;
            }
            else
            {
                Volume += volume;
            }

            Close = close;
            if (high > High) High = high;
            if (low < Low) Low = low;

            return excessVolume;
        }
    }
}