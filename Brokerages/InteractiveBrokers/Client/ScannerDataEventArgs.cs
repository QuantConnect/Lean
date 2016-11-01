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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.ScannerData"/> event
    /// </summary>
    public class ScannerDataEventArgs : EventArgs
    {
        /// <summary>
        /// The request's identifier.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// The ranking within the response of this bar.
        /// </summary>
        public int Rank { get; private set; }

        /// <summary>
        /// This structure contains a full description of the contract that was executed.
        /// </summary>
        public ContractDetails ContractDetails { get; private set; }

        /// <summary>
        /// Varies based on query.
        /// </summary>
        public string Distance { get; private set; }

        /// <summary>
        /// Varies based on query.
        /// </summary>
        public string Benchmark { get; private set; }

        /// <summary>
        /// Varies based on query.
        /// </summary>
        public string Projection { get; private set; }

        /// <summary>
        /// Describes combo legs when scan is returning EFP.
        /// </summary>
        public string LegsStr { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScannerDataEventArgs"/> class
        /// </summary>
        public ScannerDataEventArgs(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            RequestId = reqId;
            Rank = rank;
            ContractDetails = contractDetails;
            Distance = distance;
            Benchmark = benchmark;
            Projection = projection;
            LegsStr = legsStr;
        }
    }
}