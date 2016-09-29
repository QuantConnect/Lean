using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class TickEfpEventArgs : EventArgs
    {
        public int TickerId { get; private set; }
        public int TickType { get; private set; }
        public double BasisPoints { get; private set; }
        public string FormattedBasisPoints { get; private set; }
        public double ImpliedFuture { get; private set; }
        public int HoldDays { get; private set; }
        public string FutureExpiry { get; private set; }
        public double DividendImpact { get; private set; }
        public double DividendsToExpiry { get; private set; }
        public TickEfpEventArgs(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            TickerId = tickerId;
            TickType = tickType;
            BasisPoints = basisPoints;
            FormattedBasisPoints = formattedBasisPoints;
            ImpliedFuture = impliedFuture;
            HoldDays = holdDays;
            FutureExpiry = futureExpiry;
            DividendImpact = dividendImpact;
            DividendsToExpiry = dividendsToExpiry;
        }
    }
}