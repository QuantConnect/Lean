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
using System.IO;
using System.Linq;
using System.Net;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using QuantConnect.Logging;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Calculate all the statistics required from the backtest, based on the equity curve and the profit loss statement.
    /// </summary>
    /// <remarks>This is a particularly ugly class and one of the first ones written. It should be thrown out and re-written.</remarks>
    public class Statistics
    {
        /// <summary>
        /// Drawdown maximum percentage.
        /// </summary>
        /// <param name="equityOverTime"></param>
        /// <param name="rounding"></param>
        /// <returns></returns>
        public static decimal DrawdownPercent(SortedDictionary<DateTime, decimal> equityOverTime, int rounding = 2)
        {
            var dd = 0m;
            try
            {
                var lPrices = equityOverTime.Values.ToList();
                var lDrawdowns = new List<decimal>();
                var high = lPrices[0];
                foreach (var price in lPrices)
                {
                    if (price >= high) high = price;
                    lDrawdowns.Add((price/high) - 1);
                }
                dd = Math.Round(Math.Abs(lDrawdowns.Min()), rounding);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return dd;
        }

        /// <summary>
        /// Annual compounded returns statistic based on the final-starting capital and years.
        /// </summary>
        /// <param name="startingCapital">Algorithm starting capital</param>
        /// <param name="finalCapital">Algorithm final capital</param>
        /// <param name="years">Years trading</param>
        /// <returns>Decimal fraction for annual compounding performance</returns>
        public static decimal CompoundingAnnualPerformance(decimal startingCapital, decimal finalCapital, decimal years)
        {
            if (years == 0 || startingCapital == 0)
            {
                return 0;
            }
            var power = 1 / (double)years;
            var baseNumber = (double)finalCapital / (double)startingCapital;
            var result = Math.Pow(baseNumber, power) - 1;
            return result.IsNaNOrInfinity() ? 0 : result.SafeDecimalCast();
        }

        /// <summary>
        /// Annualized return statistic calculated as an average of daily trading performance multiplied by the number of trading days per year.
        /// </summary>
        /// <param name="performance">Dictionary collection of double performance values</param>
        /// <param name="tradingDaysPerYear">Trading days per year for the assets in portfolio</param>
        /// <remarks>May be unaccurate for forex algorithms with more trading days in a year</remarks>
        /// <returns>Double annual performance percentage</returns>
        public static double AnnualPerformance(List<double> performance, double tradingDaysPerYear)
        {
            return Math.Pow((performance.Average() + 1), tradingDaysPerYear) - 1;
        }

        /// <summary>
        /// Annualized variance statistic calculation using the daily performance variance and trading days per year.
        /// </summary>
        /// <param name="performance"></param>
        /// <param name="tradingDaysPerYear"></param>
        /// <remarks>Invokes the variance extension in the MathNet Statistics class</remarks>
        /// <returns>Annual variance value</returns>
        public static double AnnualVariance(List<double> performance, double tradingDaysPerYear)
        {
            var variance = performance.Variance();
            return variance.IsNaNOrZero() ? 0 : variance * tradingDaysPerYear;
        }

        /// <summary>
        /// Annualized standard deviation
        /// </summary>
        /// <param name="performance">Collection of double values for daily performance</param>
        /// <param name="tradingDaysPerYear">Number of trading days for the assets in portfolio to get annualize standard deviation.</param>
        /// <remarks>
        ///     Invokes the variance extension in the MathNet Statistics class.
        ///     Feasibly the trading days per year can be fetched from the dictionary of performance which includes the date-times to get the range; if is more than 1 year data.
        /// </remarks>
        /// <returns>Value for annual standard deviation</returns>
        public static double AnnualStandardDeviation(List<double> performance, double tradingDaysPerYear)
        {
            return Math.Sqrt(AnnualVariance(performance, tradingDaysPerYear));
        }

        /// <summary>
        /// Annualized variance statistic calculation using the daily performance variance and trading days per year.
        /// </summary>
        /// <param name="performance"></param>
        /// <param name="tradingDaysPerYear"></param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return</param>
        /// <remarks>Invokes the variance extension in the MathNet Statistics class</remarks>
        /// <returns>Annual variance value</returns>
        public static double AnnualDownsideVariance(List<double> performance, double tradingDaysPerYear, double minimumAcceptableReturn = 0)
        {
            return AnnualVariance(performance.Where(ret => ret < minimumAcceptableReturn).ToList(), tradingDaysPerYear);
        }

        /// <summary>
        /// Annualized downside standard deviation
        /// </summary>
        /// <param name="performance">Collection of double values for daily performance</param>
        /// <param name="tradingDaysPerYear">Number of trading days for the assets in portfolio to get annualize standard deviation.</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return</param>
        /// <returns>Value for annual downside standard deviation</returns>
        public static double AnnualDownsideStandardDeviation(List<double> performance, double tradingDaysPerYear, double minimumAcceptableReturn = 0)
        {
            return Math.Sqrt(AnnualDownsideVariance(performance, tradingDaysPerYear, minimumAcceptableReturn));
        }

        /// <summary>
        /// Tracking error volatility (TEV) statistic - a measure of how closely a portfolio follows the index to which it is benchmarked
        /// </summary>
        /// <remarks>If algo = benchmark, TEV = 0</remarks>
        /// <param name="algoPerformance">Double collection of algorithm daily performance values</param>
        /// <param name="benchmarkPerformance">Double collection of benchmark daily performance values</param>
        /// <param name="tradingDaysPerYear">Number of trading days per year</param>
        /// <returns>Value for tracking error</returns>
        public static double TrackingError(List<double> algoPerformance, List<double> benchmarkPerformance, double tradingDaysPerYear)
        {
            // Un-equal lengths will blow up other statistics, but this will handle the case here
            if (algoPerformance.Count() != benchmarkPerformance.Count())
            {
                return 0.0;
            }

            var performanceDifference = new List<double>();
            for (var i = 0; i < algoPerformance.Count(); i++)
            {
                performanceDifference.Add(algoPerformance[i] - benchmarkPerformance[i]);
            }

            return Math.Sqrt(AnnualVariance(performanceDifference, tradingDaysPerYear));
        }

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        /// <param name="averagePerformance">Average daily performance</param>
        /// <param name="standardDeviation">Standard deviation of the daily performance</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <returns>Value for sharpe ratio</returns>
        public static double SharpeRatio(double averagePerformance, double standardDeviation, double riskFreeRate)
        {
            return standardDeviation == 0 ? 0 : (averagePerformance - riskFreeRate) / standardDeviation;
        }

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        /// <param name="averagePerformance">Average daily performance</param>
        /// <param name="standardDeviation">Standard deviation of the daily performance</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <returns>Value for sharpe ratio</returns>
        public static decimal SharpeRatio(decimal averagePerformance, decimal standardDeviation, decimal riskFreeRate)
        {
            return SharpeRatio((double)averagePerformance, (double)standardDeviation, (double)riskFreeRate).SafeDecimalCast();
        }

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        /// <param name="algoPerformance">Collection of double values for the algorithm daily performance</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="tradingDaysPerYear">Trading days per year for the assets in portfolio</param>
        /// <returns>Value for sharpe ratio</returns>
        public static double SharpeRatio(List<double> algoPerformance, double riskFreeRate, double tradingDaysPerYear)
        {
            return SharpeRatio(AnnualPerformance(algoPerformance, tradingDaysPerYear), AnnualStandardDeviation(algoPerformance, tradingDaysPerYear), riskFreeRate);
        }

        /// <summary>
        /// Sortino ratio with respect to risk free rate: measures excess of return per unit of downside risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        /// <param name="algoPerformance">Collection of double values for the algorithm daily performance</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="tradingDaysPerYear">Trading days per year for the assets in portfolio</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return for Sortino ratio calculation</param>
        /// <returns>Value for Sortino ratio</returns>
        public static double SortinoRatio(List<double> algoPerformance, double riskFreeRate, double tradingDaysPerYear, double minimumAcceptableReturn = 0)
        {
            return SharpeRatio(AnnualPerformance(algoPerformance, tradingDaysPerYear), AnnualDownsideStandardDeviation(algoPerformance, tradingDaysPerYear, minimumAcceptableReturn), riskFreeRate);
        }

        /// <summary>
        /// Helper method to calculate the probabilistic sharpe ratio
        /// </summary>
        /// <param name="listPerformance">The list of algorithm performance values</param>
        /// <param name="benchmarkSharpeRatio">The benchmark sharpe ratio to use</param>
        /// <returns>Probabilistic Sharpe Ratio</returns>
        public static double ProbabilisticSharpeRatio(List<double> listPerformance,
             double benchmarkSharpeRatio)
        {
            var observedSharpeRatio = ObservedSharpeRatio(listPerformance);

            var skewness = listPerformance.Skewness();
            var kurtosis = listPerformance.Kurtosis();

            var operandA = skewness * observedSharpeRatio;
            var operandB = ((kurtosis - 1) / 4) * (Math.Pow(observedSharpeRatio, 2));

            // Calculated standard deviation of point estimate
            var estimateStandardDeviation = Math.Pow((1 - operandA + operandB) / (listPerformance.Count - 1), 0.5);

            if (double.IsNaN(estimateStandardDeviation))
            {
                return 0;
            }

            // Calculate PSR(benchmark)
            var value = estimateStandardDeviation.IsNaNOrZero() ? 0 : (observedSharpeRatio - benchmarkSharpeRatio) / estimateStandardDeviation;
            return (new Normal()).CumulativeDistribution(value);
        }

        /// <summary>
        /// Calculates the observed sharpe ratio
        /// </summary>
        /// <param name="listPerformance">The performance samples to use</param>
        /// <returns>The observed sharpe ratio</returns>
        public static double ObservedSharpeRatio(List<double> listPerformance)
        {
            var performanceAverage = listPerformance.Average();
            var standardDeviation = listPerformance.StandardDeviation();
            // we don't annualize it
            return standardDeviation.IsNaNOrZero() ? 0 : performanceAverage / standardDeviation;
        }

        /// <summary>
        /// Calculate the drawdown between a high and current value
        /// </summary>
        /// <param name="current">Current value</param>
        /// <param name="high">Latest maximum</param>
        /// <param name="roundingDecimals">Digits to round the result too</param>
        /// <returns>Drawdown percentage</returns>
        public static decimal DrawdownPercent(decimal current, decimal high, int roundingDecimals = 2)
        {
            if (high == 0)
            {
                throw new ArgumentException("High value must not be 0");
            }

            var drawdownPercentage = ((current / high) - 1) * 100;
            return Math.Round(drawdownPercentage, roundingDecimals);
        }

    } // End of Statistics

} // End of Namespace
