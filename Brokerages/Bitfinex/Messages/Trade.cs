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
    public class Trade : BaseMessage
    {
        private const int _channelId = 0;
        private const int _term = 1;
        private const int _seq = 2;
        private const int _timestamp = 3;
        private const int _price = 4;
        private const int _amount = 5;

        /// <summary>
        /// Ticker Message constructor
        /// </summary>
        /// <param name="values"></param>
        public Trade(string[] values)
            : base(values)
        {
            ChannelId = GetInt(_channelId);
            Term = values[_term];
            Seq = values[_seq];
            Timestamp = GetInt(_timestamp);
            Price = TryGetDecimal(_price);
            Amount = TryGetDecimal(_amount);
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// te or tu
        /// </summary>
        public string Term { get; set; }

        /// <summary>
        /// sequence number
        /// </summary>
        public string Seq { get; set; }

        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public int Timestamp { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}