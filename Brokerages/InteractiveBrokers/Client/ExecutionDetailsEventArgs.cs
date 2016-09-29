using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class ExecutionDetailsEventArgs : EventArgs
    {
        public int RequestId { get; private set; }
        public ExecutionDetails ExecutionDetails { get; private set; }
        public ExecutionDetailsEventArgs(int requestId, Contract contract, Execution execution)
        {
            RequestId = requestId;
            ExecutionDetails = new ExecutionDetails(contract, execution);
        }
    }
}