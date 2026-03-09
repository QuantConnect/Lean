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
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// Single Chart Point Value Type for QCAlgorithm.Plot();
    /// </summary>
    [JsonConverter(typeof(ChartPointJsonConverter))]
    public class ChartPoint : ISeriesPoint
    {
        private DateTime _time;
        private long _x;
        private decimal? _y;

        /// <summary>
        /// Time of this chart series point
        /// </summary>
        [JsonIgnore]
        public DateTime Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
                _x = Convert.ToInt64(QuantConnect.Time.DateTimeToUnixTimeStamp(_time));
            }
        }

        /// <summary>
        /// Chart point time
        /// </summary>
        /// <remarks>Lower case for javascript encoding simplicity</remarks>
        public long x
        {
            get
            {
                return _x;
            }
            set
            {
                _time = QuantConnect.Time.UnixTimeStampToDateTime(value);
                _x = value;
            }
        }

        /// <summary>
        /// Chart point value
        /// </summary>
        /// <remarks>Lower case for javascript encoding simplicity</remarks>
        public decimal? y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value.SmartRounding();
            }
        }

        /// <summary>
        /// Shortcut for <see cref="x"/> for C# naming conventions
        /// </summary>
        [JsonIgnore]
        public long X => x;

        /// <summary>
        /// Shortcut for <see cref="y"/> for C# naming conventions
        /// </summary>
        [JsonIgnore]
        public decimal? Y => y;

        /// <summary>
        /// Default constructor. Using in SeriesSampler.
        /// </summary>
        public ChartPoint() { }

        /// <summary>
        /// Constructor that takes both x, y value pairs
        /// </summary>
        /// <param name="xValue">X value often representing a time in seconds</param>
        /// <param name="yValue">Y value</param>
        public ChartPoint(long xValue, decimal? yValue)
            : this()
        {
            x = xValue;
            y = yValue;
        }

        /// <summary>
        /// Constructor that takes both x, y value pairs
        /// </summary>
        /// <param name="time">This point time</param>
        /// <param name="value">Y value</param>
        public ChartPoint(DateTime time, decimal? value)
            : this()
        {
            Time = time;
            y = value;
        }

        ///Cloner Constructor:
        public ChartPoint(ChartPoint point)
        {
            _time = point._time;
            _x = point._x;
            _y = point._y;
        }

        /// <summary>
        /// Provides a readable string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return Messages.ChartPoint.ToString(this);
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns>Clone of this instance</returns>
        public virtual ISeriesPoint Clone()
        {
            return new ChartPoint(this);
        }
    }
}
