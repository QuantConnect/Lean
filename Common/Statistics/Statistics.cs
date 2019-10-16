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
using System.Globalization;
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
        /// Retrieve a static S-P500 Benchmark for the statistics calculations. Update the benchmark once per day.
        /// </summary>
        public static SortedDictionary<DateTime, decimal> YahooSPYBenchmark
        {
            get
            {
                var benchmark = new SortedDictionary<DateTime, decimal>();
                var url = "http://real-chart.finance.yahoo.com/table.csv?s=SPY&a=11&b=31&c=1997&d=" + (DateTime.Now.Month - 1) + "&e=" + DateTime.Now.Day + "&f=" + DateTime.Now.Year + "&g=d&ignore=.csv";
                using (var net = new WebClient())
                {
                    net.Proxy = WebRequest.GetSystemWebProxy();
                    var data = net.DownloadString(url);
                    var first = true;
                    using (var sr = new StreamReader(data.ToStream()))
                    {
                        while (sr.Peek() >= 0)
                        {
                            var line = sr.ReadLine();
                            if (first)
                            {
                                first = false;
                                continue;
                            }
                            if (line == null) continue;
                            var csv = line.Split(',');
                            benchmark.Add(Parse.DateTime(csv[0]), csv[6].ConvertInvariant<decimal>());
                        }
                    }
                }
                return benchmark;
            }
        }

        /// <summary>
        /// Convert the charting data into an equity array.
        /// </summary>
        /// <remarks>This is required to convert the equity plot into a usable form for the statistics calculation</remarks>
        /// <param name="points">ChartPoints Array</param>
        /// <returns>SortedDictionary of the equity decimal values ordered in time</returns>
        private static SortedDictionary<DateTime, decimal> ChartPointToDictionary(IEnumerable<ChartPoint> points)
        {
            var dictionary = new SortedDictionary<DateTime, decimal>();
            try
            {
                foreach (var point in points)
                {
                    var x = Time.UnixTimeStampToDateTime(point.x);
                    if (!dictionary.ContainsKey(x))
                    {
                        dictionary.Add(x, point.y);
                    }
                    else
                    {
                        dictionary[x] = point.y;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return dictionary;
        }


        /// <summary>
        /// Run a full set of orders and return a Dictionary of statistics.
        /// </summary>
        /// <param name="pointsEquity">Equity value over time.</param>
        /// <param name="profitLoss">profit loss from trades</param>
        /// <param name="pointsPerformance"> Daily performance</param>
        /// <param name="unsortedBenchmark"> Benchmark data as dictionary. Data does not need to be ordered</param>
        /// <param name="startingCash">Amount of starting cash in USD </param>
        /// <param name="totalFees">The total fees incurred over the life time of the algorithm</param>
        /// <param name="totalTrades">Total number of orders executed.</param>
        /// <param name="tradingDaysPerYear">Number of trading days per year</param>
        /// <returns>Statistics Array, Broken into Annual Periods</returns>
        public static Dictionary<string, string> Generate(IEnumerable<ChartPoint> pointsEquity,
            SortedDictionary<DateTime, decimal> profitLoss,
            IEnumerable<ChartPoint> pointsPerformance,
            Dictionary<DateTime, decimal> unsortedBenchmark,
            decimal startingCash,
            decimal totalFees,
            decimal totalTrades,
            double tradingDaysPerYear = 252
            )
        {
            //Initialise the response:
            double riskFreeRate = 0;
            decimal totalClosedTrades = 0;
            decimal totalWins = 0;
            decimal totalLosses = 0;
            decimal averageWin = 0;
            decimal averageLoss = 0;
            decimal averageWinRatio = 0;
            decimal winRate = 0;
            decimal lossRate = 0;
            decimal totalNetProfit = 0;
            double fractionOfYears = 1;
            decimal profitLossValue = 0, runningCash = startingCash;
            decimal algoCompoundingPerformance = 0;
            decimal finalBenchmarkCash = 0;
            decimal benchCompoundingPerformance = 0;
            var years = new List<int>();
            var annualTrades = new SortedDictionary<int, int>();
            var annualWins = new SortedDictionary<int, int>();
            var annualLosses = new SortedDictionary<int, int>();
            var annualLossTotal = new SortedDictionary<int, decimal>();
            var annualWinTotal = new SortedDictionary<int, decimal>();
            var annualNetProfit = new SortedDictionary<int, decimal>();
            var statistics = new Dictionary<string, string>();
            var dtPrevious = new DateTime();
            var listPerformance = new List<double>();
            var listBenchmark = new List<double>();
            var equity = new SortedDictionary<DateTime, decimal>();
            var performance = new SortedDictionary<DateTime, decimal>();
            SortedDictionary<DateTime, decimal>  benchmark = null;
            try
            {
                //Get array versions of the performance:
                performance = ChartPointToDictionary(pointsPerformance);
                equity = ChartPointToDictionary(pointsEquity);
                performance.Values.ToList().ForEach(i => listPerformance.Add((double)(i / 100)));
                benchmark = new SortedDictionary<DateTime, decimal>(unsortedBenchmark);

                // to find the delta in benchmark for first day, we need to know the price at the opening
                // moment of the day, but since we cannot find this, we cannot find the first benchmark's delta,
                // so we pad it with Zero. If running a short backtest this will skew results, longer backtests
                // will not be affected much
                listBenchmark.Add(0);

                //Get benchmark performance array for same period:
                benchmark.Keys.ToList().ForEach(dt =>
                {
                    if (dt >= equity.Keys.FirstOrDefault().AddDays(-1) && dt < equity.Keys.LastOrDefault())
                    {
                        decimal previous;
                        if (benchmark.TryGetValue(dtPrevious, out previous) && previous != 0)
                        {
                            var deltaBenchmark = (benchmark[dt] - previous)/previous;
                            listBenchmark.Add((double)(deltaBenchmark));
                        }
                        else
                        {
                            listBenchmark.Add(0);
                        }
                        dtPrevious = dt;
                    }
                });

                // TODO : if these lists are required to be the same length then we should create structure to pair the values, this way, by contract it will be enforced.

                //THIS SHOULD NEVER HAPPEN --> But if it does, log it and fail silently.
                while (listPerformance.Count < listBenchmark.Count)
                {
                    listPerformance.Add(0);
                    Log.Error("Statistics.Generate(): Padded Performance");
                }
                while (listPerformance.Count > listBenchmark.Count)
                {
                    listBenchmark.Add(0);
                    Log.Error("Statistics.Generate(): Padded Benchmark");
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "Dic-Array Convert:");
            }

            try
            {
                //Number of years in this dataset:
                fractionOfYears = (equity.Keys.LastOrDefault() - equity.Keys.FirstOrDefault()).TotalDays / 365;
            }
            catch (Exception err)
            {
                Log.Error(err, "Fraction of Years:");
            }

            try
            {
                if (benchmark != null)
                {
                    algoCompoundingPerformance = CompoundingAnnualPerformance(startingCash, equity.Values.LastOrDefault(), (decimal) fractionOfYears);
                    finalBenchmarkCash = ((benchmark.Values.Last() - benchmark.Values.First())/benchmark.Values.First())*startingCash;
                    benchCompoundingPerformance = CompoundingAnnualPerformance(startingCash, finalBenchmarkCash, (decimal) fractionOfYears);
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "Compounding:");
            }

            try
            {
                //Run over each equity day:
                foreach (var closedTrade in profitLoss.Keys)
                {
                    profitLossValue = profitLoss[closedTrade];

                    //Check if this date is in the "years" array:
                    var year = closedTrade.Year;
                    if (!years.Contains(year))
                    {
                        //Initialise a new year holder:
                        years.Add(year);
                        annualTrades.Add(year, 0);
                        annualWins.Add(year, 0);
                        annualWinTotal.Add(year, 0);
                        annualLosses.Add(year, 0);
                        annualLossTotal.Add(year, 0);
                    }

                    //Add another trade:
                    annualTrades[year]++;

                    //Profit loss tracking:
                    if (profitLossValue > 0)
                    {
                        annualWins[year]++;
                        annualWinTotal[year] += profitLossValue / runningCash;
                    }
                    else
                    {
                        annualLosses[year]++;
                        annualLossTotal[year] += profitLossValue / runningCash;
                    }

                    //Increment the cash:
                    runningCash += profitLossValue;
                }

                //Get the annual percentage of profit and loss:
                foreach (var year in years)
                {
                    annualNetProfit[year] = (annualWinTotal[year] + annualLossTotal[year]);
                }

                //Sum the totals:
                try
                {
                    if (profitLoss.Keys.Count > 0)
                    {
                        totalClosedTrades = annualTrades.Values.Sum();
                        totalWins = annualWins.Values.Sum();
                        totalLosses = annualLosses.Values.Sum();
                        totalNetProfit = (equity.Values.LastOrDefault() / startingCash) - 1;

                        //-> Handle Div/0 Errors
                        if (totalWins == 0)
                        {
                            averageWin = 0;
                        }
                        else
                        {
                            averageWin = annualWinTotal.Values.Sum() / totalWins;
                        }
                        if (totalLosses == 0)
                        {
                            averageLoss = 0;
                            averageWinRatio = 0;
                        }
                        else
                        {
                            averageLoss = annualLossTotal.Values.Sum() / totalLosses;
                            averageWinRatio = Math.Abs(averageWin / averageLoss);
                        }
                        if (totalTrades == 0)
                        {
                            winRate = 0;
                            lossRate = 0;
                        }
                        else
                        {
                            winRate = Math.Round(totalWins / totalClosedTrades, 5);
                            lossRate = Math.Round(totalLosses / totalClosedTrades, 5);
                        }
                    }

                }
                catch (Exception err)
                {
                    Log.Error(err, "Second Half:");
                }

                var profitLossRatio = ProfitLossRatio(averageWin, averageLoss);
                var profitLossRatioHuman = profitLossRatio.ToString(CultureInfo.InvariantCulture);
                if (profitLossRatio == -1) profitLossRatioHuman = "0";

                //Add the over all results first, break down by year later:
                statistics = new Dictionary<string, string> {
                    { "Total Trades", Math.Round(totalTrades, 0).ToStringInvariant() },
                    { "Average Win", Math.Round(averageWin * 100, 2).ToStringInvariant() + "%"  },
                    { "Average Loss", Math.Round(averageLoss * 100, 2).ToStringInvariant() + "%" },
                    { "Compounding Annual Return", Math.Round(algoCompoundingPerformance * 100, 3).ToStringInvariant() + "%" },
                    { "Drawdown", (DrawdownPercent(equity, 3) * 100).ToStringInvariant() + "%" },
                    { "Expectancy", Math.Round((winRate * averageWinRatio) - (lossRate), 3).ToStringInvariant() },
                    { "Net Profit", Math.Round(totalNetProfit * 100, 3).ToStringInvariant() + "%"},
                    { "Sharpe Ratio", Math.Round(SharpeRatio(listPerformance, riskFreeRate), 3).ToStringInvariant() },
                    { "Loss Rate", Math.Round(lossRate * 100).ToStringInvariant() + "%" },
                    { "Win Rate", Math.Round(winRate * 100).ToStringInvariant() + "%" },
                    { "Profit-Loss Ratio", profitLossRatioHuman },
                    { "Alpha", Math.Round(Alpha(listPerformance, listBenchmark, riskFreeRate), 3).ToStringInvariant() },
                    { "Beta", Math.Round(Beta(listPerformance, listBenchmark), 3).ToStringInvariant() },
                    { "Annual Standard Deviation", Math.Round(AnnualStandardDeviation(listPerformance, tradingDaysPerYear), 3).ToStringInvariant() },
                    { "Annual Variance", Math.Round(AnnualVariance(listPerformance, tradingDaysPerYear), 3).ToStringInvariant() },
                    { "Information Ratio", Math.Round(InformationRatio(listPerformance, listBenchmark), 3).ToStringInvariant() },
                    { "Tracking Error", Math.Round(TrackingError(listPerformance, listBenchmark), 3).ToStringInvariant() },
                    { "Treynor Ratio", Math.Round(TreynorRatio(listPerformance, listBenchmark, riskFreeRate), 3).ToStringInvariant() },
                    { "Total Fees", "$" + totalFees.ToStringInvariant("0.00") }
                };
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return statistics;
        }

        /// <summary>
        /// Return profit loss ratio safely avoiding divide by zero errors.
        /// </summary>
        /// <param name="averageWin"></param>
        /// <param name="averageLoss"></param>
        /// <returns></returns>
        public static decimal ProfitLossRatio(decimal averageWin, decimal averageLoss)
        {
            if (averageLoss == 0) return -1;
            return Math.Round(averageWin / Math.Abs(averageLoss), 2);
        }

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
        /// Drawdown maximum value
        /// </summary>
        /// <param name="equityOverTime">Array of portfolio value over time.</param>
        /// <param name="rounding">Round the drawdown statistics.</param>
        /// <returns>Draw down percentage over period.</returns>
        public static decimal DrawdownValue(SortedDictionary<DateTime, decimal> equityOverTime, int rounding = 2)
        {
            //Initialise:
            var priceMaximum = 0;
            var previousMinimum = 0;
            var previousMaximum = 0;

            try
            {
                var lPrices = equityOverTime.Values.ToList();

                for (var id = 0; id < lPrices.Count; id++)
                {
                    if (lPrices[id] >= lPrices[priceMaximum])
                    {
                        priceMaximum = id;
                    }
                    else
                    {
                        if ((lPrices[priceMaximum] - lPrices[id]) > (lPrices[previousMaximum] - lPrices[previousMinimum]))
                        {
                            previousMaximum = priceMaximum;
                            previousMinimum = id;
                        }
                    }
                }
                return Math.Round((lPrices[previousMaximum] - lPrices[previousMinimum]), rounding);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return 0;
        } // End Drawdown:


        /// <summary>
        /// Annual compounded returns statistic based on the final-starting capital and years.
        /// </summary>
        /// <param name="startingCapital">Algorithm starting capital</param>
        /// <param name="finalCapital">Algorithm final capital</param>
        /// <param name="years">Years trading</param>
        /// <returns>Decimal fraction for annual compounding performance</returns>
        public static decimal CompoundingAnnualPerformance(decimal startingCapital, decimal finalCapital, decimal years)
        {
            return (years == 0 ? 0d : Math.Pow((double)finalCapital / (double)startingCapital, 1 / (double)years) - 1).SafeDecimalCast();
        }

        /// <summary>
        /// Annualized return statistic calculated as an average of daily trading performance multiplied by the number of trading days per year.
        /// </summary>
        /// <param name="performance">Dictionary collection of double performance values</param>
        /// <param name="tradingDaysPerYear">Trading days per year for the assets in portfolio</param>
        /// <remarks>May be unaccurate for forex algorithms with more trading days in a year</remarks>
        /// <returns>Double annual performance percentage</returns>
        public static double AnnualPerformance(List<double> performance, double tradingDaysPerYear = 252)
        {
            return performance.Average() * tradingDaysPerYear;
        }

        /// <summary>
        /// Annualized variance statistic calculation using the daily performance variance and trading days per year.
        /// </summary>
        /// <param name="performance"></param>
        /// <param name="tradingDaysPerYear"></param>
        /// <remarks>Invokes the variance extension in the MathNet Statistics class</remarks>
        /// <returns>Annual variance value</returns>
        public static double AnnualVariance(List<double> performance, double tradingDaysPerYear = 252)
        {
            return (performance.Variance())*tradingDaysPerYear;
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
        public static double AnnualStandardDeviation(List<double> performance, double tradingDaysPerYear = 252)
        {
            return Math.Sqrt(performance.Variance() * tradingDaysPerYear);
        }

        /// <summary>
        /// Algorithm "beta" statistic - the covariance between the algorithm and benchmark performance, divided by benchmark's variance
        /// </summary>
        /// <param name="algoPerformance">Collection of double values for algorithm daily performance.</param>
        /// <param name="benchmarkPerformance">Collection of double benchmark daily performance values.</param>
        /// <remarks>Invokes the variance and covariance extensions in the MathNet Statistics class</remarks>
        /// <returns>Value for beta</returns>
        public static double Beta(List<double> algoPerformance, List<double> benchmarkPerformance)
        {
            return algoPerformance.Covariance(benchmarkPerformance) / benchmarkPerformance.Variance();
        }

        /// <summary>
        /// Algorithm "Alpha" statistic - abnormal returns over the risk free rate and the relationshio (beta) with the benchmark returns.
        /// </summary>
        /// <param name="algoPerformance">Collection of double algorithm daily performance values.</param>
        /// <param name="benchmarkPerformance">Collection of double benchmark daily performance values.</param>
        /// <param name="riskFreeRate">Risk free rate of return for the T-Bonds.</param>
        /// <returns>Value for alpha</returns>
        public static double Alpha(List<double> algoPerformance, List<double> benchmarkPerformance, double riskFreeRate)
        {
            return AnnualPerformance(algoPerformance) - (riskFreeRate + Beta(algoPerformance, benchmarkPerformance) * (AnnualPerformance(benchmarkPerformance) - riskFreeRate));
        }

        /// <summary>
        /// Tracking error volatility (TEV) statistic - a measure of how closely a portfolio follows the index to which it is benchmarked
        /// </summary>
        /// <remarks>If algo = benchmark, TEV = 0</remarks>
        /// <param name="algoPerformance">Double collection of algorithm daily performance values</param>
        /// <param name="benchmarkPerformance">Double collection of benchmark daily performance values</param>
        /// <returns>Value for tracking error</returns>
        public static double TrackingError(List<double> algoPerformance, List<double> benchmarkPerformance)
        {
            return Math.Sqrt(AnnualVariance(algoPerformance) - 2 * Correlation.Pearson(algoPerformance, benchmarkPerformance) * AnnualStandardDeviation(algoPerformance) * AnnualStandardDeviation(benchmarkPerformance) + AnnualVariance(benchmarkPerformance));
        }


        /// <summary>
        /// Information ratio - risk adjusted return
        /// </summary>
        /// <param name="algoPerformance">Collection of doubles for the daily algorithm daily performance</param>
        /// <param name="benchmarkPerformance">Collection of doubles for the benchmark daily performance</param>
        /// <remarks>(risk = tracking error volatility, a volatility measures that considers the volatility of both algo and benchmark)</remarks>
        /// <seealso cref="TrackingError"/>
        /// <returns>Value for information ratio</returns>
        public static double InformationRatio(List<double> algoPerformance, List<double> benchmarkPerformance)
        {
            return (AnnualPerformance(algoPerformance) - AnnualPerformance(benchmarkPerformance)) / (TrackingError(algoPerformance, benchmarkPerformance));
        }

        /// <summary>
        /// Sharpe ratio with respect to risk free rate: measures excess of return per unit of risk.
        /// </summary>
        /// <remarks>With risk defined as the algorithm's volatility</remarks>
        /// <param name="algoPerformance">Collection of double values for the algorithm daily performance</param>
        /// <param name="riskFreeRate"></param>
        /// <returns>Value for sharpe ratio</returns>
        public static double SharpeRatio(List<double> algoPerformance, double riskFreeRate)
        {
            return (AnnualPerformance(algoPerformance) - riskFreeRate) / (AnnualStandardDeviation(algoPerformance));
        }

        /// <summary>
        /// Treynor ratio statistic is a measurement of the returns earned in excess of that which could have been earned on an investment that has no diversifiable risk
        /// </summary>
        /// <param name="algoPerformance">Collection of double algorithm daily performance values</param>
        /// <param name="benchmarkPerformance">Collection of double benchmark daily performance values</param>
        /// <param name="riskFreeRate">Risk free rate of return</param>
        /// <returns>double Treynor ratio</returns>
        public static double TreynorRatio(List<double> algoPerformance, List<double> benchmarkPerformance, double riskFreeRate)
        {
            return (AnnualPerformance(algoPerformance) - riskFreeRate) / (Beta(algoPerformance, benchmarkPerformance));
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
    } // End of Statistics

} // End of Namespace
