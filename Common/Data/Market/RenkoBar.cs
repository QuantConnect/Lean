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
    public class RenkoBar : BaseData
    {
        /// <summary>
        /// Gets the height of the bar (BarHigh - BarLow)
        /// </summary>
        public decimal BrickSize { get; private set; }

        /// <summary>
        /// Gets the percentage required for the value to move beyond the high or low end of a bar to create a new one
        /// </summary>
        public decimal Overlap { get; private set; }

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
        /// Gets the volume of trades during the bar.
        /// </summary>
        public long Volume { get; private set; }

        /// <summary>
        /// Gets the end time of this renko bar or the most recent update time if it <see cref="IsClosed"/>
        /// </summary>
        public DateTime End { get; private set; }

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
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Initializes a new default instance of the <see cref="RenkoBar"/> class.
        /// </summary>
        public RenkoBar()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoBar"/> class with the specified values
        /// </summary>
        /// <param name="symbol">The symbol of this data</param>
        /// <param name="time">The start time of the bar</param>
        /// <param name="brickSize">The size of each renko brick</param>
        /// <param name="open">The opening price for the new bar</param>
        /// <param name="volume">Any initial volume associated with the data</param>
        /// <param name="overlap">The percentage the value must move beyond the high/low to start a new bar</param>
        public RenkoBar(string symbol, DateTime time, decimal brickSize, decimal open, long volume, decimal overlap = 1m)
        {
            Symbol = symbol;
            Start = time;
            End = time;
            BrickSize = brickSize;
            Open = open;
            Close = open;
            Volume = volume;
            Overlap = overlap;
        }

        /// <summary>
        /// Updates this <see cref="RenkoBar"/> with the specified values and returns whether or not this bar is closed
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="currentValue">The current value</param>
        /// <param name="volumeSinceLastUpdate">The volume since the last update called on this instance</param>
        /// <returns>True if this bar <see cref="IsClosed"/></returns>
        public bool Update(DateTime time, decimal currentValue, long volumeSinceLastUpdate)
        {
            // can't update a closed renko bar
            if (IsClosed) return true;
            if (Start == DateTime.MinValue) Start = time;
            End = time;

            decimal lowClose = Open - BrickSize * Overlap;
            decimal highClose = Open + BrickSize * Overlap;

            Close = Math.Min(highClose, Math.Max(lowClose, currentValue));
            Volume += volumeSinceLastUpdate;

            if (currentValue < lowClose  || currentValue > highClose)
            {
                IsClosed = true;
            }

            return IsClosed;
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException("RenkoBar does not support the Reader function. This function should never be called on this type.");
        }

        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException("RenkoBar does not support the GetSource function. This function should never be called on this type.");
        }
    }
}