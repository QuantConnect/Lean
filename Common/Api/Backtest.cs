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

namespace QuantConnect.Api
{
    /// <summary>
    /// Defines the scores given to a particular insight
    /// </summary>
    public class InsightScore
    {
        /// <summary>
        /// The time these scores were last updated
        /// </summary>
        [JsonProperty(PropertyName = "UpdatedTimeUtc")]
        public string UpdatedTimeUtc {  get; set; }

        /// <summary>
        /// The direction score
        /// </summary>
        [JsonProperty(PropertyName = "Direction")]
        public string Direction { get; set; }

        /// <summary>
        /// The magnitude score
        /// </summary>
        [JsonProperty(PropertyName = "Magnitude")]
        public decimal Magnitude { get; set; }

        /// <summary>
        /// Is the insight past its expiry time and score can be finalized
        /// </summary>
        [JsonProperty(PropertyName = "IsFinalScore")]
        public bool IsFinalScore { get; set; }
    }

    /// <summary>
    /// Contains insight population run time statistics
    /// </summary>
    public class AlphaRuntimeStatistics
    {
        [JsonProperty(PropertyName = "MeanPopulationScore")]
        public InsightScore MeanPopulationScore { get; set; }

        [JsonProperty(PropertyName = "RollingAveragedPopulationScore")]
        public InsightScore RollingAveragedPopulationScore { get; set; }

        /// <summary>
        /// Gets the total number of insights with an up direction
        /// </summary>
        [JsonProperty(PropertyName = "LongCount")]
        public string LongCount { get; set; }

        /// <summary>
        /// Gets the total number of insights with a down direction
        /// </summary>
        [JsonProperty(PropertyName = "ShortCount")]
        public string ShortCount { get; set; }

        /// <summary>
        /// The ratio of InsightDirection.Up over InsightDirection.Down
        /// </summary>
        [JsonProperty(PropertyName = "LongShortRatio")]
        public decimal LongShortRatio { get; set; }

        /// <summary>
        /// The total accumulated estimated value of trading all insights
        /// </summary>
        [JsonProperty(PropertyName = "TotalAccumulatedEstimatedAlphaValue")]
        public decimal TotalAccumulatedEstimatedAlphaValue { get; set; }

        /// <summary>
        /// Score of the strategy's insights predictive power
        /// </summary>
        [JsonProperty(PropertyName = "KellyCriterionEstimate")]
        public decimal KellyCriterionEstimate { get; set; }

        /// <summary>
        /// The p-value or probability value of the KellyCriterionEstimate
        /// </summary>
        [JsonProperty(PropertyName = "KellyCriterionProbabilityValue")]
        public decimal KellyCriterionProbabilityValue { get; set; }

        /// <summary>
        /// Score of the strategy's performance, and suitability for the Alpha Stream Market
        /// </summary>
        [JsonProperty(PropertyName = "FitnessScore")]
        public decimal FitnessScore { get; set; }

        /// <summary>
        /// Measurement of the strategies trading activity with respect to the portfolio value. Calculated as the sales volume with respect to the average total portfolio value
        /// </summary>
        [JsonProperty(PropertyName = "PortfolioTurnover")]
        public decimal PortfolioTurnover { get; set; }

        /// <summary>
        /// Provides a risk adjusted way to factor in the returns and drawdown of the strategy. It is calculated by dividing the Portfolio Annualized Return by the Maximum Drawdown seen during the backtest
        /// </summary>
        [JsonProperty(PropertyName = "ReturnOverMaxDrawdown")]
        public decimal ReturnOverMaxDrawdown { get; set; }

        /// <summary>
        /// Gives a relative picture of the strategy volatility. It is calculated by taking a portfolio's annualized rate of return and subtracting the risk free rate of return
        /// </summary>
        [JsonProperty(PropertyName = "SortinoRatio")]
        public decimal SortinoRatio { get; set; }

        /// <summary>
        /// Suggested Value of the Alpha On A Monthly Basis For Licensing
        /// </summary>
        [JsonProperty(PropertyName = "EstimatedMonthlyAlphaValue")]
        public decimal EstimatedMonthlyAlphaValue { get; set; }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(PropertyName = "TotalInsightsGenerated")]
        public string TotalInsightsGenerated { get; set; }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(PropertyName = "TotalInsightsClosed")]
        public string TotalInsightsClosed { get; set; }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(PropertyName = "TotalInsightsAnalysisCompleted")]
        public string TotalInsightsAnalysisCompleted { get; set; }

        /// <summary>
        /// Gets the mean estimated insight value
        /// </summary>
        [JsonProperty(PropertyName = "MeanPopulationEstimatedInsightValue")]
        public decimal MeanPopulationEstimatedInsightValue { get; set; }
    }

    /// <summary>
    /// The API method call could not be completed as requested
    /// </summary>
    public class ResearchGuide
    {
        /// <summary>
        /// Number of minutes used in developing the current backtest
        /// </summary>
        [JsonProperty(PropertyName = "minutes")]
        public int Minutes { get; set; }

        /// <summary>
        /// Backtest count of the current backtest in the project
        /// </summary>
        [JsonProperty(PropertyName = "backtestCount")]
        public int BacktestCount { get; set; }

        /// <summary>
        /// Number of parameters detected
        /// </summary>
        [JsonProperty(PropertyName = "parameters")]
        public int Parameters { get; set; }
    }

    /// <summary>
    /// Results object class. Results are exhaust from backtest or live algorithms running in LEAN
    /// </summary>
    public class Backtest : RestResponse
    {
        /// <summary>
        /// Name of the backtest
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Note on the backtest attached by the user
        /// </summary>
        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }

        /// <summary>
        /// Assigned backtest Id
        /// </summary>
        [JsonProperty(PropertyName = "backtestId")]
        public string BacktestId { get; set; }

        /// <summary>
        /// Boolean true when the backtest is completed.
        /// </summary>
        [JsonProperty(PropertyName = "completed")]
        public bool Completed { get; set; }

        /// <summary>
        /// Progress of the backtest in percent 0-1.
        /// </summary>
        [JsonProperty(PropertyName = "progress")]
        public decimal Progress { get; set; }

        /// <summary>
        /// Backtest error message
        /// </summary>
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        /// <summary>
        /// Backtest error stacktrace
        /// </summary>
        [JsonProperty(PropertyName = "stacktrace")]
        public string StackTrace { get; set; }

        /// <summary>
        /// Backtest creation date and time
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        [JsonProperty(PropertyName = "rollingWindow", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, AlgorithmPerformance> RollingWindow { get; set; }

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        [JsonProperty(PropertyName = "totalPerformance", NullValueHandling = NullValueHandling.Ignore)]
        public AlgorithmPerformance TotalPerformance { get; set; }

        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        [JsonProperty(PropertyName = "charts", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, Chart> Charts { get; set; }

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        [JsonProperty(PropertyName = "statistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Statistics { get; set; }

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        [JsonProperty(PropertyName = "runtimeStatistics", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> RuntimeStatistics { get; set; }

        /// <summary>
        /// Optimization parameters
        /// </summary>
        [JsonProperty(PropertyName = "parameterSet")]
        public ParameterSet ParameterSet { get; set; }

        /// <summary>
        /// Collection of tags for the backtest
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Organization ID
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        public int OrganizationId { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; set; }

        /// <summary>
        /// Optimization task ID, if the backtest is part of an optimization
        /// </summary>
        [JsonProperty(PropertyName = "optimizationId")]
        public string OptimizationId { get; set; }

        /// <summary>
        /// Number of tradeable days
        /// </summary>
        [JsonProperty(PropertyName = "tradeableDates")]
        public int TradeableDates { get; set; }

        [JsonProperty(PropertyName = "researchGuide")]
        public ResearchGuide ResearchGuide { get; set; }

        /// <summary>
        /// The starting time of the backtest
        /// </summary>
        [JsonProperty(PropertyName = "backtestStart")]
        public string BacktestStart { get; set; }

        /// <summary>
        /// The ending time of the backtest
        /// </summary>
        [JsonProperty(PropertyName = "backtestEnd")]
        public string BacktestEnd { get; set; }

        /// <summary>
        /// Snapshot id of this backtest result
        /// </summary>
        [JsonProperty(PropertyName = "snapshotId")]
        public int SnapShotId { get; set; }

        /// <summary>
        /// Status of the backtest
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// time-PnL pairs of profit/loss at a timestamp
        /// </summary>
        [JsonProperty(PropertyName = "profitLoss")]
        public List<IDictionary<DateTime, decimal>> ProfilLoss { get; set; }

        /// <summary>
        /// Indicates if the backtest has error during initialization
        /// </summary>
        [JsonProperty(PropertyName = "hasInitializeError")]
        public bool HasInitializeError { get; set; }

        [JsonProperty(PropertyName = "alphaRuntimeStatistics")]
        public AlphaRuntimeStatistics AlphaRuntimeStatistics { get; set; }

        [JsonProperty(PropertyName = "signals")]
        public string Signals { get; set; }

        [JsonProperty(PropertyName = "insights")]
        public string Insights { get; set; }

        /// <summary>
        /// The backtest node name
        /// </summary>
        [JsonProperty(PropertyName = "nodeName")]
        public string NodeName { get; set; }

        /// <summary>
        /// End date of out of sample data
        /// </summary>
        [JsonProperty(PropertyName = "outOfSampleMaxEndDate")]
        public string OutOfSampleMaxEndDate { get; set; }

        /// <summary>
        /// Number of days of out of sample days
        /// </summary>
        [JsonProperty(PropertyName = "outOfSampleDays")]
        public int? OutOfSampleDays { get; set; }
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
        [JsonProperty(PropertyName = "backtest")]
        public Backtest Backtest { get; set; }
    }

    /// <summary>
    /// Collection container for a list of backtests for a project
    /// </summary>
    public class BacktestList : RestResponse
    {
        /// <summary>
        /// Collection of summarized backtest objects
        /// </summary>
        [JsonProperty(PropertyName = "backtests")]
        public List<Backtest> Backtests { get; set; }
    }

    /// <summary>
    /// Collection container for a list of backtest tags
    /// </summary>
    public class BacktestTags : RestResponse
    {
        /// <summary>
        /// Collection of tags for a backtest
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; }
    }
}
