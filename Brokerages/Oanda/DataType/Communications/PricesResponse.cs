using System.Collections.Generic;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    public class PricesResponse
    {
        public long time;
        public List<Price> prices;
    }
}
