namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickPriceEventArgs : TickEventArgs
    {
        public double Price { get; private set; }
        public int CanAutoExecute { get; private set; }
        public TickPriceEventArgs(int tickerId, int field, double price, int canAutoExecute)
            :base(tickerId, field)
        {
            Price = price;
            CanAutoExecute = canAutoExecute;
        }
    }
}