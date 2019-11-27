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

using System.Collections.Generic;
using System.IO;
using Python.Runtime;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Report.ReportElements;

namespace QuantConnect.Report
{
    internal class Report
    {
        private const string _template = "template.html";
        private readonly IReadOnlyCollection<IReportElement> _elements;

        /// <summary>
        /// Creating beautiful HTML and PDF Reports based on backtest and live data.
        /// </summary>
        /// <param name="name">Name of the strategy</param>
        /// <param name="description">Description of the strategy</param>
        /// <param name="version">Version number of the strategy</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        public Report(string name, string description, string version, BacktestResult backtest, LiveResult live)
        {
            _elements = new List<ReportElement>
            {
                //Basics
                new TextReportElement("strategy name", ReportKey.StrategyName, name),
                new TextReportElement("description", ReportKey.StrategyDescription, description),
                new TextReportElement("version", ReportKey.StrategyVersion, version),

                //KPI's Backtest:
                new EstimatedCapacityReportElement("estimated capacity kpi", ReportKey.EstimatedCapacity, backtest, live),
                new CAGRReportElement("cagr kpi", ReportKey.CAGR, backtest, live),
                new TurnoverReportElement("turnover kpi", ReportKey.Turnover, backtest, live),
                new MaxDrawdownReportElement("max drawdown kpi", ReportKey.MaxDrawdown, backtest, live),
                new KellyEstimateReportElement("kelly estimate kpi", ReportKey.KellyEstimate, backtest, live),
                new SharpeRatioReportElement("sharpe kpi", ReportKey.SharpeRatio, backtest, live),
                new PSRReportElement("psr kpi", ReportKey.PSR, backtest, live),
                new InformationRatioReportElement("psr kpi", ReportKey.InformationRatio, backtest, live),
                new MarketsReportElement("markets kpi", ReportKey.Markets, backtest, live),
                new TradesPerDayReportElement("trades per day kpi", ReportKey.TradesPerDay, backtest, live),

                // Generate and insert plots MonthlyReturnsReportElement
                new MonthlyReturnsReportElement("monthly return plot", ReportKey.MonthlyReturns, backtest, live),
                new CumulativeReturnsReportElement("cumulative returns", ReportKey.CumulativeReturns, backtest, live),
                // Array of Crisis Plots:
                new CrisisReportElement("crisis plots", ReportKey.CrisisPlots, backtest, live)
            };
        }

        /// <summary>
        /// Compile the backtest data into a report
        /// </summary>
        /// <returns></returns>
        public string Compile()
        {
            var html = File.ReadAllText(_template);

            // Render the output and replace the report section
            foreach (var element in _elements)
            {
                Log.Trace($"QuantConnect.Report.Compile(): Rendering {element.Name}...");
                html = html.Replace(element.Key, element.Render());
            }

            return html;
        }
    }
}