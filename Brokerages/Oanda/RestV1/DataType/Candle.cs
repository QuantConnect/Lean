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

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType
{
#pragma warning disable 1591
    public struct Candle
    {
        public string time { get; set; }
		public int volume { get; set; }
		public bool complete { get; set; }

		// Midpoint candles
		public double openMid { get; set; }
        public double highMid { get; set; }
        public double lowMid { get; set; }
        public double closeMid { get; set; }
		
		// Bid/Ask candles
		public double openBid { get; set; }
		public double highBid { get; set; }
		public double lowBid { get; set; }
		public double closeBid { get; set; }
		public double openAsk { get; set; }
		public double highAsk { get; set; }
		public double lowAsk { get; set; }
		public double closeAsk { get; set; }

		
    }
#pragma warning restore 1591
}
