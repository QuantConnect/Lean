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
using QuantConnect.Brokerages.Oanda.DataType.Communications;

namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents a Transaction object with details about an Oanda transaction.
    /// </summary>
    public class Transaction : Response
    {
        public int id { get; set; }
        public int accountId { get; set; }
		public string time { get; set; }
		public string type { get; set; }
        public string instrument { get; set; }
		public string side { get; set; }
		public int units { get; set; }
        public double price { get; set; }
		public double lowerBound { get; set; }
		public double upperBound { get; set; }
		public double takeProfitPrice { get; set; }
		public double stopLossPrice { get; set; }
		public double trailingStopLossDistance { get; set; }
		public double pl { get; set; }
		public double interest { get; set; }
		public double accountBalance { get; set; }
		public int tradeId { get; set; }
	    public int orderId { get; set; }
		public TradeData tradeOpened { get; set; }
		public TradeData tradeReduced { get; set; }
		public string reason { get; set; }
		public string expiry { get; set; }

        /// <summary>
        /// Gets a basic title for the type of transaction
        /// </summary>
        /// <returns></returns>
        public string GetTitle()
        {
            switch ( type )
            {
                case "CloseOrder":
                    return "Order Closed";
                case "SellLimit":
                    return "Sell Limit Order Created";
                case "BuyLimit":
                    return "Buy Limit Order Created";
            }
            return type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetReadableString()
        {
            string readable = units + " " + instrument + " at " + price;
            if ( pl != 0 )
            {
                readable += "\nP/L: " + pl;
            }
            return readable;
        }
    }
}
