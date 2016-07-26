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

namespace QuantConnect.Packets
{

    /// <summary>
    /// Market today information class
    /// </summary>
    public class MarketToday
    {
        /// <summary>
        /// Date this packet was generated.
        /// </summary>
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Given the dates and times above, what is the current market status - open or closed.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status = "";

        /// <summary>
        /// Premarket hours for today
        /// </summary>
        [JsonProperty(PropertyName = "premarket")]
        public MarketHours PreMarket;

        /// <summary>
        /// Normal trading market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "open")]
        public MarketHours Open;

        /// <summary>
        /// Post market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "postmarket")]
        public MarketHours PostMarket;

        /// <summary>
        /// Default constructor (required for JSON serialization)
        /// </summary>
        public MarketToday()
        { }
    }

    /// <summary>
    /// Market open hours model for pre, normal and post market hour definitions.
    /// </summary>
    public class MarketHours
    {
        /// <summary>
        /// Start time for this market hour category
        /// </summary>
        [JsonProperty(PropertyName = "start")]
        public DateTime Start;

        /// <summary>
        /// End time for this market hour category
        /// </summary>
        [JsonProperty(PropertyName = "end")]
        public DateTime End;

        /// <summary>
        /// Market hours initializer given an hours since midnight measure for the market hours today
        /// </summary>
        /// <param name="referenceDate">Reference date used for as base date from the specified hour offsets</param>
        /// <param name="defaultStart">Time in hours since midnight to start this open period.</param>
        /// <param name="defaultEnd">Time in hours since midnight to end this open period.</param>
        public MarketHours(DateTime referenceDate, double defaultStart, double defaultEnd)
        {
            Start = referenceDate.Date.AddHours(defaultStart);
            End = referenceDate.Date.AddHours(defaultEnd);
            if (defaultEnd == 24)
            {
                // when we mark it as the end of the day other code that relies on .TimeOfDay has issues
                End = End.AddTicks(-1);
            }
        }
    }
}
