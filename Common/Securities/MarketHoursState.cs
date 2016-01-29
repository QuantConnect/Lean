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

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Specifies the open/close state for a <see cref="MarketHoursSegment"/>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarketHoursState
    {
        /// <summary>
        /// The market is not open
        /// </summary>
        [EnumMember(Value = "closed")]
        Closed,

        /// <summary>
        /// The market is open, but before normal trading hours
        /// </summary>
        [EnumMember(Value = "premarket")]
        PreMarket,

        /// <summary>
        /// The market is open and within normal trading hours
        /// </summary>
        [EnumMember(Value = "market")]
        Market,

        /// <summary>
        /// The market is open, but after normal trading hours
        /// </summary>
        [EnumMember(Value = "postmarket")]
        PostMarket
    }
}