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

using System.ComponentModel;

namespace QuantConnect.Brokerages.Oanda.RestV1.DataType.Communications.Requests
{
#pragma warning disable 1591
    public class CandlesRequest : Request
	{
		public CandlesRequest()
		{
		}

		public SmartProperty<string> instrument;

		[IsOptional]
		[DefaultValue(EGranularity.S5)]
		public SmartProperty<EGranularity> granularity;

		[IsOptional]
		[DefaultValue(500)]
		public SmartProperty<int> count;

		[IsOptional]
		public SmartProperty<string> start;

		[IsOptional]
		public SmartProperty<string> end;

		[IsOptional]
		[DefaultValue(ECandleFormat.bidask)]
		public SmartProperty<ECandleFormat> candleFormat;

		[IsOptional]
		//[DefaultValue(true)]
		public SmartProperty<bool> includeFirst;

		[IsOptional]
		public SmartProperty<string> dailyAlignment;

		[IsOptional]
		public SmartProperty<string> weeklyAlignment;

		public override string EndPoint
		{
			get { return "candles"; }
		}
	}

	

	public enum ECandleFormat
	{
		bidask,
		midpoint
	}

	public enum EGranularity
	{
		S5,
		S10,
		S15,
		S30,
		M1,
		M2,
		M3,
		M5,
		M10,
		M15,
		M30,
		H1,
		H2,
		H3,
		H4,
		H6,
		H8,
		H12,
		D,
		W,
		M
	}
#pragma warning restore 1591
}
