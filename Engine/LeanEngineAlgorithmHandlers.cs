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

using System;
using System.ComponentModel.Composition;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Provides a container for the algorithm specific handlers
    /// </summary>
    public class LeanEngineAlgorithmHandlers : IDisposable
    {
        private readonly IDataFeed _dataFeed;
        private readonly ISetupHandler _setup;
        private readonly IResultHandler _results;
        private readonly IRealTimeHandler _realTime;
        private readonly ITransactionHandler _transactions;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly IFactorFileProvider _factorFileProvider;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Gets the result handler used to communicate results from the algorithm
        /// </summary>
        public IResultHandler Results
        {
            get { return _results; }
        }

        /// <summary>
        /// Gets the setup handler used to initialize the algorithm state
        /// </summary>
        public ISetupHandler Setup
        {
            get { return _setup; }
        }

        /// <summary>
        /// Gets the data feed handler used to provide data to the algorithm
        /// </summary>
        public IDataFeed DataFeed
        {
            get { return _dataFeed; }
        }

        /// <summary>
        /// Gets the transaction handler used to process orders from the algorithm
        /// </summary>
        public ITransactionHandler Transactions
        {
            get { return _transactions; }
        }

        /// <summary>
        /// Gets the real time handler used to process real time events
        /// </summary>
        public IRealTimeHandler RealTime
        {
            get { return _realTime; }
        }

        /// <summary>
        /// Gets the map file provider used as a map file source for the data feed
        /// </summary>
        public IMapFileProvider MapFileProvider
        {
            get { return _mapFileProvider; }
        }

        /// <summary>
        /// Gets the map file provider used as a map file source for the data feed
        /// </summary>
        public IFactorFileProvider FactorFileProvider
        {
            get { return _factorFileProvider; }
        }

        /// <summary>
        /// Gets the data file provider used to retrieve security data if it is not on the file system
        /// </summary>
        public IDataProvider DataProvider
        {
            get { return _dataProvider; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanEngineAlgorithmHandlers"/> class from the specified handlers
        /// </summary>
        /// <param name="results">The result handler for communicating results from the algorithm</param>
        /// <param name="setup">The setup handler used to initialize algorithm state</param>
        /// <param name="dataFeed">The data feed handler used to pump data to the algorithm</param>
        /// <param name="transactions">The transaction handler used to process orders from the algorithm</param>
        /// <param name="realTime">The real time handler used to process real time events</param>
        /// <param name="commandQueue">The command queue handler used to receive external commands for the algorithm</param>
        /// <param name="mapFileProvider">The map file provider used to retrieve map files for the data feed</param>
        /// <param name="factorFileProvider">Map file provider used as a map file source for the data feed</param>
        /// <param name="dataProvider">file provider used to retrieve security data if it is not on the file system</param>
        public LeanEngineAlgorithmHandlers(IResultHandler results,
            ISetupHandler setup,
            IDataFeed dataFeed,
            ITransactionHandler transactions,
            IRealTimeHandler realTime,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider
            )
        {
            if (results == null)
            {
                throw new ArgumentNullException("results");
            }
            if (setup == null)
            {
                throw new ArgumentNullException("setup");
            }
            if (dataFeed == null)
            {
                throw new ArgumentNullException("dataFeed");
            }
            if (transactions == null)
            {
                throw new ArgumentNullException("transactions");
            }
            if (realTime == null)
            {
                throw new ArgumentNullException("realTime");
            }
            if (mapFileProvider == null)
            {
                throw new ArgumentNullException("mapFileProvider");
            }
            if (factorFileProvider == null)
            {
                throw new ArgumentNullException("factorFileProvider");
            }
            if (dataProvider == null)
            {
                throw new ArgumentNullException("dataProvider");
            }
            _results = results;
            _setup = setup;
            _dataFeed = dataFeed;
            _transactions = transactions;
            _realTime = realTime;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataProvider = dataProvider;
        }
        
        /// <summary>
        /// Creates a new instance of the <see cref="LeanEngineAlgorithmHandlers"/> class from the specified composer using type names from configuration
        /// </summary>
        /// <param name="composer">The composer instance to obtain implementations from</param>
        /// <returns>A fully hydrates <see cref="LeanEngineSystemHandlers"/> instance.</returns>
        /// <exception cref="CompositionException">Throws a CompositionException during failure to load</exception>
        public static LeanEngineAlgorithmHandlers FromConfiguration(Composer composer)
        {
            var setupHandlerTypeName = Config.Get("setup-handler", "ConsoleSetupHandler");
            var transactionHandlerTypeName = Config.Get("transaction-handler", "BacktestingTransactionHandler");
            var realTimeHandlerTypeName = Config.Get("real-time-handler", "BacktestingRealTimeHandler");
            var dataFeedHandlerTypeName = Config.Get("data-feed-handler", "FileSystemDataFeed");
            var resultHandlerTypeName = Config.Get("result-handler", "BacktestingResultHandler");
            var mapFileProviderTypeName = Config.Get("map-file-provider", "LocalDiskMapFileProvider");
            var factorFileProviderTypeName = Config.Get("factor-file-provider", "LocalDiskFactorFileProvider");
            var dataProviderTypeName = Config.Get("data-provider", "DefaultDataProvider");

            return new LeanEngineAlgorithmHandlers(
                composer.GetExportedValueByTypeName<IResultHandler>(resultHandlerTypeName),
                composer.GetExportedValueByTypeName<ISetupHandler>(setupHandlerTypeName),
                composer.GetExportedValueByTypeName<IDataFeed>(dataFeedHandlerTypeName),
                composer.GetExportedValueByTypeName<ITransactionHandler>(transactionHandlerTypeName),
                composer.GetExportedValueByTypeName<IRealTimeHandler>(realTimeHandlerTypeName),
                composer.GetExportedValueByTypeName<IMapFileProvider>(mapFileProviderTypeName),
                composer.GetExportedValueByTypeName<IFactorFileProvider>(factorFileProviderTypeName),
                composer.GetExportedValueByTypeName<IDataProvider>(dataProviderTypeName)
                );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Setup.Dispose();

            Log.Trace("LeanEngineAlgorithmHandlers.Dispose(): Disposed of algorithm handlers.");
        }
    }
}