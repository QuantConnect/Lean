using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class UpdateAccountValueEventArgs : EventArgs
    {
        public string Key { get; private set; }
        public string Value { get; private set; }
        public string Currency { get; private set; }
        public string AccountName { get; private set; }
        public UpdateAccountValueEventArgs(string key, string value, string currency, string accountName)
        {
            Key = key;
            Value = value;
            Currency = currency;
            AccountName = accountName;
        }
    }
}