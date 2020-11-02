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
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// CME Option Chain Settlements API call root response
    /// </summary>
    public class CMEOptionChainSettlements
    {
        /// <summary>
        /// The future options contracts with/without settlements
        /// </summary>
        [JsonProperty("settlements")]
        public List<CMEOptionChainSettlement> Settlements { get; private set; }
    }

    /// <summary>
    /// Option chain entry settlement values, containing strike price
    /// </summary>
    public class CMEOptionChainSettlement
    {
        /// <summary>
        /// Option right (call/put)
        /// </summary>
        [JsonProperty("type")]
        public string OptionRight { get; private set; }

        /// <summary>
        /// Strike price of the future option quote entry
        /// </summary>
        [JsonProperty("strike"), JsonConverter(typeof(StringDecimalJsonConverter), true)]
        public decimal StrikePrice { get; private set; }
    }
}
