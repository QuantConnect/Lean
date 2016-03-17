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
    /// Trade Message
    /// </summary>
    public class TradeMessage : BaseMessage
    {

        const int _trd_seq = 0;
        int _trd_id;
        int _trd_pair;
        int _trd_timestamp;
        int _trd_ord_id;
        int _trd_amount_executed;
        int _trd_price_executed;
        int _ord_type;
        int _ord_price;
        int _fee;
        int _fee_currency;

        /// <summary>
        /// Constructor for Trade Message
        /// </summary>
        /// <param name="values"></param>
        public TradeMessage(string[] values)
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

            TRD_SEQ = AllValues[_trd_seq];
            TRD_PAIR = AllValues[_trd_pair];
            TRD_TIMESTAMP = GetDateTime(_trd_timestamp);
            TRD_ORD_ID = GetInt(_trd_ord_id);
            TRD_AMOUNT_EXECUTED = GetDecimal(_trd_amount_executed);
            TRD_PRICE_EXECUTED = GetDecimal(_trd_price_executed);
            ORD_TYPE = AllValues[_ord_type];
            ORD_PRICE = GetDecimal(_ord_price);
            if (AllValues.Length == 11)
            {
                TRD_ID = TryGetInt(_trd_id);
                FEE = GetDecimalFromScientific(_fee);
                FEE_CURRENCY = AllValues[_fee_currency];
            }
        }

        /// <summary>
        /// Trade sequence
        /// </summary>
        public string TRD_SEQ { get; set; }
        /// <summary>
        /// Trade Id
        /// </summary>
        public int TRD_ID { get; set; }
        /// <summary>
        /// Currency Pair
        /// </summary>
        public string TRD_PAIR { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime TRD_TIMESTAMP { get; set; }
        /// <summary>
        /// Order Id
        /// </summary>
        public int TRD_ORD_ID { get; set; }
        /// <summary>
        /// Amount Executed
        /// </summary>
        public decimal TRD_AMOUNT_EXECUTED { get; set; }
        /// <summary>
        /// Price Executed
        /// </summary>
        public decimal TRD_PRICE_EXECUTED { get; set; }
        /// <summary>
        /// Order type
        /// </summary>
        public string ORD_TYPE { get; set; }
        /// <summary>
        /// Order Price
        /// </summary>
        public decimal ORD_PRICE { get; set; }
        /// <summary>
        /// Fee
        /// </summary>
        public decimal FEE { get; set; }
        /// <summary>
        /// Fee Currency
        /// </summary>
        public string FEE_CURRENCY { get; set; }

    }
}
