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

        /// <summary>
        /// Constructor for Trade Message
        /// </summary>
        /// <param name="values"></param>
        public TradeMessage(string[] values)
            : base(values)
        {

            if (AllValues.Length == 11)
            {
                AllKeys = new string[] { "TRD_SEQ", "TRD_ID","TRD_PAIR","TRD_TIMESTAMP", "TRD_ORD_ID",  "TRD_AMOUNT_EXECUTED",
            "TRD_PRICE_EXECUTED", "ORD_TYPE", "ORD_PRICE", "FEE", "FEE_CURRENCY" };
            }
            else
            {
                AllKeys = new string[] { "TRD_SEQ", "TRD_PAIR","TRD_TIMESTAMP", "TRD_ORD_ID",  "TRD_AMOUNT_EXECUTED",
            "TRD_PRICE_EXECUTED", "ORD_TYPE", "ORD_PRICE" };
            }



            TRD_SEQ = AllValues[Array.IndexOf(AllKeys, "TRD_SEQ")];
            TRD_PAIR = AllValues[Array.IndexOf(AllKeys, "TRD_PAIR")];
            TRD_TIMESTAMP = GetDateTime("TRD_TIMESTAMP");
            TRD_ORD_ID = GetInt("TRD_ORD_ID");
            TRD_AMOUNT_EXECUTED = GetDecimal("TRD_AMOUNT_EXECUTED");
            TRD_PRICE_EXECUTED = GetDecimal("TRD_PRICE_EXECUTED");
            ORD_TYPE = AllValues[Array.IndexOf(AllKeys, "ORD_TYPE")];
            ORD_PRICE = GetDecimal("ORD_PRICE");
            if (AllValues.Length == 11)
            {
                TRD_ID = TryGetInt("TRD_ID");
                FEE = GetDecimalFromScientific("FEE");
                FEE_CURRENCY = AllValues[Array.IndexOf(AllKeys, "FEE_CURRENCY")];
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
