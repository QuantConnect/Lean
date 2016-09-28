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

using System.Threading;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    public class ExecutionDetails
    {
        public ExecutionDetails(int requestId, Contract contract, Execution execution)
        {
            this.RequestId = requestId;
            this.Contract = contract;
            this.Execution = execution;
            this.ExecutionDetailsResetEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Empty Constructor for the Executions
        /// </summary>
        public ExecutionDetails() { }
        
        /// <summary>
        /// Request Id
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// This structure contains a full description of the contract that was executed.
        /// </summary>
        /// <seealso cref="Contract"/>
        public Contract Contract { get; set; }

        /// <summary>
        /// This structure contains addition order execution details.
        /// </summary>
        /// <seealso cref="Execution"/>
        public Execution Execution { get; set; }

        /// <summary>
        /// Reset Event for each Execution for a specific request id
        /// </summary>
        public ManualResetEvent ExecutionDetailsResetEvent { get; set; }
    }
    
}
