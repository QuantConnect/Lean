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
    public class RenkoBar : BaseData, IBaseDataBar
    {
        /// <summary>
        /// Gets the kind of the bar
        /// </summary>
        public RenkoType Type { get; private set; }

        /// <summary>
        /// Gets the height of the bar
        /// </summary>
        public decimal BrickSize { get; private set; }

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
        /// Gets the end time of this renko bar or the most recent update time if it <see cref="IsClosed"/>
        /// </summary>
        public override DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the end time of this renko bar or the most recent update time if it <see cref="IsClosed"/>
        /// </summary>
        [Obsolete("RenkoBar.End is obsolete. Please use RenkoBar.EndTime property instead.")]
        public DateTime End
        {
            get { return EndTime; }
            set { EndTime = value; }
        }

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
        /// The trend of the bar (i.e. Rising, Falling or NoDelta)
        /// </summary>
        public BarDirection Direction
        {
            get
            {
                if (Open < Close)
                    return BarDirection.Rising;
                else if (Open > Close)
                    return BarDirection.Falling;
                else
                    return BarDirection.NoDelta;
            }
        }

        /// <summary>
        /// The "spread" of the bar
        /// </summary>
        public decimal Spread
        {
            get
            {
                return Math.Abs(Close - Open);
            }
        }

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
        public RenkoBar(Symbol symbol, DateTime time, decimal brickSize, 
            decimal open, decimal volume)
        {
            Type = RenkoType.Classic;

            Symbol = symbol;
            Start = time;
            EndTime = time;
            BrickSize = brickSize;
            Open = open;
            Close = open;
            Volume = volume;
            High = open;
            Low = open;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenkoBar"/> class with the specified values
        /// </summary>
        /// <param name="symbol">The symbol of this data</param>
        /// <param name="start">The start time of the bar</param>
        /// <param name="endTime">The end time of the bar</param>
        /// <param name="brickSize">The size of each wicko brick</param>
        /// <param name="open">The opening price for the new bar</param>
        /// <param name="high">The high price for the new bar</param>
        /// <param name="low">The low price for the new bar</param>
        /// <param name="close">The closing price for the new bar</param>
        public RenkoBar(Symbol symbol, DateTime start, DateTime endTime,
            decimal brickSize, decimal open, decimal high, decimal low, decimal close)
        {
            Type = RenkoType.Wicked;

            Symbol = symbol;
            Start = start;
            EndTime = endTime;
            BrickSize = brickSize;
            Open = open;
            Close = close;
            Volume = 0;
            High = high;
            Low = low;
        }

        /// <summary>
        /// Updates this <see cref="RenkoBar"/> with the specified values and returns whether or not this bar is closed
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="currentValue">The current value</param>
        /// <param name="volumeSinceLastUpdate">The volume since the last update called on this instance</param>
        /// <returns>True if this bar <see cref="IsClosed"/></returns>
        public bool Update(DateTime time, decimal currentValue, decimal volumeSinceLastUpdate)
        {
            if (Type == RenkoType.Wicked)
                throw new InvalidOperationException("A \"Wicked\" RenkoBar cannot be updated!");

            // can't update a closed renko bar
            if (IsClosed) return true;
            if (Start == DateTime.MinValue) Start = time;
            EndTime = time;

            // compute the min/max closes this renko bar can have
            decimal lowClose = Open - BrickSize;
            decimal highClose = Open + BrickSize;

            Close = Math.Min(highClose, Math.Max(lowClose, currentValue));
            Volume += volumeSinceLastUpdate;

            // determine if this data caused the bar to close
            if (currentValue <= lowClose  || currentValue >= highClose)
            {
                IsClosed = true;
            }

            if (Close > High) High = Close;
            if (Close < Low) Low = Close;

            return IsClosed;
        }

        /// <summary>
        /// Reader Method :: using set of arguements we specify read out type. Enumerate
        /// until the end of the data stream or file. E.g. Read CSV file line by line and convert
        /// into data types.
        /// </summary>
        /// <returns>BaseData type set by Subscription Method.</returns>
        /// <param name="config">Config.</param>
        /// <param name="line">Line.</param>
        /// <param name="date">Date.</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            throw new NotSupportedException("RenkoBar does not support the Reader function. This function should never be called on this type.");
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String URL of source file.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            throw new NotSupportedException("RenkoBar does not support the GetSource function. This function should never be called on this type.");
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
            return new RenkoBar
            {
                Type = Type,
                BrickSize = BrickSize,
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
