namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// When retrieving tick data, this represent the candle stick for that particular timeframe.
    /// </summary>
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
}
