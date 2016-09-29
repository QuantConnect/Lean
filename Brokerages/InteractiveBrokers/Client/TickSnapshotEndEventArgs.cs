using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickSnapshotEndEventArgs : EventArgs
    {
        public int TickerId { get; private set; }
        public TickSnapshotEndEventArgs(int tickerId)
        {
            TickerId = tickerId;
        }
    }
}