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
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Brokerages.Backtesting;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Backtesting setup handler processes the algorithm initialize method and sets up the internal state of the algorithm class.
    /// </summary>
    public class BacktestingSetupHandler : ISetupHandler
    {
        /// <summary>
        /// The worker thread instance the setup handler should use
        /// </summary>
        public WorkerThread WorkerThread { get; set; }

        /// <summary>
        /// Internal errors list from running the setup procedures.
        /// </summary>
        public List<Exception> Errors { get; set; }

        /// <summary>
        /// Maximum runtime of the algorithm in seconds.
        /// </summary>
        /// <remarks>Maximum runtime is a formula based on the number and resolution of symbols requested, and the days backtesting</remarks>
        public TimeSpan MaximumRuntime { get; protected set; }

        /// <summary>
        /// Starting capital according to the users initialize routine.
        /// </summary>
        /// <remarks>Set from the user code.</remarks>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public decimal StartingPortfolioValue { get; protected set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(DateTime)"/>
        public DateTime StartingDate { get; protected set; }

        /// <summary>
        /// Maximum number of orders for this backtest.
        /// </summary>
        /// <remarks>To stop algorithm flooding the backtesting system with hundreds of megabytes of order data we limit it to 100 per day</remarks>
        public int MaxOrders { get; protected set; }

        /// <summary>
        /// Initialize the backtest setup handler.
        /// </summary>
        public BacktestingSetupHandler()
        {
            MaximumRuntime = TimeSpan.FromSeconds(300);
            Errors = new List<Exception>();
            StartingDate = new DateTime(1998, 01, 01);
        }

        /// <summary>
        /// Create a new instance of an algorithm from a physical dll path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly's location</param>
        /// <param name="algorithmNodePacket">Details of the task required</param>
        /// <returns>A new instance of IAlgorithm, or throws an exception if there was an error</returns>
        public virtual IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;

            var debugNode = algorithmNodePacket as BacktestNodePacket;
            var debugging = debugNode != null && debugNode.Debugging || Config.GetBool("debugging", false);

            if (debugging && !BaseSetupHandler.InitializeDebugging(algorithmNodePacket, WorkerThread))
            {
                throw new AlgorithmSetupException("Failed to initialize debugging");
            }

            // Limit load times to 90 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(debugging, algorithmNodePacket.Language, BaseSetupHandler.AlgorithmCreationTimeout, names => names.SingleOrAlgorithmTypeName(Config.Get("algorithm-type-name", algorithmNodePacket.AlgorithmId)), WorkerThread);
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
        public virtual IBrokerage CreateBrokerage(AlgorithmNodePacket algorithmNodePacket, IAlgorithm uninitializedAlgorithm, out IBrokerageFactory factory)
        {
            factory = new BacktestingBrokerageFactory();
            return new BacktestingBrokerage(uninitializedAlgorithm);
        }

        /// <summary>
        /// Setup the algorithm cash, dates and data subscriptions as desired.
        /// </summary>
        /// <param name="parameters">The parameters object to use</param>
        /// <returns>Boolean true on successfully initializing the algorithm</returns>
        public bool Setup(SetupHandlerParameters parameters)
        {
            var algorithm = parameters.Algorithm;
            var job = parameters.AlgorithmNodePacket as BacktestNodePacket;
            if (job == null)
            {
                throw new ArgumentException("Expected BacktestNodePacket but received " + parameters.AlgorithmNodePacket.GetType().Name);
            }

            Log.Trace($"BacktestingSetupHandler.Setup(): Setting up job: UID: {job.UserId.ToStringInvariant()}, " +
                $"PID: {job.ProjectId.ToStringInvariant()}, Version: {job.Version}, Source: {job.RequestSource}"
            );

            if (algorithm == null)
            {
                Errors.Add(new AlgorithmSetupException("Could not create instance of algorithm"));
                return false;
            }

            algorithm.Name = job.Name;

            //Make sure the algorithm start date ok.
            if (job.PeriodStart == default(DateTime))
            {
                Errors.Add(new AlgorithmSetupException("Algorithm start date was never set"));
                return false;
            }

            var controls = job.Controls;
            var isolator = new Isolator();
            var initializeComplete = isolator.ExecuteWithTimeLimit(TimeSpan.FromMinutes(5), () =>
            {
                try
                {
                    parameters.ResultHandler.SendStatusUpdate(AlgorithmStatus.Initializing, "Initializing algorithm...");
                    //Set our parameters
                    algorithm.SetParameters(job.Parameters);
                    algorithm.SetAvailableDataTypes(BaseSetupHandler.GetConfiguredDataFeeds());

                    //Algorithm is backtesting, not live:
                    algorithm.SetAlgorithmMode(job.AlgorithmMode);
                    algorithm.SetDeploymentTarget(job.DeploymentTarget);

                    //Set the source impl for the event scheduling
                    algorithm.Schedule.SetEventSchedule(parameters.RealTimeHandler);

                    // set the option chain provider
                    algorithm.SetOptionChainProvider(new CachingOptionChainProvider(new BacktestingOptionChainProvider(parameters.DataCacheProvider, parameters.MapFileProvider)));

                    // set the future chain provider
                    algorithm.SetFutureChainProvider(new CachingFutureChainProvider(new BacktestingFutureChainProvider(parameters.DataCacheProvider)));

                    // before we call initialize
                    BaseSetupHandler.LoadBacktestJobAccountCurrency(algorithm, job);

                    //Initialise the algorithm, get the required data:
                    algorithm.Initialize();

                    // set start and end date if present in the job
                    if (job.PeriodStart.HasValue)
                    {
                        algorithm.SetStartDate(job.PeriodStart.Value);
                    }
                    if (job.PeriodFinish.HasValue)
                    {
                        algorithm.SetEndDate(job.PeriodFinish.Value);
                    }

                    if(job.OutOfSampleMaxEndDate.HasValue)
                    {
                        if(algorithm.EndDate > job.OutOfSampleMaxEndDate.Value)
                        {
                            Log.Trace($"BacktestingSetupHandler.Setup(): setting end date to {job.OutOfSampleMaxEndDate.Value:yyyyMMdd}");
                            algorithm.SetEndDate(job.OutOfSampleMaxEndDate.Value);

                            if (algorithm.StartDate > algorithm.EndDate)
                            {
                                algorithm.SetStartDate(algorithm.EndDate);
                            }
                        }
                    }

                    // after we call initialize
                    BaseSetupHandler.LoadBacktestJobCashAmount(algorithm, job);

                    // after algorithm was initialized, should set trading days per year for our great portfolio statistics
                    BaseSetupHandler.SetBrokerageTradingDayPerYear(algorithm);

                    // finalize initialization
                    algorithm.PostInitialize();
                }
                catch (Exception err)
                {
                    Errors.Add(new AlgorithmSetupException("During the algorithm initialization, the following exception has occurred: ", err));
                }
            }, controls.RamAllocation,
                sleepIntervalMillis: 100,  // entire system is waiting on this, so be as fast as possible
                workerThread: WorkerThread);

            if (Errors.Count > 0)
            {
                // if we already got an error just exit right away
                return false;
            }

            //Before continuing, detect if this is ready:
            if (!initializeComplete) return false;

            MaximumRuntime = TimeSpan.FromMinutes(job.Controls.MaximumRuntimeMinutes);

            BaseSetupHandler.SetupCurrencyConversions(algorithm, parameters.UniverseSelection);
            StartingPortfolioValue = algorithm.Portfolio.Cash;

            // Get and set maximum orders for this job
            MaxOrders = job.Controls.BacktestingMaxOrders;
            algorithm.SetMaximumOrders(MaxOrders);

            //Starting date of the algorithm:
            StartingDate = algorithm.StartDate;

            //Put into log for debugging:
            Log.Trace("SetUp Backtesting: User: " + job.UserId + " ProjectId: " + job.ProjectId + " AlgoId: " + job.AlgorithmId);
            Log.Trace($"Dates: Start: {algorithm.StartDate.ToStringInvariant("d")} " +
                      $"End: {algorithm.EndDate.ToStringInvariant("d")} " +
                      $"Cash: {StartingPortfolioValue.ToStringInvariant("C")} " +
                      $"MaximumRuntime: {MaximumRuntime} " +
                      $"MaxOrders: {MaxOrders}");

            return initializeComplete;
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
