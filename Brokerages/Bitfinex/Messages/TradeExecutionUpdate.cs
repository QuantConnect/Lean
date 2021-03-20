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
    /// Trade execution event on the account.
    /// </summary>
    [JsonConverter(typeof(TradeExecutionUpdateConverter))]
    public class TradeExecutionUpdate
    {
        /// <summary>
        /// Trade database id
        /// </summary>
        public long TradeId { get; set; }

        /// <summary>
        /// Symbol (tBTCUSD, …)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Execution timestamp
        /// </summary>
        public long MtsCreate { get; set; }

        /// <summary>
        /// Order id
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// Positive means buy, negative means sell
        /// </summary>
        public decimal ExecAmount { get; set; }

        /// <summary>
        /// Execution price
        /// </summary>
        public decimal ExecPrice { get; set; }

        /// <summary>
        /// Order type
        /// </summary>
        public string OrderType { get; set; }

        /// <summary>
        /// Order price
        /// </summary>
        public decimal OrderPrice { get; set; }

        /// <summary>
        /// 1 if true, -1 if false
        /// </summary>
        public int Maker { get; set; }

        /// <summary>
        /// Fee ('tu' only)
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// Fee currency ('tu' only)
        /// </summary>
        public string FeeCurrency { get; set; }
    }
}
