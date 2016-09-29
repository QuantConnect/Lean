using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public abstract class TickEventArgs : EventArgs
    {
        public int TickerId { get; private set; }
        public int Field { get; private set; }
        protected TickEventArgs(int tickerId, int field)
        {
            TickerId = tickerId;
            Field = field;
        }
    }
}