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

using Deedle;
using MathNet.Numerics.Statistics;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report
{
    /// <summary>
    /// Calculations required to generate some reports and additional helper methods
    /// </summary>
    public static class Calculations
    {
        /// <summary>
        /// Calculates the cumulative sum for the given series
        /// </summary>
        /// <param name="input">Series to calculate cumulative sum for</param>
        /// <returns>Cumulative sum in series form</returns>
        public static Series<DateTime, double> CumulativeSum(this Series<DateTime, double> input)
        {
            var cumulativeSums = new List<double>();
            var prev = 0.0;

            return input.SelectValues(current =>
            {
                var sum = prev + current;
                cumulativeSums.Add(sum);
                prev = sum;

                return sum;
            });
        }

        /// <summary>
        /// Calculates the cumulative product of the series. This is equal to the python pandas method: `df.cumprod()`
        /// </summary>
        /// <param name="input">Input series</param>
        /// <returns>Cumulative product</returns>
        public static Series<DateTime, double> CumulativeProduct(this Series<DateTime, double> input)
        {
            var cumulativeProducts = new List<double>();
            var prev = 1.0;

            return input.SelectValues(current =>
            {
                var product = prev * current;
                cumulativeProducts.Add(product);
                prev = product;

                return product;
            });
        }

        /// <summary>
        /// Calculates the cumulative max of the series. This is equal to the python pandas method: `df.cummax()`.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Series<DateTime, double> CumulativeMax(this Series<DateTime, double> input)
        {
            var prevMax = double.NegativeInfinity;
            var values = new List<double>();

            foreach (var point in input.Values)
            {
                if (point > prevMax)
                {
                    prevMax = point;
                }

                values.Add(prevMax);
            }

            return new Series<DateTime, double>(input.Keys, values);
        }

        /// <summary>
        /// Calculates the percentage change from the previous value to the current
        /// </summary>
        /// <param name="input">Series to calculate percentage change for</param>
        /// <returns>Percentage change in series form</returns>
        public static Series<DateTime, double> PercentChange(this Series<DateTime, double> input)
        {
            var outputDates = new List<DateTime>();
            var outputValues = new List<double>();

            for (var i = 1; i < input.ValueCount; i++)
            {
                var current = input.GetAt(i);
                var previous = input.GetAt(i - 1);

                outputDates.Add(input.Index.KeyAt(i));

                if (previous == 0.0 || double.IsNegativeInfinity(previous))
                {
                    outputValues.Add(double.NaN);
                    continue;
                }

                outputValues.Add((current - previous) / previous);
            }

            return new Series<DateTime, double>(outputDates, outputValues);
        }

        /// <summary>
        /// Calculate the rolling beta with the given window size (in days)
        /// </summary>
        /// <param name="equityCurve">The equity curve you want to measure beta for</param>
        /// <param name="benchmarkSeries">The benchmark/series you want to calculate beta with</param>
        /// <param name="windowSize">Days/window to lookback</param>
        /// <returns>Rolling beta</returns>
        public static Series<DateTime, double> RollingBeta(this Series<DateTime, double> equityCurve, Series<DateTime, double> benchmarkSeries, int windowSize = 132)
        {
            var dailyReturnsSeries = equityCurve.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum());
            var benchmarkReturns = benchmarkSeries.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum());

            var returns = Frame.CreateEmpty<DateTime, string>();
            returns["strategy"] = dailyReturnsSeries;
            returns["benchmark"] = benchmarkReturns;

            var correlation = returns
                .Window(windowSize)
                .SelectValues(x => Correlation.Pearson(x["strategy"].Values, x["benchmark"].Values));

            var portfolioStandardDeviation = dailyReturnsSeries.Window(windowSize).SelectValues(s => s.StdDev());
            var benchmarkStandardDeviation = benchmarkReturns.Window(windowSize).SelectValues(s => s.StdDev());

            return correlation * (portfolioStandardDeviation / benchmarkStandardDeviation);
        }

        /// <summary>
        /// Get the rolling sharpe of the given series with a lookback of <paramref name="months"/>. The risk free rate is adjustable
        /// </summary>
        /// <param name="equityCurve">Equity curve to calculate rolling sharpe for</param>
        /// <param name="months">Number of months to calculate the rolling period for</param>
        /// <param name="riskFreeRate">Risk free rate</param>
        /// <returns>Rolling sharpe ratio</returns>
        public static Series<DateTime, double> RollingSharpe(this Series<DateTime, double> equityCurve, int months, double riskFreeRate = 0.0)
        {
            if (equityCurve.IsEmpty)
            {
                return equityCurve;
            }

            var dailyReturns = equityCurve.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum());
            var rollingSharpeData = new List<KeyValuePair<DateTime, double>>();
            var firstDate = equityCurve.FirstKey();

            foreach (var date in equityCurve.Keys)
            {
                var nMonthsAgo = date.AddMonths(-months);
                if (nMonthsAgo < firstDate)
                {
                    continue;
                }

                var algoPerformanceLookback = dailyReturns.Between(nMonthsAgo, date);
                rollingSharpeData.Add(
                    new KeyValuePair<DateTime, double>(
                        date,
                        Statistics.Statistics.SharpeRatio(algoPerformanceLookback.Values.ToList(), riskFreeRate)
                    )
                );
            }

            return new Series<DateTime, double>(rollingSharpeData.Select(kvp => kvp.Key), rollingSharpeData.Select(kvp => kvp.Value));
        }

        /// <summary>
        /// Calculates the leverage used from trades. The series used to call this extension function should
        /// be the equity curve with the associated <see cref="Order"/> objects that go along with it.
        /// </summary>
        /// <param name="equityCurve">Equity curve series</param>
        /// <param name="orders">Orders associated with the equity curve</param>
        /// <returns></returns>
        public static Series<DateTime, double> LeverageUtilization(this Series<DateTime, double> equityCurve, List<Order> orders)
        {
            if (equityCurve.IsEmpty)
            {
                return equityCurve;
            }

            var ordersTime = orders.Select(x => x.Time).Distinct().ToList();
            var equityIndex = Frame.CreateEmpty<DateTime, string>()
                .Join("equity", equityCurve)
                .Join("index", new Series<DateTime, double>(ordersTime, ordersTime.Select(x => 0.0)))
                .FillMissing(Direction.Forward)["equity"]
                .DropMissing();

            var curve = equityCurve.Keys.Zip(equityCurve.Values, (first, second) => new KeyValuePair<DateTime, double>(first, second)).ToList();

            return new Series<DateTime, double>(
                new PortfolioLooper(curve, orders).ProcessOrders(orders)
                    .ToList() // Required because for some reason our AbsoluteHoldingsValue is multiplied by two whenever we GroupBy on the raw IEnumerable
                    .GroupBy(portfolio => portfolio.Time)
                    .Select(group => new KeyValuePair<DateTime, double>(
                         group.Key,
                         group.Last().Holdings.Select(holdings => (double)holdings.AbsoluteHoldingsValue).Sum() / equityIndex[group.Key]
                    ))
                )
                .FillMissing(Direction.Forward)
                .DropMissing();
        }

        /// <summary>
        /// Calculates the portfolio's asset allocation percentage over time. The series used to call this extension function should
        /// be the equity curve with the associated <see cref="Order"/> objects that go along with it.
        /// </summary>
        /// <param name="equityCurve">Equity curve series</param>
        /// <param name="orders">Orders associated with the equity curve</param>
        /// <returns></returns>
        public static Series<Symbol, double> AssetAllocations(this Series<DateTime, double> equityCurve, List<Order> orders)
        {
            if (equityCurve.IsEmpty)
            {
                return new Series<Symbol, double>(new Symbol[] { }, new double[] { });
            }

            var curve = equityCurve.Keys
                .Zip(equityCurve.Values, (first, second) => new KeyValuePair<DateTime, double>(first, second))
                .ToList();

            var portfolioHoldings = new PortfolioLooper(curve, orders).ProcessOrders(orders)
                .ToList() // Required because for some reason our AbsoluteHoldingsValue is multiplied by two whenever we GroupBy on the raw IEnumerable
                .GroupBy(x => x.Time)
                .Select(kvp => kvp.Last())
                .ToList();

            var totalPortfolioValueOverTime = (double)portfolioHoldings.Sum(x => x.Holdings.Sum(y => y.AbsoluteHoldingsValue));
            var holdingsBySymbolOverTime = new Dictionary<Symbol, double>();

            foreach (var portfolio in portfolioHoldings)
            {
                foreach (var holding in portfolio.Holdings)
                {
                    if (!holdingsBySymbolOverTime.ContainsKey(holding.Symbol))
                    {
                        holdingsBySymbolOverTime[holding.Symbol] = (double)holding.AbsoluteHoldingsValue;
                        continue;
                    }

                    holdingsBySymbolOverTime[holding.Symbol] = holdingsBySymbolOverTime[holding.Symbol] + (double)holding.AbsoluteHoldingsValue;
                }
            }

            return new Series<Symbol, double>(
                holdingsBySymbolOverTime.Keys,
                holdingsBySymbolOverTime.Values.Select(x => x / totalPortfolioValueOverTime).ToList()
            ).DropMissing();
        }

        public static Frame<DateTime, Tuple<SecurityType, OrderDirection>> Exposure(this Series<DateTime, double> equityCurve, List<Order> orders, OrderDirection direction)
        {
            if (equityCurve.IsEmpty)
            {
                return Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();
            }

            var ordersTime = orders.Select(x => x.Time).Distinct().ToList();
            var equityIndex = Frame.CreateEmpty<DateTime, string>()
                .Join("equity", equityCurve)
                .Join("index", new Series<DateTime, double>(ordersTime, ordersTime.Select(x => 0.0)))
                .FillMissing(Direction.Forward)["equity"]
                .DropMissing();

            var holdingsByAssetClass = new Dictionary<SecurityType, List<KeyValuePair<DateTime, double>>>();
            var multiplier = direction == OrderDirection.Sell ? -1 : 1;
            var curve = equityCurve.Keys.Zip(equityCurve.Values, (first, second) => new KeyValuePair<DateTime, double>(first, second)).ToList();
            var portfolioLooper = new PortfolioLooper(curve, orders);

            foreach (var portfolio in portfolioLooper.ProcessOrders(orders))
            {
                List<KeyValuePair<DateTime, double>> holdings;
                if (!holdingsByAssetClass.TryGetValue(portfolio.Order.SecurityType, out holdings))
                {
                    holdings = new List<KeyValuePair<DateTime, double>>();
                    holdingsByAssetClass[portfolio.Order.SecurityType] = holdings;
                }

                var assets = portfolio.Holdings
                   .Where(pointInTimeHoldings => pointInTimeHoldings.Symbol.SecurityType == portfolio.Order.SecurityType)
                   .ToList();

                if (assets.Count > 0)
                {
                    var sum = (double)assets.Where(pointInTimeHoldings => multiplier * pointInTimeHoldings.Quantity > 0)
                       .Select(pointInTimeHoldings => pointInTimeHoldings.AbsoluteHoldingsValue)
                       .Sum();

                    holdings.Add(new KeyValuePair<DateTime, double>(portfolio.Time, sum / equityIndex[portfolio.Time]));
                }
            }

            var frame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();

            foreach (var kvp in holdingsByAssetClass)
            {
                // Skip Base asset class since we need it as a special value
                // (and it can't be traded on either way)
                if (kvp.Key == SecurityType.Base)
                {
                    continue;
                }

                // Select the last entry of a given time to get accurate results of the portfolio's actual value
                frame = frame.Join(
                    new Tuple<SecurityType, OrderDirection>(kvp.Key, direction),
                    new Series<DateTime, double>(kvp.Value.GroupBy(x => x.Key).Select(x => x.Last()))
                );
            }

            // Equivalent to `pd.fillna(method='ffill').dropna(axis=1, how='all').dropna(how='all')`
            // First drops any missing SecurityTypes, then drops the rows with missing values
            // to get rid of any empty data prior to the first value.
            return frame.FillMissing(Direction.Forward)
                .DropSparseColumnsAll()
                .DropSparseRowsAll();
        }

        /// <summary>
        /// Drops sparse columns only if every value is `missing` in the column
        /// </summary>
        /// <typeparam name="TRowKey">Frame row key</typeparam>
        /// <typeparam name="TColumnKey">Frame column key</typeparam>
        /// <param name="frame">Data Frame</param>
        /// <returns>new Frame with sparse columns dropped</returns>
        /// <remarks>Equivalent to `pd.dropna(axis=1, how='all')`</remarks>
        public static Frame<TRowKey, TColumnKey> DropSparseColumnsAll<TRowKey, TColumnKey>(this Frame<TRowKey, TColumnKey> frame)
        {
            var newFrame = frame.Clone();

            foreach (var key in frame.ColumnKeys)
            {
                if (newFrame[key].DropMissing().ValueCount == 0)
                {
                    newFrame.DropColumn(key);
                }
            }

            return newFrame;
        }

        /// <summary>
        /// Drops sparse rows if and only if every value is `missing` in the Frame
        /// </summary>
        /// <typeparam name="TRowKey">Frame row key</typeparam>
        /// <typeparam name="TColumnKey">Frame column key</typeparam>
        /// <param name="frame">Data Frame</param>
        /// <returns>new Frame with sparse rows dropped</returns>
        /// <remarks>Equivalent to `pd.dropna(how='all')`</remarks>
        public static Frame<TRowKey, TColumnKey> DropSparseRowsAll<TRowKey, TColumnKey>(this Frame<TRowKey, TColumnKey> frame)
        {
            if (frame.ColumnKeys.Count() == 0)
            {
                return Frame.CreateEmpty<TRowKey, TColumnKey>();
            }

            var newFrame = frame.Clone().Transpose();

            foreach (var key in frame.RowKeys)
            {
                if (newFrame[key].DropMissing().ValueCount == 0)
                {
                    newFrame.DropColumn(key);
                }
            }

            return newFrame.Transpose();
        }

        /// <summary>
        /// Get the equity chart points
        /// </summary>
        /// <param name="result">Result object to extract the chart points</param>
        /// <returns></returns>
        public static SortedList<DateTime, double> EquityPoints(Result result)
        {
            var points = new SortedList<DateTime, double>();
            if (result == null)
            {
                return points;
            }

            foreach (var point in result.Charts["Strategy Equity"].Series["Equity"].Values)
            {
                points[Time.UnixTimeStampToDateTime(point.x)] = Convert.ToDouble(point.y);
            }

            return points;
        }

        /// <summary>
        /// Convert cumulative return to daily returns percentage
        /// </summary>
        /// <param name="chart"></param>
        /// <returns></returns>
        public static SortedList<DateTime, double> EquityReturns(SortedList<DateTime, double> chart)
        {
            var returns = new SortedList<DateTime, double>();
            double previous = 0;
            if (chart == null)
            {
                return returns;
            }

            foreach (var point in chart)
            {
                if (returns.Count == 0)
                {
                    returns.Add(point.Key, 0);
                    previous = point.Value;
                    continue;
                }

                var delta = (point.Value / previous) - 1;
                returns.Add(point.Key, delta);
            }
            return returns;
        }

        /// <summary>
        /// Gets the points of the benchmark
        /// </summary>
        /// <param name="result">Backtesting or live results</param>
        /// <returns>Sorted list keyed by date and value</returns>
        public static SortedList<DateTime, double> BenchmarkPoints(Result result)
        {
            var points = new SortedList<DateTime, double>();
            if (result == null)
            {
                return points;
            }

            foreach (var point in result.Charts["Benchmark"].Series["Benchmark"].Values)
            {
                points[Time.UnixTimeStampToDateTime(point.x)] = Convert.ToDouble(point.y);
            }

            return points;
        }
    }
}
