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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// PerformanceMetrics contains the names of the various performance metrics used for evaluation purposes.
    /// </summary>
    public static class PerformanceMetrics
    {
        /// <summary>
        /// Algorithm "Alpha" statistic - abnormal returns over the risk free rate and the relationshio (beta) with the benchmark returns.
        /// </summary>
        public const string Alpha = "Alpha";

        /// <summary>
        /// Annualized standard deviation
        /// </summary>
        public const string AnnualStandardDeviation = "Annual Standard Deviation";

        /// <summary>
        /// Annualized variance statistic calculation using the daily performance variance and trading days per year.
        /// </summary>
        public const string AnnualVariance = "Annual Variance";

        /// <summary>
        /// The average rate of return for losing trades
        /// </summary>
        public const string AverageLoss = "Average Loss";

        /// <summary>
        /// The average rate of return for winning trades
        /// </summary>
        public const string AverageWin = "Average Win";

        /// <summary>
        /// Algorithm "beta" statistic - the covariance between the algorithm and benchmark performance, divided by benchmark's variance
        /// </summary>
        public const string Beta = "Beta";

        /// <summary>
        /// Annual compounded returns statistic based on the final-starting capital and years.
        /// </summary>
        public const string CompoundingAnnualReturn = "Compounding Annual Return";

        /// <summary>
        /// Drawdown maximum percentage.
        /// </summary>
        public const string Drawdown = "Drawdown";

        /// <summary>
        /// Total capacity of the algorithm
        /// </summary>
        public const string EstimatedStrategyCapacity = "Estimated Strategy Capacity";

        /// <summary>
        /// The expected value of the rate of return
        /// </summary>
        public const string Expectancy = "Expectancy";

        /// <summary>
        /// Initial Equity Total Value
        /// </summary>
        public const string StartEquity = "Start Equity";

        /// <summary>
        /// Final Equity Total Value
        /// </summary>
        public const string EndEquity = "End Equity";

        /// <summary>
        /// Information ratio - risk adjusted return
        /// </summary>
        public const string InformationRatio = "Information Ratio";

        /// <summary>
        /// The ratio of the number of losing trades to the total number of trades
        /// </summary>
        public const string LossRate = "Loss Rate";

        /// <summary>
        /// Total net profit percentage
        /// </summary>
        public const string NetProfit = "Net Profit";

        /// <summary>
        /// Probabilistic Sharpe Ratio is a probability measure associated with the Sharpe ratio.
        /// It informs us of the probability that the estimated Sharpe ratio is greater than a chosen benchmark
        /// </summary>
        /// <remarks>See https://www.quantconnect.com/forum/discussion/6483/probabilistic-sharpe-ratio/p1</remarks>
        public const string ProbabilisticSharpeRatio = "Probabilistic Sharpe Ratio";

        /// <summary>
        /// The ratio of the average win rate to the average loss rate
        /// </summary>
        /// <remarks>If the average loss rate is zero, ProfitLossRatio is set to 0</remarks>
        public const string ProfitLossRatio = "Profit-Loss Ratio";

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        public const string SharpeRatio = "Sharpe Ratio";

        /// <summary>
        /// Sortino ratio with respect to risk free rate: measures excess of return per unit of downside risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        public const string SortinoRatio = "Sortino Ratio";

        /// <summary>
        /// Total amount of fees in the account currency
        /// </summary>
        public const string TotalFees = "Total Fees";

        /// <summary>
        /// Total amount of orders in the algorithm
        /// </summary>
        public const string TotalOrders = "Total Orders";

        /// <summary>
        /// Tracking error volatility (TEV) statistic - a measure of how closely a portfolio follows the index to which it is benchmarked
        /// </summary>
        /// <remarks>If algo = benchmark, TEV = 0</remarks>
        public const string TrackingError = "Tracking Error";

        /// <summary>
        /// Treynor ratio statistic is a measurement of the returns earned in excess of that which could have been earned on an investment that has no diversifiable risk
        /// </summary>
        public const string TreynorRatio = "Treynor Ratio";

        /// <summary>
        /// The ratio of the number of winning trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, WinRate is set to zero</remarks>
        public const string WinRate = "Win Rate";

        /// <summary>
        /// Provide a reference to the lowest capacity symbol used in scaling down the capacity for debugging.
        /// </summary>
        public const string LowestCapacityAsset = "Lowest Capacity Asset";

        /// <summary>
        /// The average Portfolio Turnover
        /// </summary>
        public const string PortfolioTurnover = "Portfolio Turnover";
    }
}
