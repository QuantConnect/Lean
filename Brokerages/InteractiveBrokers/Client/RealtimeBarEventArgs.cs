using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class RealtimeBarEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public long Time { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public long Volume { get; private set; }
        public double WAP { get; private set; }
        public int Count { get; private set; }
        public RealtimeBarEventArgs(int requestId, long time, double open, double high, double low, double close, long volume, double wap, int count)
        {
            RequestId = requestId;
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            WAP = wap;
            Count = count;
        }
    }
}