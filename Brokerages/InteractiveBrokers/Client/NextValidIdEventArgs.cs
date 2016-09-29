using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class NextValidIdEventArgs : EventArgs
    {
        public int OrderId { get; private set; }
        public NextValidIdEventArgs(int orderId)
        {
            OrderId = orderId;
        }
    }
}