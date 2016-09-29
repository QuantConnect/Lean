using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class MarketDataTypeEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public int MarketDataType { get; private set; }
        public MarketDataTypeEventArgs(int requestId, int marketDataType)
        {
            RequestId = requestId;
            MarketDataType = marketDataType;
        }
    }
}