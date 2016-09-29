using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class RequestEndEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public RequestEndEventArgs(int requestId)
        {
            RequestId = requestId;
        }
    }
}