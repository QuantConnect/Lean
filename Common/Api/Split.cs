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
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// Split returned from the api
    /// </summary>
    public class Split
    {
        /// <summary>
        /// The Symbol
        /// </summary>
        public Symbol Symbol
        {
            get
            {
                var sid = SecurityIdentifier.Parse(SymbolID);
                return new Symbol(sid, sid.Symbol);
            }
        }

        /// <summary>
        /// The requested symbol ID
        /// </summary>
        [JsonProperty(PropertyName = "symbol_id")]
        public string SymbolID { get; set; }

        /// <summary>
        /// The date of the split
        /// </summary>
        [JsonProperty(PropertyName = "date")]
        [JsonConverter(typeof(DateTimeJsonConverter), "yyyyMMdd")]
        public DateTime Date { get; set; }

        /// <summary>
        /// The split factor
        /// </summary>
        [JsonProperty(PropertyName = "split_factor")]
        public decimal SplitFactor { get; set; }

        /// <summary>
        /// The reference price for the split
        /// </summary>
        [JsonProperty(PropertyName = "reference_price")]
        public decimal ReferencePrice { get; set; }
    }

    /// <summary>
    /// Collection container for a list of split objects
    /// </summary>
    public class SplitList : RestResponse
    {
        /// <summary>
        /// The splits list
        /// </summary>
        [JsonProperty(PropertyName = "splits")]
        public List<Split> Splits { get; set; }
    }
}
