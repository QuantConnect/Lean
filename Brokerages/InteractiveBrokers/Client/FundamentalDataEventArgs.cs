using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class FundamentalDataEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public string Data { get; private set; }
        public FundamentalDataEventArgs(int requestId, string data)
        {
            RequestId = requestId;
            Data = data;
        }
    }
}