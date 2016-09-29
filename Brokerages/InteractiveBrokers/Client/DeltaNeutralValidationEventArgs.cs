using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class DeltaNeutralValidationEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public UnderComp UnderComp { get; private set; }
        public DeltaNeutralValidationEventArgs(int requestId, UnderComp underComp)
        {
            RequestId = requestId;
            UnderComp = underComp;
        }
    }
}