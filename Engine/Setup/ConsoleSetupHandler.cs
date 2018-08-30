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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Console setup handler to initialize and setup the Lean Engine properties for a local backtest
    /// </summary>
    public class ConsoleSetupHandler : ISetupHandler
    {
        /// <summary>
        /// The worker thread instance the setup handler should use
        /// </summary>
        public WorkerThread WorkerThread { get; set; }

        /// <summary>
        /// Error which occured during setup may appear here.
        /// </summary>
        public List<Exception> Errors { get; set; }

        /// <summary>
        /// Maximum runtime of the strategy. (Set to 10 years for local backtesting).
        /// </summary>
        public TimeSpan MaximumRuntime { get; }

        /// <summary>
        /// Starting capital for the algorithm (Loaded from the algorithm code).
        /// </summary>
        public decimal StartingPortfolioValue { get; private set; }

        /// <summary>
        /// Start date for the backtest.
        /// </summary>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for this backtest.
        /// </summary>
        public int MaxOrders { get; }

        /// <summary>
        /// Setup the algorithm data, cash, job start end date etc:
        /// </summary>
        public ConsoleSetupHandler()
        {
            MaxOrders = int.MaxValue;
            StartingPortfolioValue = 0;
            StartingDate = new DateTime(1998, 01, 01);
            MaximumRuntime = TimeSpan.FromDays(10 * 365);
            Errors = new List<Exception>();
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <param name="algorithmNodePacket">Details of the task required</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        public IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;
            var algorithmName = Config.Get("algorithm-type-name");

            var debugNode = algorithmNodePacket as BacktestNodePacket;
            var debugging = debugNode != null && debugNode.IsDebugging || Config.GetBool("debugging", false);

            if (debugging && !BaseSetupHandler.InitializeDebugging(algorithmNodePacket, WorkerThread))
            {
                throw new AlgorithmSetupException("Failed to initialize debugging");
            }

            // don't force load times to be fast here since we're running locally, this allows us to debug
            // and step through some code that may take us longer than the default 10 seconds
            var loader = new Loader(debugging, algorithmNodePacket.Language, TimeSpan.FromHours(1), names => names.SingleOrDefault(name => MatchTypeName(name, algorithmName)), WorkerThread);
            var complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, algorithmNodePacket.RamAllocation, out algorithm, out error);
            if (!complete) throw new AlgorithmSetupException($"During the algorithm initialization, the following exception has occurred: {error}");

            return algorithm;
        }

        /// <summary>
        /// Creates a new <see cref="BacktestingBrokerage"/> instance
        /// </summary>
        /// <param name="algorithmNodePacket">Job packet</param>
        /// <param name="uninitializedAlgorithm">The algorithm instance before Initialize has been called</param>
        /// <param name="factory">The brokerage factory</param>
        /// <returns>The brokerage instance, or throws if error creating instance</returns>
        public IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory)
        {
            factory = new BacktestingBrokerageFactory();
            var optionMarketSimulation = new BasicOptionAssignmentSimulation();

            return new BacktestingBrokerage(uninitializedAlgorithm, optionMarketSimulation);
        }

        /// <summary>
        /// Setup the algorithm cash, dates and portfolio as desired.
        /// </summary>
        /// <param name="parameters">The parameters object to use</param>
        /// <returns>Boolean true on successfully setting up the console.</returns>
        public bool Setup(SetupHandlerParameters parameters)
        {
            var algorithm = parameters.Algorithm;
            var baseJob = parameters.AlgorithmNodePacket;
            var initializeComplete = false;
            try
            {
                //Set common variables for console programs:

                if (baseJob.Type == PacketType.BacktestNode)
                {
                    var backtestJob = baseJob as BacktestNodePacket;
                    if (backtestJob == null)
                    {
                        throw new ArgumentException("Expected BacktestNodePacket but received " + baseJob.GetType().Name);
                    }

                    algorithm.SetMaximumOrders(int.MaxValue);

                    // set our parameters
                    algorithm.SetParameters(baseJob.Parameters);
                    algorithm.SetLiveMode(false);
                    algorithm.SetAvailableDataTypes(GetConfiguredDataFeeds());

                    //Set the source impl for the event scheduling
                    algorithm.Schedule.SetEventSchedule(parameters.RealTimeHandler);

                    // set the option chain provider
                    algorithm.SetOptionChainProvider(new CachingOptionChainProvider(new BacktestingOptionChainProvider()));

                    // set the future chain provider
                    algorithm.SetFutureChainProvider(new CachingFutureChainProvider(new BacktestingFutureChainProvider()));

                    // set the object store
                    algorithm.SetObjectStore(parameters.ObjectStore);

                    var isolator = new Isolator();
                    isolator.ExecuteWithTimeLimit(TimeSpan.FromMinutes(5),
                        () =>
                        {
                            //Setup Base Algorithm:
                            algorithm.Initialize();
                        }, baseJob.Controls.RamAllocation,
                        sleepIntervalMillis: 50,
                        workerThread: WorkerThread);

                    // set start and end date if present in the job
                    if (backtestJob.PeriodStart.HasValue)
                    {
                        algorithm.SetStartDate(backtestJob.PeriodStart.Value);
                    }
                    if (backtestJob.PeriodFinish.HasValue)
                    {
                        algorithm.SetEndDate(backtestJob.PeriodFinish.Value);
                    }

                    //Finalize Initialization
                    algorithm.PostInitialize();

                    //Set the time frontier of the algorithm
                    algorithm.SetDateTime(algorithm.StartDate.ConvertToUtc(algorithm.TimeZone));

                    //Backtest Specific Parameters:
                    StartingDate = algorithm.StartDate;

                    BaseSetupHandler.SetupCurrencyConversions(algorithm, parameters.UniverseSelection);
                    StartingPortfolioValue = algorithm.Portfolio.Cash;

                    // we set the free portfolio value based on the initial total value and the free percentage value
                    algorithm.Settings.FreePortfolioValue =
                        algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;
                }
                else
                {
                    throw new Exception("The ConsoleSetupHandler is for backtests only. Use the BrokerageSetupHandler.");
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                Errors.Add(new AlgorithmSetupException("During the algorithm initialization, the following exception has occurred: ", err));
            }

            if (Errors.Count == 0)
            {
                initializeComplete = true;
            }

            return initializeComplete;
        }

        /// <summary>
        /// Get the available data feeds from config.json,
        /// If none available, throw an error
        /// </summary>
        private static Dictionary<SecurityType, List<TickType>> GetConfiguredDataFeeds()
        {
            var dataFeedsConfigString = Config.Get("security-data-feeds");

            var dataFeeds = new Dictionary<SecurityType, List<TickType>>();
            if (dataFeedsConfigString != string.Empty)
            {
                dataFeeds = JsonConvert.DeserializeObject<Dictionary<SecurityType, List<TickType>>>(dataFeedsConfigString);
            }

            return dataFeeds;
        }

        /// <summary>
        /// Matches type names as namespace qualified or just the name
        /// If expectedTypeName is null or empty, this will always return true
        /// </summary>
        /// <param name="currentTypeFullName"></param>
        /// <param name="expectedTypeName"></param>
        /// <returns>True on matching the type name</returns>
        private static bool MatchTypeName(string currentTypeFullName, string expectedTypeName)
        {
            if (string.IsNullOrEmpty(expectedTypeName))
            {
                return true;
            }
            return currentTypeFullName == expectedTypeName
                || currentTypeFullName.Substring(currentTypeFullName.LastIndexOf('.') + 1) == expectedTypeName;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

    } // End Result Handler Thread:

} // End Namespace
