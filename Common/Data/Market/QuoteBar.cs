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
using System.Globalization;
using System.IO;
using QuantConnect.Logging;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// TradeBar class for second and minute resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class QuoteBar : Bar
    {
        /// <summary>
        /// Average size of Bid
        /// </summary>
        public long AvgBidSize { get; set; }

        /// <summary>
        /// Average size of Ask
        /// </summary>
        public long AvgAskSize { get; set; }

        /// <summary>
        /// Bid OHLC
        /// </summary>
        public Bar Bid { get; set; }

        /// <summary>
        /// Ask OHLC
        /// </summary>
        public Bar Ask { get; set; }

        //In Base Class: Symbol of Asset.
        //public Symbol Symbol;

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //In Base Class: DateTime of this TradeBar
        //public DateTime Time;

        //In Bar Class: OHLC
        //public decimal Open;
        //public decimal High;
        //public decimal Low;
        //public decimal Close;

        //In Bar Class: Period of this TradeBar
        // public TimeSpan Period;

        /// <summary>
        /// Default initializer to setup an empty quotebar.
        /// </summary>
        public QuoteBar()
            : base()
        {
            DataType = MarketDataType.QuoteBar;
            AvgBidSize = 0;
            AvgAskSize = 0;
            Bid = new Bar();
            Ask = new Bar();
        }

        /// <summary>
        /// Initialize Quote Bar with Bid(OHLC) and Ask(OHLC) Values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="bidopen">Decimal Opening Price</param>
        /// <param name="bidhigh">Decimal High Price of this bar</param>
        /// <param name="bidlow">Decimal Low Price of this bar</param>
        /// <param name="bidclose">Decimal Close price of this bar</param>
        /// <param name="avgbidsize">Volume sum over day</param>
        /// <param name="askopen">Decimal Opening Price</param>
        /// <param name="askhigh">Decimal High Price of this bar</param>
        /// <param name="asklow">Decimal Low Price of this bar</param>
        /// <param name="askclose">Decimal Close price of this bar</param>
        /// <param name="avgasksize">Volume sum over day</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public QuoteBar(DateTime time, Symbol symbol, decimal bidopen, decimal bidhigh, decimal bidlow, decimal bidclose, long avgbidsize, decimal askopen, decimal askhigh, decimal asklow, decimal askclose, long avgasksize, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = (bidclose + askclose) / 2;
            AvgBidSize = avgbidsize;
            AvgAskSize = avgasksize;
            Period = period ?? TimeSpan.FromMinutes(1);
            DataType = MarketDataType.TradeBar;

            Bid = new Bar(time, symbol, bidopen, bidhigh, bidlow, bidclose, period);
            Ask = new Bar(time, symbol, askopen, askhigh, asklow, askclose, period);  
        }
    }
}
