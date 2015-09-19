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
        private readonly IHistoryProvider _historyProvider;

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
        /// Gets the history provider used to process historical data requests within the algorithm
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get { return _historyProvider; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanEngineAlgorithmHandlers"/> class from the specified handlers
        /// </summary>
        /// <param name="results">The result handler for communicating results from the algorithm</param>
        /// <param name="setup">The setup handler used to initialize algorithm state</param>
        /// <param name="dataFeed">The data feed handler used to pump data to the algorithm</param>
        /// <param name="transactions">The transaction handler used to process orders from the algorithm</param>
        /// <param name="realTime">The real time handler used to process real time events</param>
        /// <param name="historyProvider">The history provider used to process historical data requests</param>
        public LeanEngineAlgorithmHandlers(IResultHandler results,
            ISetupHandler setup,
            IDataFeed dataFeed,
            ITransactionHandler transactions,
            IRealTimeHandler realTime,
            IHistoryProvider historyProvider)
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
            if (historyProvider == null)
            {
                throw new ArgumentNullException("realTime");
            }
            _results = results;
            _setup = setup;
            _dataFeed = dataFeed;
            _transactions = transactions;
            _realTime = realTime;
            _historyProvider = historyProvider;
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
            var resultHandlerTypeName = Config.Get("result-handler", "ConsoleResultHandler");
            var historyProviderTypeName = Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider");

            return new LeanEngineAlgorithmHandlers(
                composer.GetExportedValueByTypeName<IResultHandler>(resultHandlerTypeName),
                composer.GetExportedValueByTypeName<ISetupHandler>(setupHandlerTypeName),
                composer.GetExportedValueByTypeName<IDataFeed>(dataFeedHandlerTypeName),
                composer.GetExportedValueByTypeName<ITransactionHandler>(transactionHandlerTypeName),
                composer.GetExportedValueByTypeName<IRealTimeHandler>(realTimeHandlerTypeName),
                composer.GetExportedValueByTypeName<IHistoryProvider>(historyProviderTypeName)
                );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Setup.Dispose();
        }
    }
}