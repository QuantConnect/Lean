using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public class UpdatePortfolioEventArgs : EventArgs
    {
        public Contract Contract { get; private set; }
        public int Position { get; private set; }
        public double MarketPrice { get; private set; }
        public double MarketValue { get; private set; }
        public double AverageCost { get; private set; }
        public double UnrealisedPNL { get; private set; }
        public double RealisedPNL { get; private set; }
        public string AccountName { get; private set; }
        public UpdatePortfolioEventArgs(Contract contract, int position, double marketPrice, double marketValue, double averageCost, double unrealisedPnl, double realisedPnl, string accountName)
        {
            Contract = contract;
            Position = position;
            MarketPrice = marketPrice;
            MarketValue = marketValue;
            AverageCost = averageCost;
            UnrealisedPNL = unrealisedPnl;
            RealisedPNL = realisedPnl;
            AccountName = accountName;
        }
    }
}