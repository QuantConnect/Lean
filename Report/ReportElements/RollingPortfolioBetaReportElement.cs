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
            var backtestPoints = EquityPoints(_backtest);
            var backtestBenchmarkPoints = BenchmarkPoints(_backtest);
            var livePoints = EquityPoints(_live);
            var liveBenchmarkPoints = BenchmarkPoints(_live);

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                var backtestDates = backtestPoints.Keys.ToList();
                var backtestPercentChange = Pandas.Series(backtestPoints.Values.ToList().ToPython(), backtestDates.ToPython()).pct_change().dropna();
                var backtestBenchmarkDates = backtestBenchmarkPoints.Keys.ToList();
                var backtestBenchmarkPercentChange = Pandas.Series(backtestBenchmarkPoints.Values.ToList().ToPython(), backtestBenchmarkDates.ToPython()).pct_change().dropna();

                backtestList.Append(backtestPercentChange.index);
                backtestList.Append(backtestPercentChange.values);
                backtestList.Append(backtestBenchmarkPercentChange.index);
                backtestList.Append(backtestBenchmarkPercentChange.values);

                var liveDates = livePoints.Keys.ToList();
                var livePercentChange = Pandas.Series(livePoints.Values.ToList().ToPython(), liveDates.ToPython()).pct_change().dropna();
                var liveBenchmarkDates = liveBenchmarkPoints.Keys.ToList();
                var liveBenchmarkPercentChange = Pandas.Series(liveBenchmarkPoints.Values.ToList().ToPython(), liveBenchmarkDates.ToPython()).pct_change().dropna();

                liveList.Append(livePercentChange.index);
                liveList.Append(livePercentChange.values);
                liveList.Append(liveBenchmarkPercentChange.index);
                liveList.Append(liveBenchmarkPercentChange.values);

                base64 = Charting.GetRollingBeta(backtestList, liveList);
            }

            return base64;
        }
    }
}