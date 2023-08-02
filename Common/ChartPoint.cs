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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// Single Chart Point Value Type for QCAlgorithm.Plot();
    /// </summary>
    [JsonObject]
    public class ChartPoint : ISeriesPoint
    {
        private DateTime _time;
        private long _x;

        /// <summary>
        /// Time of this chart series point
        /// </summary>
        public DateTime Time
        {
            get
            {
                return _time;
            }
            set
            {
                _x = Convert.ToInt64(QuantConnect.Time.DateTimeToUnixTimeStamp(value));
                _time = value;
            }
        }

        /// <summary>
        /// List of values for this chart series point
        /// </summary>
        /// <remarks>
        /// A single (x, y) value is represented as a list of length 1, with x being the <see cref="Time"/> and y being the value.
        /// </remarks>
        [JsonIgnore]
        public List<decimal> Values { get; }

        /// Time of this chart point: lower case for javascript encoding simplicity
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

        /// Value of this chart point:  lower case for javascript encoding simplicity
        public decimal y
        {
            get
            {
                return Values.Count > 0 ? Values[0] : default;
            }
            set
            {
                if (Values.Count == 0)
                {
                    Values.Add(value);
                }
                else
                {
                    Values[0] = value;
                }
            }
        }

        /// <summary>
        /// Default constructor. Using in SeriesSampler.
        /// </summary>
        public ChartPoint()
        {
            Values = new List<decimal>();
        }

        /// <summary>
        /// Constructor that takes both x, y value pairs
        /// </summary>
        /// <param name="xValue">X value often representing a time in seconds</param>
        /// <param name="yValue">Y value</param>
        public ChartPoint(long xValue, decimal yValue)
            : this()
        {
            x = xValue;
            y = yValue.SmartRounding();
        }

        ///Constructor for datetime-value arguments:
        public ChartPoint(DateTime time, decimal value)
            : this()
        {
            Time = time.ToUniversalTime();
            y = value.SmartRounding();
        }

        ///Cloner Constructor:
        public ChartPoint(ChartPoint point)
            : this(point.Time.ToUniversalTime(), point.y)
        {
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
        public ISeriesPoint Clone()
        {
            return new ChartPoint(this);
        }
    }
}
