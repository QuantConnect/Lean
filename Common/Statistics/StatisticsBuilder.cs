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
using System.Runtime.CompilerServices;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// The <see cref="StatisticsBuilder"/> class creates summary and rolling statistics from trades, equity and benchmark points
    /// </summary>
    public static class StatisticsBuilder
    {
        /// <summary>
        /// Generates the statistics and returns the results
        /// </summary>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="pointsEquity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="pointsPortfolioTurnover">The list of portfolio turnover daily samples</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <param name="totalFees">The total fees</param>
        /// <param name="totalOrders">The total number of transactions</param>
        /// <param name="estimatedStrategyCapacity">The estimated capacity of this strategy</param>
        /// <param name="accountCurrencySymbol">The account currency symbol</param>
        /// <param name="transactions">
        /// The transaction manager to get number of winning and losing transactions
        /// </param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to use</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year</param>       
        /// <returns>Returns a <see cref="StatisticsResults"/> object</returns>
        public static StatisticsResults Generate(
            List<Trade> trades,
            SortedDictionary<DateTime, decimal> profitLoss,
            List<ISeriesPoint> pointsEquity,
            List<ISeriesPoint> pointsPerformance,
            List<ISeriesPoint> pointsBenchmark,
            List<ISeriesPoint> pointsPortfolioTurnover,
            decimal startingCapital,
            decimal totalFees,
            int totalOrders,
            CapacityEstimate estimatedStrategyCapacity,
            string accountCurrencySymbol,
            SecurityTransactionManager transactions,
            IRiskFreeInterestRateModel riskFreeInterestRateModel,
            int tradingDaysPerYear)
        {
            var equity = ChartPointToDictionary(pointsEquity);

            var firstDate = equity.Keys.FirstOrDefault().Date;
            var lastDate = equity.Keys.LastOrDefault().Date;

            var totalPerformance = GetAlgorithmPerformance(firstDate, lastDate, trades, profitLoss, equity, pointsPerformance, pointsBenchmark,
                pointsPortfolioTurnover, startingCapital, transactions, riskFreeInterestRateModel, tradingDaysPerYear);
            var rollingPerformances = GetRollingPerformances(firstDate, lastDate, trades, profitLoss, equity, pointsPerformance, pointsBenchmark,
                pointsPortfolioTurnover, startingCapital, transactions, riskFreeInterestRateModel, tradingDaysPerYear);
            var summary = GetSummary(totalPerformance, estimatedStrategyCapacity, totalFees, totalOrders, accountCurrencySymbol);

            return new StatisticsResults(totalPerformance, rollingPerformances, summary);
        }

        /// <summary>
        /// Returns the performance of the algorithm in the specified date range
        /// </summary>
        /// <param name="fromDate">The initial date of the range</param>
        /// <param name="toDate">The final date of the range</param>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="pointsPortfolioTurnover">The list of portfolio turnover daily samples</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <param name="transactions">
        /// The transaction manager to get number of winning and losing transactions
        /// </param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to use</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year</param>
        /// <returns>The algorithm performance</returns>
        private static AlgorithmPerformance GetAlgorithmPerformance(
            DateTime fromDate,
            DateTime toDate,
            List<Trade> trades,
            SortedDictionary<DateTime, decimal> profitLoss,
            SortedDictionary<DateTime, decimal> equity,
            List<ISeriesPoint> pointsPerformance,
            List<ISeriesPoint> pointsBenchmark,
            List<ISeriesPoint> pointsPortfolioTurnover,
            decimal startingCapital,
            SecurityTransactionManager transactions,
            IRiskFreeInterestRateModel riskFreeInterestRateModel,
            int tradingDaysPerYear)
        {
            var periodEquity = new SortedDictionary<DateTime, decimal>(equity.Where(x => x.Key.Date >= fromDate && x.Key.Date < toDate.AddDays(1)).ToDictionary(x => x.Key, y => y.Value));

            // No portfolio equity for the period means that there is no performance to be computed
            if (periodEquity.IsNullOrEmpty())
            {
                return new AlgorithmPerformance();
            }

            var periodTrades = trades.Where(x => x.ExitTime.Date >= fromDate && x.ExitTime < toDate.AddDays(1)).ToList();
            var periodProfitLoss = new SortedDictionary<DateTime, decimal>(profitLoss.Where(x => x.Key >= fromDate && x.Key.Date < toDate.AddDays(1)).ToDictionary(x => x.Key, y => y.Value));
            var periodWinCount = transactions.WinningTransactions.Count(x => x.Key >= fromDate && x.Key.Date < toDate.AddDays(1));
            var periodLossCount = transactions.LosingTransactions.Count(x => x.Key >= fromDate && x.Key.Date < toDate.AddDays(1));

            // Convert our charts to dictionaries
            // NOTE: Day 0 refers to sample taken at 12AM on StartDate, performance[0] always = 0, benchmark[0] is benchmark value preceding start date.
            var benchmark = ChartPointToDictionary(pointsBenchmark, fromDate, toDate);
            var performance = ChartPointToDictionary(pointsPerformance, fromDate, toDate);
            var portfolioTurnover = ChartPointToDictionary(pointsPortfolioTurnover, fromDate, toDate);

            // Ensure our series are aligned
            if (benchmark.Count != performance.Count)
            {
                throw new ArgumentException($"Benchmark and performance series has {Math.Abs(benchmark.Count - performance.Count)} misaligned values.");
            }

            // Convert our benchmark values into a percentage daily performance of the benchmark, this will shorten the series by one since
            // its the percentage change between each entry (No day 0 sample)
            var benchmarkEnumerable = CreateBenchmarkDifferences(benchmark, fromDate, toDate);

            var listBenchmark = benchmarkEnumerable.Select(x => x.Value).ToList();
            var listPerformance = PreprocessPerformanceValues(performance).Select(x => x.Value).ToList();

            var runningCapital = equity.Count == periodEquity.Count ? startingCapital : periodEquity.Values.FirstOrDefault();

            return new AlgorithmPerformance(periodTrades, periodProfitLoss, periodEquity, portfolioTurnover, listPerformance, listBenchmark,
                runningCapital, periodWinCount, periodLossCount, riskFreeInterestRateModel, tradingDaysPerYear);
        }

        /// <summary>
        /// Returns the rolling performances of the algorithm
        /// </summary>
        /// <param name="firstDate">The first date of the total period</param>
        /// <param name="lastDate">The last date of the total period</param>
        /// <param name="trades">The list of closed trades</param>
        /// <param name="profitLoss">Trade record of profits and losses</param>
        /// <param name="equity">The list of daily equity values</param>
        /// <param name="pointsPerformance">The list of algorithm performance values</param>
        /// <param name="pointsBenchmark">The list of benchmark values</param>
        /// <param name="pointsPortfolioTurnover">The list of portfolio turnover daily samples</param>
        /// <param name="startingCapital">The algorithm starting capital</param>
        /// <param name="transactions">
        /// The transaction manager to get number of winning and losing transactions
        /// </param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to use</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year</param>
        /// <returns>A dictionary with the rolling performances</returns>
        private static Dictionary<string, AlgorithmPerformance> GetRollingPerformances(
            DateTime firstDate,
            DateTime lastDate,
            List<Trade> trades,
            SortedDictionary<DateTime, decimal> profitLoss,
            SortedDictionary<DateTime, decimal> equity,
            List<ISeriesPoint> pointsPerformance,
            List<ISeriesPoint> pointsBenchmark,
            List<ISeriesPoint> pointsPortfolioTurnover,
            decimal startingCapital,
            SecurityTransactionManager transactions,
            IRiskFreeInterestRateModel riskFreeInterestRateModel,
            int tradingDaysPerYear)
        {
            var rollingPerformances = new Dictionary<string, AlgorithmPerformance>();

            var monthPeriods = new[] { 1, 3, 6, 12 };
            foreach (var monthPeriod in monthPeriods)
            {
                var ranges = GetPeriodRanges(monthPeriod, firstDate, lastDate);

                foreach (var period in ranges)
                {
                    var key = $"M{monthPeriod}_{period.EndDate.ToStringInvariant("yyyyMMdd")}";
                    var periodPerformance = GetAlgorithmPerformance(period.StartDate, period.EndDate, trades, profitLoss, equity, pointsPerformance,
                        pointsBenchmark, pointsPortfolioTurnover, startingCapital, transactions, riskFreeInterestRateModel, tradingDaysPerYear);
                    rollingPerformances[key] = periodPerformance;
                }
            }

            return rollingPerformances;
        }

        /// <summary>
        /// Returns a summary of the algorithm performance as a dictionary
        /// </summary>
        private static Dictionary<string, string> GetSummary(AlgorithmPerformance totalPerformance, CapacityEstimate estimatedStrategyCapacity,
            decimal totalFees, int totalOrders, string accountCurrencySymbol)
        {
            var capacity = 0m;
            var lowestCapacitySymbol = Symbol.Empty;
            if (estimatedStrategyCapacity != null)
            {
                capacity = estimatedStrategyCapacity.Capacity;
                lowestCapacitySymbol = estimatedStrategyCapacity.LowestCapacityAsset ?? Symbol.Empty;
            }

            return new Dictionary<string, string>
            {
                { PerformanceMetrics.TotalOrders, totalOrders.ToStringInvariant() },
                { PerformanceMetrics.AverageWin, Math.Round(totalPerformance.PortfolioStatistics.AverageWinRate.SafeMultiply100(), 2).ToStringInvariant() + "%"  },
                { PerformanceMetrics.AverageLoss, Math.Round(totalPerformance.PortfolioStatistics.AverageLossRate.SafeMultiply100(), 2).ToStringInvariant() + "%" },
                { PerformanceMetrics.CompoundingAnnualReturn, Math.Round(totalPerformance.PortfolioStatistics.CompoundingAnnualReturn.SafeMultiply100(), 3).ToStringInvariant() + "%" },
                { PerformanceMetrics.Drawdown, Math.Round(totalPerformance.PortfolioStatistics.Drawdown.SafeMultiply100(), 3).ToStringInvariant() + "%" },
                { PerformanceMetrics.Expectancy, Math.Round(totalPerformance.PortfolioStatistics.Expectancy, 3).ToStringInvariant() },
                { PerformanceMetrics.StartEquity, Math.Round(totalPerformance.PortfolioStatistics.StartEquity, 2).ToStringInvariant() },
                { PerformanceMetrics.EndEquity, Math.Round(totalPerformance.PortfolioStatistics.EndEquity, 2).ToStringInvariant() },
                { PerformanceMetrics.NetProfit, Math.Round(totalPerformance.PortfolioStatistics.TotalNetProfit.SafeMultiply100(), 3).ToStringInvariant() + "%"},
                { PerformanceMetrics.SharpeRatio, Math.Round((double)totalPerformance.PortfolioStatistics.SharpeRatio, 3).ToStringInvariant() },
                { PerformanceMetrics.SortinoRatio, Math.Round((double)totalPerformance.PortfolioStatistics.SortinoRatio, 3).ToStringInvariant() },
                { PerformanceMetrics.ProbabilisticSharpeRatio, Math.Round(totalPerformance.PortfolioStatistics.ProbabilisticSharpeRatio.SafeMultiply100(), 3).ToStringInvariant() + "%"},
                { PerformanceMetrics.LossRate, Math.Round(totalPerformance.PortfolioStatistics.LossRate.SafeMultiply100()).ToStringInvariant() + "%" },
                { PerformanceMetrics.WinRate, Math.Round(totalPerformance.PortfolioStatistics.WinRate.SafeMultiply100()).ToStringInvariant() + "%" },
                { PerformanceMetrics.ProfitLossRatio, Math.Round(totalPerformance.PortfolioStatistics.ProfitLossRatio, 2).ToStringInvariant() },
                { PerformanceMetrics.Alpha, Math.Round((double)totalPerformance.PortfolioStatistics.Alpha, 3).ToStringInvariant() },
                { PerformanceMetrics.Beta, Math.Round((double)totalPerformance.PortfolioStatistics.Beta, 3).ToStringInvariant() },
                { PerformanceMetrics.AnnualStandardDeviation, Math.Round((double)totalPerformance.PortfolioStatistics.AnnualStandardDeviation, 3).ToStringInvariant() },
                { PerformanceMetrics.AnnualVariance, Math.Round((double)totalPerformance.PortfolioStatistics.AnnualVariance, 3).ToStringInvariant() },
                { PerformanceMetrics.InformationRatio, Math.Round((double)totalPerformance.PortfolioStatistics.InformationRatio, 3).ToStringInvariant() },
                { PerformanceMetrics.TrackingError, Math.Round((double)totalPerformance.PortfolioStatistics.TrackingError, 3).ToStringInvariant() },
                { PerformanceMetrics.TreynorRatio, Math.Round((double)totalPerformance.PortfolioStatistics.TreynorRatio, 3).ToStringInvariant() },
                { PerformanceMetrics.TotalFees, accountCurrencySymbol + totalFees.ToStringInvariant("0.00") },
                { PerformanceMetrics.EstimatedStrategyCapacity, accountCurrencySymbol + capacity.RoundToSignificantDigits(2).ToStringInvariant() },
                { PerformanceMetrics.LowestCapacityAsset, lowestCapacitySymbol != Symbol.Empty ? lowestCapacitySymbol.ID.ToString() : "" },
                { PerformanceMetrics.PortfolioTurnover, Math.Round(totalPerformance.PortfolioStatistics.PortfolioTurnover.SafeMultiply100(), 2).ToStringInvariant() + "%" }
            };
        }

        /// <summary>
        /// Helper class for rolling statistics
        /// </summary>
        private class PeriodRange
        {
            internal DateTime StartDate { get; set; }
            internal DateTime EndDate { get; set; }
        }

        /// <summary>
        /// Gets a list of date ranges for the requested monthly period
        /// </summary>
        /// <remarks>The first and last ranges created are partial periods</remarks>
        /// <param name="periodMonths">The number of months in the period (valid inputs are [1, 3, 6, 12])</param>
        /// <param name="firstDate">The first date of the total period</param>
        /// <param name="lastDate">The last date of the total period</param>
        /// <returns>The list of date ranges</returns>
        private static IEnumerable<PeriodRange> GetPeriodRanges(int periodMonths, DateTime firstDate, DateTime lastDate)
        {
            // get end dates
            var date = lastDate.Date;
            var endDates = new List<DateTime>();
            do
            {
                endDates.Add(date);
                date = new DateTime(date.Year, date.Month, 1).AddDays(-1);
            } while (date >= firstDate);

            // build period ranges
            var ranges = new List<PeriodRange> { new PeriodRange { StartDate = firstDate, EndDate = endDates[endDates.Count - 1] } };
            for (var i = endDates.Count - 2; i >= 0; i--)
            {
                var startDate = ranges[ranges.Count - 1].EndDate.AddDays(1).AddMonths(1 - periodMonths);
                if (startDate < firstDate) startDate = firstDate;

                ranges.Add(new PeriodRange
                {
                    StartDate = startDate,
                    EndDate = endDates[i]
                });
            }

            return ranges;
        }

        /// <summary>
        /// Convert the charting data into an equity array.
        /// </summary>
        /// <remarks>This is required to convert the equity plot into a usable form for the statistics calculation</remarks>
        /// <param name="points">ChartPoints Array</param>
        /// <param name="fromDate">An optional starting date</param>
        /// <param name="toDate">An optional ending date</param>
        /// <returns>SortedDictionary of the equity decimal values ordered in time</returns>
        private static SortedDictionary<DateTime, decimal> ChartPointToDictionary(IEnumerable<ISeriesPoint> points, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var dictionary = new SortedDictionary<DateTime, decimal>();

            foreach (var point in points)
            {
                if (fromDate != null && point.Time.Date < fromDate) continue;
                if (toDate != null && point.Time.Date >= ((DateTime)toDate).AddDays(1)) break;

                dictionary[point.Time] = GetPointValue(point);
            }

            return dictionary;
        }

        /// <summary>
        /// Gets the value of a point, either ChartPoint.y or Candlestick.Close
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal GetPointValue(ISeriesPoint point)
        {
            if (point is ChartPoint)
            {
                return ((ChartPoint)point).y.Value;
            }

            return ((Candlestick)point).Close.Value;
        }

        /// <summary>
        /// Yields pairs of date and percentage change for the period
        /// </summary>
        /// <param name="points">The values to calculate percentage change for</param>
        /// <param name="fromDate">Starting date (inclusive)</param>
        /// <param name="toDate">Ending date (inclusive)</param>
        /// <returns>Pairs of date and percentage change</returns>
        public static IEnumerable<KeyValuePair<DateTime, double>> CreateBenchmarkDifferences(IEnumerable<KeyValuePair<DateTime, decimal>> points, DateTime fromDate, DateTime toDate)
        {
            DateTime dtPrevious = default;
            var previous = 0m;
            var firstValueSkipped = false;
            double deltaPercentage;

            // Get points performance array for the given period:
            foreach (var kvp in points.Where(kvp => kvp.Key >= fromDate.Date && kvp.Key.Date <= toDate))
            {
                var dt = kvp.Key;
                var value = kvp.Value;

                if (dtPrevious != default)
                {
                    deltaPercentage = 0;
                    if (previous != 0)
                    {
                        deltaPercentage = (double)((value - previous) / previous);
                    }

                    // We will skip past day 1 of performance values to deal with the OnOpen orders causing misalignment between benchmark and
                    // algorithm performance. So we drop the first value of listBenchmark (Day 1), and drop two values from performance (Day 0, Day 1)
                    if (firstValueSkipped)
                    {
                        yield return new KeyValuePair<DateTime, double>(dt, deltaPercentage);
                    }
                    else
                    {
                        firstValueSkipped = true;
                    }
                }

                dtPrevious = dt;
                previous = value;
            }
        }

        /// <summary>
        /// Skips the first two entries from the given points and divides each entry by 100
        /// </summary>
        /// <param name="points">The values to divide by 100</param>
        /// <returns>Pairs of date and performance value divided by 100</returns>
        public static IEnumerable<KeyValuePair<DateTime, double>> PreprocessPerformanceValues(IEnumerable<KeyValuePair<DateTime, decimal>> points)
        {
            // We will skip past day 1 of performance values to deal with the OnOpen orders causing misalignment between benchmark and
            // algorithm performance. So we drop two values from performance (Day 0, Day 1)
            foreach (var kvp in points.Skip(2))
            {
                yield return new KeyValuePair<DateTime, double>(kvp.Key, (double)(kvp.Value / 100));
            }
        }
    }
}
