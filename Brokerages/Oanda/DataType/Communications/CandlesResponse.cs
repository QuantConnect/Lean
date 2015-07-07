using System.Collections.Generic;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    public class CandlesResponse
    {
	    public string instrument;
	    public string granularity;
        public List<Candle> candles;
    }
}
