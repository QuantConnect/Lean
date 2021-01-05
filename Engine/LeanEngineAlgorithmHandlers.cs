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
using QuantConnect.Lean.Engine.Alpha;
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
        /// <summary>
        /// Gets the result handler used to communicate results from the algorithm
        /// </summary>
        public IResultHandler Results { get; }

        /// <summary>
        /// Gets the setup handler used to initialize the algorithm state
        /// </summary>
        public ISetupHandler Setup { get; }

        /// <summary>
        /// Gets the data feed handler used to provide data to the algorithm
        /// </summary>
        public IDataFeed DataFeed { get; }

        /// <summary>
        /// Gets the transaction handler used to process orders from the algorithm
        /// </summary>
        public ITransactionHandler Transactions { get; }

        /// <summary>
        /// Gets the real time handler used to process real time events
        /// </summary>
        public IRealTimeHandler RealTime { get; }

        /// <summary>
        /// Gets the map file provider used as a map file source for the data feed
        /// </summary>
        public IMapFileProvider MapFileProvider { get; }

        /// <summary>
        /// Gets the map file provider used as a map file source for the data feed
        /// </summary>
        public IFactorFileProvider FactorFileProvider { get; }

        /// <summary>
        /// Gets the data file provider used to retrieve security data if it is not on the file system
        /// </summary>
        public IDataProvider DataProvider { get; }

        /// <summary>
        /// Gets the alpha handler used to process algorithm generated insights
        /// </summary>
        public IAlphaHandler Alphas { get; }

        /// <summary>
        /// Gets the object store used for persistence
        /// </summary>
        public IObjectStore ObjectStore { get; }

        /// <summary>
        /// Entity in charge of handling data permissions
        /// </summary>
        public IDataPermissionManager DataPermissionsManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanEngineAlgorithmHandlers"/> class from the specified handlers
        /// </summary>
        /// <param name="results">The result handler for communicating results from the algorithm</param>
        /// <param name="setup">The setup handler used to initialize algorithm state</param>
        /// <param name="dataFeed">The data feed handler used to pump data to the algorithm</param>
        /// <param name="transactions">The transaction handler used to process orders from the algorithm</param>
        /// <param name="realTime">The real time handler used to process real time events</param>
        /// <param name="mapFileProvider">The map file provider used to retrieve map files for the data feed</param>
        /// <param name="factorFileProvider">Map file provider used as a map file source for the data feed</param>
        /// <param name="dataProvider">file provider used to retrieve security data if it is not on the file system</param>
        /// <param name="alphas">The alpha handler used to process generated insights</param>
        /// <param name="objectStore">The object store used for persistence</param>
        /// <param name="dataPermissionsManager">The data permission manager to use</param>
        public LeanEngineAlgorithmHandlers(IResultHandler results,
            ISetupHandler setup,
            IDataFeed dataFeed,
            ITransactionHandler transactions,
            IRealTimeHandler realTime,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider,
            IAlphaHandler alphas,
            IObjectStore objectStore,
            IDataPermissionManager dataPermissionsManager
            )
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }
            if (dataFeed == null)
            {
                throw new ArgumentNullException(nameof(dataFeed));
            }
            if (transactions == null)
            {
                throw new ArgumentNullException(nameof(transactions));
            }
            if (realTime == null)
            {
                throw new ArgumentNullException(nameof(realTime));
            }
            if (mapFileProvider == null)
            {
                throw new ArgumentNullException(nameof(mapFileProvider));
            }
            if (factorFileProvider == null)
            {
                throw new ArgumentNullException(nameof(factorFileProvider));
            }
            if (dataProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProvider));
            }
            if (alphas == null)
            {
                throw new ArgumentNullException(nameof(alphas));
            }
            if (objectStore == null)
            {
                throw new ArgumentNullException(nameof(objectStore));
            }
            if (dataPermissionsManager == null)
            {
                throw new ArgumentNullException(nameof(dataPermissionsManager));
            }

            Results = results;
            Setup = setup;
            DataFeed = dataFeed;
            Transactions = transactions;
            RealTime = realTime;
            MapFileProvider = mapFileProvider;
            FactorFileProvider = factorFileProvider;
            DataProvider = dataProvider;
            Alphas = alphas;
            ObjectStore = objectStore;
            DataPermissionsManager = dataPermissionsManager;
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
            var alphaHandlerTypeName = Config.Get("alpha-handler", "DefaultAlphaHandler");
            var objectStoreTypeName = Config.Get("object-store", "LocalObjectStore");
            var dataPermissionManager = Config.Get("data-permission-manager", "DataPermissionManager");

            return new LeanEngineAlgorithmHandlers(
                composer.GetExportedValueByTypeName<IResultHandler>(resultHandlerTypeName),
                composer.GetExportedValueByTypeName<ISetupHandler>(setupHandlerTypeName),
                composer.GetExportedValueByTypeName<IDataFeed>(dataFeedHandlerTypeName),
                composer.GetExportedValueByTypeName<ITransactionHandler>(transactionHandlerTypeName),
                composer.GetExportedValueByTypeName<IRealTimeHandler>(realTimeHandlerTypeName),
                composer.GetExportedValueByTypeName<IMapFileProvider>(mapFileProviderTypeName),
                composer.GetExportedValueByTypeName<IFactorFileProvider>(factorFileProviderTypeName),
                composer.GetExportedValueByTypeName<IDataProvider>(dataProviderTypeName),
                composer.GetExportedValueByTypeName<IAlphaHandler>(alphaHandlerTypeName),
                composer.GetExportedValueByTypeName<IObjectStore>(objectStoreTypeName),
                composer.GetExportedValueByTypeName<IDataPermissionManager>(dataPermissionManager)
                );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Log.Trace("LeanEngineAlgorithmHandlers.Dispose(): start...");

            Setup.DisposeSafely();
            ObjectStore.DisposeSafely();

            Log.Trace("LeanEngineAlgorithmHandlers.Dispose(): Disposed of algorithm handlers.");
        }
    }
}