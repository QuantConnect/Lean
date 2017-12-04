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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Trade Message
    /// </summary>
    public class Fill : BaseMessage
    {
        private const int _trd_seq = 0;
        private readonly int _trd_id;
        private readonly int _trd_pair;
        private readonly int _trd_timestamp;
        private readonly int _trd_ord_id;
        private readonly int _trd_amount_executed;
        private readonly int _trd_price_executed;
        private readonly int _ord_type;
        private readonly int _ord_price;
        private readonly int _fee;
        private readonly int _fee_currency;

        public Fill(string term, string[] values)
            : base(values)
        {
            if (AllValues.Length == 11)
            {
                _trd_id = 1;
                _trd_pair = 2;
                _trd_timestamp = 3;
                _trd_ord_id = 4;
                _trd_amount_executed = 5;
                _trd_price_executed = 6;
                _ord_type = 7;
                _ord_price = 8;
                _fee = 9;
                _fee_currency = 10;
            }
            else
            {
                _trd_pair = 1;
                _trd_timestamp = 2;
                _trd_ord_id = 3;
                _trd_amount_executed = 4;
                _trd_price_executed = 5;
                _ord_type = 6;
                _ord_price = 7;
            }

            Seq = AllValues[_trd_seq];
            Pair = AllValues[_trd_pair];
            Timestamp = GetDateTime(_trd_timestamp);
            OrdId = GetLong(_trd_ord_id);
            AmountExecuted = GetDecimal(_trd_amount_executed);
            PriceExecuted = GetDecimal(_trd_price_executed);
            Type = AllValues[_ord_type];
            Price = TryGetDecimal(_ord_price);
            if (AllValues.Length == 11)
            {
                Id = TryGetLong(_trd_id);
                Fee = TryGetDecimal(_fee);
                FeeCurrency = AllValues[_fee_currency];
            }

            IsTradeUpdate = term == "tu";
        }

        /// <summary>
        /// Trade sequence
        /// </summary>
        public string Seq { get; set; }

        /// <summary>
        /// Trade Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Currency Pair
        /// </summary>
        public string Pair { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Order Id
        /// </summary>
        public long OrdId { get; set; }

        /// <summary>
        /// Amount Executed
        /// </summary>
        public decimal AmountExecuted { get; set; }

        /// <summary>
        /// Price Executed
        /// </summary>
        public decimal PriceExecuted { get; set; }

        /// <summary>
        /// Order type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Order Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Fee
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// Fee Currency
        /// </summary>
        public string FeeCurrency { get; set; }

        /// <summary>
        /// Execution or update
        /// </summary>
        public bool IsTradeUpdate { get; set; }
    }
}