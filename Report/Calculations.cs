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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

                if (previous == 0.0)
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
        /// <param name="series">The series you want to measure beta for</param>
        /// <param name="benchmarkSeries">The benchmark/series you want to calculate beta with</param>
        /// <param name="windowSize">Days/window to lookback</param>
        /// <returns>Rolling beta</returns>
        public static Series<DateTime, double> RollingBeta(this Series<DateTime, double> series, Series<DateTime, double> benchmarkSeries, int windowSize = 132)
        {
            var dailyReturnsSeries = series.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum());
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
        /// <param name="series">Series to calculate rolling sharpe for</param>
        /// <param name="months">Number of months to calculate the rolling period for</param>
        /// <param name="riskFreeRate">Risk free rate</param>
        /// <returns>Rolling sharpe ratio</returns>
        public static Series<DateTime, double> RollingSharpe(this Series<DateTime, double> series, int months, double riskFreeRate = 0.0)
        {
            var dailyReturns = series.PercentChange().ResampleEquivalence(date => date.Date, s => s.Sum());
            var rollingSharpeData = new List<KeyValuePair<DateTime, double>>();
            var firstDate = series.FirstKey();

            foreach (var date in series.Keys)
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
        /// be the equity curve with the associated <see cref="Orders.Order"/> objects that go along with it.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Series<DateTime, double> LeverageUtilization(this Series<DateTime, double> series, IEnumerable<Orders.Order> orders)
        {
            var holdings = new List<KeyValuePair<DateTime, double>>();

            var timeKeeper = new TimeKeeper(series.FirstKey(), TimeZones.Utc);
            var securityManager = new SecurityManager(timeKeeper);
            var securityTransactionManager = new SecurityTransactionManager((IAlgorithm)null, securityManager);
            var portfolioManager = new SecurityPortfolioManager(securityManager, securityTransactionManager);
            var cash = new Cash("USD", (decimal)series.FirstValue(), 1m);

            var startingCash = series.FirstValue();
            portfolioManager.SetCash((decimal)startingCash);

            foreach (var order in orders)
            {
                var orderSecurity = new Security(
                    order.Symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash("USD", 0, 1m),
                    SymbolProperties.GetDefault("USD"),
                    new IdentityCurrencyConverter("USD"),
                    new RegisteredSecurityDataTypesProvider(),
                    new SecurityCache());

                orderSecurity.SetMarketPrice(new Tick { Quantity = order.Quantity, AskPrice = order.Price, BidPrice = order.Price, Value = order.Price });

                // We need to add the security to the portfolio before we can do anything with it
                portfolioManager.Securities.Add(order.Symbol, orderSecurity);

                // If we fill with the quantity provided, TotalPortfolioValue doesn't return the proper results
                var orderEvent = new Orders.OrderEvent(order, order.Time, Orders.Fees.OrderFee.Zero) { FillPrice = order.Price, FillQuantity = order.Quantity };

                portfolioManager.ProcessFill(orderEvent);
                timeKeeper.SetUtcDateTime(order.Time);

                holdings.Add(new KeyValuePair<DateTime, double>(order.Time, (double)portfolioManager.TotalPortfolioValue));
            }

            // Get the last value for a given time to get the final holdings value
            var holdingsGroup = holdings.GroupBy(x => x.Key).Select(x => new KeyValuePair<DateTime, double>(x.Key, x.Select(y => y.Value).Last()));
            var holdingsSeries = new Series<DateTime, double>(holdingsGroup);

            var frame = Frame.CreateEmpty<DateTime, string>();
            frame["equity"] = series;
            frame = frame.Join("holdings", holdingsSeries).FillMissing(Direction.Forward).DropSparseRows();

            return frame["holdings"] / frame["equity"];
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
