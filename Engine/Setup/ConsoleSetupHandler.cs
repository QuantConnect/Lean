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
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Console setup handler to initialize and setup the Lean Engine properties for a local backtest
    /// </summary>
    public class ConsoleSetupHandler : ISetupHandler
    {
        /// <summary>
        /// Error which occured during setup may appear here.
        /// </summary>
        public List<string> Errors { get;  set; }

        /// <summary>
        /// Maximum runtime of the strategy. (Set to 10 years for local backtesting).
        /// </summary>
        public TimeSpan MaximumRuntime { get; private set; }

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
        public int MaxOrders { get; private set; }

        /// <summary>
        /// Setup the algorithm data, cash, job start end date etc:
        /// </summary>
        public ConsoleSetupHandler()
        {
            MaxOrders = int.MaxValue;
            StartingPortfolioValue = 0;
            StartingDate = new DateTime(1998, 01, 01);
            MaximumRuntime = TimeSpan.FromDays(10 * 365);
            Errors = new List<string>();
        }

        /// <summary>
        /// Creates a new algorithm instance. Checks configuration for a specific type name, and if present will
        /// force it to find that one
        /// </summary>
        /// <param name="assemblyPath">Physical path of the algorithm dll.</param>
        /// <param name="language">Language of the assembly.</param>
        /// <returns>Algorithm instance</returns>
        public IAlgorithm CreateAlgorithmInstance(string assemblyPath, Language language)
        {
            string error;
            IAlgorithm algorithm;
            var algorithmName = Config.Get("algorithm-type-name");

            // don't force load times to be fast here since we're running locally, this allows us to debug
            // and step through some code that may take us longer than the default 10 seconds
            var loader = new Loader(language, TimeSpan.FromHours(1), names => names.SingleOrDefault(name => MatchTypeName(name, algorithmName)));
            var complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, out algorithm, out error);
            if (!complete) throw new Exception(error + ": try re-building algorithm.");

            return algorithm;
        }

        /// <summary>
        /// Creates a new <see cref="BacktestingBrokerage"/> instance
        /// </summary>
        /// <param name="algorithmNodePacket">Job packet</param>
        /// <param name="uninitializedAlgorithm">The algorithm instance before Initialize has been called</param>
        /// <returns>The brokerage instance, or throws if error creating instance</returns>
        public IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory)
        {
            factory = new BacktestingBrokerageFactory();
            return new BacktestingBrokerage(uninitializedAlgorithm);
        }

        /// <summary>
        /// Setup the algorithm cash, dates and portfolio as desired.
        /// </summary>
        /// <param name="algorithm">Existing algorithm instance</param>
        /// <param name="brokerage">New brokerage instance</param>
        /// <param name="baseJob">Backtesting job</param>
        /// <param name="resultHandler">The configured result handler</param>
        /// <param name="transactionHandler">The configuration transaction handler</param>
        /// <param name="realTimeHandler">The configured real time handler</param>
        /// <returns>Boolean true on successfully setting up the console.</returns>
        public bool Setup(IAlgorithm algorithm, IBrokerage brokerage, AlgorithmNodePacket baseJob, IResultHandler resultHandler, ITransactionHandler transactionHandler, IRealTimeHandler realTimeHandler)
        {
            var initializeComplete = false;

            try
            {
                //Set common variables for console programs:

                if (baseJob.Type == PacketType.BacktestNode)
                {
                    var backtestJob = baseJob as BacktestNodePacket;
                    
                    algorithm.SetMaximumOrders(int.MaxValue);
                    // set our parameters
                    algorithm.SetParameters(baseJob.Parameters);
                    algorithm.SetLiveMode(false);
                    //Set the source impl for the event scheduling
                    algorithm.Schedule.SetEventSchedule(realTimeHandler);
                    //Setup Base Algorithm:
                    algorithm.Initialize();
                    //Set the time frontier of the algorithm
                    algorithm.SetDateTime(algorithm.StartDate.ConvertToUtc(algorithm.TimeZone));

                    //Construct the backtest job packet:
                    backtestJob.PeriodStart = algorithm.StartDate;
                    backtestJob.PeriodFinish = algorithm.EndDate;
                    backtestJob.BacktestId = algorithm.GetType().Name;
                    backtestJob.Type = PacketType.BacktestNode;
                    backtestJob.UserId = baseJob.UserId;
                    backtestJob.Channel = baseJob.Channel;
       
                    //Backtest Specific Parameters:
                    StartingDate = backtestJob.PeriodStart;
                    StartingPortfolioValue = algorithm.Portfolio.Cash;
                }
                else
                {
                    throw new Exception("The ConsoleSetupHandler is for backtests only. Use the BrokerageSetupHandler.");
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                Errors.Add("Failed to initialize algorithm: Initialize(): " + err.Message);
            }

            if (Errors.Count == 0)
            {
                initializeComplete = true;
            }

            algorithm.Transactions.SetOrderProcessor(transactionHandler);
            algorithm.PostInitialize();

            return initializeComplete;
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
            // nothing to clean up
        }

    } // End Result Handler Thread:

} // End Namespace
