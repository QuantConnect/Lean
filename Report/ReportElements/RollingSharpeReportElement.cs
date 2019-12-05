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
        /// Create a new plot of the rolling sharpe ratio
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public RollingSharpeReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the rolling sharpe using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestPoints = Calculations.EquityPoints(_backtest);
            var livePoints = Calculations.EquityPoints(_live);

            var backtestRollingSharpe = new Series<DateTime, double>(backtestPoints.Keys, backtestPoints.Values).RollingSharpe(6);
            var liveRollingSharpe = new Series<DateTime, double>(livePoints.Keys, livePoints.Values).RollingSharpe(6);

            var base64 = "";
            using (Py.GIL())
            {
                var backtestList = new PyList();
                var liveList = new PyList();

                backtestList.Append(backtestRollingSharpe.Keys.ToList().ToPython());
                backtestList.Append(backtestRollingSharpe.Values.ToList().ToPython());

                liveList.Append(liveRollingSharpe.Keys.ToList().ToPython());
                liveList.Append(liveRollingSharpe.Values.ToList().ToPython());

                base64 = Charting.GetRollingSharpeRatio(backtestList, liveList);
            }

            return base64;
        }
    }
}