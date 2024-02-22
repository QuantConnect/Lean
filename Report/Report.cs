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
using Deedle;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Report.ReportElements;
using QuantConnect.Orders;
using System.Text.RegularExpressions;

namespace QuantConnect.Report
{
    /// <summary>
    /// Report class
    /// </summary>
    public class Report
    {
        private string _template;
        private readonly List<IReportElement> _elements;

        /// <summary>
        /// File name for statistics
        /// </summary>
        public const string StatisticsFileName = "report-statistics.json";

        /// <summary>
        /// Create beautiful HTML and PDF Reports based on backtest and live data.
        /// </summary>
        /// <param name="name">Name of the strategy</param>
        /// <param name="description">Description of the strategy</param>
        /// <param name="version">Version number of the strategy</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="pointInTimePortfolioDestination">Point in time portfolio json output base filename</param>
        /// <param name="cssOverride">CSS file that overrides some of the default rules defined in report.css</param>
        /// <param name="htmlCustom">Custom HTML file to replace the default template</param>
        public Report(string name, string description, string version, BacktestResult backtest, LiveResult live, string pointInTimePortfolioDestination = null, string cssOverride = null, string htmlCustom = null)
        {
            _template = htmlCustom ?? File.ReadAllText("template.html");
            var crisisHtmlContent = GetRegexInInput(@"<!--crisis(\r|\n)*((\r|\n|.)*?)crisis-->", _template);
            var parametersHtmlContent = GetRegexInInput(@"<!--parameters(\r|\n)*((\r|\n|.)*?)parameters-->", _template);

            var backtestCurve = new Series<DateTime, double>(ResultsUtil.EquityPoints(backtest));
            var liveCurve = new Series<DateTime, double>(ResultsUtil.EquityPoints(live));

            var backtestOrders = backtest?.Orders?.Values.ToList() ?? new List<Order>();
            var liveOrders = live?.Orders?.Values.ToList() ?? new List<Order>();

            var backtestConfiguration = backtest?.AlgorithmConfiguration;
            var liveConfiguration = live?.AlgorithmConfiguration;

            // Earlier we use constant's value tradingDaysPerYear = 252
            // backtestConfiguration?.TradingDaysPerYear equal liveConfiguration?.TradingDaysPerYear
            var tradingDayPerYear = backtestConfiguration?.TradingDaysPerYear ?? 252;

            Log.Trace($"QuantConnect.Report.Report(): Processing backtesting orders");
            var backtestPortfolioInTime = PortfolioLooper.FromOrders(backtestCurve, backtestOrders, backtestConfiguration).ToList();
            Log.Trace($"QuantConnect.Report.Report(): Processing live orders");
            var livePortfolioInTime = PortfolioLooper.FromOrders(liveCurve, liveOrders, liveConfiguration, liveSeries: true).ToList();

            var destination = pointInTimePortfolioDestination ?? Config.Get("report-destination");
            if (!string.IsNullOrWhiteSpace(destination))
            {
                if (backtestPortfolioInTime.Count != 0)
                {
                    var dailyBacktestPortfolioInTime = backtestPortfolioInTime
                        .Select(x => new PointInTimePortfolio(x, x.Time.Date).NoEmptyHoldings())
                        .GroupBy(x => x.Time.Date)
                        .Select(kvp => kvp.Last())
                        .OrderBy(x => x.Time)
                        .ToList();

                    var outputFile = destination.Replace(".html", string.Empty) + "-backtesting-portfolio.json";
                    Log.Trace($"Report.Report(): Writing backtest point-in-time portfolios to JSON file: {outputFile}");
                    var backtestPortfolioOutput = JsonConvert.SerializeObject(dailyBacktestPortfolioInTime);
                    File.WriteAllText(outputFile, backtestPortfolioOutput);
                }
                if (livePortfolioInTime.Count != 0)
                {
                    var dailyLivePortfolioInTime = livePortfolioInTime
                        .Select(x => new PointInTimePortfolio(x, x.Time.Date).NoEmptyHoldings())
                        .GroupBy(x => x.Time.Date)
                        .Select(kvp => kvp.Last())
                        .OrderBy(x => x.Time)
                        .ToList();

                    var outputFile = destination.Replace(".html", string.Empty) + "-live-portfolio.json";
                    Log.Trace($"Report.Report(): Writing live point-in-time portfolios to JSON file: {outputFile}");
                    var livePortfolioOutput = JsonConvert.SerializeObject(dailyLivePortfolioInTime);
                    File.WriteAllText(outputFile, livePortfolioOutput);
                }
            }

            _elements = new List<IReportElement>
            {
                //Basics
                new TextReportElement("strategy name", ReportKey.StrategyName, name),
                new TextReportElement("description", ReportKey.StrategyDescription, description),
                new TextReportElement("version", ReportKey.StrategyVersion, version),
                new TextReportElement("stylesheet", ReportKey.Stylesheet, File.ReadAllText("css/report.css") + (cssOverride)),
                new TextReportElement("live marker key", ReportKey.LiveMarker, live == null ? string.Empty : "Live "),

                //KPI's Backtest:
                new RuntimeDaysReportElement("runtime days kpi", ReportKey.BacktestDays, backtest, live),
                new CAGRReportElement("cagr kpi", ReportKey.CAGR, backtest, live),
                new TurnoverReportElement("turnover kpi", ReportKey.Turnover, backtest, live),
                new MaxDrawdownReportElement("max drawdown kpi", ReportKey.MaxDrawdown, backtest, live),
                new SharpeRatioReportElement("sharpe kpi", ReportKey.SharpeRatio, backtest, live, tradingDayPerYear),
                new SortinoRatioReportElement("sortino kpi", ReportKey.SortinoRatio, backtest, live, tradingDayPerYear),
                new PSRReportElement("psr kpi", ReportKey.PSR, backtest, live, tradingDayPerYear),
                new InformationRatioReportElement("ir kpi", ReportKey.InformationRatio, backtest, live),
                new MarketsReportElement("markets kpi", ReportKey.Markets, backtest, live),
                new TradesPerDayReportElement("trades per day kpi", ReportKey.TradesPerDay, backtest, live),
                new EstimatedCapacityReportElement("estimated algorithm capacity", ReportKey.StrategyCapacity, backtest, live),

                // Generate and insert plots MonthlyReturnsReportElement
                new MonthlyReturnsReportElement("monthly return plot", ReportKey.MonthlyReturns, backtest, live),
                new CumulativeReturnsReportElement("cumulative returns", ReportKey.CumulativeReturns, backtest, live),
                new AnnualReturnsReportElement("annual returns", ReportKey.AnnualReturns, backtest, live),
                new ReturnsPerTradeReportElement("returns per trade", ReportKey.ReturnsPerTrade, backtest, live),
                new AssetAllocationReportElement("asset allocation over time pie chart", ReportKey.AssetAllocation, backtest, live, backtestPortfolioInTime, livePortfolioInTime),
                new DrawdownReportElement("drawdown plot", ReportKey.Drawdown, backtest, live),
                new DailyReturnsReportElement("daily returns plot", ReportKey.DailyReturns, backtest, live),
                new RollingPortfolioBetaReportElement("rolling beta to equities plot", ReportKey.RollingBeta, backtest, live, tradingDayPerYear),
                new RollingSharpeReportElement("rolling sharpe ratio plot", ReportKey.RollingSharpe, backtest, live, tradingDayPerYear),
                new LeverageUtilizationReportElement("leverage plot", ReportKey.LeverageUtilization, backtest, live, backtestPortfolioInTime, livePortfolioInTime),
                new ExposureReportElement("exposure plot", ReportKey.Exposure, backtest, live, backtestPortfolioInTime, livePortfolioInTime)
            };

            // Include Algorithm Parameters
            if (parametersHtmlContent != null)
            {
                _elements.Add(new ParametersReportElement("parameters page", ReportKey.ParametersPageStyle, backtestConfiguration, liveConfiguration, parametersHtmlContent));
                _elements.Add(new ParametersReportElement("parameters", ReportKey.Parameters, backtestConfiguration, liveConfiguration, parametersHtmlContent));
            }

            // Array of Crisis Plots:
            if (crisisHtmlContent != null)
            {
                _elements.Add(new CrisisReportElement("crisis page", ReportKey.CrisisPageStyle, backtest, live, crisisHtmlContent));
                _elements.Add(new CrisisReportElement("crisis plots", ReportKey.CrisisPlots, backtest, live, crisisHtmlContent));
            }

        }

        /// <summary>
        /// Compile the backtest data into a report
        /// </summary>
        /// <returns></returns>
        public void Compile(out string html, out string reportStatistics)
        {
            html = _template;
            var statistics = new Dictionary<string, object>();

            // Render the output and replace the report section
            foreach (var element in _elements)
            {
                Log.Trace($"QuantConnect.Report.Compile(): Rendering {element.Name}...");
                html = html.Replace(element.Key, element.Render());

                if (element is TextReportElement || element is CrisisReportElement || element is ParametersReportElement ||(element as ReportElement) == null)
                {
                    continue;
                }

                var reportElement = element as ReportElement;
                statistics[reportElement.JsonKey] = reportElement.Result;
            }

            reportStatistics = JsonConvert.SerializeObject(statistics, Formatting.None);
        }

        /// <summary>
        /// Gets the regex pattern in the given input string
        /// </summary>
        /// <param name="pattern">Regex pattern to be find the input string</param>
        /// <param name="input">Input string that may contain the regex pattern</param>
        /// <returns>The regex pattern in the input string if found. Otherwise, null</returns>
        public static string GetRegexInInput(string pattern, string input)
        {
            var regex = new Regex(pattern);
            var match = regex.Match(input);
            var regexWithinInput = match.Success ? match.Groups[2].Value : null;
            return regexWithinInput;
        }
    }
}
