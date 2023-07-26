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
    /// Represents a bar sectioned not by time, but by some amount of movement in a value (for example, Closing price moving in $10 bar sizes)
    /// </summary>
    public class RangeBar: TradeBar
    {
        /// <summary>
        /// Gets the range of the bar.
        /// </summary>
        public decimal RangeSize { get; private set; }

        /// <summary>
        /// Gets whether or not this bar is considered closed.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Initialize a new default instance of <see cref="RangeBar"/> class.
        /// </summary>
        public RangeBar()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBar"/> class with the specified values
        /// </summary>
        /// <param name="symbol">The symbol of this data</param>
        /// <param name="endTime">The end time of the bar</param>
        /// <param name="rangeSize">The size of each range bar</param>
        /// <param name="open">The opening price for the new bar</param>
        /// <param name="high">The high price for the new bar</param>
        /// <param name="low">The low price for the new bar</param>
        /// <param name="close">The closing price for the new bar</param>
        /// <param name="volume">The volume value for the new bar</param>
        public RangeBar(Symbol symbol, DateTime endTime,
            decimal rangeSize, decimal open, decimal? high = null, decimal? low = null, decimal? close = null, decimal volume = 0)
        {
            Symbol = symbol;
            EndTime = endTime;
            RangeSize = rangeSize;
            Open = open;
            Close = close ?? open;
            Volume = volume;
            High = high ?? open;
            Low = low ?? open;
        }

        /// <summary>
        /// Updates this <see cref="RangeBar"/> with the specified values
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="currentValue">The current value</param>
        /// <param name="volumeSinceLastUpdate">The volume since the last update called on this instance</param>
        public void Update(DateTime time, decimal currentValue, decimal volumeSinceLastUpdate)
        {
            EndTime = time;

            if (currentValue < Low)
            {
                if ((High - currentValue) > RangeSize)
                {
                    IsClosed = true;
                    Low = High - RangeSize;
                    Close = Low;
                    return;
                }
                else
                {
                    Low = currentValue;
                }
            } 
            else if (currentValue > High)
            {
                if ((currentValue - Low) > RangeSize)
                {
                    IsClosed = true;
                    High = Low + RangeSize;
                    Close = High;
                    return;
                }
                else
                {
                    High = currentValue;
                }
            }

            Volume += volumeSinceLastUpdate;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new RangeBar
            {
                RangeSize = RangeSize,
                Open = Open,
                Volume = Volume,
                Close = Close,
                EndTime = EndTime,
                High = High,
                IsClosed = IsClosed,
                Low = Low,
                Time = Time,
                Value = Value,
                Symbol = Symbol,
                DataType = DataType
            };
        }
    }
}
