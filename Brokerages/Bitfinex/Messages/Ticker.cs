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
    /// A high level overview of the state of the market for a specified pair
    /// </summary>
    [JsonConverter(typeof(TickerConverter))]
    public class Ticker
    {
        /// <summary>
        /// Price of last highest bid
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// Sum of the 25 highest bid sizes
        /// </summary>
        public decimal BidSize { get; set; }

        /// <summary>
        /// Price of last lowest ask
        /// </summary>
        public decimal Ask { get; set; }

        /// <summary>
        /// Sum of the 25 lowest ask sizes
        /// </summary>
        public decimal AskSize { get; set; }

        /// <summary>
        /// Amount that the last price has changed since yesterday
        /// </summary>
        public decimal DailyChange { get; set; }

        /// <summary>
        /// Relative price change since yesterday (*100 for percentage change)
        /// </summary>
        public decimal DailyChangeRelative { get; set; }

        /// <summary>
        /// Price of the last trade
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Daily volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Daily high
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Daily low
        /// </summary>
        public decimal Low { get; set; }
    }
}
