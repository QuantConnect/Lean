using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class ContractDetailsEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public ContractDetails ContractDetails { get; private set; }
        public ContractDetailsEventArgs(int requestId, ContractDetails contractDetails)
        {
            RequestId = requestId;
            ContractDetails = contractDetails;
        }
    }
}