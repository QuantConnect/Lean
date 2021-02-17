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
using System.Linq;
using Deedle;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class MaxDrawdownReportElement : ReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;

        /// <summary>
        /// Estimate the max drawdown of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public MaxDrawdownReportElement(string name, string key, BacktestResult backtest, LiveResult live)
        {
            _live = live;
            _backtest = backtest;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            if (_live == null)
            {
                var backtestDrawdown = _backtest?.TotalPerformance?.PortfolioStatistics?.Drawdown;
                Result = backtestDrawdown;
                return backtestDrawdown?.ToString("P1") ?? "-";
            }

            var equityCurve = new SortedDictionary<DateTime, decimal>(DrawdownCollection.NormalizeResults(_backtest, _live)
                .Observations
                .ToDictionary(kvp => kvp.Key, kvp => (decimal)kvp.Value));

            var maxDrawdown = Statistics.Statistics.DrawdownPercent(equityCurve);
            Result = maxDrawdown;

            return $"{maxDrawdown:P1}";
        }
    }
}
