using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class ManagedAccountsEventArgs : EventArgs
    {
        public string AccountsList { get; private set; }
        public ManagedAccountsEventArgs(string accountsList)
        {
            AccountsList = accountsList;
        }
    }
}