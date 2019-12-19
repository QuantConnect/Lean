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
using System.Collections.Generic;

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
                var backtestBenchmarkSeries = new Series<DateTime, double>(benchmarkTime, benchmarkPoints);
                var liveBenchmarkSeries = new Series<DateTime, double>(liveBenchmarkTime, liveBenchmarkStrategy);

                // Equivalent in python using pandas for the following operations is:
                //
                // df.pct_change().cumsum().mul(100)
                var backtestCumulativePercent = (backtestSeries.PercentChange().CumulativeSum() * 100).FillMissing(Direction.Forward).DropMissing();
                var backtestBenchmarkCumulativePercent = (backtestBenchmarkSeries.PercentChange().CumulativeSum() * 100).FillMissing(Direction.Forward).DropMissing();

                // Equivalent in python using pandas for the following operations is:
                // --------------------------------------------------
                // # note: [...] denotes the data we're passing in
                // bt = pd.Series([...], index=time)
                // df.pct_change().cumsum().mul(100).add(bt.iloc[-1])
                // --------------------------------------------------
                //
                // We add the final value of the backtest and benchmark to have a continuous graph showing the performance out of sample
                // as a continuation of the cumulative returns graph. Otherwise, we start plotting from 0% and not the last value of the backtest data

                var backtestLastValue = backtestCumulativePercent.IsEmpty ? 0 : backtestCumulativePercent.LastValue();
                var backtestBenchmarkLastValue = backtestBenchmarkCumulativePercent.IsEmpty ? 0 : backtestBenchmarkCumulativePercent.LastValue();

                var liveCumulativePercent = (liveSeries.PercentChange().CumulativeSum() * 100) + backtestLastValue;
                var liveBenchmarkCumulativePercent = (liveBenchmarkSeries.PercentChange().CumulativeSum() * 100) + backtestBenchmarkLastValue;

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