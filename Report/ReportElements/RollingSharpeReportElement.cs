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
    internal sealed class RollingSharpeReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// The number of trading days per year to get better result of statistics
        /// </summary>
        private int _tradingDaysPerYear;

        /// <summary>
        /// Create a new plot of the rolling sharpe ratio
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="tradingDaysPerYear">The number of trading days per year to get better result of statistics</param>
        public RollingSharpeReportElement(string name, string key, BacktestResult backtest, LiveResult live, int tradingDaysPerYear)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
            _tradingDaysPerYear = tradingDaysPerYear;
        }

        /// <summary>
        /// Generate the rolling sharpe using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestPoints = ResultsUtil.EquityPoints(_backtest);
            var livePoints = ResultsUtil.EquityPoints(_live);

            var backtestSeries = new Series<DateTime, double>(backtestPoints);
            var liveSeries = new Series<DateTime, double>(livePoints);

            var backtestRollingSharpeSixMonths = Rolling.Sharpe(backtestSeries, 6, _tradingDaysPerYear).DropMissing();
            var backtestRollingSharpeTwelveMonths = Rolling.Sharpe(backtestSeries, 12, _tradingDaysPerYear).DropMissing();
            var liveRollingSharpeSixMonths = Rolling.Sharpe(liveSeries, 6, _tradingDaysPerYear).DropMissing();
            var liveRollingSharpeTwelveMonths = Rolling.Sharpe(liveSeries, 12, _tradingDaysPerYear).DropMissing();

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                backtestList.Append(backtestRollingSharpeSixMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingSharpeSixMonths.Values.ToList().ToPython());
                backtestList.Append(backtestRollingSharpeTwelveMonths.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingSharpeTwelveMonths.Values.ToList().ToPython());

                liveList.Append(liveRollingSharpeSixMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingSharpeSixMonths.Values.ToList().ToPython());
                liveList.Append(liveRollingSharpeTwelveMonths.Keys.ToList().ToPython());
                liveList.Append(liveRollingSharpeTwelveMonths.Values.ToList().ToPython());

                base64 = Charting.GetRollingSharpeRatio(backtestList, liveList);
            }

            return base64;
        }
    }
}
