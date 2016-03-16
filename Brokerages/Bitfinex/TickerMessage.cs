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
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Ticker message object
    /// </summary>
    public class TickerMessage : BaseMessage
    {

        const int _channel_id = 0;
        const int _bid = 1;
        const int _bid_size = 2;
        const int _ask = 3;
        const int _ask_size = 4;
        const int _daily_change = 5;
        const int _daily_change_perc = 6;
        const int _last_price = 7;
        const int _volume = 8;
        const int _high = 9;
        const int _low = 10;

        /// <summary>
        /// Ticker Message constructor
        /// </summary>
        /// <param name="values"></param>
        public TickerMessage(string[] values)
            : base(values)
        {
            CHANNEL_ID = GetInt(_channel_id);
            BID = GetDecimal(_bid);
            BID_SIZE = TryGetDecimal(_bid_size);
            ASK = GetDecimal(_ask);
            ASK_SIZE = TryGetDecimal(_ask_size);
            DAILY_CHANGE = GetDecimal(_daily_change);
            DAILY_CHANGE_PERC = GetDecimal(_daily_change_perc);
            LAST_PRICE = GetDecimal(_last_price);
            VOLUME = GetDecimal(_volume);
            HIGH = GetDecimal(_high);
            LOW = GetDecimal(_low);
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int CHANNEL_ID { get; set; }
        /// <summary>
        /// Bid
        /// </summary>
        public decimal BID { get; set; }
        /// <summary>
        /// Bid Size
        /// </summary>
        public decimal BID_SIZE { get; set; }
        /// <summary>
        /// Ask
        /// </summary>
        public decimal ASK { get; set; }
        /// <summary>
        /// Ask Size
        /// </summary>
        public decimal ASK_SIZE { get; set; }
        /// <summary>
        /// Daily Change
        /// </summary>
        public decimal DAILY_CHANGE { get; set; }
        /// <summary>
        /// Daily Change %
        /// </summary>
        public decimal DAILY_CHANGE_PERC { get; set; }
        /// <summary>
        /// Last Price
        /// </summary>
        public decimal LAST_PRICE { get; set; }
        /// <summary>
        /// Volume
        /// </summary>
        public decimal VOLUME { get; set; }
        /// <summary>
        /// High
        /// </summary>
        public decimal HIGH { get; set; }
        /// <summary>
        /// Low
        /// </summary>
        public decimal LOW { get; set; }

    }
}
