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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// PerformanceMetrics contains the names of the various performance metrics used for evaluation purposes.
    /// </summary>
    public static class PerformanceMetrics
    {
        public const string Alpha = "Alpha";
        public const string AnnualStandardDeviation = "Annual Standard Deviation";
        public const string AnnualVariance = "Annual Variance";
        public const string AverageLoss = "Average Loss";
        public const string AverageWin = "Average Win";
        public const string Beta = "Beta";
        public const string CompoundingAnnualReturn = "Compounding Annual Return";
        public const string Drawdown = "Drawdown";
        public const string EstimatedStrategyCapacity = "Estimated Strategy Capacity";
        public const string Expectancy = "Expectancy";
        public const string StartEquity = "Start Equity";
        public const string EndEquity = "End Equity";
        public const string InformationRatio = "Information Ratio";
        public const string LossRate = "Loss Rate";
        public const string NetProfit = "Net Profit";
        public const string ProbabilisticSharpeRatio = "Probabilistic Sharpe Ratio";
        public const string ProfitLossRatio = "Profit-Loss Ratio";
        public const string SharpeRatio = "Sharpe Ratio";
        public const string SortinoRatio = "Sortino Ratio";
        public const string TotalFees = "Total Fees";
        public const string TotalOrders = "Total Orders";
        public const string TrackingError = "Tracking Error";
        public const string TreynorRatio = "Treynor Ratio";
        public const string WinRate = "Win Rate";
        public const string LowestCapacityAsset = "Lowest Capacity Asset";
        public const string PortfolioTurnover = "Portfolio Turnover";
    }
}
