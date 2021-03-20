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

using Newtonsoft.Json;
using QuantConnect.Brokerages.Bitfinex.Converters;

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Account wallet balance
    /// </summary>
    [JsonConverter(typeof(WalletConverter))]
    public class Wallet
    {
        /// <summary>
        /// Wallet name (exchange, margin, funding)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Currency (e.g. USD, ...)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Wallet balance
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Unsettled interest
        /// </summary>
        public decimal UnsettledInterest { get; set; }
    }
}
