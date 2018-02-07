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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Ticker message object
    /// </summary>
    public class Ticker : BaseMessage
    {
        private const int _channel_id = 0;
        private const int _bid = 1;
        private const int _bid_size = 2;
        private const int _ask = 3;
        private const int _ask_size = 4;
        private const int _daily_change = 5;
        private const int _daily_change_perc = 6;
        private const int _last_price = 7;
        private const int _volume = 8;
        private const int _high = 9;
        private const int _low = 10;

        /// <summary>
        /// Ticker Message constructor
        /// </summary>
        /// <param name="values"></param>
        public Ticker(string[] values)
            : base(values)
        {
            ChannelId = GetInt(_channel_id);
            Bid = GetDecimal(_bid);
            BidSize = TryGetDecimal(_bid_size);
            Ask = GetDecimal(_ask);
            AskSize = TryGetDecimal(_ask_size);
            DailyChange = TryGetDecimal(_daily_change);
            DailyChangePerc = TryGetDecimal(_daily_change_perc);
            LastPrice = GetDecimal(_last_price);
            Volume = GetDecimal(_volume);
            High = GetDecimal(_high);
            Low = GetDecimal(_low);
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// Bid
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// Bid Size
        /// </summary>
        public decimal BidSize { get; set; }

        /// <summary>
        /// Ask
        /// </summary>
        public decimal Ask { get; set; }

        /// <summary>
        /// Ask Size
        /// </summary>
        public decimal AskSize { get; set; }

        /// <summary>
        /// Daily Change
        /// </summary>
        public decimal DailyChange { get; set; }

        /// <summary>
        /// Daily Change %
        /// </summary>
        public decimal DailyChangePerc { get; set; }

        /// <summary>
        /// Last Price
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// High
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low
        /// </summary>
        public decimal Low { get; set; }
    }
}