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
using System.Linq;
using Python.Runtime;
using QuantConnect.Packets;
using System;

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
        public CumulativeReturnsReportElement(string name, string key, BacktestResult backtest, LiveResult live)
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
            var backtestReturns = Calculations.EquityPoints(_backtest);
            var benchmark = Calculations.BenchmarkPoints(_backtest);
            var liveReturns = Calculations.EquityPoints(_live);
            var liveBenchmark = Calculations.BenchmarkPoints(_live);

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

                //                var backtestSeries = Pandas.Series(backtestStrategy.ToPython(), backtestTime.ToPython());
                //                var backtestCumulativePercent = backtestSeries.pct_change().cumsum().mul(100);
                //                var benchmarkCumulativePercent = Pandas.Series(benchmarkPoints.ToPython()).pct_change().cumsum().mul(100);
                //                benchmarkList.Append(backtestTime.ToPython());
                //                benchmarkList.Append(backtestCumulativePercent.values);
                //                benchmarkList.Append(benchmarkTime.ToPython());
                //                benchmarkList.Append(benchmarkCumulativePercent.values);
                //
                //                // Gets the last element of the benchmark and add it to the live strategy and benchmark to
                //                // start in the same position as the end of the live strategy or benchmark.
                //                var liveCumulativePercent = Pandas.Series(liveStrategy.ToPython()).pct_change().cumsum().mul(100).add(backtestCumulativePercent.iloc[-1]);
                //                var liveBenchmarkCumulativePercent = Pandas.Series(liveBenchmarkStrategy.ToPython()).pct_change().cumsum().mul(100).add(benchmarkCumulativePercent.iloc[-1]);
                //                liveList.Append(liveTime.ToPython());
                //                liveList.Append(liveCumulativePercent.values);
                //                liveList.Append(liveBenchmarkTime.ToPython());
                //                liveList.Append(liveBenchmarkCumulativePercent.values);

                var backtestSeries = new Series<DateTime, double>(backtestTime, backtestStrategy);
                var liveSeries = new Series<DateTime, double>(liveTime, liveStrategy);
                var backtestBenchmarkSeries = new Series<DateTime, double>(benchmarkTime, benchmarkPoints);
                var liveBenchmarkSeries = new Series<DateTime, double>(liveBenchmarkTime, liveBenchmarkStrategy);

                // Equivalent in python using pandas for the following operations is:
                //
                // df.pct_change().cumsum().mul(100)
                var backtestCumulativePercent = backtestSeries.PercentChange().CumulativeSum() * 100;
                var backtestBenchmarkCumulativePercent = backtestBenchmarkSeries.PercentChange().CumulativeSum() * 100;

                // Equivalent in python using pandas for the following operations is:
                // --------------------------------------------------
                // # note: [...] denotes the data we're passing in
                // bt = pd.Series([...], index=time)
                // df.pct_change().cumsum().mul(100).add(bt.iloc[-1])
                // --------------------------------------------------
                //
                // We add the final value of the backtest and benchmark to have a continuous graph showing the performance out of sample
                // as a continuation of the cumulative returns graph. Otherwise, we start plotting from 0% and not the last value of the backtest data
                var liveCumulativePercent = (liveSeries.PercentChange().CumulativeSum() * 100) + backtestCumulativePercent.LastValue();
                var liveBenchmarkCumulativePercent = (liveBenchmarkSeries.PercentChange().CumulativeSum() * 100) + backtestBenchmarkCumulativePercent.LastValue();

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