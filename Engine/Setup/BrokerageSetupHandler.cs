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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Defines a set up handler that initializes the algorithm instance using values retrieved from the user's brokerage account
    /// </summary>
    public class BrokerageSetupHandler : ISetupHandler
    {
        /// <summary>
        /// Any errors from the initialization stored here:
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Get the maximum runtime for this algorithm job.
        /// </summary>
        public TimeSpan MaximumRuntime { get; private set; }

        /// <summary>
        /// Algorithm starting capital for statistics calculations
        /// </summary>
        public decimal StartingCapital { get; private set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for the algorithm run -- applicable for backtests only.
        /// </summary>
        public int MaxOrders { get; private set; }

        // saves ref to algo so we can call quit if runtime error encountered
        private IAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new BrokerageSetupHandler
        /// </summary>
        public BrokerageSetupHandler()
        {
            Errors = new List<string>();
            MaximumRuntime = TimeSpan.FromDays(10*365);
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        public IAlgorithm CreateAlgorithmInstance(string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;

            // limit load times to 10 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(TimeSpan.FromSeconds(10), names =>
            {
                // if there's only one use that guy
                if (names.Count == 1)
                {
                    return names.Single();
                }

                // if there's more than one then check configuration for which one we should use
                var algorithmName = Config.Get("algorithm-type-name");
                return names.Single(x => x.Contains(algorithmName));
            });

            var complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, out algorithm, out error);
            if (!complete) throw new Exception(error + " Try re-building algorithm.");

            return algorithm;
        }

        /// <summary>
        /// Primary entry point to setup a new algorithm
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">New brokerage output instance</param>
        /// <param name="job">Algorithm job task</param>
        /// <returns>True on successfully setting up the algorithm state, or false on error.</returns>
        public bool Setup(IAlgorithm algorithm, out IBrokerage brokerage, AlgorithmNodePacket job)
        {
            _algorithm = algorithm;
            brokerage = default(IBrokerage);

            // verify we were given the correct job packet type
            var liveJob = job as LiveNodePacket;
            if (liveJob == null)
            {
                AddInitializationError("BrokerageSetupHandler requires a LiveNodePacket");
                return false;
            }

            // verify the brokerage was specified
            if (string.IsNullOrWhiteSpace(liveJob.Brokerage))
            {
                AddInitializationError("A brokerage must be specified");
                return false;
            }

            try
            {
                //Execute the initialize code:
                var initializeComplete = Isolator.ExecuteWithTimeLimit(TimeSpan.FromSeconds(10), () =>
                {
                    try
                    {                
                        //Set the live trading level asset/ram allocation limits. 
                        //Protects algorithm from linux killing the job by excess memory:
                        switch (job.ServerType)
                        {
                            case ServerType.Server1024:
                                algorithm.SetAssetLimits(100, 20, 10);
                                break;

                            case ServerType.Server2048:
                                algorithm.SetAssetLimits(400, 50, 30);
                                break;

                            default: //512
                                algorithm.SetAssetLimits(50, 10, 5);
                                break;
                        }

                        //Algorithm is live, not backtesting:
                        algorithm.SetLiveMode(true);

                        //Initialise the algorithm, get the required data:
                        algorithm.Initialize();
                    }
                    catch (Exception err)
                    {
                        Errors.Add("Failed to initialize algorithm: Initialize(): " + err.Message);
                    }
                });

                if (!initializeComplete)
                {
                    AddInitializationError("Failed to initialize algorithm.");
                    return false;
                }

                // find the correct brokerage factory based on the specified brokerage in the live job packet
                var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveJob.Brokerage));

                // initialize the correct brokerage using the resolved factory
                brokerage = brokerageFactory.CreateBrokerage(liveJob, algorithm);

                brokerage.Connect();

                // set the algorithm's cash balance
                var cashBalance = brokerage.GetCashBalance();
                algorithm.SetCash(cashBalance);

                // populate the algorithm with the account's outstanding orders
                var openOrders = brokerage.GetOpenOrders();
                foreach (var order in openOrders)
                {
                    // be sure to assign order IDs such that we increment from the SecurityTransactionManager to avoid ID collisions
                    order.Id = algorithm.Transactions.GetIncrementOrderId();
                    algorithm.Orders.AddOrUpdate(order.Id, order, (i, o) => order);
                }

                // populate the algorithm with the account's current holdings
                var holdings = brokerage.GetAccountHoldings();
                var minResolution = new Lazy<Resolution>(() => algorithm.Securities.Min(x => x.Value.Resolution));
                foreach (var holding in holdings)
                {
                    if (!algorithm.Portfolio.ContainsKey(holding.Symbol))
                    {
                        // for items not directly requested set leverage to 1 and at the min resolution
                        algorithm.AddSecurity(holding.Type, holding.Symbol, minResolution.Value, true, 1.0m, false);
                    }
                    algorithm.Portfolio[holding.Symbol].SetHoldings(holding.AveragePrice, (int)holding.Quantity);
                }
            }
            catch (Exception err)
            {
                AddInitializationError(err.Message);
            }

            return Errors.Count == 0;
        }

        /// <summary>
        /// Setup the error handler for the brokerage errors.
        /// </summary>
        /// <param name="results">Result handler.</param>
        /// <param name="brokerage">Brokerage endpoint.</param>
        /// <returns>True on successfully setting up the error handlers.</returns>
        public bool SetupErrorHandler(IResultHandler results, IBrokerage brokerage)
        {
            brokerage.Message += (sender, message) =>
            {
                if (message.Type == BrokerageMessageType.Error)
                {
                    _algorithm.RunTimeError = new Exception(message.Message);
                    _algorithm.Quit();
                }
            };
            return true;
        }

        private void AddInitializationError(string message)
        {
            Errors.Add("Failed to initialize algorithm: Initialize(): " + message);
        }
    }
}
