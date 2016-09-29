using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class OpenOrderEventArgs : EventArgs
    {
        public int OrderId { get; private set; }
        public Contract Contract { get; private set; }
        public Order Order { get; private set; }
        public OrderState OrderState { get; private set; }
        public OpenOrderEventArgs(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OrderId = orderId;
            Contract = contract;
            Order = order;
            OrderState = orderState;
        }
    }
}