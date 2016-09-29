using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class AccountSummaryEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public string Account { get; private set; }
        public string Tag { get; private set; }
        public string Value { get; private set; }
        public string Currency { get; private set; }
        public AccountSummaryEventArgs(int requestId, string account, string tag, string value, string currency)
        {
            RequestId = requestId;
            Account = account;
            Tag = tag;
            Value = value;
            Currency = currency;
        }
    }
}