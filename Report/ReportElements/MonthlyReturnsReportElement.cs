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
    internal sealed class MonthlyReturnsReportElement : ChartReportElement
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
        public MonthlyReturnsReportElement(string name, string key, BacktestResult backtest, LiveResult live)
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
            var backtestPoints = ResultsUtil.EquityPoints(_backtest);
            var livePoints = ResultsUtil.EquityPoints(_live);

            var backtestSeries = new Series<DateTime, double>(backtestPoints.Keys, backtestPoints.Values);
            var liveSeries = new Series<DateTime, double>(livePoints.Keys, livePoints.Values);

            // Equivalent to python pandas line: `backtestSeries.resample('M').apply(lambda x: x.pct_change().sum())`
            var backtestMonthlyReturns = backtestSeries.ResampleEquivalence(date => new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1))
                .Select(kvp => kvp.Value.PercentChange().Sum());

            var liveMonthlyReturns = liveSeries.ResampleEquivalence(date => new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1))
                .Select(kvp => kvp.Value.PercentChange().Sum());

            var base64 = "";
            using (Py.GIL())
            {
                var backtestResults = new PyDict();
                foreach (var kvp in backtestMonthlyReturns.GroupBy(kvp => kvp.Key.Year).GetObservations())
                {
                    var key = kvp.Key.ToStringInvariant();
                    var values = (kvp.Value * 100).Values.ToList();

                    while (values.Count != 12)
                    {
                        values.Add(double.NaN);
                    }
                    backtestResults.SetItem(key.ToPython(), values.ToPython());
                }

                var liveResults = new PyDict();
                foreach (var kvp in liveMonthlyReturns.GroupBy(kvp => kvp.Key.Year).GetObservations())
                {
                    var key = kvp.Key.ToStringInvariant();
                    var values = (kvp.Value * 100).Values.ToList();
                    while (values.Count != 12)
                    {
                        values.Add(double.NaN);
                    }
                    liveResults.SetItem(key.ToPython(), values.ToPython());
                }

                base64 = Charting.GetMonthlyReturns(backtestResults, liveResults);
            }

            return base64;
        }
    }
}