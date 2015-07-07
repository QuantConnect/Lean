using System.Collections.Generic;
using OANDARestLibrary.TradeLibrary.DataTypes;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    public class TradesResponse
    {
        public List<TradeData> trades;
        public string nextPage;
    }
}
