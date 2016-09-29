using System;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class UpdateAccountTimeEventArgs : EventArgs
    {
        public string Timestamp { get; private set; }
        public UpdateAccountTimeEventArgs(string timestamp)
        {
            Timestamp = timestamp;
        }
    }
}