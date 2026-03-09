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
using Python.Runtime;
using QuantConnect.Packets;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class RollingPortfolioBetaReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// The number of trading days per year to get better result of statistics
        /// </summary>
        private int _tradingDaysPerYear;

        /// <summary>
        /// Create a new plot of the rolling portfolio beta to equities
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year to get better result of statistics</param>
        public RollingPortfolioBetaReportElement(string name, string key, BacktestResult backtest, LiveResult live, int tradingDaysPerYear)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
            _tradingDaysPerYear = tradingDaysPerYear;
        }

        /// <summary>
        /// Generate the rolling portfolio beta to equities plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestPoints = GetReturnSeries(_backtest);
            var backtestBenchmarkPoints = ResultsUtil.BenchmarkPoints(_backtest);
            var livePoints = GetReturnSeries(_live);
            var liveBenchmarkPoints = ResultsUtil.BenchmarkPoints(_live);

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                var backtestRollingBetaSixMonths = Rolling.Beta(backtestPoints, backtestBenchmarkPoints, windowSize: 22 * 6);
                var backtestRollingBetaTwelveMonths = Rolling.Beta(backtestPoints, backtestBenchmarkPoints, windowSize: _tradingDaysPerYear);

                backtestList.Append(backtestRollingBetaSixMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingBetaSixMonths.Values.ToList().ToPython());
                backtestList.Append(backtestRollingBetaTwelveMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingBetaTwelveMonths.Values.ToList().ToPython());

                var liveRollingBetaSixMonths = Rolling.Beta(livePoints, liveBenchmarkPoints, windowSize: 22 * 6);
                var liveRollingBetaTwelveMonths = Rolling.Beta(livePoints, liveBenchmarkPoints, windowSize: _tradingDaysPerYear);

                liveList.Append(liveRollingBetaSixMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingBetaSixMonths.Values.ToList().ToPython());
                liveList.Append(liveRollingBetaTwelveMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingBetaTwelveMonths.Values.ToList().ToPython());

                base64 = Charting.GetRollingBeta(backtestList, liveList);
            }

            return base64;
        }

        private static SortedList<DateTime, double> GetReturnSeries(Result leanResult)
        {
            var returnSeries = ResultsUtil.EquityPoints(leanResult, BaseResultsHandler.ReturnKey);
            if (returnSeries == null || returnSeries.Count == 0)
            {
                // for backwards compatibility
                returnSeries = ResultsUtil.EquityPoints(leanResult, "Daily Performance");
            }
            return returnSeries;
        }
    }
}
