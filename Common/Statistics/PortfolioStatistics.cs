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
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The <see cref="PortfolioStatistics"/> class represents a set of statistics calculated from equity and benchmark samples
    /// </summary>
    public class PortfolioStatistics
    {
        /// <summary>
        /// The average rate of return for winning trades
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageWinRate { get; set; }

        /// <summary>
        /// The average rate of return for losing trades
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AverageLossRate { get; set; }

        /// <summary>
        /// The ratio of the average win rate to the average loss rate
        /// </summary>
        /// <remarks>If the average loss rate is zero, ProfitLossRatio is set to 0</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProfitLossRatio { get; set; }

        /// <summary>
        /// The ratio of the number of winning trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, WinRate is set to zero</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal WinRate { get; set; }

        /// <summary>
        /// The ratio of the number of losing trades to the total number of trades
        /// </summary>
        /// <remarks>If the total number of trades is zero, LossRate is set to zero</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal LossRate { get; set; }

        /// <summary>
        /// The expected value of the rate of return
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal Expectancy { get; set; }

        /// <summary>
        /// Initial Equity Total Value
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal StartEquity { get; set; }

        /// <summary>
        /// Final Equity Total Value
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal EndEquity { get; set; }

        /// <summary>
        /// Annual compounded returns statistic based on the final-starting capital and years.
        /// </summary>
        /// <remarks>Also known as Compound Annual Growth Rate (CAGR)</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal CompoundingAnnualReturn { get; set; }

        /// <summary>
        /// Drawdown maximum percentage.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal Drawdown { get; set; }

        /// <summary>
        /// The total net profit percentage.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TotalNetProfit { get; set; }

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal SharpeRatio { get; set; }

        /// <summary>
        /// Probabilistic Sharpe Ratio is a probability measure associated with the Sharpe ratio.
        /// It informs us of the probability that the estimated Sharpe ratio is greater than a chosen benchmark
        /// </summary>
        /// <remarks>See https://www.quantconnect.com/forum/discussion/6483/probabilistic-sharpe-ratio/p1</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ProbabilisticSharpeRatio { get; set; }

        /// <summary>
        /// Sortino ratio with respect to risk free rate: measures excess of return per unit of downside risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal SortinoRatio { get; set; }

        /// <summary>
        /// Algorithm "Alpha" statistic - abnormal returns over the risk free rate and the relationshio (beta) with the benchmark returns.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal Alpha { get; set; }

        /// <summary>
        /// Algorithm "beta" statistic - the covariance between the algorithm and benchmark performance, divided by benchmark's variance
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal Beta { get; set; }

        /// <summary>
        /// Annualized standard deviation
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AnnualStandardDeviation { get; set; }

        /// <summary>
        /// Annualized variance statistic calculation using the daily performance variance and trading days per year.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal AnnualVariance { get; set; }

        /// <summary>
        /// Information ratio - risk adjusted return
        /// </summary>
        /// <remarks>(risk = tracking error volatility, a volatility measures that considers the volatility of both algo and benchmark)</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal InformationRatio { get; set; }

        /// <summary>
        /// Tracking error volatility (TEV) statistic - a measure of how closely a portfolio follows the index to which it is benchmarked
        /// </summary>
        /// <remarks>If algo = benchmark, TEV = 0</remarks>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TrackingError { get; set; }

        /// <summary>
        /// Treynor ratio statistic is a measurement of the returns earned in excess of that which could have been earned on an investment that has no diversifiable risk
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal TreynorRatio { get; set; }

        /// <summary>
        /// The average Portfolio Turnover
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal PortfolioTurnover { get; set; }

        /// <summary>
        /// The 1-day VaR for the portfolio, using the Variance-covariance approach.
        /// Assumes a 99% confidence level, 1 year lookback period, and that the returns are normally distributed.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ValueAtRisk99 { get; set; }

        /// <summary>
        /// The 1-day VaR for the portfolio, using the Variance-covariance approach.
        /// Assumes a 95% confidence level, 1 year lookback period, and that the returns are normally distributed.
        /// </summary>
        [JsonConverter(typeof(JsonRoundingConverter))]
        public decimal ValueAtRisk95 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioStatistics"/> class
        /// </summary>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="portfolioTurnover">The algorithm portfolio turnover</param>
        /// <param name="listPerformance">The list of algorithm performance values</param>
        /// <param name="listBenchmark">The list of benchmark values</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to use</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year</param>
        /// <param name="winCount">
        /// The number of wins, including ITM options with profitLoss less than 0.
        /// If this and <paramref name="lossCount"/> are null, they will be calculated from <paramref name="profitLoss"/>
        /// </param>
        /// <param name="lossCount">The number of losses</param>
        public PortfolioStatistics(
            SortedDictionary<DateTime, decimal> profitLoss,
            SortedDictionary<DateTime, decimal> equity,
            SortedDictionary<DateTime, decimal> portfolioTurnover,
            List<double> listPerformance,
            List<double> listBenchmark,
            decimal startingCapital,
            IRiskFreeInterestRateModel riskFreeInterestRateModel,
            int tradingDaysPerYear,
            int? winCount = null,
            int? lossCount = null)
        {
            StartEquity = startingCapital;
            EndEquity = equity.LastOrDefault().Value;

            if (portfolioTurnover.Count > 0)
            {
                PortfolioTurnover = portfolioTurnover.Select(kvp => kvp.Value).Average();
            }

            if (startingCapital == 0
                // minimum amount of samples to calculate variance
                || listBenchmark.Count < 2
                || listPerformance.Count < 2)
            {
                return;
            }

            var runningCapital = startingCapital;
            var totalProfit = 0m;
            var totalLoss = 0m;
            var totalWins = 0;
            var totalLosses = 0;
            foreach (var pair in profitLoss)
            {
                var tradeProfitLoss = pair.Value;

                if (tradeProfitLoss > 0)
                {
                    totalProfit += tradeProfitLoss / runningCapital;
                    totalWins++;
                }
                else
                {
                    totalLoss += tradeProfitLoss / runningCapital;
                    totalLosses++;
                }

                runningCapital += tradeProfitLoss;
            }

            AverageWinRate = totalWins == 0 ? 0 : totalProfit / totalWins;
            AverageLossRate = totalLosses == 0 ? 0 : totalLoss / totalLosses;
            ProfitLossRatio = AverageLossRate == 0 ? 0 : AverageWinRate / Math.Abs(AverageLossRate);

            // Set the actual total wins and losses count.
            // Some options assignments (ITM) count as wins even though they are losses.
            if (winCount.HasValue && lossCount.HasValue)
            {
                totalWins = winCount.Value;
                totalLosses = lossCount.Value;
            }

            var totalTrades = totalWins + totalLosses;
            WinRate = totalTrades == 0 ? 0 : (decimal) totalWins / totalTrades;
            LossRate = totalTrades == 0 ? 0 : (decimal) totalLosses / totalTrades;
            Expectancy = WinRate * ProfitLossRatio - LossRate;

            if (startingCapital != 0)
            {
                TotalNetProfit = equity.Values.LastOrDefault() / startingCapital - 1;
            }

            var fractionOfYears = (decimal) (equity.Keys.LastOrDefault() - equity.Keys.FirstOrDefault()).TotalDays / 365;
            CompoundingAnnualReturn = Statistics.CompoundingAnnualPerformance(startingCapital, equity.Values.LastOrDefault(), fractionOfYears);

            Drawdown = DrawdownPercent(equity, 3);

            AnnualVariance = Statistics.AnnualVariance(listPerformance, tradingDaysPerYear).SafeDecimalCast();
            AnnualStandardDeviation = (decimal) Math.Sqrt((double) AnnualVariance);

            var benchmarkAnnualPerformance = GetAnnualPerformance(listBenchmark, tradingDaysPerYear);
            var annualPerformance = GetAnnualPerformance(listPerformance, tradingDaysPerYear);

            var riskFreeRate = riskFreeInterestRateModel.GetAverageRiskFreeRate(equity.Select(x => x.Key));
            SharpeRatio = AnnualStandardDeviation == 0 ? 0 : Statistics.SharpeRatio(annualPerformance, AnnualStandardDeviation, riskFreeRate);

            var annualDownsideDeviation = Statistics.AnnualDownsideStandardDeviation(listPerformance, tradingDaysPerYear).SafeDecimalCast();
            SortinoRatio = annualDownsideDeviation == 0 ? 0 : Statistics.SharpeRatio(annualPerformance, annualDownsideDeviation, riskFreeRate);

            var benchmarkVariance = listBenchmark.Variance();
            Beta = benchmarkVariance.IsNaNOrZero() ? 0 : (decimal) (listPerformance.Covariance(listBenchmark) / benchmarkVariance);

            Alpha = Beta == 0 ? 0 : annualPerformance - (riskFreeRate + Beta * (benchmarkAnnualPerformance - riskFreeRate));

            TrackingError = (decimal)Statistics.TrackingError(listPerformance, listBenchmark, (double)tradingDaysPerYear);

            InformationRatio = TrackingError == 0 ? 0 : (annualPerformance - benchmarkAnnualPerformance) / TrackingError;

            TreynorRatio = Beta == 0 ? 0 : (annualPerformance - riskFreeRate) / Beta;

            // deannualize a 1 sharpe ratio
            var benchmarkSharpeRatio = 1.0d / Math.Sqrt(tradingDaysPerYear);
            ProbabilisticSharpeRatio = Statistics.ProbabilisticSharpeRatio(listPerformance, benchmarkSharpeRatio).SafeDecimalCast();
            
            ValueAtRisk99 = GetValueAtRisk(listPerformance, tradingDaysPerYear, 0.99d);
            ValueAtRisk95 = GetValueAtRisk(listPerformance, tradingDaysPerYear, 0.95d);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioStatistics"/> class
        /// </summary>
        public PortfolioStatistics()
        {
        }

        /// <summary>
        /// Drawdown maximum percentage.
        /// </summary>
        /// <param name="equityOverTime">The list of daily equity values</param>
        /// <param name="rounding">The number of decimal places to round the result</param>
        /// <returns>The drawdown percentage</returns>
        private static decimal DrawdownPercent(SortedDictionary<DateTime, decimal> equityOverTime, int rounding = 2)
        {
            var prices = equityOverTime.Values.ToList();
            if (prices.Count == 0) return 0;

            var drawdowns = new List<decimal>();
            var high = prices[0];
            foreach (var price in prices)
            {
                if (price > high) high = price;
                if (high > 0) drawdowns.Add(price / high - 1);
            }

            return Math.Round(Math.Abs(drawdowns.Min()), rounding);
        }

        /// <summary>
        /// Annualized return statistic calculated as an average of daily trading performance multiplied by the number of trading days per year.
        /// </summary>
        /// <param name="performance">Dictionary collection of double performance values</param>
        /// <param name="tradingDaysPerYear">Trading days per year for the assets in portfolio</param>
        /// <remarks>May be inaccurate for forex algorithms with more trading days in a year</remarks>
        /// <returns>Double annual performance percentage</returns>
        private static decimal GetAnnualPerformance(List<double> performance, int tradingDaysPerYear)
        {
            try
            {
                return Statistics.AnnualPerformance(performance, tradingDaysPerYear).SafeDecimalCast();
            }
            catch (ArgumentException ex)
            {
                var partialSums = 0.0;
                var points = 0;
                double troublePoint = default;
                foreach(var point in performance)
                {
                    points++;
                    partialSums += point;
                    if (Math.Pow(partialSums / points, tradingDaysPerYear).IsNaNOrInfinity())
                    {
                        troublePoint = point;
                        break;
                    }
                }

                throw new ArgumentException($"PortfolioStatistics.GetAnnualPerformance(): An exception was thrown when trying to cast the annual performance value due to the following performance point: {troublePoint}. " +
                    $"The exception thrown was the following: {ex.Message}.");
            }
        }

        private static decimal GetValueAtRisk(
            List<double> performance,
            int lookbackPeriodDays,
            double confidenceLevel,
            int rounding = 3)
        {
            var periodPerformance = performance.TakeLast(lookbackPeriodDays);
            var mean = periodPerformance.Mean();
            var standardDeviation = periodPerformance.StandardDeviation();
            var valueAtRisk = (decimal)Normal.InvCDF(mean, standardDeviation, 1 - confidenceLevel);
            return Math.Round(valueAtRisk, rounding);
        }
    }
}
