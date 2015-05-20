using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of order mapping
    /// </summary>
    public class OrderMapping : IOrderMapping
    {
        private static int _orderID;
        private readonly IList<Order> _orders;

        public OrderMapping(IList<Order> orders)
        {
            _orders = orders;
        }

        public OrderMapping()
        {
            _orders = new List<Order>();
        }

        public void Add(Order order)
        {
            order.Id = Interlocked.Increment(ref _orderID);
            _orders.Add(order);
        }

        public Order GetOrderById(int orderId)
        {
            return _orders.FirstOrDefault(x => x.Id == orderId);
        }

        public Order GetOrderByBrokerageId(int brokerageId)
        {
            return _orders.FirstOrDefault(x => x.BrokerId.Contains(brokerageId));
        }
    }
}