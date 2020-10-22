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
 *
*/

namespace QuantConnect.ToolBox.IEX.Response
{
	public class QuoteSSE
	{
        public string Symbol { get; set; }

		public string CompanyName { get; set; }

		public string PrimaryExchange { get; set; }

		public string CalculationPrice { get; set; }

		public decimal? Open { get; set; }

		public long? OpenTime { get; set; }

		public string OpenSource { get; set; }

		public decimal? Close { get; set; }

		public long? CloseTime { get; set; }

		public string CloseSource { get; set; }

		public decimal High { get; set; }

		public long HighTime { get; set; }

		public string HighSource { get; set; }

		public decimal Low { get; set; }

		public long LowTime { get; set; }

		public string LowSource { get; set; }

		public decimal LatestPrice { get; set; }

		public string LatestSource { get; set; }

		public string LatestTime { get; set; }

		public long LatestUpdate { get; set; }

		public long LatestVolume { get; set; }

		public decimal IexRealtimePrice { get; set; }

		public int IexRealtimeSize { get; set; }

		public long IexLastUpdated { get; set; }

		public decimal? DelayedPrice { get; set; }

		public long? DelayedPriceTime { get; set; }

		public decimal? OddLotDelayedPrice { get; set; }

		public long? OddLotDelayedPriceTime { get; set; }

		public decimal? ExtendedPrice { get; set; }

		public decimal? ExtendedChange { get; set; }

		public decimal? ExtendedChangePercent { get; set; }

		public long? ExtendedPriceTime { get; set; }

		public decimal PreviousClose { get; set; }

		public long PreviousVolume { get; set; }

		public decimal Change { get; set; }

		public decimal ChangePercent { get; set; }

		public long Volume { get; set; }

		public decimal? IexMarketPercent { get; set; }

		public int IexVolume { get; set; }

		public int AvgTotalVolume { get; set; }

		public decimal IexBidPrice { get; set; }

		public int IexBidSize { get; set; }

		public decimal IexAskPrice { get; set; }

		public int IexAskSize { get; set; }

		public decimal? IexOpen { get; set; }

		public long? IexOpenTime { get; set; }

		public decimal? IexClose { get; set; }

		public long? IexCloseTime { get; set; }

		public long MarketCap { get; set; }

		public decimal? PeRatio { get; set; }

		public decimal Week52High { get; set; }

		public decimal Week52Low { get; set; }

		public decimal YtdChange { get; set; }

		public long LastTradeTime { get; set; }

        public override string ToString()
        {
			return $"{Symbol},{LatestTime},{LatestPrice},{LatestVolume}";
		}
			
	}
}
