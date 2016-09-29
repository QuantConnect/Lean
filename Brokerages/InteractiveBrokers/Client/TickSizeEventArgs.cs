namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickSizeEventArgs : TickEventArgs
    {
        public int Size { get; private set; }
        public TickSizeEventArgs(int tickerId, int field, int size)
            : base(tickerId, field)
        {
            Size = size;
        }
    }
}