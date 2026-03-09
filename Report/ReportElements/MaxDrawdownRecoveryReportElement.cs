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
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class MaxDrawdownRecoveryReportElement : ReportElement
    {
        private LiveResult _liveResult;
        private BacktestResult _backtestResult;

        /// <summary>
        /// Estimate the max drawdown of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtestResult">Backtest result object</param>
        /// <param name="liveResult">Live result object</param>
        public MaxDrawdownRecoveryReportElement(string name, string key, BacktestResult backtestResult, LiveResult liveResult)
        {
            _liveResult = liveResult;
            _backtestResult = backtestResult;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            if (_liveResult == null)
            {
                var backtestDrawdownRecovery = _backtestResult?.TotalPerformance?.PortfolioStatistics?.DrawdownRecovery;
                Result = backtestDrawdownRecovery;
                return backtestDrawdownRecovery?.ToStringInvariant() ?? "-";
            }
            var equityCurve = new SortedDictionary<DateTime, decimal>(DrawdownCollection.NormalizeResults(_backtestResult, _liveResult)
                .Observations
                .ToDictionary(kvp => kvp.Key, kvp => (decimal)kvp.Value));

            var maxDrawdownRecovery = Statistics.Statistics.CalculateDrawdownMetrics(equityCurve).DrawdownRecovery;
            Result = maxDrawdownRecovery;

            return $"{maxDrawdownRecovery}";
        }
    }
}