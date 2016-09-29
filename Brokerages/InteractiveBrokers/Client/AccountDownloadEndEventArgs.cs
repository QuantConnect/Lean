using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class AccountDownloadEndEventArgs : EventArgs
    {
        public string Accunt { get; private set; }
        public AccountDownloadEndEventArgs(string accunt)
        {
            Accunt = accunt;
        }
    }
}