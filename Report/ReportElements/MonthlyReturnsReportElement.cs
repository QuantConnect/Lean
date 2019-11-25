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
            var result = new Dictionary<string, List<double>>();
            var backtestReturns = EquityReturns(EquityPoints(_backtest));

            var returnsByMonth = backtestReturns.Select(day => new {day.Key.Year, day.Key.Month, day.Value}).GroupBy(
                y => new {y.Year, y.Month},
                (key, group) => new
                {
                    Year = key.Year.ToString(),
                    Month = key.Month,
                    Returns = group.Sum(day => day.Value)
                });

            foreach (var a in returnsByMonth)
            {
                if (!result.ContainsKey(a.Year))
                {
                    result.Add(a.Year, new List<double>());
                }
                result[a.Year].Add(a.Returns);
            }

            var base64 = "";
            using (Py.GIL())
            {
                base64 = Charting.GetMonthlyReturns(result);
            }

            return base64;
        }
    }
}