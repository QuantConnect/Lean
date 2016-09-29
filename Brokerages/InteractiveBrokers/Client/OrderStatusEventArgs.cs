using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class OrderStatusEventArgs : EventArgs
    {
        public int OrderId { get; private set; }
        public string Status { get; private set; }
        public int Filled { get; private set; }
        public int Remaining { get; private set; }
        public double AverageFillPrice { get; private set; }
        public int PermId { get; private set; }
        public int Parentd { get; private set; }
        public double LastFillPrice { get; private set; }
        public int ClientId { get; private set; }
        public string WhyHeld { get; private set; }
        public OrderStatusEventArgs(int orderId, string status, int filled, int remaining, double averageFillPrice, int permId, int parentd, double lastFillPrice, int clientId, string whyHeld)
        {
            OrderId = orderId;
            Status = status;
            Filled = filled;
            Remaining = remaining;
            AverageFillPrice = averageFillPrice;
            PermId = permId;
            Parentd = parentd;
            LastFillPrice = lastFillPrice;
            ClientId = clientId;
            WhyHeld = whyHeld;
        }
    }
}