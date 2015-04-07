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
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents a piece of data at a specific time
    /// </summary>
    public class IndicatorDataPoint : BaseData, IEquatable<IndicatorDataPoint>
    {
        /// <summary>
        /// Initializes a new default instance of IndicatorDataPoint with a time of
        /// DateTime.MinValue and a Value of 0m.
        /// </summary>
        public IndicatorDataPoint()
        {
            Value = 0m;
            Time = DateTime.MinValue;
        }

        /// <summary>
        /// Initializes a new instance of the DataPoint type using the specified time/data
        /// </summary>
        /// <param name="time">The time this data was produced</param>
        /// <param name="value">The data</param>
        public IndicatorDataPoint(DateTime time, decimal value)
        {
            Time = time;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the DataPoint type using the specified time/data
        /// </summary>
        /// <param name="symbol">The symbol associated with this data</param>
        /// <param name="time">The time this data was produced</param>
        /// <param name="value">The data</param>
        public IndicatorDataPoint(string symbol, DateTime time, decimal value)
        {
            Symbol = symbol;
            Time = time;
            Value = value;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(IndicatorDataPoint other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Time == Time && other.Value == Value;
        }

        /// <summary>
        /// Returns a string representation of this DataPoint instance using ISO8601 formatting for the date
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String" /> containing a fully qualified type name.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("{0} - {1}", Time.ToString("s"), Value);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>
        /// true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false.
        /// </returns>
        /// <param name="obj">Another object to compare to. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IndicatorDataPoint && Equals((IndicatorDataPoint) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Value.GetHashCode()*397) ^ Time.GetHashCode();
            }
        }

        /// <summary>
        /// Returns the data held within the instance
        /// </summary>
        /// <param name="instance">The DataPoint instance</param>
        /// <returns>The data held within the instance</returns>
        public static implicit operator decimal(IndicatorDataPoint instance)
        {
            return instance.Value;
        }

        /// <summary>
        /// This function is purposefully not implemented.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException("IndicatorDataPoint does not support the Reader function. This function should never be called on this type.");
        }

        /// <summary>
        /// This function is purposefully not implemented.
        /// </summary>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            throw new NotImplementedException("IndicatorDataPoint does not support the GetSource function. This function should never be called on this type.");
        }
    }
}