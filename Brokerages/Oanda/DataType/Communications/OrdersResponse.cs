using System.Collections.Generic;
using QuantConnect.Brokerages.Oanda.DataType;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
    public class OrdersResponse
    {
        public List<Order> orders;
        public string nextPage;
    }
}
