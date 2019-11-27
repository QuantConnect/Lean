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
using Python.Runtime;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class CumulativeReturnsReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new array of crisis event plots
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
        /// Generate the monthly returns plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestReturns = EquityPoints(_backtest);
            var benchmark = BenchmarkPoints(_backtest);
            var liveReturns = EquityPoints(_live);
            var liveBenchmark = BenchmarkPoints(_live);

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
                var benchmarkList = new PyList();
                var liveList = new PyList();

                var backtestCumulativePercent = Pandas.Series(backtestStrategy.ToPython()).pct_change().cumsum().mul(100);
                var benchmarkCumulativePercent = Pandas.Series(benchmarkPoints.ToPython()).pct_change().cumsum().mul(100);
                benchmarkList.Append(backtestTime.ToPython());
                benchmarkList.Append(backtestCumulativePercent.values);
                benchmarkList.Append(benchmarkTime.ToPython());
                benchmarkList.Append(benchmarkCumulativePercent.values);

                // Gets the last element of the benchmark and add it to the live strategy and benchmark to
                // start in the same position as the end of the live strategy or benchmark.
                var liveCumulativePercent = Pandas.Series(liveStrategy.ToPython()).pct_change().cumsum().mul(100).add(backtestCumulativePercent.iloc[-1]);
                var liveBenchmarkCumulativePercent = Pandas.Series(liveBenchmarkStrategy.ToPython()).pct_change().cumsum().mul(100).add(benchmarkCumulativePercent.iloc[-1]);
                liveList.Append(liveTime.ToPython());
                liveList.Append(liveCumulativePercent.values);
                liveList.Append(liveBenchmarkTime.ToPython());
                liveList.Append(liveBenchmarkCumulativePercent.values);

                base64 = Charting.GetCumulativeReturns(benchmarkList, liveList);
            }

            return base64;
        }
    }
}