using System.Collections.Generic;

namespace QuantConnect.Brokerages.Oanda.DataType.Communications
{
    public class OrdersResponse
    {
        public List<Order> orders;
        public string nextPage;
    }
}
