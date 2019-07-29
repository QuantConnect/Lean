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
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Single Chart Point Value Type for QCAlgorithm.Plot();
    /// </summary>
    [JsonObject]
    public class ChartPoint
    {
        /// Time of this chart point: lower case for javascript encoding simplicty
        public long x;

        /// Value of this chart point:  lower case for javascript encoding simplicty
        public decimal y;

        /// <summary>
        /// Default constructor. Using in SeriesSampler.
        /// </summary>
        public ChartPoint() { }

        /// <summary>
        /// Constructor that takes both x, y value paris
        /// </summary>
        /// <param name="xValue">X value often representing a time in seconds</param>
        /// <param name="yValue">Y value</param>
        public ChartPoint(long xValue, decimal yValue)
        {
            x = xValue;
            y = yValue;
        }

        ///Constructor for datetime-value arguements:
        public ChartPoint(DateTime time, decimal value)
        {
            x = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(time.ToUniversalTime()));
            y = value.SmartRounding();
        }

        ///Cloner Constructor:
        public ChartPoint(ChartPoint point)
        {
            x = point.x;
            y = point.y.SmartRounding();
        }

        /// <summary>
        /// Provides a readable string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Time.UnixTimeStampToDateTime(x):o} - {y}");
        }
    }
}
