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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the state of an exchange during a specified time range
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class MarketHoursSegment
    {
        /// <summary>
        /// Gets the start time for this segment
        /// </summary>
        [JsonProperty("start")]
        public TimeSpan Start { get; private set; }

        /// <summary>
        /// Gets the end time for this segment
        /// </summary>
        [JsonProperty("end")]
        public TimeSpan End { get; private set; }

        /// <summary>
        /// Gets the market hours state for this segment
        /// </summary>
        [JsonProperty("state")]
        public MarketHoursState State { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHoursSegment"/> class
        /// </summary>
        /// <param name="state">The state of the market during the specified times</param>
        /// <param name="start">The start time of the segment</param>
        /// <param name="end">The end time of the segment</param>
        public MarketHoursSegment(MarketHoursState state, TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
            State = state;
        }

        /// <summary>
        /// Gets a new market hours segment representing being open all day
        /// </summary>
        public static MarketHoursSegment OpenAllDay()
        {
            return new MarketHoursSegment(MarketHoursState.Market, TimeSpan.Zero, Time.OneDay);
        }

        /// <summary>
        /// Gets a new market hours segment representing being open all day
        /// </summary>
        public static MarketHoursSegment ClosedAllDay()
        {
            return new MarketHoursSegment(MarketHoursState.Closed, TimeSpan.Zero, Time.OneDay);
        }

        /// <summary>
        /// Determines whether or not the specified time is contained within this segment
        /// </summary>
        /// <param name="time">The time to check</param>
        /// <returns>True if this segment contains the specified time, false otherwise</returns>
        public bool Contains(TimeSpan time)
        {
            return time >= Start && time < End;
        }

        /// <summary>
        /// Determines whether or not the specified time range overlaps with this segment
        /// </summary>
        /// <param name="start">The start of the range</param>
        /// <param name="end">The end of the range</param>
        /// <returns>True if the specified range overlaps this time segment, false otherwise</returns>
        public bool Overlaps(TimeSpan start, TimeSpan end)
        {
            return Start < end && End > start;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}-{2}", State, Start, End);
        }
    }
}