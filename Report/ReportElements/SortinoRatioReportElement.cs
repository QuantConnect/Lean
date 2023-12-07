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

using QuantConnect.Packets;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class SortinoRatioReportElement : SharpeRatioReportElement
    {
        /// <summary>
        /// Sortino ratio from a backtest
        /// </summary>
        public override decimal? BacktestResultValue => BacktestResult?.TotalPerformance?.PortfolioStatistics?.SortinoRatio;

        /// <summary>
        /// Estimate the Sortino ratio of the strategy.
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public SortinoRatioReportElement(string name, string key, BacktestResult backtest, LiveResult live)
            : base(name, key, backtest, live)
        {
        }

        /// <summary>
        /// Get the live result value
        /// </summary>
        /// <param name="trailingPerformance">The performance for the last period</param>
        /// <returns>The desired metric. Sortini Ratio in this class.</returns>
        public override double? GetLiveResultValue(List<double> trailingPerformance)
        {
            if (trailingPerformance.Count < 7 || Statistics.Statistics.AnnualStandardDeviation(trailingPerformance.Where(ret => ret < 0).ToList()) == 0)
            {
                return null;
            }

            return Statistics.Statistics.SortinoRatio(trailingPerformance, 0.0);
        }
    }
}
