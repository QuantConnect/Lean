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
    /// CME Option Chain Quotes API call root response
    /// </summary>
    public class CMEOptionChainQuotes
    {
        /// <summary>
        /// The future options contracts with/without quotes
        /// </summary>
        [JsonProperty("optionContractQuotes")]
        public List<CMEOptionChainQuoteEntry> OptionContractQuotes { get; private set; }
    }

    /// <summary>
    /// Option chain quotes contained within the root response under the "optionContractQuotes" JSON paramter.
    /// </summary>
    public class CMEOptionChainQuoteEntry
    {
        /// <summary>
        /// Call-side quote information. This will contain the strike price information for the entire chain.
        /// </summary>
        [JsonProperty("call")]
        public CMEOptionChainQuote Call { get; private set; }

        /// <summary>
        /// Put-side quote information. This will contain the strike price information for the entire chain.
        /// </summary>
        [JsonProperty("put")]
        public CMEOptionChainQuote Put { get; private set; }
    }

    /// <summary>
    /// Option chain entry quote values
    /// </summary>
    public class CMEOptionChainQuote
    {
        private decimal _strikePrice;

        /// <summary>
        /// Ticker code, including strike price and expiry information
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; private set; }

        /// <summary>
        /// Strike price of the future option quote entry
        /// </summary>
        [JsonIgnore]
        public decimal StrikePrice
        {
            get
            {
                if (_strikePrice != default(decimal))
                {
                    return _strikePrice;
                }

                // The quote is formatted as the following: `<TICKER><EXPIRY> <CHAR><STRIKE>`
                // We parse the strike from the code itself rather than using the `strike` property since
                // we're unable to determine where the scaling factor is for that value (it varies, might be
                // 100x or 50x, but no information from the API calls we've made contain that information).
                _strikePrice = decimal.Parse(Code.Split(' ')[1].Substring(1), CultureInfo.InvariantCulture);
                return _strikePrice;
            }
        }
    }
}
