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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
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
        private bool _dataMonitorWired;

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
        /// Gets the data file provider used to retrieve security data if it is not on the file system
        /// </summary>
        public IDataCacheProvider DataCacheProvider { get; }

        /// <summary>
        /// Gets the object store used for persistence
        /// </summary>
        public IObjectStore ObjectStore { get; }

        /// <summary>
        /// Entity in charge of handling data permissions
        /// </summary>
        public IDataPermissionManager DataPermissionsManager { get; }

        /// <summary>
        /// Monitors data requests and reports on missing data
        /// </summary>
        public IDataMonitor DataMonitor { get; }

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
        /// <param name="objectStore">The object store used for persistence</param>
        /// <param name="dataPermissionsManager">The data permission manager to use</param>
        /// <param name="liveMode">True for live mode, false otherwise</param>
        /// <param name="researchMode">True for research mode, false otherwise. This has less priority than liveMode</param>
        /// <param name="dataMonitor">Optionally the data monitor instance to use</param>
        public LeanEngineAlgorithmHandlers(IResultHandler results,
            ISetupHandler setup,
            IDataFeed dataFeed,
            ITransactionHandler transactions,
            IRealTimeHandler realTime,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider,
            IObjectStore objectStore,
            IDataPermissionManager dataPermissionsManager,
            bool liveMode,
            bool researchMode = false,
            IDataMonitor dataMonitor = null
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
            ObjectStore = objectStore;
            DataPermissionsManager = dataPermissionsManager;
            DataCacheProvider = new ZipDataCacheProvider(DataProvider, isDataEphemeral: liveMode);
            DataMonitor = dataMonitor ?? new DataMonitor();

            if (!liveMode && !researchMode)
            {
                _dataMonitorWired = true;
                DataProvider.NewDataRequest += DataMonitor.OnNewDataRequest;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LeanEngineAlgorithmHandlers"/> class from the specified composer using type names from configuration
        /// </summary>
        /// <param name="composer">The composer instance to obtain implementations from</param>
        /// <param name="researchMode">True for research mode, false otherwise</param>
        /// <returns>A fully hydrates <see cref="LeanEngineSystemHandlers"/> instance.</returns>
        /// <exception cref="CompositionException">Throws a CompositionException during failure to load</exception>
        public static LeanEngineAlgorithmHandlers FromConfiguration(Composer composer, bool researchMode = false)
        {
            var setupHandlerTypeName = Config.Get("setup-handler", "ConsoleSetupHandler");
            var transactionHandlerTypeName = Config.Get("transaction-handler", "BacktestingTransactionHandler");
            var realTimeHandlerTypeName = Config.Get("real-time-handler", "BacktestingRealTimeHandler");
            var dataFeedHandlerTypeName = Config.Get("data-feed-handler", "FileSystemDataFeed");
            var resultHandlerTypeName = Config.Get("result-handler", "BacktestingResultHandler");
            var mapFileProviderTypeName = Config.Get("map-file-provider", "LocalDiskMapFileProvider");
            var factorFileProviderTypeName = Config.Get("factor-file-provider", "LocalDiskFactorFileProvider");
            var dataProviderTypeName = Config.Get("data-provider", "DefaultDataProvider");
            var objectStoreTypeName = Config.Get("object-store", "LocalObjectStore");
            var dataPermissionManager = Config.Get("data-permission-manager", "DataPermissionManager");
            var dataMonitor = Config.Get("data-monitor", "QuantConnect.Data.DataMonitor");

            var result = new LeanEngineAlgorithmHandlers(
                composer.GetExportedValueByTypeName<IResultHandler>(resultHandlerTypeName),
                composer.GetExportedValueByTypeName<ISetupHandler>(setupHandlerTypeName),
                composer.GetExportedValueByTypeName<IDataFeed>(dataFeedHandlerTypeName),
                composer.GetExportedValueByTypeName<ITransactionHandler>(transactionHandlerTypeName),
                composer.GetExportedValueByTypeName<IRealTimeHandler>(realTimeHandlerTypeName),
                composer.GetExportedValueByTypeName<IMapFileProvider>(mapFileProviderTypeName),
                composer.GetExportedValueByTypeName<IFactorFileProvider>(factorFileProviderTypeName),
                composer.GetExportedValueByTypeName<IDataProvider>(dataProviderTypeName),
                composer.GetExportedValueByTypeName<IObjectStore>(objectStoreTypeName),
                composer.GetExportedValueByTypeName<IDataPermissionManager>(dataPermissionManager),
                Globals.LiveMode,
                researchMode,
                composer.GetExportedValueByTypeName<IDataMonitor>(dataMonitor)
                );

            result.FactorFileProvider.Initialize(result.MapFileProvider, result.DataProvider);
            result.MapFileProvider.Initialize(result.DataProvider);

            if (result.DataProvider is ApiDataProvider
                && (result.FactorFileProvider is not LocalZipFactorFileProvider || result.MapFileProvider is not LocalZipMapFileProvider))
            {
                throw new ArgumentException($"The {typeof(ApiDataProvider)} can only be used with {typeof(LocalZipFactorFileProvider)}" +
                    $" and {typeof(LocalZipMapFileProvider)}, please update 'config.json'");
            }

            FundamentalService.Initialize(result.DataProvider, Globals.LiveMode);

            return result;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Log.Trace("LeanEngineAlgorithmHandlers.Dispose(): start...");

            DataCacheProvider.DisposeSafely();
            Setup.DisposeSafely();
            ObjectStore.DisposeSafely();
            if (_dataMonitorWired)
            {
                DataProvider.NewDataRequest -= DataMonitor.OnNewDataRequest;
            }
            DataMonitor.DisposeSafely();

            Log.Trace("LeanEngineAlgorithmHandlers.Dispose(): Disposed of algorithm handlers.");
        }
    }
}
