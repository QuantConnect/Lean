using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class CurrentTimeEventArgs : EventArgs
    {
        public DateTime CurrentTimeUtc { get; private set; }
        public CurrentTimeEventArgs(DateTime currentTimeUtc)
        {
            CurrentTimeUtc = currentTimeUtc;
        }
    }
}
