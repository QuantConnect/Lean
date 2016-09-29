using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class BondContractDetailsEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public ContractDetails Contract { get; private set; }
        public BondContractDetailsEventArgs(int requestId, ContractDetails contract)
        {
            RequestId = requestId;
            Contract = contract;
        }
    }
}