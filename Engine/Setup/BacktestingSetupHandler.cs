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
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;

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
        public TimeSpan MaximumRuntime { get; private set; }

        /// <summary>
        /// Starting capital according to the users initialize routine.
        /// </summary>
        /// <remarks>Set from the user code.</remarks>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public decimal StartingPortfolioValue { get; private set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(DateTime)"/>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for this backtest.
        /// </summary>
        /// <remarks>To stop algorithm flooding the backtesting system with hundreds of megabytes of order data we limit it to 100 per day</remarks>
        public int MaxOrders { get; private set; }

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

            // limit load times to 60 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(debugging, algorithmNodePacket.Language, TimeSpan.FromSeconds(60), names => names.SingleOrAlgorithmTypeName(Config.Get("algorithm-type-name")), WorkerThread);
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

                    //Algorithm is backtesting, not live:
                    algorithm.SetLiveMode(false);

                    //Set the source impl for the event scheduling
                    algorithm.Schedule.SetEventSchedule(parameters.RealTimeHandler);

                    // set the option chain provider
                    algorithm.SetOptionChainProvider(new CachingOptionChainProvider(new BacktestingOptionChainProvider()));

                    // set the future chain provider
                    algorithm.SetFutureChainProvider(new CachingFutureChainProvider(new BacktestingFutureChainProvider()));

                    //Initialise the algorithm, get the required data:
                    algorithm.Initialize();

                    // finalize initialization
                    algorithm.PostInitialize();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    Errors.Add(new AlgorithmSetupException("During the algorithm initialization, the following exception has occurred: ", err));
                }
            }, controls.RamAllocation,
                sleepIntervalMillis:50,  // entire system is waiting on this, so be as fast as possible
                workerThread: WorkerThread);

            //Before continuing, detect if this is ready:
            if (!initializeComplete) return false;

            // TODO: Refactor the BacktestResultHandler to use algorithm not job to set times
            job.PeriodStart = algorithm.StartDate;
            job.PeriodFinish = algorithm.EndDate;

            //Calculate the max runtime for the strategy
            MaximumRuntime = GetMaximumRuntime(job.PeriodStart, job.PeriodFinish, algorithm.SubscriptionManager, algorithm.UniverseManager, parameters.AlgorithmNodePacket.Controls);

            // Python takes forever; lets give it 10x longer to finish.
            if (job.Language == Language.Python)
            {
                MaximumRuntime = MaximumRuntime.Add(TimeSpan.FromSeconds(MaximumRuntime.TotalSeconds * 9));
            }

            BaseSetupHandler.SetupCurrencyConversions(algorithm, parameters.UniverseSelection);
            StartingPortfolioValue = algorithm.Portfolio.Cash;

            //Max Orders: 10k per backtest:
            if (job.UserPlan == UserPlan.Free)
            {
                MaxOrders = 10000;
            }
            else
            {
                MaxOrders = int.MaxValue;
                MaximumRuntime += MaximumRuntime;
            }

            MaxOrders = job.Controls.BacktestingMaxOrders;

            //Set back to the algorithm,
            algorithm.SetMaximumOrders(MaxOrders);

            //Starting date of the algorithm:
            StartingDate = job.PeriodStart;

            //Put into log for debugging:
            Log.Trace("SetUp Backtesting: User: " + job.UserId + " ProjectId: " + job.ProjectId + " AlgoId: " + job.AlgorithmId);
            Log.Trace($"Dates: Start: {job.PeriodStart.ToStringInvariant("d")} " +
                $"End: {job.PeriodFinish.ToStringInvariant("d")} " +
                $"Cash: {StartingPortfolioValue.ToStringInvariant("C")}"
            );

            if (Errors.Count > 0)
            {
                initializeComplete = false;
            }
            return initializeComplete;
        }

        /// <summary>
        /// Calculate the maximum runtime for this algorithm job.
        /// </summary>
        /// <param name="start">State date of the algorithm</param>
        /// <param name="finish">End date of the algorithm</param>
        /// <param name="subscriptionManager">Subscription Manager</param>
        /// <param name="universeManager">Universe manager containing configured universes</param>
        /// <param name="controls">Job controls instance</param>
        /// <returns>Timespan maximum run period</returns>
        private TimeSpan GetMaximumRuntime(DateTime start, DateTime finish, SubscriptionManager subscriptionManager, UniverseManager universeManager, Controls controls)
        {
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
