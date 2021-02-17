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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report
{
    /// <summary>
    /// Rolling window functions
    /// </summary>
    public static class Rolling
    {
        /// <summary>
        /// Calculate the rolling beta with the given window size (in days)
        /// </summary>
        /// <param name="equityCurve">The equity curve you want to measure beta for</param>
        /// <param name="benchmarkSeries">The benchmark/series you want to calculate beta with</param>
        /// <param name="windowSize">Days/window to lookback</param>
        /// <returns>Rolling beta</returns>
        public static Series<DateTime, double> Beta(Series<DateTime, double> equityCurve, Series<DateTime, double> benchmarkSeries, int windowSize = 132)
        {
            var dailyReturnsSeries = equityCurve.ResampleEquivalence(date => date.Date, s => s.LastValue())
                .PercentChange();

            var benchmarkReturns = benchmarkSeries.ResampleEquivalence(date => date.Date, s => s.LastValue())
                .CumulativeReturns();

            var returns = Frame.CreateEmpty<DateTime, string>();
            returns["strategy"] = dailyReturnsSeries;
            returns = returns.Join("benchmark", benchmarkReturns)
                .FillMissing(Direction.Forward)
                .DropSparseRows();

            var correlation = returns
                .Window(windowSize)
                .SelectValues(x => Correlation.Pearson(x["strategy"].Values, x["benchmark"].Values));

            var portfolioStandardDeviation = dailyReturnsSeries.Window(windowSize).SelectValues(s => s.StdDev());
            var benchmarkStandardDeviation = benchmarkReturns.Window(windowSize).SelectValues(s => s.StdDev());

            return (correlation * (portfolioStandardDeviation / benchmarkStandardDeviation))
                .FillMissing(Direction.Forward)
                .DropMissing();
        }

        /// <summary>
        /// Get the rolling sharpe of the given series with a lookback of <paramref name="months"/>. The risk free rate is adjustable
        /// </summary>
        /// <param name="equityCurve">Equity curve to calculate rolling sharpe for</param>
        /// <param name="months">Number of months to calculate the rolling period for</param>
        /// <param name="riskFreeRate">Risk free rate</param>
        /// <returns>Rolling sharpe ratio</returns>
        public static Series<DateTime, double> Sharpe(Series<DateTime, double> equityCurve, int months, double riskFreeRate = 0.0)
        {
            if (equityCurve.IsEmpty)
            {
                return equityCurve;
            }

            var dailyReturns = equityCurve.ResampleEquivalence(date => date.Date, s => s.LastValue())
                .PercentChange();

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
    }
}
