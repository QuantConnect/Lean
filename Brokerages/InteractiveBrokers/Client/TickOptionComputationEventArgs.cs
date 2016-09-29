namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickOptionComputationEventArgs : TickEventArgs
    {
        public double ImpliedVolatility { get; private set; }
        public double Delta { get; private set; }
        public double OptionPrice { get; private set; }
        public double PvDividend { get; private set; }
        public double Gamma { get; private set; }
        public double Vega { get; private set; }
        public double Theta { get; private set; }
        public double UnderlyingPrice { get; private set; }
        public TickOptionComputationEventArgs(int tickerId, int field, double impliedVolatility, double delta, double optionPrice, double pvDividend, double gamma, double vega, double theta, double underlyingPrice)
            : base(tickerId, field)
        {
            ImpliedVolatility = impliedVolatility;
            Delta = delta;
            OptionPrice = optionPrice;
            PvDividend = pvDividend;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            UnderlyingPrice = underlyingPrice;
        }
    }
}