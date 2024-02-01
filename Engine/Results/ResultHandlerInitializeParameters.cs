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
 *
*/

using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.TransactionHandlers;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// DTO parameters class to initialize a result handler
    /// </summary>
    public class ResultHandlerInitializeParameters
    {
        /// <summary>
        /// The algorithm job
        /// </summary>
        public AlgorithmNodePacket Job { get; set; }

        /// <summary>
        /// The messaging handler
        /// </summary>
        public IMessagingHandler MessagingHandler { get; set; }

        /// <summary>
        /// The Api instance
        /// </summary>
        public IApi Api { get; set; }

        /// <summary>
        /// The transaction handler
        /// </summary>
        public ITransactionHandler TransactionHandler { get; set; }

        /// <summary>
        /// The map file provider instance to use
        /// </summary>
        public IMapFileProvider MapFileProvider { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ResultHandlerInitializeParameters(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler, IMapFileProvider mapFileProvider)
        {
            Job = job;
            Api = api;
            MapFileProvider = mapFileProvider;
            MessagingHandler = messagingHandler;
            TransactionHandler = transactionHandler;
        }
    }
}
