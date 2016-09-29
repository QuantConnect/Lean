namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickGenericEventArgs : TickEventArgs
    {
        public double Value { get; private set; }
        public TickGenericEventArgs(int tickerId, int field, double value)
            : base(tickerId, field)
        {
            Value = value;
        }
    }
}