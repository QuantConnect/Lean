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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Represents the set of parameters for the <see cref="IResultHandler.Initialize"/> method
    /// </summary>
    public class ResultHandlerInitializeParameters
    {
        /// <summary>
        /// The job
        /// </summary>
        public AlgorithmNodePacket Job { get; }

        /// <summary>
        /// The messaging handler provider to use
        /// </summary>
        public IMessagingHandler MessagingHandler { get; }

        /// <summary>
        /// The API instance
        /// </summary>
        public IApi Api { get; }

        /// <summary>
        /// The transaction handler to use
        /// </summary>
        public ITransactionHandler TransactionHandler { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultHandlerInitializeParameters"/> class from the specified parameters
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="messagingHandler">The messaging handler provider to use</param>
        /// <param name="api">The API instance</param>
        /// <param name="transactionHandler">The transaction handler to use</param>
        /// <param name="dataMonitor">The data monitor used to watch for data requests and report on missing files</param>
        public ResultHandlerInitializeParameters(
            AlgorithmNodePacket job, 
            IMessagingHandler messagingHandler, 
            IApi api, 
            ITransactionHandler transactionHandler)
        {
            Job = job;
            MessagingHandler = messagingHandler;
            Api = api;
            TransactionHandler = transactionHandler;
        }
    }
}
