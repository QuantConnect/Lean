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
    /// Event arguments class for the following events:
    /// <see cref="InteractiveBrokersClient.ContractDetails"/>
    /// <see cref="InteractiveBrokersClient.BondContractDetails"/>
    /// </summary>
    public sealed class ContractDetailsEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the data request. Ensures that responses are matched to requests if several requests are in process.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// This structure contains a full description of the contract being looked up.
        /// </summary>
        public ContractDetails ContractDetails { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractDetailsEventArgs"/> class
        /// </summary>
        public ContractDetailsEventArgs(int requestId, ContractDetails contractDetails)
        {
            RequestId = requestId;
            ContractDetails = contractDetails;
        }
    }
}