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
using Newtonsoft.Json;
using QuantConnect.Statistics;
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// A power gauge for backtests, time and parameters to estimate the overfitting risk
    /// </summary>
    public class ResearchGuide
    {
        /// <summary>
        /// Number of minutes used in developing the current backtest
        /// </summary>
        public int Minutes { get; set; }

        /// <summary>
        /// The quantity of backtests run in the project
        /// </summary>
        public int BacktestCount { get; set; }

        /// <summary>
        /// Number of parameters detected
        /// </summary>
        public int Parameters { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        public int ProjectId { get; set; }
    }

    /// <summary>
    /// Base class for backtest result object response
    /// </summary>
    public class BasicBacktest : RestResponse
    {
        /// <summary>
        /// Backtest error message
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Backtest error stacktrace
        /// </summary>
        public string Stacktrace { get; set; }

        /// <summary>
        /// Assigned backtest Id
        /// </summary>
        public string BacktestId { get; set; }

        /// <summary>
        /// Status of the backtest
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Name of the backtest
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Backtest creation date and time
        /// </summary>
        [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.UI)]
        public DateTime Created { get; set; }

        /// <summary>
        /// Progress of the backtest in percent 0-1.
        /// </summary>
        public decimal Progress { get; set; }

        /// <summary>
        /// Optimization task ID, if the backtest is part of an optimization
        /// </summary>
        public string OptimizationId { get; set; }

        /// <summary>
        /// Number of tradeable days
        /// </summary>
        public int TradeableDates { get; set; }

        /// <summary>
        /// Optimization parameters
        /// </summary>
        public ParameterSet ParameterSet { get; set; }

        /// <summary>
        /// Snapshot id of this backtest result
        /// </summary>
        public int SnapShotId { get; set; }
    }

    /// <summary>
    /// Results object class. Results are exhaust from backtest or live algorithms running in LEAN
    /// </summary>
    public class Backtest : BasicBacktest
    {
        /// <summary>
        /// Note on the backtest attached by the user
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Boolean true when the backtest is completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Organization ID
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, AlgorithmPerformance> RollingWindow { get; set; }

        /// <summary>
        /// Total algorithm performance statistics.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AlgorithmPerformance TotalPerformance { get; set; }

        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Chart> Charts { get; set; }

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Statistics { get; set; }

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics { get; set; }

        /// <summary>
        /// A power gauge for backtests, time and parameters to estimate the overfitting risk
        /// </summary>
        public ResearchGuide ResearchGuide { get; set; }

        /// <summary>
        /// The starting time of the backtest
        /// </summary>
        public DateTime? BacktestStart { get; set; }

        /// <summary>
        /// The ending time of the backtest
        /// </summary>
        public DateTime? BacktestEnd { get; set; }

        /// <summary>
        /// Indicates if the backtest has error during initialization
        /// </summary>
        public bool HasInitializeError { get; set; }

        /// <summary>
        /// The backtest node name
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// The associated project id
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// End date of out of sample data
        /// </summary>
        public DateTime? OutOfSampleMaxEndDate { get; set; }

        /// <summary>
        /// Number of days of out of sample days
        /// </summary>
        public int? OutOfSampleDays { get; set; }
    }

    /// <summary>
    /// Result object class for the List Backtest response from the API
    /// </summary>
    public class BacktestSummary : BasicBacktest
    {
        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk
        /// </summary>
        public decimal? SharpeRatio { get; set; }

        /// <summary>
        /// Algorithm "Alpha" statistic - abnormal returns over the risk free rate and the relationshio (beta) with the benchmark returns
        /// </summary>
        public decimal? Alpha { get; set; }

        /// <summary>
        /// Algorithm "beta" statistic - the covariance between the algorithm and benchmark performance, divided by benchmark's variance
        /// </summary>
        public decimal? Beta { get; set; }

        /// <summary>
        /// Annual compounded returns statistic based on the final-starting capital and years
        /// </summary>
        public decimal? CompoundingAnnualReturn { get; set; }

        /// <summary>
        /// Drawdown maximum percentage
        /// </summary>
        public decimal? Drawdown { get; set; }

        /// <summary>
        /// The ratio of the number of losing trades to the total number of trades
        /// </summary>
        public decimal? LossRate { get; set; }

        /// <summary>
        /// Net profit percentage
        /// </summary>
        public decimal? NetProfit { get; set; }

        /// <summary>
        /// Number of parameters in the backtest
        /// </summary>
        public int? Parameters { get; set; }

        /// <summary>
        /// Price-to-sales ratio
        /// </summary>
        public decimal? Psr { get; set; }

        /// <summary>
        /// SecurityTypes present in the backtest
        /// </summary>
        public string? SecurityTypes { get; set; }

        /// <summary>
        /// Sortino ratio with respect to risk free rate: measures excess of return per unit of downside risk
        /// </summary>
        public decimal? SortinoRatio { get; set; }

        /// <summary>
        /// Number of trades in the backtest
        /// </summary>
        public int? Trades { get; set; }

        /// <summary>
        /// Treynor ratio statistic is a measurement of the returns earned in excess of that which could have been earned on an investment that has no diversifiable risk
        /// </summary>
        public decimal? TreynorRatio { get; set; }

        /// <summary>
        /// The ratio of the number of winning trades to the total number of trades
        /// </summary>
        public decimal? WinRate { get; set; }

        /// <summary>
        /// Collection of tags for the backtest
        /// </summary>
        public List<string> Tags { get; set; }
    }

    /// <summary>
    /// Wrapper class for Backtest/* endpoints JSON response
    /// Currently used by Backtest/Read and Backtest/Create
    /// </summary>
    public class BacktestResponseWrapper : RestResponse
    {
        /// <summary>
        /// Backtest Object
        /// </summary>
        public Backtest Backtest { get; set; }

        /// <summary>
        /// Indicates if the backtest is run under debugging mode
        /// </summary>
        public bool Debugging { get; set; }
    }

    /// <summary>
    /// Collection container for a list of backtests for a project
    /// </summary>
    public class BacktestList : RestResponse
    {
        /// <summary>
        /// Collection of summarized backtest objects
        /// </summary>
        public List<Backtest> Backtests { get; set; }
    }

    /// <summary>
    /// Collection container for a list of backtest summaries for a project
    /// </summary>
    public class BacktestSummaryList : RestResponse
    {
        /// <summary>
        /// Collection of summarized backtest summary objects
        /// </summary>
        public List<BacktestSummary> Backtests { get; set; }

        /// <summary>
        /// Number of backtest summaries retrieved in the response
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Collection container for a list of backtest tags
    /// </summary>
    public class BacktestTags : RestResponse
    {
        /// <summary>
        /// Collection of tags for a backtest
        /// </summary>
        public List<string> Tags { get; set; }
    }
}
