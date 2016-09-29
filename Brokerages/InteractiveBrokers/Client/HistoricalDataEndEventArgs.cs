using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class HistoricalDataEndEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public string Start { get; private set; }
        public string End { get; private set; }
        public HistoricalDataEndEventArgs(int requestId, string start, string end)
        {
            RequestId = requestId;
            Start = start;
            End = end;
        }
    }
}