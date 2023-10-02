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

using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Defines the parameters for <see cref="ISetupHandler"/>
    /// </summary>
    public class SetupHandlerParameters
    {
        /// <summary>
        /// Gets the universe selection
        /// </summary>
        public UniverseSelection UniverseSelection { get; }

        /// <summary>
        /// Gets the algorithm
        /// </summary>
        public IAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the Brokerage
        /// </summary>
        public IBrokerage Brokerage { get; }

        /// <summary>
        /// Gets the algorithm node packet
        /// </summary>
        public AlgorithmNodePacket AlgorithmNodePacket { get; }

        /// <summary>
        /// Gets the algorithm node packet
        /// </summary>
        public IResultHandler ResultHandler { get; }

        /// <summary>
        /// Gets the TransactionHandler
        /// </summary>
        public ITransactionHandler TransactionHandler { get; }

        /// <summary>
        /// Gets the RealTimeHandler
        /// </summary>
        public IRealTimeHandler RealTimeHandler { get; }

        /// <summary>
        /// Gets the DataCacheProvider
        /// </summary>
        public IDataCacheProvider DataCacheProvider { get; }

        /// <summary>
        /// The map file provider instance of the algorithm
        /// </summary>
        public IMapFileProvider MapFileProvider { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="universeSelection">The universe selection instance</param>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">New brokerage output instance</param>
        /// <param name="algorithmNodePacket">Algorithm job task</param>
        /// <param name="resultHandler">The configured result handler</param>
        /// <param name="transactionHandler">The configured transaction handler</param>
        /// <param name="realTimeHandler">The configured real time handler</param>
        /// <param name="dataCacheProvider">The configured data cache provider</param>
        /// <param name="mapFileProvider">The map file provider</param>
        public SetupHandlerParameters(UniverseSelection universeSelection,
            IAlgorithm algorithm,
            IBrokerage brokerage,
            AlgorithmNodePacket algorithmNodePacket,
            IResultHandler resultHandler,
            ITransactionHandler transactionHandler,
            IRealTimeHandler realTimeHandler,
            IDataCacheProvider dataCacheProvider,
            IMapFileProvider mapFileProvider
            )
        {
            UniverseSelection = universeSelection;
            Algorithm = algorithm;
            Brokerage = brokerage;
            AlgorithmNodePacket = algorithmNodePacket;
            ResultHandler = resultHandler;
            TransactionHandler = transactionHandler;
            RealTimeHandler = realTimeHandler;
            DataCacheProvider = dataCacheProvider;
            MapFileProvider = mapFileProvider;
        }
    }
}
