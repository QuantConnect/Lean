using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public class HistoricalDataEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public string Date { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public int Volume { get; private set; }
        public int Count { get; private set; }
        public double WAP { get; private set; }
        public bool HasGaps { get; private set; }
        public HistoricalDataEventArgs(int requestId, string date, double open, double high, double low, double close, int volume, int count, double wap, bool hasGaps)
        {
            RequestId = requestId;
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Count = count;
            WAP = wap;
            HasGaps = hasGaps;
        }
    }
}