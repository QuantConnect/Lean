using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class PositionEventArgs : EventArgs
    {
        public string Account { get; private set; }
        public Contract Contract { get; private set; }
        public int Position { get; private set; }
        public double AverageCost { get; private set; }
        public PositionEventArgs(string account, Contract contract, int position, double averageCost)
        {
            Account = account;
            Contract = contract;
            Position = position;
            AverageCost = averageCost;
        }
    }
}