namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickStringEventArgs : TickEventArgs
    {
        public string Value { get; private set; }
        public TickStringEventArgs(int tickerId, int field, string value)
            : base(tickerId, field)
        {
            Value = value;
        }
    }
}