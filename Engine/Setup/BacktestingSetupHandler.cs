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
using System.Linq;
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
            var debugging = debugNode != null && debugNode.IsDebugging || Config.GetBool("debugging", false);

            if (debugging && !BaseSetupHandler.InitializeDebugging(algorithmNodePacket, WorkerThread))
            {
                throw new AlgorithmSetupException("Failed to initialize debugging");
            }

            // Limit load times to 90 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(debugging, algorithmNodePacket.Language, BaseSetupHandler.AlgorithmCreationTimeout, names => names.SingleOrAlgorithmTypeName(Config.Get("algorithm-type-name")), WorkerThread);
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

            Log.Trace($"BacktestingSetupHandler.Setup(): Setting up job: Plan: {job.UserPlan}, UID: {job.UserId.ToStringInvariant()}, " +
                $"PID: {job.ProjectId.ToStringInvariant()}, Version: {job.Version}, Source: {job.RequestSource}"
            );

            if (algorithm == null)
            {
                Errors.Add(new AlgorithmSetupException("Could not create instance of algorithm"));
                return false;
            }

            algorithm.Name = job.GetAlgorithmName();

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
                    algorithm.SetLiveMode(false);

                    //Set the source impl for the event scheduling
                    algorithm.Schedule.SetEventSchedule(parameters.RealTimeHandler);

                    // set the option chain provider
                    algorithm.SetOptionChainProvider(new CachingOptionChainProvider(new BacktestingOptionChainProvider(parameters.DataProvider)));

                    // set the future chain provider
                    algorithm.SetFutureChainProvider(new CachingFutureChainProvider(new BacktestingFutureChainProvider(parameters.DataProvider)));

                    // set the object store
                    algorithm.SetObjectStore(parameters.ObjectStore);

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

                    // after we call initialize
                    BaseSetupHandler.LoadBacktestJobCashAmount(algorithm, job);

                    // finalize initialization
                    algorithm.PostInitialize();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    Errors.Add(new AlgorithmSetupException("During the algorithm initialization, the following exception has occurred: ", err));
                }
            }, controls.RamAllocation,
                sleepIntervalMillis: 100,  // entire system is waiting on this, so be as fast as possible
                workerThread: WorkerThread);

            //Before continuing, detect if this is ready:
            if (!initializeComplete) return false;

            //Calculate the max runtime for the strategy
            MaximumRuntime = GetMaximumRuntime(parameters);

            BaseSetupHandler.SetupCurrencyConversions(algorithm, parameters.UniverseSelection);
            StartingPortfolioValue = algorithm.Portfolio.Cash;

            // we set the free portfolio value based on the initial total value and the free percentage value
            algorithm.Settings.FreePortfolioValue =
                algorithm.Portfolio.TotalPortfolioValue * algorithm.Settings.FreePortfolioValuePercentage;

            // Get and set maximum orders for this job
            MaxOrders = GetMaximumOrders(job);
            algorithm.SetMaximumOrders(MaxOrders);

            //Starting date of the algorithm:
            StartingDate = algorithm.StartDate;

            //Put into log for debugging:
            Log.Trace("SetUp Backtesting: User: " + job.UserId + " ProjectId: " + job.ProjectId + " AlgoId: " + job.AlgorithmId);
            Log.Trace($"Dates: Start: {algorithm.StartDate.ToStringInvariant("d")} " +
                      $"End: {algorithm.EndDate.ToStringInvariant("d")} " +
                      $"Cash: {StartingPortfolioValue.ToStringInvariant("C")}");

            if (Errors.Count > 0)
            {
                initializeComplete = false;
            }
            return initializeComplete;
        }

        /// <summary>
        /// Get maximum orders for our algorithm, restricted by UserPlan
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected virtual int GetMaximumOrders(BacktestNodePacket job)
        {
            if (job == null)
            {
                throw new ArgumentException("Job must not be null");
            }

            return job.Controls.BacktestingMaxOrders;
        }

        /// <summary>
        /// Calculate the maximum runtime for this algorithm job.
        /// </summary>
        /// <param name="parameters">Setup handler parameters</param>
        /// <returns>Timespan maximum run period</returns>
        protected virtual TimeSpan GetMaximumRuntime(SetupHandlerParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentException("Parameters must not be null");
            }

            var start = parameters.Algorithm.StartDate;
            var finish = parameters.Algorithm.EndDate;
            var subscriptionManager = parameters.Algorithm.SubscriptionManager;
            var universeManager = parameters.Algorithm.UniverseManager;
            var controls = parameters.AlgorithmNodePacket.Controls;

            // option/futures chain subscriptions
            var derivativeSubscriptions = subscriptionManager.Subscriptions
                .Where(x => x.Symbol.IsCanonical())
                .Select(x => controls.GetLimit(x.Resolution))
                .Sum();

            // universe coarse/fine/custom subscriptions
            var universeSubscriptions = universeManager
                // use max limit for universes without explicitly added securities
                .Sum(u => u.Value.Members.Count == 0 ? controls.GetLimit(u.Value.UniverseSettings.Resolution) : u.Value.Members.Count);

            var subscriptionCount = derivativeSubscriptions + universeSubscriptions;

            double maxRunTime = 0;
            var jobDays = (finish - start).TotalDays;

            maxRunTime = 10 * subscriptionCount * jobDays;

            //Rationalize:
            if ((maxRunTime / 3600) > 12)
            {
                //12 hours maximum
                maxRunTime = 3600 * 12;
            }
            else if (maxRunTime < 60)
            {
                //If less than 60 seconds.
                maxRunTime = 60;
            }

            Log.Trace("BacktestingSetupHandler.GetMaxRunTime(): Job Days: " + jobDays + " Max Runtime: " + Math.Round(maxRunTime / 60) + " min");

            //Override for windows:
            if (OS.IsWindows)
            {
                maxRunTime = 24 * 60 * 60;
            }

            // Python takes forever; lets give it 10x longer to finish.
            if (parameters.AlgorithmNodePacket.Language == Language.Python)
            {
                maxRunTime *= 10;
            }

            // For non-free plans double maximum runtime
            if (parameters.AlgorithmNodePacket.UserPlan != UserPlan.Free)
            {
                maxRunTime += maxRunTime;
            }

            return TimeSpan.FromSeconds(maxRunTime);
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
