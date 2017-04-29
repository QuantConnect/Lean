/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2013 OANDA Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without 
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
 * Software, and to permit persons to whom the Software is furnished  to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
 * the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications;

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType
{
#pragma warning disable 1591
    /// <summary>
    /// Represents a Transaction object with details about an Oanda transaction.
    /// </summary>
    public class Transaction : Response
    {
        public long id { get; set; }
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
		public long tradeId { get; set; }
	    public long orderId { get; set; }
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
#pragma warning restore 1591
}
