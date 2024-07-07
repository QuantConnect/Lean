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
using Deedle;
using Python.Runtime;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class CumulativeReturnsReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new array of cumulative percentage return of strategy and benchmark
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public CumulativeReturnsReportElement(
            string name,
            string key,
            BacktestResult backtest,
            LiveResult live
        )
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the cumulative return of the backtest, benchmark, and live
        /// strategy using the ReportCharts.py python library
        /// </summary>
        public override string Render()
        {
            var backtestReturns = ResultsUtil.EquityPoints(_backtest);
            var benchmark = ResultsUtil.BenchmarkPoints(_backtest);
            var liveReturns = ResultsUtil.EquityPoints(_live);
            var liveBenchmark = ResultsUtil.BenchmarkPoints(_live);

            var backtestTime = backtestReturns.Keys.ToList();
            var backtestStrategy = backtestReturns.Values.ToList();
            var benchmarkTime = benchmark.Keys.ToList();
            var benchmarkPoints = benchmark.Values.ToList();

            var liveTime = liveReturns.Keys.ToList();
            var liveStrategy = liveReturns.Values.ToList();
            var liveBenchmarkTime = liveBenchmark.Keys.ToList();
            var liveBenchmarkStrategy = liveBenchmark.Values.ToList();

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                var backtestSeries = new Series<DateTime, double>(backtestTime, backtestStrategy);
                var liveSeries = new Series<DateTime, double>(liveTime, liveStrategy);
                var backtestBenchmarkSeries = new Series<DateTime, double>(
                    benchmarkTime,
                    benchmarkPoints
                );
                var liveBenchmarkSeries = new Series<DateTime, double>(
                    liveBenchmarkTime,
                    liveBenchmarkStrategy
                );

                // Equivalent in python using pandas for the following operations is:
                // --------------------------------------------------
                // >>> # note: [...] denotes the data we're passing in
                // >>> df = pd.Series([...], index=time)
                // >>> df_live = pd.Series([...], index=live_time)
                // >>> df_live = df_live.mul(df.iloc[-1] / df_live.iloc[0]).fillna(method='ffill').dropna()
                // >>> df_final = pd.concat([df, df_live], axis=0)
                // >>> df_cumulative_returns = ((df_final.pct_change().dropna() + 1).cumprod() - 1)
                // --------------------------------------------------
                //
                // We multiply the final value of the backtest and benchmark to have a continuous graph showing the performance out of sample
                // as a continuation of the cumulative returns graph. Otherwise, we start plotting from 0% and not the last value of the backtest data

                var backtestLastValue =
                    backtestSeries.ValueCount == 0 ? 0 : backtestSeries.LastValue();
                var backtestBenchmarkLastValue =
                    backtestBenchmarkSeries.ValueCount == 0
                        ? 0
                        : backtestBenchmarkSeries.LastValue();

                var liveContinuousEquity = liveSeries;
                var liveBenchContinuousEquity = liveBenchmarkSeries;

                if (liveSeries.ValueCount != 0)
                {
                    liveContinuousEquity = (
                        liveSeries * (backtestLastValue / liveSeries.FirstValue())
                    )
                        .FillMissing(Direction.Forward)
                        .DropMissing();
                }
                if (liveBenchmarkSeries.ValueCount != 0)
                {
                    liveBenchContinuousEquity = (
                        liveBenchmarkSeries
                        * (backtestBenchmarkLastValue / liveBenchmarkSeries.FirstValue())
                    )
                        .FillMissing(Direction.Forward)
                        .DropMissing();
                }

                var liveStart =
                    liveContinuousEquity.ValueCount == 0
                        ? DateTime.MaxValue
                        : liveContinuousEquity.DropMissing().FirstKey();
                var liveBenchStart =
                    liveBenchContinuousEquity.ValueCount == 0
                        ? DateTime.MaxValue
                        : liveBenchContinuousEquity.DropMissing().FirstKey();

                var finalEquity = backtestSeries
                    .Where(kvp => kvp.Key < liveStart)
                    .Observations.ToList();
                var finalBenchEquity = backtestBenchmarkSeries
                    .Where(kvp => kvp.Key < liveBenchStart)
                    .Observations.ToList();

                finalEquity.AddRange(liveContinuousEquity.Observations);
                finalBenchEquity.AddRange(liveBenchContinuousEquity.Observations);

                var finalSeries = (
                    new Series<DateTime, double>(finalEquity).CumulativeReturns() * 100
                )
                    .FillMissing(Direction.Forward)
                    .DropMissing();

                var finalBenchSeries = (
                    new Series<DateTime, double>(finalBenchEquity).CumulativeReturns() * 100
                )
                    .FillMissing(Direction.Forward)
                    .DropMissing();

                var backtestCumulativePercent = finalSeries.Where(kvp => kvp.Key < liveStart);
                var backtestBenchmarkCumulativePercent = finalBenchSeries.Where(kvp =>
                    kvp.Key < liveBenchStart
                );

                var liveCumulativePercent = finalSeries.Where(kvp => kvp.Key >= liveStart);
                var liveBenchmarkCumulativePercent = finalBenchSeries.Where(kvp =>
                    kvp.Key >= liveBenchStart
                );

                backtestList.Append(backtestCumulativePercent.Keys.ToList().ToPython());
                backtestList.Append(backtestCumulativePercent.Values.ToList().ToPython());
                backtestList.Append(backtestBenchmarkCumulativePercent.Keys.ToList().ToPython());
                backtestList.Append(backtestBenchmarkCumulativePercent.Values.ToList().ToPython());

                liveList.Append(liveCumulativePercent.Keys.ToList().ToPython());
                liveList.Append(liveCumulativePercent.Values.ToList().ToPython());
                liveList.Append(liveBenchmarkCumulativePercent.Keys.ToList().ToPython());
                liveList.Append(liveBenchmarkCumulativePercent.Values.ToList().ToPython());

                base64 = Charting.GetCumulativeReturns(backtestList, liveList);
            }

            return base64;
        }
    }
}
