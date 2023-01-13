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
    public class VolumeRenkoBar : BaseRenkoBar
    {
        /// <summary>
        /// Gets whether or not this bar is considered closed.
        /// </summary>
        public override bool IsClosed => Volume >= BrickSize;

        /// <summary>
        /// Initializes a new default instance of the <see cref="RenkoBar"/> class.
        /// </summary>
        public VolumeRenkoBar()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeRenkoBar"/> class with the specified values
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
            BrickSize = brickSize;

            Symbol = symbol;
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
            // can't update a closed renko bar
            if (IsClosed) return 0m;
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

        /// <summary>
        /// Create a new <see cref="VolumeRenkoBar"/> with previous information rollover
        /// </summary>
        public VolumeRenkoBar Rollover()
        {
            return new VolumeRenkoBar
            {
                Type = Type,
                BrickSize = BrickSize,
                Symbol = Symbol,
                Open = Close,           // rollover open is the previous close
                High = High,
                Low = Low,
                Close = Close,
                Start = EndTime,        // rollover start time is the previous end time
                EndTime = EndTime,
                Volume = 0m
            };
        }
    }
}