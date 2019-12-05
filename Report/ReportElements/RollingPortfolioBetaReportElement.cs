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
using System.Linq;
using Deedle;
using Python.Runtime;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class RollingPortfolioBetaReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Create a new plot of the rolling portfolio beta to equities
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public RollingPortfolioBetaReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the rolling portfolio beta to equities plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestPoints = Calculations.EquityPoints(_backtest);
            var backtestBenchmarkPoints = Calculations.BenchmarkPoints(_backtest);
            var livePoints = Calculations.EquityPoints(_live);
            var liveBenchmarkPoints = Calculations.BenchmarkPoints(_live);

            var backtestSeries = new Series<DateTime, double>(backtestPoints.Keys, backtestPoints.Values);
            var backtestBenchmarkSeries = new Series<DateTime, double>(backtestBenchmarkPoints.Keys, backtestBenchmarkPoints.Values);
            var liveSeries = new Series<DateTime, double>(livePoints.Keys, livePoints.Values);
            var liveBenchmarkSeries = new Series<DateTime, double>(liveBenchmarkPoints.Keys, liveBenchmarkPoints.Values);

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                var backtestPercentChange = backtestSeries.PercentChange();
                var backtestBenchmarkPercentChange = backtestBenchmarkSeries.PercentChange();
                var backtestRollingBetaSixMonths = backtestSeries.RollingBeta(backtestBenchmarkSeries, windowSize: 22 * 6);
                var backtestRollingBetaTwelveMonths = backtestSeries.RollingBeta(backtestBenchmarkSeries, windowSize: 252);

                backtestList.Append(backtestPercentChange.Keys.ToList().ToPython());
                backtestList.Append(backtestPercentChange.Values.ToList().ToPython());
                backtestList.Append(backtestBenchmarkPercentChange.Keys.ToList().ToPython());
                backtestList.Append(backtestBenchmarkPercentChange.Values.ToList().ToPython());
                backtestList.Append(backtestRollingBetaSixMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingBetaSixMonths.Values.ToList().ToPython());
                backtestList.Append(backtestRollingBetaTwelveMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingBetaTwelveMonths.Values.ToList().ToPython());

                var livePercentChange = liveSeries.PercentChange();
                var liveBenchmarkPercentChange = liveBenchmarkSeries.PercentChange();
                var liveRollingBetaSixMonths = liveSeries.RollingBeta(liveBenchmarkSeries, windowSize: 22 * 6);
                var liveRollingBetaTwelveMonths = liveSeries.RollingBeta(liveBenchmarkSeries, windowSize: 252);

                liveList.Append(livePercentChange.Keys.ToList().ToPython());
                liveList.Append(livePercentChange.Values.ToList().ToPython());
                liveList.Append(liveBenchmarkPercentChange.Keys.ToList().ToPython());
                liveList.Append(liveBenchmarkPercentChange.Values.ToList().ToPython());
                liveList.Append(liveRollingBetaSixMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingBetaSixMonths.Values.ToList().ToPython());
                liveList.Append(liveRollingBetaTwelveMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingBetaTwelveMonths.Values.ToList().ToPython());

                base64 = Charting.GetRollingBeta(backtestList, liveList);
            }

            return base64;
        }
    }
}