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

namespace QuantConnect.Report
{
    /// <summary>
    /// Helper shortcuts for report injection points.
    /// </summary>
    internal static class ReportKey
    {
        public const string Stylesheet = @"{{$REPORT-STYLESHEET}}";
        public const string StrategyName = @"{{$TEXT-STRATEGY-NAME}}";
        public const string StrategyDescription = @"{{$TEXT-STRATEGY-DESCRIPTION}}";
        public const string StrategyVersion = @"{{$TEXT-STRATEGY-VERSION}}";
        public const string LiveMarker = @"{{$LIVE-MARKER}}";
        public const string ParametersPageStyle = @"{{$CSS-PARAMETERS-PAGE-STYLE}}";
        public const string Parameters = @"{{$PARAMETERS}}";

        public const string CAGR = @"{{$KPI-CAGR}}";
        public const string Turnover = @"{{$KPI-TURNOVER}}";
        public const string MaxDrawdown = @"{{$KPI-DRAWDOWN}}";
        public const string KellyEstimate = @"{{$KPI-KELLY-ESTIMATE}}";
        public const string SharpeRatio = @"{{$KPI-SHARPE}}";
        public const string SortinoRatio = @"{{$KPI-SORTINO}}";
        public const string BacktestDays = @"{{$KPI-BACKTEST-DAYS}}";
        public const string DaysLive = @"{{$KPI-DAYS-LIVE}}";
        public const string InformationRatio = @"{{$KPI-INFORMATION-RATIO}}";
        public const string TradesPerDay = @"{{$KPI-TRADES-PER-DAY}}";
        public const string Markets = @"{{$KPI-MARKETS}}";
        public const string PSR = @"{{$KPI-PSR}}";
        public const string StrategyCapacity = @"{{$KPI-STRATEGY-CAPACITY}}";

        public const string MonthlyReturns = @"{{$PLOT-MONTHLY-RETURNS}}";
        public const string CumulativeReturns = @"{{$PLOT-CUMULATIVE-RETURNS}}";
        public const string AnnualReturns = @"{{$PLOT-ANNUAL-RETURNS}}";
        public const string ReturnsPerTrade = @"{{$PLOT-RETURNS-PER-TRADE}}";
        public const string AssetAllocation = @"{{$PLOT-ASSET-ALLOCATION}}";
        public const string Drawdown = @"{{$PLOT-DRAWDOWN}}";
        public const string DailyReturns = @"{{$PLOT-DAILY-RETURNS}}";
        public const string RollingBeta = @"{{$PLOT-BETA}}";
        public const string RollingSharpe = @"{{$PLOT-SHARPE}}";
        public const string LeverageUtilization = @"{{$PLOT-LEVERAGE}}";
        public const string Exposure = @"{{$PLOT-EXPOSURE}}";
        public const string CrisisPageStyle = @"{{$CSS-CRISIS-PAGE-STYLE}}";
        public const string CrisisPlots = @"{{$HTML-CRISIS-PLOTS}}";
        public const string CrisisTitle = @"{{$TEXT-CRISIS-TITLE}}";
        public const string CrisisContents = @"{{$PLOT-CRISIS-CONTENT}}";
    }
}
