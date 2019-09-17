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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Statistics;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides base functionality to the implementations of <see cref="IResultHandler"/>
    /// </summary>
    public abstract class BaseResultsHandler
    {
        /// <summary>
        /// Lock to be used when accessing the chart collection
        /// </summary>
        protected object ChartLock { get; }

        /// <summary>
        /// The algorithm unique compilation id
        /// </summary>
        protected string CompileId { get; set; }

        /// <summary>
        /// The algorithm job id.
        /// This is the deploy id for live, backtesting id for backtesting
        /// </summary>
        protected string JobId { get; set; }

        /// <summary>
        /// The result handler start time
        /// </summary>
        protected DateTime StartTime { get; }

        /// <summary>
        /// Customizable dynamic statistics <see cref="IAlgorithm.RuntimeStatistics"/>
        /// </summary>
        protected Dictionary<string, string> RuntimeStatistics { get; }

        /// <summary>
        /// The handler responsible for communicating messages to listeners
        /// </summary>
        protected IMessagingHandler MessagingHandler;

        /// <summary>
        /// The transaction handler used to get the algorithms Orders information
        /// </summary>
        protected ITransactionHandler TransactionHandler;

        /// <summary>
        /// The algorithms starting portfolio value.
        /// Used to calculate the portfolio return
        /// </summary>
        protected decimal StartingPortfolioValue { get; set; }

        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; set; }

        /// <summary>
        /// The data manager, used to access current subscriptions
        /// </summary>
        protected IDataFeedSubscriptionManager DataManager;

        /// <summary>
        /// Gets or sets the current alpha runtime statistics
        /// </summary>
        protected AlphaRuntimeStatistics AlphaRuntimeStatistics { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        protected BaseResultsHandler()
        {
            RuntimeStatistics = new Dictionary<string, string>();
            StartTime = DateTime.UtcNow;
            CompileId = "";
            JobId = "";
            ChartLock = new object();
        }

        /// <summary>
        /// Returns the location of the logs
        /// </summary>
        /// <param name="id">Id that will be incorporated into the algorithm log name</param>
        /// <param name="logs">The logs to save</param>
        /// <returns>The path to the logs</returns>
        public virtual string SaveLogs(string id, IEnumerable<string> logs)
        {
            var path = $"{id}-log.txt";
            File.WriteAllLines(path, logs);
            return Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        /// <summary>
        /// Save the results to disk
        /// </summary>
        /// <param name="name">The name of the results</param>
        /// <param name="result">The results to save</param>
        public virtual void SaveResults(string name, Result result)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), name), JsonConvert.SerializeObject(result, Formatting.Indented));
        }

        /// <summary>
        /// Sets the current alpha runtime statistics
        /// </summary>
        /// <param name="statistics">The current alpha runtime statistics</param>
        public virtual void SetAlphaRuntimeStatistics(AlphaRuntimeStatistics statistics)
        {
            AlphaRuntimeStatistics = statistics;
        }

        /// <summary>
        /// Sets the current Data Manager instance
        /// </summary>
        public virtual void SetDataManager(IDataFeedSubscriptionManager dataManager)
        {
            DataManager = dataManager;
        }

        /// <summary>
        /// Gets the algorithm net return
        /// </summary>
        protected decimal GetNetReturn()
        {
            //Some users have $0 in their brokerage account / starting cash of $0. Prevent divide by zero errors
            return StartingPortfolioValue > 0 ?
                (Algorithm.Portfolio.TotalPortfolioValue - StartingPortfolioValue) / StartingPortfolioValue
                : 0;
        }

        /// <summary>
        /// Gets the algorithm runtime statistics
        /// </summary>
        /// <remarks>
        /// TODO: we should not be adding ':' in the collection key
        /// </remarks>
        protected Dictionary<string, string> GetAlgorithmRuntimeStatistics(
            Dictionary<string, string> runtimeStatistics = null,
            bool addColon = false)
        {

            if (runtimeStatistics == null)
            {
                runtimeStatistics = new Dictionary<string, string>();
            }

            runtimeStatistics["Unrealized" + (addColon ? ":" : string.Empty)] = "$" + Algorithm.Portfolio.TotalUnrealizedProfit.ToStringInvariant("N2");
            runtimeStatistics["Fees" + (addColon ? ":" : string.Empty)] = "-$" + Algorithm.Portfolio.TotalFees.ToStringInvariant("N2");
            runtimeStatistics["Net Profit" + (addColon ? ":" : string.Empty)] = "$" + Algorithm.Portfolio.TotalProfit.ToStringInvariant("N2");
            runtimeStatistics["Return" + (addColon ? ":" : string.Empty)] = GetNetReturn().ToStringInvariant("P");
            runtimeStatistics["Equity" + (addColon ? ":" : string.Empty)] = "$" + Algorithm.Portfolio.TotalPortfolioValue.ToStringInvariant("N2");
            runtimeStatistics["Holdings" + (addColon ? ":" : string.Empty)] = "$" + Algorithm.Portfolio.TotalHoldingsValue.ToStringInvariant("N2");
            runtimeStatistics["Volume" + (addColon ? ":" : string.Empty)] = "$" + Algorithm.Portfolio.TotalSaleVolume.ToStringInvariant("N2");

            return runtimeStatistics;
        }

        /// <summary>
        /// Will generate the statistics results and update the provided runtime statistics
        /// </summary>
        protected StatisticsResults GenerateStatisticsResults(Dictionary<string, Chart> charts,
            SortedDictionary<DateTime, decimal> profitLoss)
        {
            var statisticsResults = new StatisticsResults();
            try
            {
                //Generates error when things don't exist (no charting logged, runtime errors in main algo execution)
                const string strategyEquityKey = "Strategy Equity";
                const string equityKey = "Equity";
                const string dailyPerformanceKey = "Daily Performance";
                const string benchmarkKey = "Benchmark";

                // make sure we've taken samples for these series before just blindly requesting them
                if (charts.ContainsKey(strategyEquityKey) &&
                    charts[strategyEquityKey].Series.ContainsKey(equityKey) &&
                    charts[strategyEquityKey].Series.ContainsKey(dailyPerformanceKey) &&
                    charts.ContainsKey(benchmarkKey) &&
                    charts[benchmarkKey].Series.ContainsKey(benchmarkKey))
                {
                    var equity = charts[strategyEquityKey].Series[equityKey].Values;
                    var performance = charts[strategyEquityKey].Series[dailyPerformanceKey].Values;
                    var totalTransactions = Algorithm.Transactions.GetOrders(x => x.Status.IsFill()).Count();
                    var benchmark = charts[benchmarkKey].Series[benchmarkKey].Values;

                    var trades = Algorithm.TradeBuilder.ClosedTrades;

                    statisticsResults = StatisticsBuilder.Generate(trades, profitLoss, equity, performance, benchmark,
                        StartingPortfolioValue, Algorithm.Portfolio.TotalFees, totalTransactions);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "BaseResultsHandler.GenerateStatisticsResults(): Error generating statistics packet");
            }

            return statisticsResults;
        }
    }
}
