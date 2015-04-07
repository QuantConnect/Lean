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
/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Wrapper Containers for Deserializing Calendar Information from API.
    /// </summary>
    public class MarketTodayContainer
    {
        /// Todays Data: aMarkets
        [JsonProperty(PropertyName = "aSecurityTypes")]
        public Dictionary<SecurityType, MarketToday> Markets;
    }

    /// <summary>
    /// Market today information class
    /// </summary>
    public class MarketToday
    {
        /// <summary>
        /// Time this packet was generated.
        /// </summary>
        [JsonProperty(PropertyName = "dtNow")]
        public readonly DateTime Now = new DateTime();

        /// <summary>
        /// Date this packet was generated.
        /// </summary>
        public DateTime Date
        {
            get 
            {
                return Now.Date;
            }
        }

        /// <summary>
        /// Given the dates and times above, what is the current market status - open or closed.
        /// </summary>
        [JsonProperty(PropertyName = "sStatus")]
        public string Status = "";
        
        /// <summary>
        /// Premarket hours for today
        /// </summary>
        [JsonProperty(PropertyName = "aPremarket")]
        public MarketHours PreMarket = new MarketHours(4, 9.5);
        
        /// <summary>
        /// Normal trading market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "aOpen")]
        public MarketHours Open = new MarketHours(9.5, 16);
        
        /// <summary>
        /// Post market hours for today
        /// </summary>
        [JsonProperty(PropertyName = "aPostmarket")]
        public MarketHours PostMarket = new MarketHours(16, 20);
        
        /// Default Constructor:
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
        [JsonProperty(PropertyName = "tsStart")]
        public DateTime Start;

        /// <summary>
        /// End time for this market hour category
        /// </summary>
        [JsonProperty(PropertyName = "tsEnd")]
        public DateTime End;
        
        /// <summary>
        /// Market hours initializer given an hours since midnight measure for the market hours today
        /// </summary>
        /// <param name="defaultStart">Time in hours since midnight to start this open period.</param>
        /// <param name="defaultEnd">Time in hours since midnight to end this open period.</param>
        public MarketHours(double defaultStart, double defaultEnd)
        {
            Start = DateTime.Now.Date.AddHours(defaultStart);
            End = DateTime.Now.Date.AddHours(defaultEnd);
        }
    }

} // End QC Namespace
