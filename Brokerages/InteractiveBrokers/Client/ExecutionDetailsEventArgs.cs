/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.ExecutionDetails"/> event
    /// </summary>
    public sealed class ExecutionDetailsEventArgs : EventArgs
    {
        /// <summary>
        /// The request's identifier.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// This structure contains a full description of the contract that was executed.
        /// </summary>
        public Contract Contract { get; private set; }

        /// <summary>
        /// This structure contains addition order execution details.
        /// </summary>
        public Execution Execution { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionDetailsEventArgs"/> class
        /// </summary>
        public ExecutionDetailsEventArgs(int requestId, Contract contract, Execution execution)
        {
            RequestId = requestId;
            Contract = contract;
            Execution = execution;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                "RequestId: {0}, Symbol: {1}, OrderId: {2}, Time: {3}, Side: {4}, Shares: {5}, Price: {6}, CumQty: {7}, PermId: {8}",
                RequestId, Contract.Symbol, Execution.OrderId, Execution.Time, Execution.Side, Execution.Shares, Execution.Price, Execution.CumQty, Execution.PermId);
        }
    }
}